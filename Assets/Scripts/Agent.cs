using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// This class is supposed to mainly represent a single primitive movement destination, but I guess it can also be used to describe a map location in general.
/// the advantage of having an additional wrapper and not just using vectors straight away is, that this class wraps a location both as a 2D vector in map space and 
/// a 3D vector in world space!
/// </summary>
public class CellMove
{
    public Vector2Int positionMap;
    public Vector3 positionWorld;

    public CellMove(Vector2Int positionMap, Vector3 positionWorld)
    {
        this.positionMap = positionMap;
        this.positionWorld = positionWorld;
        this.positionWorld.z = 0;
    }
}

/// <summary>
/// This class wraps the functionality required of the movement queue for an agent.
/// 
/// WHAT IS A MOVEMENT QUEUE?
/// 
/// This would be the first important question. What does a "movement queue" refer to. This comes down to the way that agent movement ist implemented / realized. 
/// One of the main challenges of movement for the 2D agents is path finding. In the end it should be possible to "simply" provide an agent with any place on the map and 
/// it should, on it's own, come up with a (good) path to get to this location. This in itself is not simple. In the majority of cases a simple straight line from point 
/// A to be B will not be possible because inbetween will be various untraversable tiles. This means there needs to be at least some sort of algorithm which considers 
/// these blockages and finds a way around them. Thus a single walk from point A to B will have to be expressed as a series of elementary movements. This is the 
/// purpose of the movement queue. It holds all the primitive movement instructions which make up the currently active instruction of the agent.
/// 
/// REQUIREMENTS
/// 
/// The movement queue actually has to be represented by a game object, which has to be passed to the constructor. This game object has to contain a LineRenderer 
/// component, which will be used to draw the preview of these movements as lines onto the map.
/// 
/// </summary>
public class MoveQueue
{
    // The reference to the game object which represents the movement queue. Now the reason why this relatively simple concept of a movement queue needs to be additionally 
    // represented as a seperate game object is that it should be possible to actually draw(!) this movement queue in the game. A series of lines is supposed to give an 
    // indication / preview of which path the agent will take. Realizing this is the most simple with a seperate game object, because actually displaying this path in the 
    // game screen can then be easily toggled by setting the game object active / inactive.
    public GameObject gameObject;

    // The LineRenderer component (which has to be part of the passed game object) is used to actually draw / render the lines for the path preview.
    public LineRenderer pathRenderer;

    // https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1?view=net-5.0
    // The actual Queue part of the movement queue is realized as this list. The first question might be why this is a list then and not a queue. I thought a list might be 
    // more flexible in the future should the need arise to insert additional moves somewhere in the middle or something. The functionality of a queue is in the end rather 
    // easy to slap on to a list.
    // The actual items of this list (which represent the movement operations) are the custom CellMove objects. These simply wrap the location of the map to walk towards. 
    // They contain these location both in map and world coordinates.
    public List<CellMove> moves;
    public bool visible = true;

    public MoveQueue(GameObject gameObject)
    {
        this.moves = new List<CellMove>();

        this.gameObject = gameObject;
        this.pathRenderer = this.gameObject.GetComponent<LineRenderer>();
    }

    public int Count()
    {
        return this.moves.Count;
    }

    public void Add(CellMove move)
    {
        // I think "add" simple puts the new item as the end of the list?
        this.moves.Add(move);
    }

    public void Next()
    {
        this.moves.RemoveAt(0);
    }

    public CellMove GetCurrentMove()
    {
        return this.moves[0];
    }

    /// <summary>
    /// Given the start position as a 3D Vector, this method will update the line renderer vertices to match the movement destinations in the movement queue.
    /// As this is supposed to be used by an agent, the starting position vector is supposed to be the current location of the agent, from which a line will be 
    /// drawn to the current target destination (element 0) and from there on to the next element in the queue etc.
    /// </summary>
    /// <param name="startPositionWorld">The 3D vector representation of the current location of the agent to whom this movement queue belongs to</param>
    public void Draw(Vector3 startPositionWorld)
    {
        // https://answers.unity.com/questions/720109/create-a-line-between-two-points-in-2d-a-vehicle-w.html
        // We need to set the vertex count of the line renderer before actually messing with the vertices.
        this.pathRenderer.SetVertexCount(this.moves.Count + 1);
        // The starting point obviously always is the current location of the agent as passed as argument to this method
        startPositionWorld.z = Map.AGENT_LAYER;
        this.pathRenderer.SetPosition(0, startPositionWorld);

        // Then we can simply iterate the movement instructions left in the queue and add these as additional vertices for the line to be drawn.
        for (int index = 0; index < this.moves.Count; index++)
        {
            Vector3 lineCorner = this.moves[index].positionWorld;
            lineCorner.z = Map.AGENT_LAYER;
            this.pathRenderer.SetPosition(index + 1, lineCorner);
        }
    }
}


