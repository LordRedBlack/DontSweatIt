using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Assertions;

public class Map : MonoBehaviour
{

    public Grid BaseGrid;
    public int OriginX;
    public int OriginY;
    public float CellSize;

    public Tilemap GroundTilemap;

    // Start is called before the first frame update
    void Start()
    {
        this.BaseGrid = GetComponent<Grid>();
        if (this.BaseGrid.cellSize.x != this.BaseGrid.cellSize.y)
        {
            Debug.LogError("The underlying Grid does not have a symmetric cell size! This should not be the case!");
        } else
        {
            this.CellSize = this.BaseGrid.cellSize.x;
            Debug.Log(this.CellSize);
        }
        
        // REDO this ugly
        this.GroundTilemap = this.transform.GetChild(0).gameObject.GetComponent<Tilemap>();
        Vector3 cellPos = this.GroundTilemap.CellToWorld(new Vector3Int(-2, 5, 0));
        this.OriginX = this.GroundTilemap.origin.x;
        this.OriginY = this.GroundTilemap.origin.y;

        Debug.Log(GroundTilemap.cellBounds.size);
        Debug.Log(GroundTilemap.origin);
    }

    public Vector3Int MapToCell(int mapX, int mapY)
    {
        return new Vector3Int(this.OriginX + mapX, this.OriginY + mapY, 0);
    }

    public Vector3 GetCellCenter(int mapX, int mapY)
    {
        Vector3Int cellPosition = this.MapToCell(mapX, mapY);
        Vector3 centerCoordinates = this.GroundTilemap.GetCellCenterWorld(cellPosition);
        return centerCoordinates;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
