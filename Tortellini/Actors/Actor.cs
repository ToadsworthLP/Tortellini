using Godot;
using System;

public class Actor : KinematicBody
{
    public Vector3 Velocity = Vector3.Zero;
    protected Vector3 FloorNormal = new Vector3(0, 1, 0);
    protected ActorState CurrentState;
    protected ActorState PreviousState;
    protected float StateChangeTimer;

    //Actor methods - override these instead of Godot's!
    public virtual void AReady() { }
    public virtual void APhysicsProcess(float delta) { }
    public virtual void APhysicsPostProcess(float delta) { }
    public virtual void AProcess(float delta) { }
    public virtual void APostProcess(float delta) { }

    public virtual ActorState GetDefaultState() { return null; }
    public virtual void Damage(Actor other) { }
    public virtual void Kill(Actor other) { }

    //Behind-the-scenes implementation of certain systems that call the A-prefixed methods at the appropriate time

    public override void _Ready()
    {
        AReady();

        MoveLockZ = true;

        CurrentState = GetDefaultState();
        PreviousState = CurrentState;
        CurrentState?.OnEnterState();
    }

    public override void _PhysicsProcess(float delta)
    {
        StateChangeTimer += delta;

        APhysicsProcess(delta);
        CurrentState?.OnPhysicsProcessState(delta);

        APhysicsPostProcess(delta);

        MoveAndSlide(Velocity, floorMaxAngle: 0.9f, floorNormal: FloorNormal);
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

    //Helper methods to assist handling 2D movement in a 3D world
    public void ApplyForce2D(Vector2 force)
    {
        Velocity.x += force.x;
        Velocity.y += force.y;
    }

    public void ApplyForce2D(Vector2 direction, float speed)
    {
        Vector2 normalized = direction.Normalized();
        Vector2 force = normalized * speed;

        Velocity.x += force.x;
        Velocity.y += force.y;
    }
}