/// <summary>
/// Base class for any kind of agent which interacts with the game world. Can be extended for specific kinds of agents such as player controllable avatars, AI enemies etc. 
/// Implements the rudimentary basic functionality such as moving over the map.
/// 
/// REQUIREMENTS
/// - The main game object itself will have to have a RigidBody2D component, which is used for the movement
/// - The first child game object of an agent is the game object which represents the movement queue.
///     - This child needs to have a "LineRenderer" component which will be used to draw out all the movements in the queue
/// 
/// 
/// </summary>
public class Agent : MonoBehaviour
{
    // The agent obviously will have to have access to the global map instance. This object will have to be queried to get information regarding the map boundries for 
    // example and all other information regarding the tiles, such as tile variables and the like.
    public Map CurrentMap;

    // The agent will have to have a rigid body component. This is used to implement the movement. The most simply version of movement can be achieved by simply setting 
    // the velocity vector of the rigidbody to a certain direction. If I am not mistaken, doing it this way will prevent wall clipping with non traversable terrain. 
    // Because no matter the velocity, the physics collision detection will prevent the clipping.
    // Having a rigidbody also allows there to be really easy implementations of things like weapon knockback, recoil and explosion forces later on.
    public Rigidbody2D RigidBody;

    // == MOVEMENT
    // Now movement of an agent is not quite trivial. The main concept is that of a movement queue. This queue saves a series of primitive movement instructions for an 
    // agent to be completed in the order in which they appear. Primitive movements thereby refer to the fact that for each single instruction in this queue the agent will 
    // take the straight linear path towards this location. But in total, the whole queue traces a non-linear path. The idea is that the queue should contain all the 
    // necessary primitive instructions which complete the currently active action instruction. These could be obtained through some sort of path finding algorithm.
    public MoveQueue moveQueue;
    // The move queue's items are CellMove objects which are simply wrappers for map locations. The currentMove will contain the move which is currently being executed.
    public CellMove currentMove;

    // During all times, the current position of the agent will be saved as these two attributes, which represent the current location in both world space as well as 
    // map space.
    public Vector2Int currentPositionMap = new Vector2Int(0, 0);
    public Vector3 currentPositionWorld;

    // This flag controls if the agent is currently moving or not. If active, the agent will immediatly start moving if there are any instructions in the movement queue. 
    // if this flag is false, the player will not move. This could for example be used to implement a "stun" mechanic. Another possibility would be to set it to false 
    // temporarily to trigger a shooting animation or something like that.
    public bool moving = false;

    // The movement speed of the player.
    float moveSpeed = 10;

    // == CHILD GAME OBJECTS

    // This is the first child GO of the main agent, it represents the movement queue.
    public GameObject moveQueueGO;


    // Controls the z value at which the element is placed. The default value of 0 is the same as the basic ground tilemap and should definitely be overwritten at some 
    // point during the initialization with a more specific value for this kind of agent.
    float zLayer = 0;

    // Start is called before the first frame update
    void Start()
    {
        this.CurrentMap = GameObject.Find("Grid").GetComponent<Map>();
        this.CurrentMap.Initialize();

        this.RigidBody = this.GetComponent<Rigidbody2D>();

        // Setting the z Layer to the generic "agent" layer, which can be overwritten if necessary
        this.zLayer = Map.AGENT_LAYER;

        // Initial position

        this.TeleportCellCenter(this.currentPositionMap);
        this.currentPositionWorld = this.CurrentMap.GetCellCenterWorld(this.currentPositionMap);
        this.currentMove = new CellMove(this.currentPositionMap, this.currentPositionWorld);
        this.moving = true;

        // -- Loading child Game Objects

        // The first child game object is the game object which will represent the movement queue of the agent. Now you might think: A movement queue seems simple enough a 
        // concept to implement as pure code, why does that need to be a seperate game object? This is because it would be very nice to be able to toggle the movement 
        // queue to actually be drawn on the screen as kind of a preview. Doing this via a LineRenderer in a seperate component seems like the most simple implementation.
        this.moveQueueGO = this.transform.GetChild(0).gameObject;
        // This game object is then passed to the constructor of the wrapper class "MoveQueue" which wraps all the necessary code to make this work
        this.moveQueue = new MoveQueue(this.moveQueueGO);
    }

