using Godot;
using System;

public class PhysicsActor : Actor
{
    [Export]
    public bool EnablePhysics = true;
    [Export]
    public Vector3 Gravity = new Vector3(0, -2, 0);
    public Vector3 Velocity = Vector3.Zero;

    [Export]
    public float MaxFloorAngle = 0.95f;
    protected Vector3 FloorNormal = new Vector3(0, 1, 0);

    public bool SnapToGround = true;

    protected Vector3 SnapVector = new Vector3(0, -0.3f, 0);
    protected Vector3 PreviousVelocity = Vector3.Zero;

    public override void _PhysicsProcess(float delta)
    {
        if (EnablePhysics)
        {
            Lifetime += delta;
            StateChangeTimer += delta;

            APhysicsPreProcess(delta);
            CurrentState?.OnPhysicsProcessState(delta);

            //If we're not on the ground, add gravity
            if (!IsOnFloor() || !SnapToGround){
                Velocity += Gravity;
            } else {
                Velocity.y = 0;
            }

            //If we're running into a wall, don't build up force
            if (IsOnWall() && Mathf.Sign(Velocity.x) == Mathf.Sign(PreviousVelocity.x)) {Velocity.x = 0;}

            //If we're running into a ceiling, don't build up force
            if (IsOnCeiling() && Mathf.Sign(Velocity.y) == Mathf.Sign(PreviousVelocity.y)) {Velocity.y = 0;}

            APhysicsPostProcess(delta);

            if(SnapToGround) {
                MoveAndSlideWithSnap(Velocity, SnapVector, upDirection: Vector3.Up, floorMaxAngle: 0.9f);
            }
            else {
                MoveAndSlide(Velocity, upDirection: Vector3.Up, floorMaxAngle: 0.9f);
            }

            if(IsOnFloor()) FloorNormal = GetFloorNormal();

            PreviousVelocity = Velocity;
        }
        else
        {
            base._PhysicsProcess(delta);
        }
    }

    //Helper methods to assist handling 2D forces in a 3D world
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
