using Godot;
using System;

public class Actor : KinematicBody
{
    public float Lifetime {get; protected set;}
    public ActorState CurrentState {get; protected set;}
    public ActorState PreviousState  {get; protected set;}
    protected float StateChangeTimer;

    //Actor methods - override these instead of Godot's!
    public virtual void AEnterTree() { }
    public virtual void APhysicsPreProcess(float delta) { }
    public virtual void APhysicsPostProcess(float delta) { }
    public virtual void AProcess(float delta) { }
    public virtual void APostProcess(float delta) { }

    public virtual ActorState GetDefaultState() { return null; }
    public virtual void Damage(Actor other) { }
    public virtual void Kill(Actor other) { }

    //Behind-the-scenes implementation of certain systems that call the A-prefixed methods at the appropriate time

    public override void _EnterTree()
    {
        AEnterTree();

        MoveLockZ = true;

        CurrentState = GetDefaultState();
        PreviousState = CurrentState;
        CurrentState?.OnEnterState();
    }

    public override void _PhysicsProcess(float delta)
    {
        Lifetime += delta;
        StateChangeTimer += delta;

        APhysicsPreProcess(delta);
        CurrentState?.OnPhysicsProcessState(delta);
        APhysicsPostProcess(delta);
    }

    public override void _Process(float delta)
    {
        AProcess(delta);
        CurrentState?.OnProcessState(delta);
        APostProcess(delta);
    }

    //Helper methods for handling states and transitions between them
    public void ChangeState(ActorState newState)
    {
        CurrentState.OnExitState();
        PreviousState = CurrentState;
        CurrentState = newState;
        StateChangeTimer = 0;
        CurrentState.OnEnterState();
    }

    public float GetElapsedTimeInState()
    {
        return StateChangeTimer;
    }
}
