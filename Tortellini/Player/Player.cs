using System;
using Godot;

public class Player : PhysicsActor
{
    [Export]
    public float Inertia = 0.8f;
    [Export]
    public float AirInertia = 0.5f;
    [Export]
    public float WalkAcceleration;
    [Export]
    public float RunAcceleration;
    [Export]
    public float AirAcceleration;

    [Export]
    public float MaxWalkSpeed;
    [Export]
    public float MaxRunSpeed;
    [Export]
    public float MaxAirSpeed;

    [Export]
    public float JumpSpeed;
    [Export]
    public float MaxJumpTime;

    private AnimatedSprite3D PlayerSprite;
    private Area ActorDetectorArea;

    private ActorState StandState, WalkState, RunState, JumpState, FallState;

    public override ActorState GetDefaultState() { return StandState; }

    public override void AReady() //TODO Try moving everything non-visual to Physics Processing
    {
        PlayerSprite = GetNodeOrNull(new NodePath("PlayerSprite")) as AnimatedSprite3D;
        ActorDetectorArea = GetNodeOrNull(new NodePath("ActorDetector")) as Area;

        if (PlayerSprite == null || ActorDetectorArea == null) GD.PrintErr("One or multiple required child nodes could not be found!");

        //Define states

        StandState = new ActorState(() =>
        { //Enter State
            PlayerSprite.SetAnimation("Idle");
            SnapToGround = true;
        }, (float delta) =>
        { //Process State
            //Switch to the walking state when directional input is given
            bool sideInput = Mathf.Abs(Input.GetActionStrength("p1_right") - Input.GetActionStrength("p1_left")) > 0;
            if (sideInput) ChangeState(WalkState);

            CanFall();
            CanJump();
        }, (float delta) =>
        { //State Physics Processing

        }, () =>
        { //Exit State

        });


        WalkState = new ActorState(() =>
        { //Enter State
            PlayerSprite.SetAnimation("Walk");
            SnapToGround = true;
        }, (float delta) =>
        { //Process State
            //Switch to the standing state when no directional input is given
            float sideInput = Input.GetActionStrength("p1_right") - Input.GetActionStrength("p1_left");
            if (sideInput == 0) ChangeState(StandState);

            //Check if the player should be running, if yes go to run state
            bool isRunPressed = Input.GetActionStrength("p1_run") > 0;
            if (isRunPressed) ChangeState(RunState);

            //Move the player
            Vector2 movement = new Vector2();
            movement.x = sideInput * (isRunPressed ? RunAcceleration : WalkAcceleration);
            ApplyForce2D(movement);

            //Update the walking animation speed
            PlayerSprite.Frames.SetAnimationSpeed("Walk", (Mathf.Abs(Velocity.x) / 2) + 8);

            CanFall();
            CanJump();
        }, (float delta) =>
        { //State Physics Processing
            //Enforce walk speed limit
            if (Math.Abs(Velocity.x) > MaxWalkSpeed) { Velocity.x = Interpolation.Lerp(Velocity.x, MaxWalkSpeed * Mathf.Sign(Velocity.x), GetElapsedTimeInState()); }
        }, () =>
        { //Exit State

        });


        RunState = new ActorState(() =>
        { //Enter State
            PlayerSprite.SetAnimation("Walk");
            SnapToGround = true;
        }, (float delta) =>
        { //Process State
            //Switch to the standing state when no directional input is given
            float sideInput = Input.GetActionStrength("p1_right") - Input.GetActionStrength("p1_left");
            if (sideInput == 0) ChangeState(StandState);

            //Check if the player should be running, if not go back to walk state
            bool isRunPressed = Input.GetActionStrength("p1_run") > 0;
            if (!isRunPressed) ChangeState(WalkState);

            //Move the player
            Vector2 movement = new Vector2();
            movement.x = sideInput * (isRunPressed ? RunAcceleration : WalkAcceleration);
            ApplyForce2D(movement);

            //Update the walking animation speed
            PlayerSprite.Frames.SetAnimationSpeed("Walk", Mathf.Max((Mathf.Abs(Velocity.x)) + 2, (Mathf.Abs(Velocity.x) / 2) + 8));

            CanFall();
            CanJump();
        }, (float delta) =>
        { //State Physics Processing
            //Enforce walk speed limit
            if (Math.Abs(Velocity.x) > MaxRunSpeed) { Velocity.x = Interpolation.Lerp(Velocity.x, MaxRunSpeed * Mathf.Sign(Velocity.x), GetElapsedTimeInState()); }
        }, () =>
        { //Exit State

        });


        JumpState = new ActorState(() =>
        { //Enter State
            GD.Print("Jump");

            PlayerSprite.SetAnimation("Jump");
            SnapToGround = false;

            //Add the initial force of the jump
            ApplyForce2D(new Vector2(0, JumpSpeed));
        }, (float delta) =>
        { //Process State
            float jumpTime = GetElapsedTimeInState();

            //Add some force for extra air time if the jump button is held
            //ApplyForce2D(Vector2.Up * Gravity.y/2);

            //Allow slight player movement
            float sideInput = Input.GetActionStrength("p1_right") - Input.GetActionStrength("p1_left");
            Vector2 movement = new Vector2();
            movement.x = sideInput * (AirAcceleration);
            ApplyForce2D(movement);

            //Exit state if required
            bool jumpPressed = Input.GetActionStrength("p1_jump") > 0;
            if(!jumpPressed || jumpTime > MaxJumpTime) ChangeState(FallState);
            if(IsOnFloor()) ChangeState(StandState);
        }, (float delta) =>
        { //State Physics Processing

        }, () =>
        { //Exit State

        });


        FallState = new ActorState(() =>
        { //Enter State
            GD.Print("Fall");

            SnapToGround = false;
            PlayerSprite.SetAnimation("Jump");
        }, (float delta) =>
        { //Process State
            //Allow slight player movement
            float sideInput = Input.GetActionStrength("p1_right") - Input.GetActionStrength("p1_left");
            Vector2 movement = new Vector2();
            movement.x = sideInput * (AirAcceleration);
            ApplyForce2D(movement);

            if (OnGround == true) ChangeState(StandState);
        }, (float delta) =>
        { //State Physics Processing
            //Enforce air speed limit
            if (Math.Abs(Velocity.x) > MaxAirSpeed) { Velocity.x = Interpolation.Lerp(Velocity.x, MaxAirSpeed * Mathf.Sign(Velocity.x), GetElapsedTimeInState()); }
        }, () =>
        { //Exit State

        });
    }

