using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    PlayerInput _playerInput;
    
    InputAction _clickPosition, _clickDown;

    Vector2 _clickScreenPos;

    Camera _mainCamera; 

    // Start is called before the first frame update
    void Start()
    {
        _mainCamera = Camera.main;

        _playerInput = GetComponent<PlayerInput>();

        _clickPosition = _playerInput.actions["ClickPosition"];
        _clickDown = _playerInput.actions["ClickDown"];

        _clickPosition.performed += ClickPosition;
        _clickDown.performed += ClickDown;
    }

    private void ClickPosition(InputAction.CallbackContext context)
    {
        _clickScreenPos = context.ReadValue<Vector2>();
    }

    private void ClickDown(InputAction.CallbackContext context)
    {
        if(context.ReadValueAsButton() == false)
            return;

        RaycastHit hit;
        Physics.Raycast(_mainCamera.ScreenPointToRay(_clickScreenPos), out hit);
        if(hit.collider != null){
            Hexagon hex = hit.collider.GetComponent<Hexagon>();
            if(hex != null){
                hex.DisplayCoordintes();
            }
        }
    }

    private void OnDestroy() {
        _clickPosition.performed -= ClickPosition;
        _clickDown.performed -= ClickDown;
    }
}
