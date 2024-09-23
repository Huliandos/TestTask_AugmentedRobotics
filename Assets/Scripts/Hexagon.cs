using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class Hexagon : MonoBehaviour
{
    #region Hex vars
    //coordinates of this hexagon
    //using axial coordinates for hexagon
    Vector2Int _coordinates = new Vector2Int();

    GameObject _canvasGO;
    #endregion

    #region Shared vars
    HexagonGridInfo _hexagonGridInfo;

    ARPlane _rootARPlane;
    #endregion

    #region Static vars
    static GameObject _hexagonCanvas;

    static float _size;
    static Material _hexagonMaterial;

    static Vector3 _up;
    static Vector3 _right;

    //helper vector2 for ease of use of this class
    //Coordinate translation to relevant neighbors
    static readonly Vector2Int _upNeighborVec = new Vector2Int(0, -1);
    static readonly Vector2Int _upRightNeighborVec = new Vector2Int(1, -1);
    static readonly Vector2Int _downRightNeighborVec = new Vector2Int(1, 0);
    static readonly Vector2Int _downNeighborVec = new Vector2Int(0, 1);
    static readonly Vector2Int _downLeftNeighborVec = new Vector2Int(-1, 1);
    static readonly Vector2Int _upLeftNeighborVec = new Vector2Int(-1, 0);
    #endregion

    #region static functions
    public static void Init(float size, Material hexagonMat, GameObject hexagonCanvas){
        _size = size;
        _hexagonMaterial = hexagonMat;
        _hexagonCanvas = hexagonCanvas;

        
        //In the flat top orientation, the horizontal distance between adjacent hexagons centers is 
        //horiz = 3/4 * width = 3/2 * size. The vertical distance is vert = height = sqrt(3) * size
        float horizontalSpacing = 1.5f * _size;
        float verticalSpacing = Mathf.Sqrt(3) * _size;

        _up = Vector3.forward * verticalSpacing;
        _right = Vector3.right * horizontalSpacing;
    }

    public static Hexagon GenerateHexagon(Transform parent, Vector2Int hexCoordinates, Vector3 posOffset, 
        ARPlane rootARPlane, HexagonGridInfo hexagonGridInfo = null){
        ///generate vertices for hexagon\\\
        Vector3[] hexVertices = new Vector3[6];
        for(int i=0; i<hexVertices.Length; i++){
            float angleDeg = 60 * i;
            float angleRad = Mathf.PI / 180 * angleDeg;

            hexVertices[i].x = _size * Mathf.Cos(angleRad);
            hexVertices[i].z = _size * Mathf.Sin(angleRad);
        }

        //if hexagon wouldn't be in polygon, then don't create it
        if(!HexagonInsidePolygon(hexVertices, parent.transform.position+posOffset, rootARPlane)){
            return null;
        }

        if(hexagonGridInfo == null)
            hexagonGridInfo = new HexagonGridInfo();
        
        //It can happen that the lists weren't updated properly before this function is called, 
        //which means this class can generate an already existing Hex
        //Thus we have to update the lists and check one last time, if this hex already esxists
        ///Hexagon in grid positioning\\\
        int yCoordinateToListIndex = hexCoordinates.y - hexagonGridInfo._lowestYCoordinate;

        //Set potential new lowest Y
        if(hexagonGridInfo._lowestYCoordinate > hexCoordinates.y)
            hexagonGridInfo._lowestYCoordinate = hexCoordinates.y;

        //Add rows to hexagon grid, if not enough rows have been added yet
        while(hexagonGridInfo._hexagonGrid.Count <= yCoordinateToListIndex){
            hexagonGridInfo._hexagonGrid.Add(new List<Hexagon>());
            hexagonGridInfo._lowestXCoordinates.Add(0);
        }
        //Add rows to the beginning of hexagon grid, if not enough rows exist before the current lowest row
        if(yCoordinateToListIndex < 0){
            for(int i=0; i<-yCoordinateToListIndex; i++){
                hexagonGridInfo._hexagonGrid.Insert(0, new List<Hexagon>());
                hexagonGridInfo._lowestXCoordinates.Insert(0, 0);
            }

            //y coordinate is now beginning of list
            yCoordinateToListIndex = 0;
        }

        int xCoordinateToListIndex = hexCoordinates.x - hexagonGridInfo._lowestXCoordinates[yCoordinateToListIndex];

        if(hexagonGridInfo._lowestXCoordinates[yCoordinateToListIndex] > hexCoordinates.x)
            hexagonGridInfo._lowestXCoordinates[yCoordinateToListIndex] = hexCoordinates.x;

        //Add colums to this row to fit the new element
        while(hexagonGridInfo._hexagonGrid[yCoordinateToListIndex].Count <= xCoordinateToListIndex)
            hexagonGridInfo._hexagonGrid[yCoordinateToListIndex].Add(null);
        //Add columns to the beginning of hexagon row, if not enough columns exist before the current lowest column
        if(xCoordinateToListIndex < 0){
            for(int i=0; i<-xCoordinateToListIndex; i++)
                hexagonGridInfo._hexagonGrid[yCoordinateToListIndex].Insert(0, null);

            //xCoordinate is now beginning of list
            xCoordinateToListIndex = 0;
        }
        
        if(hexagonGridInfo._hexagonGrid[yCoordinateToListIndex][xCoordinateToListIndex] != null)
            return null;

        ///generate triangles for hexagon\\\
        //triangulate mesh from same corner because a hexagon is convex and allows for a simple approach
        int[] hexTriangles = new int[12];
        //2,1,0 -- 3,2,0 -- 4,3,0 -- 5,4,0 etc.
        for(int i=0; i<hexTriangles.Length/3; i++){
            //order in triangulation is important, as it decided which side is UP
            //connect the next two vertices in the hex
            hexTriangles[i*3] = i+2;
            hexTriangles[i*3+1] = i+1;
            //always end triangulation at same corner
            hexTriangles[i*3+2] = 0;
        }   

        ///Generate mesh and set vertices and triangles\\\
        Mesh mesh = new Mesh();
        
        mesh.vertices = hexVertices;
        mesh.triangles = hexTriangles;


        //Instantiate object, set variables and add necessary components
        GameObject go = new GameObject($"Hexagon {hexCoordinates.x}|{hexCoordinates.y}");
        go.transform.parent = parent;
        go.transform.localPosition = posOffset;
        Hexagon hexagon = go.AddComponent<Hexagon>();

        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        MeshFilter meshFilter = go.GetComponent<MeshFilter>();

        MeshCollider meshCollider = hexagon.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        meshRenderer.material = _hexagonMaterial;
        meshFilter.mesh = mesh;

        hexagon._hexagonGridInfo = hexagonGridInfo;
        hexagon._rootARPlane = rootARPlane;

        hexagon._coordinates.x = hexCoordinates.x;
        hexagon._coordinates.y = hexCoordinates.y;

        //Add UI to hexagon
        hexagon._canvasGO = Instantiate(_hexagonCanvas, hexagon.transform);
        hexagon._canvasGO.GetComponentInChildren<TextMeshProUGUI>().text = $"Axial: {hexagon._coordinates.x}|{hexagon._coordinates.y}\n" + 
        $"Unity:\n{Mathf.Round(hexagon.transform.position.x*100)/100}|{Mathf.Round(hexagon.transform.position.y*100)/100}|{Mathf.Round(hexagon.transform.position.z*100)/100}";
        hexagon._canvasGO.SetActive(false);


        //Position Hexagon in Grid
        hexagonGridInfo._hexagonGrid[yCoordinateToListIndex][xCoordinateToListIndex] = hexagon;
        
        return hexagon;
    }

    public static bool HexagonInsidePolygon(Vector3[] hexagon, Vector3 hexagonOffset, ARPlane arPlane){
        Vector2[] castHexagon = new Vector2[hexagon.Length];
        for(int i=0; i<castHexagon.Length; i++){
            Vector3 hexagonLocalPlaneSpace = arPlane.transform.InverseTransformPoint(hexagon[i] + hexagonOffset);
            castHexagon[i] = new Vector2(hexagonLocalPlaneSpace.x, hexagonLocalPlaneSpace.z);
        }

        //because the polygon can be concave checking if all points of the Hex are in the Polygon isn't enough
        //Thus it's needed to check if any lines of the two polygons intersect
        for(int i=0; i<arPlane.boundary.Length; i++){
            for (int j=0; j<hexagon.Length; j++){
                if(GeometryHelper.DoIntersect(castHexagon[j], castHexagon[(j+1)%hexagon.Length], arPlane.boundary[i], arPlane.boundary[(i+1)%arPlane.boundary.Length]))
                    return false;
            }
        }
        //and if not, if one point of the hex is inside the polygon
        if(!GeometryHelper.PointInPolygon(castHexagon[0], arPlane.boundary))
            return false;

        //No intersection and point of hexagon inside of polygon
        return true;
    }
    #endregion

    private void Awake() {
        gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<MeshFilter>();
    }

    public void PolygonUpdated(HashSet<Hexagon> iteratedHexagons){
        GenerateNeighborHexagons();
        
        iteratedHexagons.Add(this);

        int yCoordinateToListIndex = _coordinates.y-_hexagonGridInfo._lowestYCoordinate;

        Hexagon upNeighbor = NeighborExists(yCoordinateToListIndex, _upNeighborVec);
        //up neighbor. If rows are big enough and column is long enough to contain neighbor
        if(upNeighbor != null){
            if(!iteratedHexagons.Contains(upNeighbor))
                upNeighbor.PolygonUpdated(iteratedHexagons);
        }

        Hexagon upRightNeighbor = NeighborExists(yCoordinateToListIndex, _upRightNeighborVec);
        //up right neighbor. If rows are big enough and column is long enough to contain neighbor
        if(upRightNeighbor != null){
            if(!iteratedHexagons.Contains(upRightNeighbor))
                upRightNeighbor.PolygonUpdated(iteratedHexagons);
        } 

        Hexagon downRightNeighbor = NeighborExists(yCoordinateToListIndex, _downRightNeighborVec);
        //down right neighbor. If rows are big enough and column is long enough to contain neighbor
        if(downRightNeighbor != null){
            if(!iteratedHexagons.Contains(downRightNeighbor))
                downRightNeighbor.PolygonUpdated(iteratedHexagons);
        } 
            
        Hexagon downNeighbor = NeighborExists(yCoordinateToListIndex, _downNeighborVec);
        //down neighbor. If rows are big enough and column is long enough to contain neighbor
        if(downNeighbor != null){
            if(!iteratedHexagons.Contains(downNeighbor))
                downNeighbor.PolygonUpdated(iteratedHexagons);
        }

        Hexagon downLeftNeighbor = NeighborExists(yCoordinateToListIndex, _downLeftNeighborVec);
        //down left neighbor. If rows are big enough and column is long enough to contain neighbor
        if(downLeftNeighbor != null){
            if(!iteratedHexagons.Contains(downLeftNeighbor))
                downLeftNeighbor.PolygonUpdated(iteratedHexagons);
        } 

        Hexagon upLeftNeighbor = NeighborExists(yCoordinateToListIndex, _upLeftNeighborVec);
        //up left neighbor. If rows are big enough and column is long enough to contain neighbor
        if(upLeftNeighbor != null){
            if(!iteratedHexagons.Contains(upLeftNeighbor))
                upLeftNeighbor.PolygonUpdated(iteratedHexagons);
        }
    }
    
    public void GenerateNeighborHexagons(){
        int yCoordinateToListIndex = _coordinates.y-_hexagonGridInfo._lowestYCoordinate;

        //up neighbor. If NO up neighbor can exist yet, or up neighbor exists but is null
        if(NeighborExists(yCoordinateToListIndex, _upNeighborVec) == null){
            //Generate hex
            GenerateHexagon(transform.parent, _coordinates+_upNeighborVec, transform.position+_up, _rootARPlane, _hexagonGridInfo);
        }

        //up right neighbor. If NO up right neighbor can exist yet, or up right neighbor exists but is null
        if(NeighborExists(yCoordinateToListIndex, _upRightNeighborVec) == null){
            //Generate hex
            GenerateHexagon(transform.parent, _coordinates+_upRightNeighborVec, transform.position+_up / 2 + _right, _rootARPlane, _hexagonGridInfo);
        }
        
        //down right neighbor
        if(NeighborExists(yCoordinateToListIndex, _downRightNeighborVec) == null){
            //Generate hex
            GenerateHexagon(transform.parent, _coordinates+_downRightNeighborVec, transform.position-_up / 2 + _right, _rootARPlane, _hexagonGridInfo);
        }

        //down neighbor
        if(NeighborExists(yCoordinateToListIndex, _downNeighborVec) == null){
            //Generate hex
            GenerateHexagon(transform.parent, _coordinates+_downNeighborVec, transform.position-_up, _rootARPlane, _hexagonGridInfo);
        }

        //down left neighbor
        if(NeighborExists(yCoordinateToListIndex, _downLeftNeighborVec) == null){
            //Generate hex
            GenerateHexagon(transform.parent, _coordinates+_downLeftNeighborVec, transform.position-_up / 2 - _right, _rootARPlane, _hexagonGridInfo);
        }

        //up left neighbor
        if(NeighborExists(yCoordinateToListIndex, _upLeftNeighborVec) == null){
            //Generate hex
            GenerateHexagon(transform.parent, _coordinates+_upLeftNeighborVec, transform.position+_up / 2 - _right, _rootARPlane, _hexagonGridInfo);
        }
        return;
    }

    public void DisplayCoordintes(){
        _canvasGO.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(DisableCanvasAfterSeconds());
    }

    IEnumerator DisableCanvasAfterSeconds(){
        yield return new WaitForSeconds(4);
        _canvasGO.SetActive(false);
    }

    #region Helper functions
    Hexagon NeighborExists(int yCoordinateToListIndex, Vector2Int neighborVec){ 
        yCoordinateToListIndex += neighborVec.y;
        //within Y range
        if(yCoordinateToListIndex >= 0 && _hexagonGridInfo._hexagonGrid.Count > yCoordinateToListIndex)
        {
            int xCoordinateToListIndex = _coordinates.x-_hexagonGridInfo._lowestXCoordinates[yCoordinateToListIndex] + neighborVec.x;
            
            //within x range
            if(xCoordinateToListIndex >= 0 
            && _hexagonGridInfo._hexagonGrid[yCoordinateToListIndex].Count > xCoordinateToListIndex)
            {
                //reference exists
                return _hexagonGridInfo._hexagonGrid[yCoordinateToListIndex][xCoordinateToListIndex];
            }
        }
        return null;
    }

    [ContextMenu("Print hex grid")]
    void PrintHexGrid(){
        string gridString = "";
        foreach(List<Hexagon> row in _hexagonGridInfo._hexagonGrid){
            gridString += $"{row.Count}: ";
            foreach(Hexagon column in row){
                if(column == null){
                    gridString+="null ";
                    continue;
                }
                gridString += "hex ";
            }
            gridString += "\n";
        }
        Debug.Log(_hexagonGridInfo._hexagonGrid.Count);
        Debug.Log(gridString);
    }
    #endregion
}

