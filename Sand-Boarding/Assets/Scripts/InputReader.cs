using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour, PlayerControls.IControlsActions
{

    public Vector2 MovementValue {get; private set;}
    public bool pressedJumpButton {get; private set;}
    public event Action JumpEvent;
    private PlayerControls playerControls;

    void Start()
    {
        playerControls = new PlayerControls();
        playerControls.Controls.SetCallbacks(this);

        playerControls.Controls.Enable();
    }
    public void OnDownRotation(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(!context.performed) {return;}
        JumpEvent?.Invoke();
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

    public void OnSpeedUpSlowDown(InputAction.CallbackContext context)
    {
        MovementValue = context.ReadValue<Vector2>();
        Debug.Log("Current Movment value " + MovementValue);
    }

    public void OnUpRotation(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

}
