// Default Unity:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Assertions;


public class Tile
{
    public bool isTraversable;

    public Tile(bool isTraversable)
    {
        this.isTraversable = isTraversable;
    }

    public override string ToString()
    {
        return $"TILE(isTraversable={this.isTraversable})";
    }
}


/// <summary>
/// This class represents a Map. For a 2D game such as this a "map" more accurately refers to the entirety of a tile grid.
/// 
/// ASSIGNMENT
///
/// The "Map" component is supposed to be added to the top-level "Grid" game object for every level instance. 
/// Important: The Game Object also has to be called "Grid"! This will be how it will be identified (and the reference loaded) by all other agents etc.
/// The requirements to this grid GO are the following:
/// - It has to have a child GO which is called "TilemapGround" and which represents the tilemap of the basic ground tiles.
///  
/// PLANNING
///
/// he fundamental "map" object representation will be important for many things in the future. A distant goal would be that agents on a map can move through some sort of path finding. 
/// For this feature the map obviously has to provide info about the boundries of the map and also within it. It should contain some sort of a representation of which tiles have which kind 
/// of properties such as being traversable or not. But even more than this tiles may need many additional infos in the future such as their temperature (which will be important for the 
/// core gameplay feature of freezing) damage values for structures etc.
/// But as of now, these things are in the future. The first goal would be to be able to coherently interact with the map in the form of teleportation. There should be a simple interface for
/// instantaneously transporting an agent to a certain tile of the map.
/// </summary>
public class Map : MonoBehaviour
{
    // Z LAYER DEFINITIONS
    // In Unity, a game is not truly "2D" in reality, the game world is still handled as "3D" but the 2D assets are drawn on planes and the camera is aligned towards looking down onto these 
    // planes so to say, that it looks like 2D for all intents and purposes. (This is configurable, but for this game) the 2D coordinates are X and Y with the Z coordinate being the "depth" 
    // or the parallel distance of the planes to each other. If one plane is above the other (from the perspective of the camera) It's contents will appear in the foreground blocking the 
    // content of the lower layers. Thus it is important to control on which Z layer an element is to control which game elements take priority for overlayering effects.
    // These constants defined here can be used for the respective families of game elements.
    public const float AGENT_LAYER = -1.0f;
    public const float GROUND_LAYER = 0.0f;


    public Grid BaseGrid;

    public int OriginX;
    public int OriginY;

    public int SizeX;
    public int SizeY;
    
    public float CellSize;

    public Tilemap GroundTilemap;
    public Tilemap TerrainTilemap;

    // 2D TILE ARRAY
    public Tile[,] tiles;

    // An instance of a Random object is needed to acquire random numbers. This random instance can potentially also used by other classes which have access to the map instance, but it was 
    // mainly added to implement the functionality of selecting a random tile of the map.
    public System.Random random;

    // THE INITIALIZATION
    // This flag signifies if the map has already been initialized. The initialization contains things such as building all necessary internal references and intializing state variable values. 
    // The very existence of this flag is sort of a hack though. Initialization is usually done in the "Start" method. But for this method it is done in a seperate method "Initialize" which can be 
    // called externally. This is necessary, because the map instance is needed in many other "Start" methods of agents for example and may throw an error.
    public bool isInitialized = false;

    public void Initialize()
    {
        if (!this.isInitialized)
        {
            this.ForceInitialize();
        }
    }