    // == TELEPORTATION MOVEMENT
    // Teleportation refers to the method of instantaneously placing the agent in a new position on the map

    /// <summary>
    /// Teleports the agent to the center of the given cell in map coordinates. The z coordinate will be set to the internal value 
    /// of zLayer property.
    /// </summary>
    /// <param name="cellX"></param>
    /// <param name="cellY"></param>
    public void TeleportCellCenter(int cellX, int cellY)
    {
        Vector3 cellCenterWorld = this.CurrentMap.GetCellCenterWorld(cellX, cellY);
        cellCenterWorld.z = this.zLayer;
        this.transform.position = cellCenterWorld;
    }
    
    public void TeleportCellCenter(Vector2Int positionMap)
    {
        Vector3 cellCenterWorld = this.CurrentMap.GetCellCenterWorld(positionMap);
        cellCenterWorld.z = this.zLayer;
        this.transform.position = cellCenterWorld;
    }

    /// <summary>
    /// Teleports the agent to the center of a random cell of the map. The z coordinate will be set to the internal value of 
    /// the zLayer property.
    /// </summary>
    public void TeleportRandomCell()
    {
        Vector3 cellCenterWorld = this.CurrentMap.GetRandomCellCenterWorld();
        cellCenterWorld.z = this.zLayer;
        this.transform.position = cellCenterWorld;
    }

    // Update is called once per frame
    void Update()
    {
        // CODE FOR TESTING
        if (Input.GetKeyDown(KeyCode.T))
        {
            this.TeleportRandomCell();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            Vector2Int cellPosition = this.CurrentMap.GetRandomPositionMap();
            Vector3 worldPosition = this.CurrentMap.GetCellCenterWorld(cellPosition);
            this.moveQueue.Add(new CellMove(cellPosition, worldPosition));
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (Time.timeScale > 0.1f)
            {
                Time.timeScale = 0.1f;
            } else
            {
                Time.timeScale = 1.0f;
            }
            
        }

        // -- Updating the movement / position
        // This method manages all the movement related stuff. But it also updates the current position value based on the actual world position of the transform for 
        // example. So executing this is important. It cannot just be skipped if one wishes there to be no movement. This could be done by setting the moving flag to 
        // false for example.
        this.UpdateMovement();
    }

    void UpdateMovement()
    {
        this.currentPositionWorld = this.transform.position;
        this.currentPositionWorld.z = 0;

        this.currentPositionMap = this.CurrentMap.GetPositionMap(this.currentPositionWorld);
        
        // All of the movment we only do if the moving flag is actually true.
        if (this.moving)
        {
            float distanceToTarget = Vector3.Distance(this.currentPositionWorld, this.currentMove.positionWorld);
            if (distanceToTarget > 0.1)
            {
                // First of all we need to calculate the direction into which we need to move. This should be a vector with norm == 1. So then we can multiply it 
                // with the speed value and set it as the rigidbody velocity.
                // It is also imperative that we delete any z component of this direction as we do not want our movement to affect the z layer at which the agent is 
                // displayed.
                Vector3 direction = this.currentMove.positionWorld - this.currentPositionWorld;
                direction.z = 0;
                direction.Normalize();
                this.RigidBody.velocity = direction * this.moveSpeed;

            } else
            {
                // This block is being executed when the agent has completed the current movement instruction. So if there are more movement instructions in the Queue, 
                // we will load the next next instruction to be executed next.
                this.RigidBody.velocity = new Vector3(0, 0, 0);
                
                if (this.moveQueue.Count() > 1)
                {
                    this.moveQueue.Next();
                    this.currentMove = this.moveQueue.GetCurrentMove();
                }
            }
        }

        // This method updates the lines which make up the displayed illustration of the movmenet queue. This simple update of the movement queue is independent of wheter 
        // or not the agent is moving. It should always happen.
        this.moveQueue.Draw(this.currentPositionWorld);

    }


}