//Serializable tag is here to be able to look at the contents of this class in the inspector while testing in the editor
[Serializable]
public class HexagonGridInfo{
    /*
        Datastructure that hold references to all hexes. Empty spaces are displayed by a null reference
        Used to determine neighboring hexes
        Position is determined by [y]-row [x]-column
        Upper left is lowest coordinate, lower right is highest coordinate

        Grid is structured as following:
    
        (-2, 0)  (0, -1)  (2, -2)  (4, -3) 
            (-1, 0)  (1, -1)  (3, -2)  (5, -3) 
        (-2, 1)  (0, 0)  (2, -1)  (4, -2) 
            (-1, 1)  (1, 0)  (3, -1)  (5, -2) 
        (-2, 2)  (0, 1)  (2, 0)  (4, -1) 

        These coordinates also represent the hexe's position in the 2D list

        Axial coordinates inspired by https://www.redblobgames.com/grids/hexagons/#map-storage
    */
    public List<List<Hexagon>> _hexagonGrid = new List<List<Hexagon>>();

    //lowest y coordinate any Hexagon could reach with the current size of the rows and columns
    //used as a modifier to properly access the hexagons in the grid
    public int _lowestYCoordinate;

    //lowest x coordinate of each row
    //used as a modifier to properly access the hexagons in the grid
    public List<int> _lowestXCoordinates = new List<int>();
}