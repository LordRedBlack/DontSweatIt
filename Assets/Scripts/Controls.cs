using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controls : MonoBehaviour
{
    // == REFERENCES
    public Map map;
    public Camera mainCamera;

    // == PROPERTIES 
    public Vector3 mousePositionScreen;
    public Vector3 mousePositionWorld;
    public Vector2Int mousePositionMap;


    // Start is called before the first frame update
    void Start()
    {
        this.mainCamera = this.GetComponent<Camera>();

        this.map = GameObject.Find("Grid").GetComponent<Map>();
        this.map.Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        this.mousePositionScreen = Input.mousePosition;
        this.mousePositionWorld = this.mainCamera.ScreenToWorldPoint(this.mousePositionScreen);
        this.mousePositionMap = this.map.GetPositionMap(this.mousePositionWorld);
        
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log(this.mousePositionMap);
            Vector3Int positionCell = new Vector3Int(this.mousePositionMap.x, this.mousePositionMap.y, 0);
            Debug.Log(this.map.GroundTilemap.GetTile(positionCell));
        }
    }
}
