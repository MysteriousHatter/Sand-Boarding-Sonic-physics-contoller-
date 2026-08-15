using UnityEngine;

public class StateMachine : MonoBehaviour
{

    private State currentState;

    // Update is called once per frame
    void Update()
    {
        currentState?.Tick(Time.deltaTime);
        Debug.Log($"Current State: {currentState.GetType().Name}");
    }

    protected virtual void FixedUpdate()
    {
        Debug.Log("Is fixed upate working?");
        currentState?.FixedTick(Time.fixedDeltaTime);
    }

    public void SwitchState(State newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }
}
