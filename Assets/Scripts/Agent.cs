using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 
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


public class MoveQueue
{

    public GameObject gameObject;

    // https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1?view=net-5.0
    public List<CellMove> moves;
    public LineRenderer pathRenderer;

    public bool visible = true;

    public MoveQueue(GameObject gameObject)
    {
        this.moves = new List<CellMove>();

        this.gameObject = gameObject;
        this.pathRenderer = this.gameObject.GetComponent<LineRenderer>();
        Debug.Log(this.gameObject);
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

    public void Draw(Vector3 startPositionWorld)
    {
        // https://answers.unity.com/questions/720109/create-a-line-between-two-points-in-2d-a-vehicle-w.html
        this.pathRenderer.SetVertexCount(this.moves.Count + 1);
        startPositionWorld.z = Map.AGENT_LAYER;
        this.pathRenderer.SetPosition(0, startPositionWorld);
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
/// - The first child game object of an agent is the game object which represents the movement queue.
///     - This child needs to have a "LineRenderer" component which will be used to draw out all the movements in the queue
/// 
/// 
/// </summary>
public class Agent : MonoBehaviour
{
    // The agent obviously will have to have access to the global map instance. This object will have to be queried to get information regarding the map boundries for 
    // example and all other information regarding the tiles, such as tile variables and the like.
    Map CurrentMap;

    Rigidbody2D RigidBody;

    // The way movement is planned there could be overshoot, could do an idle routine where in a radius the actor will return closer to current target coordinates.
    // Need to introduce target coordinates
    // Movement should consist of two queues:
    // - top level given by user
    // - bottom level walking only into a certain direction

    public Vector2Int currentPositionMap = new Vector2Int(0, 0);
    public Vector3 currentPositionWorld;

    public bool moving = false;
    
    public MoveQueue moveQueue;
    public CellMove currentMove;

    public GameObject moveQueueGO;


    float moveSpeed = 10;

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

        // -- Loading child Game Objects

        // The first child game object is the game object which will represent the movement queue of the agent. Now you might think: A movement queue seems simple enough a 
        // concept to implement as pure code, why does that need to be a seperate game object? This is because it would be very nice to be able to toggle the movement 
        // queue to actually be drawn on the screen as kind of a preview. Doing this via a LineRenderer in a seperate component seems like the most simple implementation.
        this.moveQueueGO = this.transform.GetChild(0).gameObject;
        // This game object is then passed to the constructor of the wrapper class "MoveQueue" which wraps all the necessary code to make this work
        this.moveQueue = new MoveQueue(this.moveQueueGO);
        Debug.Log(moveQueue);
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


    // == BASIC MOVEMENT PRIMITIVES

    public Vector3 GetDirectionWorld(int mapX, int mapY)
    {
        Vector3 currentLocationWorld = this.transform.position;
        Vector3 targetLocationWorld = this.CurrentMap.GetCellCenterWorld(mapX, mapY);
        // The direction we can easily get as the difference between the vectors
        Vector3 directionWorld = targetLocationWorld - currentLocationWorld;
        // Also the z value is irrelevant, it should not be part of the direction (and possibly fuck up the normalization)
        directionWorld.z = 0;
        directionWorld.Normalize();
        // But the direction needs to be a unit vector of length 1!
        return directionWorld;
    }

    // Update is called once per frame
    void Update()
    {
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

        this.UpdateMovement();
    }

    void UpdateMovement()
    {
        this.currentPositionWorld = this.transform.position;
        this.currentPositionWorld.z = 0;

        this.currentPositionMap = this.CurrentMap.GetPositionMap(this.currentPositionWorld);
        
        if (this.moveQueue.Count() > 0)
        {
            this.currentMove = this.moveQueue.GetCurrentMove();

            // apply velocity into a certain direction if destination is not yet reached
            if (this.moving)
            {
                // this.currentMove = this.moveQueue.GetCurrentMove();
                // First of all we need to calculate the direction into which we need to move. This should be a vector with norm == 1. So then we can multiply it 
                // with the speed value and set it as the rigidbody velocity.
                Vector3 direction = this.currentMove.positionWorld - this.currentPositionWorld;
                direction.Normalize();
                this.RigidBody.velocity = direction * this.moveSpeed;
                this.moveQueue.Draw(this.currentPositionWorld);
            }

            // Checking if the destination has been reached. If that is the case we stop moving.
            if (this.moving && Vector3.Distance(this.currentPositionWorld, this.currentMove.positionWorld) < 0.1)
            {
                this.moving = false;
                this.RigidBody.velocity = new Vector3(0, 0, 0);
            }

            if (!this.moving && this.moveQueue.Count() > 1)
            {
                this.moveQueue.Next();
                this.moving = true;
            }
        }
    }


}
