using Godot;
using System;

public class PhysicsActor : Actor
{
    [Export]
    public bool EnablePhysics = true;
    [Export]
    public Vector3 Gravity;
    [Export]

    public float MaxFloorAngle = 0.9f;

    public bool SnapToGround = true;

    protected Vector3 SnapVector = new Vector3(0, -0.25f, 0);
    protected bool OnGround;
    private Vector3 PreviousVelocity = Vector3.Zero;

    public override void _PhysicsProcess(float delta)
    {
        if (EnablePhysics)
        {
            StateChangeTimer += delta;

            OnGround = IsOnFloor();

            APhysicsProcess(delta);
            CurrentState?.OnPhysicsProcessState(delta);

            //If we're not on the ground, add gravity
            if (!IsOnFloor()){
                //if(Velocity.y >= TerminalVelocity.y) { Velocity.y += Gravity.y; } else { Velocity.y = Gravity.y; }
                Velocity += Gravity;
            } else {
                Velocity.y = Mathf.Max(Velocity.y, Gravity.y);
            }

            //If we're running into a wall, don't build up force
            if (IsOnWall() && Mathf.Sign(Velocity.x) == Mathf.Sign(PreviousVelocity.x)) {Velocity.x = 0;}

            //Enforce terminal velocity
            // if (Math.Abs(Velocity.x) > TerminalVelocity.x) { Velocity.x = TerminalVelocity.x * Mathf.Sign(Velocity.x); }
            // if (Math.Abs(Velocity.y) > TerminalVelocity.y) { Velocity.y = TerminalVelocity.y * Mathf.Sign(Velocity.y); }
            // if (Math.Abs(Velocity.z) > TerminalVelocity.z) { Velocity.z = TerminalVelocity.z * Mathf.Sign(Velocity.z); }

            //if(Velocity.y >= TerminalVelocity.y) { Velocity.y += Gravity.y; } else { Velocity.y = Gravity.y; }


            APhysicsPostProcess(delta);
            GD.Print("Snap: " + SnapToGround.ToString() + " Velocity: " + Velocity.ToString());

            if(SnapToGround) {
                MoveAndSlideWithSnap(Velocity, SnapVector, floorMaxAngle: 0.9f, floorNormal: FloorNormal);
            } else {
                MoveAndSlide(Velocity, floorMaxAngle: 0.9f, floorNormal: FloorNormal);
            }

            PreviousVelocity = Velocity;
        }
        else
        {
            base._PhysicsProcess(delta);
        }
    }
}
