using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{

    public Grid grid;

    // Start is called before the first frame update
    void Start()
    {
        this.grid = GetComponent<Grid>();
        Debug.Log(this.grid.cellLayout);
        Debug.Log(this.grid.CellToLocal(new Vector3Int(0, 0, 0)));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
