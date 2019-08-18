using Godot;
using System;

public class Player : PhysicsActor
{
    //Movement parameters
    [Export(PropertyHint.Range, "1,4")]
    public int PlayerNumber;
    [Export]
    public float WalkAcceleration;
    [Export]
    public float WalkSpeed;
    [Export]
    public float RunAcceleration;
    [Export]
    public float RunSpeed;
    [Export]
    public float LongRunAcceleration;
    [Export]
    public float LongRunSpeed;
    [Export]
    public float LongRunTime;
    [Export]
    public float IdleJumpForce;
    [Export]
    public float WalkJumpForce;
    [Export]
    public float LongRunJumpForce;
    [Export]
    public float MaxJumpSustainTime;
    [Export]
    public float JumpSustainGravityMultiplier;
    [Export]
    public float AirHorizontalAcceleration;

    [Export]
    public float FloorFriction;
    [Export]
    public float AirFriction;

    //Node references
    public AnimatedSprite3D PlayerSprite;
    public Area ActorDetectorArea;

    //Player states
    public ActorState StandState, WalkState, RunState, LongRunState, JumpState, FallState;

    //Speed limit handling
    private float SpeedLimit;
    private float SpeedLerpDuration = 0.5f;
    private float SpeedLerpStartTime;
    private float SpeedLerpStartVelocity;

    //Input manager for this player instance
    private InputManager InputManager;