    public void ForceInitialize()
    {
        // -- Loading the Grid
        // Since this script component is part of the main "Grid" game object of the level instance, this GO also has to have an actual Grid component which is loaded here.
        // The cell size refers to the size of a single cell/tile! We will make it a hard requirement that this cell size is symmetrical.
        this.BaseGrid = GetComponent<Grid>();
        if (this.BaseGrid.cellSize.x != this.BaseGrid.cellSize.y)
        {
            Debug.LogError("The underlying Grid does not have a symmetric cell size! This should not be the case!");
        }
        else
        {
            this.CellSize = this.BaseGrid.cellSize.x;
        }

        // -- Loading the individual tilemaps
        // The actual tilemap layers are supposed to be *child game objects* of the main grid GO. These are being loaded here. The way this is being done is really ugly, but sadly there are not 
        // really alternatives. This way of loading the references to the children imposes an additional assumption on the order of the children:
        // * The Ground tilemap has to be the first child

        // The Ground Tilemap is the most extensive tilemap. It actually has to span the entire map unlike all other tilemaps (they can be missing chunks). This is why we use the ground tilemap 
        // to determine the things like the size and the point of origin.
        this.GroundTilemap = this.transform.GetChild(0).gameObject.GetComponent<Tilemap>();
        this.OriginX = this.GroundTilemap.origin.x;
        this.OriginY = this.GroundTilemap.origin.y;
        this.SizeX = this.GroundTilemap.cellBounds.size.x;
        this.SizeY = this.GroundTilemap.cellBounds.size.y;

        Debug.Log($"GROUND TILEMAP SIZE: {this.GroundTilemap.cellBounds.size}");
        Debug.Log($"GROUND TILEMAP ORIGIN: {this.GroundTilemap.origin}");

        // The Terrain Tilemap
        this.TerrainTilemap = this.transform.GetChild(1).gameObject.GetComponent<Tilemap>();
        Debug.Log($"TERRAIN TILEMAP SIZE: {this.TerrainTilemap.cellBounds.size}");
        Debug.Log($"TERRAIN TILEMAP ORIGIN: {this.TerrainTilemap.origin}");

        // -- Initializing Randomness
        // For any random numbers etc to be generated, apparently an instance of the "Random" class is needed
        // https://stackoverflow.com/questions/2706500/how-do-i-generate-a-random-int-number
        this.random = new System.Random();

        // -- Loading the tile 2D array
        this.tiles = new Tile[this.SizeX, this.SizeY];
        for (int x = 0; x < this.SizeX; x++)
        {
            for (int y = 0; y < this.SizeY; y++) 
            {
                this.tiles[x, y] = new Tile(true);
            }
        }
        Debug.Log($"TILES ARRAY: {this.tiles}");
        Debug.Log($"TILES[2, 3]: {this.tiles[2, 3]}");

        // At the end we need to set the initialization flag to true such that under normal circumstances, this method is not being called twice!
        this.isInitialized = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        // If, miraculously, the initialization has not happened, it has to now.
        this.Initialize();
    }


    // == COORDINATE CONVERSIONS
    // So for a tiled 2D game like this we mainly have to deal with 2 different coordinate systems: The world coordinates are the "actual" absolute coordinates which are used by unity 
    // internally. These are also 3D, as they also include the z coordinate. But for many things the "map" coordinates are more useful. These are simply the integer 2D coordinates of the 
    // tiles on the tile grid. Both systems have their use cases so it is important to have means of conversion between those.

    public Vector2Int GetPositionMap(Vector3 positionWorld)
    {
        Vector3Int positionMap = this.GroundTilemap.WorldToCell(positionWorld);
        // 06.08.2021: This actually had to be minus the origin coordinate and not plus
        return new Vector2Int(positionMap.x - this.OriginX, positionMap.y - this.OriginY);
    }

    public Vector2Int GetRandomPositionMap()
    {
        int mapX = this.random.Next(0, this.SizeX);
        int mapY = this.random.Next(0, this.SizeY);
        return new Vector2Int(mapX, mapY);
    }

    /// <summary>
    /// Given the coordinates of a specific tile of the map, this method will return the corresponding world coordinate vector 
    /// for the center of that tile.
    /// </summary>
    /// <param name="mapX"></param>
    /// <param name="mapY"></param>
    /// <returns>The 3D vector of the cell center in world coordinates</returns>
    public Vector3 GetCellCenterWorld(int mapX, int mapY)
    {
        Vector3Int positionCenterMap = new Vector3Int(mapX + this.OriginX, mapY + this.OriginY, 0);
        Vector3 positionCenterWorld = this.GroundTilemap.GetCellCenterWorld(positionCenterMap);
        return positionCenterWorld;
    }

    public Vector3 GetCellCenterWorld(Vector2Int positionMap)
    {
        Vector3Int positionCenterMap = new Vector3Int(positionMap.x + this.OriginX, positionMap.y + this.OriginY, 0);
        Vector3 positionCenterWorld = this.GroundTilemap.GetCellCenterWorld(positionCenterMap);
        return positionCenterWorld;
    }

    /// <summary>
    /// Returns the world coordinate vector of the center of a random tile of the map.
    /// Note however that this method does not take into account what type of tile this is. It for example does not matter if it is traversible or not.
    /// </summary>
    /// <returns>The 3D vector of the cell center in world coordinates</returns>
    public Vector3 GetRandomCellCenterWorld()
    {
        Vector2Int positionMap = this.GetRandomPositionMap();
        return this.GetCellCenterWorld(positionMap);
    }
}
