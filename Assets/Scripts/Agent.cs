using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 
/// </summary>
public class CellMove
{
    public int x;
    public int y;

    public CellMove(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}



/// <summary>
/// Base class for any kind of agent which interacts with the game world. Can be extended for specific kinds of agents such as player controllable avatars, AI enemies etc. 
/// Implements the rudimentary basic functionality such as moving over the map.
/// 
/// PLANNING
/// 
/// 
/// </summary>
public class Agent : MonoBehaviour
{
    // The agent obviously will have to have access to the global map instance. This object will have to be queried to get information regarding the map boundries for 
    // example and all other information regarding the tiles, such as tile variables and the like.
    Map CurrentMap;

    Rigidbody2D RigidBody;

    int x = 0;
    int y = 0;

    // The way movement is planned there could be overshoot, could do an idle routine where in a radius the actor will return closer to current target coordinates.
    // Need to introduce target coordinates
    // Movement should consist of two queues:
    // - top level given by user
    // - bottom level walking only into a certain direction

    bool Moving = false;
    Queue<CellMove> movementQueue;
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
        this.TeleportCellCenter(this.x, this.y); 
    }

    /// <summary>
    /// Teleports the GO to the given coordinates in the "cell space".
    /// </summary>
    /// <param name="cellX"></param>
    /// <param name="cellY"></param>
    public void TeleportCellCenter(int cellX, int cellY)
    {
        Vector3 cellCenter = this.CurrentMap.GetCellCenter(cellX, cellY);
        cellCenter.z = this.zLayer;
        this.transform.position = cellCenter;
    }

    public void WalkCellCenterStupid(int cellX, int cellY)
    {
        int xDifference = cellX - this.x;
        int yDifference = cellY - this.y;

        // First walk all the y DIfference
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            this.WalkCellCenterStupid(10, 20);
        }

        this.UpdateMovement();
    }

    void UpdateMovement()
    {
        // apply velocity into a certain direction if destination is not yet reached
    }


}
