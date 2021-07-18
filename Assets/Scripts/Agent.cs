using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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


public class Agent : MonoBehaviour
{
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

    // Start is called before the first frame update
    void Start()
    {
        this.CurrentMap = GameObject.Find("Grid").GetComponent<Map>();
        this.RigidBody = this.GetComponent<Rigidbody2D>();

        // Initial position
        this.TeleportCellCenter(this.x, this.y); 
    }

    public void TeleportCellCenter(int cellX, int cellY)
    {
        Vector3 cellCenter = this.CurrentMap.GetCellCenter(cellX, cellY);
        cellCenter.z = -1;
        Debug.Log("Cell Center Position: " + cellCenter.ToString());
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
