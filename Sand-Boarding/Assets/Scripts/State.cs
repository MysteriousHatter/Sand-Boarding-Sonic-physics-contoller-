using UnityEngine;

public abstract class State 
{
    public abstract void Enter();
    public abstract void Exit();
    public abstract void Tick(float deltaTime);
    public abstract void FixedTick(float fixedDeltaTime);
}
