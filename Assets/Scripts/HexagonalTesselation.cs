using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class HexagonalTesselation : MonoBehaviour
{   
    [SerializeField][Tooltip("Size is outer radius, so from middle point of hexagon to corner point")]
    float _hexagonSize = .25f;

    [SerializeField]
    Material _hexagonMaterial;

    ARPlane _arPlane;

    Hexagon _centerHexagon;

    //using this so the hexagon grid doesn't move with the ARPlane, as the transform of the ARPlane is always at the center of the polygon
    GameObject _hexagonGridContainer;

    [SerializeField]
    GameObject _hexagonCanvas;
    
    MeshRenderer _meshRenderer;
    LineRenderer _lineRenderer;
    Material _meshRendMat, _lineRendMat;

    // Start is called before the first frame update
    void Start()
    {
        _arPlane = GetComponent<ARPlane>();

        _arPlane.boundaryChanged += OnBoundaryChanged;

        _hexagonGridContainer = new GameObject($"Hexagon grid container of {gameObject.name}");
        Hexagon.Init(_hexagonSize, _hexagonMaterial, _hexagonCanvas);
        _centerHexagon = Hexagon.GenerateHexagon(_hexagonGridContainer.transform, Vector2Int.zero, transform.position, _arPlane);

        _meshRenderer = GetComponent<MeshRenderer>();
        _lineRenderer = GetComponent<LineRenderer>();
        _meshRendMat = _meshRenderer.material;
        _lineRendMat = _lineRenderer.material;
    }

    void OnDestroy() {
        Destroy(_hexagonGridContainer);
        _arPlane.boundaryChanged -= OnBoundaryChanged;
    }

    void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs args)
    {
        if(_centerHexagon == null){
            _centerHexagon = Hexagon.GenerateHexagon(_hexagonGridContainer.transform, Vector2Int.zero, transform.position, _arPlane);

            //no room for a single hexagon yet
            if(_centerHexagon == null)
                return;
        }

        _centerHexagon.PolygonUpdated(new HashSet<Hexagon>());
    }

    public void TogglePolygonMaterial(){
        List<Material> meshRendMats = new List<Material>(), lineRendMats = new List<Material>();
        if(_meshRenderer.materials.Length == 0){
            meshRendMats.Add(_meshRendMat);
            lineRendMats.Add(_lineRendMat);
        }
        _meshRenderer.SetMaterials(meshRendMats);
        _lineRenderer.SetMaterials(lineRendMats);
    }
}