    public override ActorState GetDefaultState() {return StandState;}
    public override void AReady() {
        PlayerSprite = GetNodeOrNull(new NodePath("PlayerSprite")) as AnimatedSprite3D;
        ActorDetectorArea = GetNodeOrNull(new NodePath("ActorDetector")) as Area;
        if (PlayerSprite == null || ActorDetectorArea == null) GD.PrintErr("One or multiple required child nodes could not be found!");

        InputManager = new InputManager(PlayerNumber);

        StandState = new ActorState(() =>
        { //Enter State
            
            SnapToGround = true;
        }, (float delta) =>
        { //Process State
            if(Mathf.Abs(Velocity.x) > 0.5f) {
                PlayerSprite.SetAnimation(PlayerAnimation.WALK);
                PlayerSprite.Frames.SetAnimationSpeed(PlayerAnimation.WALK, (Mathf.Abs(Velocity.x)) + 5);
            } else {
                PlayerSprite.SetAnimation(PlayerAnimation.IDLE);
            }
        }, (float delta) =>
        { //State Physics Processing
            if(InputManager.DirectionalInput.x != 0) ChangeState(WalkState);
            CanFall();
            CanJump();
        }, () =>
        { //Exit State

        });

        WalkState = new ActorState(() =>
        { //Enter State
            SetSpeedLimit(WalkSpeed);
            SnapToGround = true;
        }, (float delta) =>
        { //Process State
            if(Mathf.Sign(Velocity.x) == Mathf.Sign(InputManager.DirectionalInput.x) || Velocity.x == 0) {
                PlayerSprite.SetAnimation(PlayerAnimation.WALK);
                PlayerSprite.Frames.SetAnimationSpeed(PlayerAnimation.WALK, (Mathf.Abs(Velocity.x)) + 5);
            } else {
                PlayerSprite.SetAnimation(PlayerAnimation.TURN);
            }
        }, (float delta) =>
        { //State Physics Processing
            if(InputManager.DirectionalInput.x == 0) ChangeState(StandState);
            if(InputManager.RunPressed || InputManager.AltRunPressed) ChangeState(RunState);
            CanFall();
            CanJump();

            float force = InputManager.DirectionalInput.x * WalkAcceleration;
            ApplyForce2D(new Vector2(force, 0));
        }, () =>
        { //Exit State

        });

        RunState = new ActorState(() =>
        { //Enter State
            SetSpeedLimit(RunSpeed);
            SnapToGround = true;
        }, (float delta) =>
        { //Process State
            if(Mathf.Sign(Velocity.x) == Mathf.Sign(InputManager.DirectionalInput.x) || Velocity.x == 0) {
                PlayerSprite.SetAnimation(PlayerAnimation.WALK);
                PlayerSprite.Frames.SetAnimationSpeed(PlayerAnimation.WALK, (Mathf.Abs(Velocity.x)) + 7);
            } else {
                PlayerSprite.SetAnimation(PlayerAnimation.TURN);
            }
        }, (float delta) =>
        { //State Physics Processing
            if(InputManager.DirectionalInput.x == 0) ChangeState(StandState);
            if(!InputManager.RunPressed && !InputManager.AltRunPressed) ChangeState(WalkState);
            if(GetElapsedTimeInState() > LongRunTime) ChangeState(LongRunState);
            CanFall();
            CanJump();


            float force = InputManager.DirectionalInput.x * RunAcceleration;
            ApplyForce2D(new Vector2(force, 0));
        }, () =>
        { //Exit State

        });

        LongRunState = new ActorState(() =>
        { //Enter State
            PlayerSprite.SetAnimation(PlayerAnimation.LONG_RUN);
            SetSpeedLimit(LongRunSpeed);
            SnapToGround = true;
        }, (float delta) =>
        { //Process State
            if(Mathf.Sign(Velocity.x) == Mathf.Sign(InputManager.DirectionalInput.x) || Velocity.x == 0) {
                PlayerSprite.SetAnimation(PlayerAnimation.LONG_RUN);
                PlayerSprite.Frames.SetAnimationSpeed(PlayerAnimation.LONG_RUN, (Mathf.Abs(Velocity.x)) + 10);
            } else {
                PlayerSprite.SetAnimation(PlayerAnimation.TURN);
            }
        }, (float delta) =>
        { //State Physics Processing
            if(InputManager.DirectionalInput.x == 0) ChangeState(StandState);
            if(!InputManager.RunPressed && !InputManager.AltRunPressed) ChangeState(WalkState);
            CanFall();
            CanJump();

            float force = InputManager.DirectionalInput.x * LongRunAcceleration;
            ApplyForce2D(new Vector2(force, 0));
        }, () =>
        { //Exit State

        });

        JumpState = new ActorState(() =>
        { //Enter State
            SnapToGround = false;
            
            float speed = Mathf.Abs(Velocity.x);
            if(speed > RunSpeed) {
                ApplyForce2D(new Vector2(0, LongRunJumpForce));
                PlayerSprite.SetAnimation(PlayerAnimation.HIGH_JUMP);
            } else if(speed > 0.5f) {
                ApplyForce2D(new Vector2(0, WalkJumpForce));
                PlayerSprite.SetAnimation(PlayerAnimation.JUMP);
            } else {
                ApplyForce2D(new Vector2(0, IdleJumpForce));
                PlayerSprite.SetAnimation(PlayerAnimation.JUMP);
            }
        }, (float delta) =>
        { //Process State
            DebugText.Display("P" + PlayerNumber + "_JumpSusTime", "P" + PlayerNumber + " Jump Sustain Time: " + (MaxJumpSustainTime - GetElapsedTimeInState()).ToString());
        }, (float delta) =>
        { //State Physics Processing
            if(IsOnFloor()) ChangeState(StandState);
            if((!InputManager.JumpPressed && !InputManager.AltJumpPressed) || GetElapsedTimeInState() > MaxJumpSustainTime) ChangeState(FallState);

            if(SpeedLimit >= LongRunSpeed && (InputManager.RunPressed || InputManager.AltRunPressed)){
                SetSpeedLimit(LongRunSpeed);
            } else if(InputManager.RunPressed || InputManager.AltRunPressed) {
                SetSpeedLimit(RunSpeed);
            } else {
                SetSpeedLimit(WalkSpeed);
            }

            //Add some force for extra air time if the jump button is held
            ApplyForce2D(Vector2.Up, Gravity.y * (1-JumpSustainGravityMultiplier));

            float force = InputManager.DirectionalInput.x * AirHorizontalAcceleration;
            ApplyForce2D(new Vector2(force, 0));
        }, () =>
        { //Exit State
            DebugText.Remove("P" + PlayerNumber + "_JumpSusTime");
        });

        FallState = new ActorState(() =>
        { //Enter State
            PlayerSprite.SetAnimation(PlayerAnimation.FALL);
            SnapToGround = false;
        }, (float delta) =>
        { //Process State

        }, (float delta) =>
        { //State Physics Processing
            if(IsOnFloor()) ChangeState(StandState);

            if(SpeedLimit >= LongRunSpeed && (InputManager.RunPressed || InputManager.AltRunPressed)){
                SetSpeedLimit(LongRunSpeed);
            } else if(InputManager.RunPressed || InputManager.AltRunPressed) {
                SetSpeedLimit(RunSpeed);
            } else {
                SetSpeedLimit(WalkSpeed);
            }

            float force = InputManager.DirectionalInput.x * AirHorizontalAcceleration;
            ApplyForce2D(new Vector2(force, 0));
        }, () =>
        { //Exit State

        });
    }

