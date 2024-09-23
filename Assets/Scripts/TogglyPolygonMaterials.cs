using UnityEngine;

public class TogglyPolygonMaterials : MonoBehaviour
{
    public void ToggleAllPolygonMaterials(){
        foreach(HexagonalTesselation hexTes in FindObjectsOfType<HexagonalTesselation>())
            hexTes.TogglePolygonMaterial();
    }
}