    public override void APhysicsProcess(float delta)
    {

    }

    public override void APhysicsPostProcess(float delta)
    {
        //Apply inertia on the x axis
        if (Mathf.Abs(Velocity.x) - (OnGround ? Inertia : AirInertia) <= 0)
        {
            Velocity.x = 0;
        }
        else
        {
            Velocity.x = (Mathf.Abs(Velocity.x) - (OnGround ? Inertia : AirInertia)) * Mathf.Sign(Velocity.x);
        }
    }

    public override void AProcess(float delta)
    {
        //Flip the sprite correctly
        if (Input.GetActionStrength("p1_right") - Input.GetActionStrength("p1_left") > 0)
        {
            PlayerSprite.SetFlipH(false);
        }
        else if (Velocity.x < 0)
        {
            PlayerSprite.SetFlipH(true);
        }

        DebugText.Display("P1_Pos", "P1 Position: " + Transform.origin.ToString());
        DebugText.Display("P1_Vel", "P1 Velocity: " + Velocity.ToString());
        DebugText.Display("P1_Grnd", "P1 On Ground: " + IsOnFloor());
        DebugText.Display("P1_Snap", "P1 Ground Snap: " + SnapToGround);
    }

    private void CanFall()
    {
        if (!OnGround) ChangeState(FallState);
    }

    private void CanJump()
    {
        if (OnGround && Input.GetActionStrength("p1_jump") > 0)
        {
            ChangeState(JumpState);
        }
    }
}