    private void CanJump(){
        if(InputManager.JumpJustPressed || InputManager.AltJumpJustPressed) ChangeState(JumpState);
    }

    private void CanFall() {
        if(!IsOnFloor()) ChangeState(FallState);
    }

    public override void APhysicsProcess(float delta){
        InputManager.UpdateInputs();
    }

    public override void APhysicsPostProcess(float delta){
        //Apply friction
        Velocity.x = Mathf.Max((Mathf.Abs(Velocity.x) - (IsOnFloor() ? FloorFriction : AirFriction)), 0) * Mathf.Sign(Velocity.x);

        //Lerp to speed limit if above
        if(Mathf.Abs(Velocity.x) > SpeedLimit) { Velocity.x = ClampedInterpolation.Lerp(SpeedLerpStartVelocity, SpeedLimit, (Lifetime - SpeedLerpStartTime) * (1/SpeedLerpDuration)) * Mathf.Sign(Velocity.x); }

        //DebugText.Display("P" + PlayerNumber + "_SpeedLimit", "P" + PlayerNumber + " Speed Limit: " + SpeedLimit.ToString());

        MoveAndSlideWithSnap(new Vector3(Velocity.x, Velocity.y, 0), SnapVector, FloorNormal, false, floorMaxAngle: MaxFloorAngle);
    }

    public override void APostProcess(float delta) {
        //Flip the sprite correctly
        if (InputManager.DirectionalInput.x > 0)
        {
            PlayerSprite.SetFlipH(false);
        }
        else if (InputManager.DirectionalInput.x < 0)
        {
            PlayerSprite.SetFlipH(true);
        }
        
        DebugText.Display("P" + PlayerNumber + "_Position", "P" + PlayerNumber + " Position: " + Transform.origin.ToString());
        DebugText.Display("P" + PlayerNumber + "_Velocity", "P" + PlayerNumber + " Velocity: " + Velocity.ToString());
        DebugText.Display("P" + PlayerNumber + "_StateTime", "P" + PlayerNumber + " Time in State: " + GetElapsedTimeInState().ToString());
    }

    private void SetSpeedLimit(float limit){
        if(limit == SpeedLimit) return;

        SpeedLerpStartTime = Lifetime;
        SpeedLerpStartVelocity = Mathf.Abs(Velocity.x);
        SpeedLimit = limit;
    }

    private struct PlayerAnimation {
        public const string IDLE = "Idle";
        public const string WALK = "Walk";
        public const string LONG_RUN = "LongRun";
        public const string TURN = "Turn";
        public const string JUMP = "Jump";
        public const string HIGH_JUMP = "HighJump";
        public const string FALL = "Fall";
        public const string CROUCH = "Crouch";
        public const string SLIDE = "Slide";
    }
}
