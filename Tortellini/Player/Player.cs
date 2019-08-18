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
    [Export(PropertyHint.Range, "-1,1")]
    public float CrouchInputThreshold;
    [Export(PropertyHint.Range, "0, 180")]
    public float SlideMinAngle;
    [Export]
    public float SlideAcceleration;
    [Export]
    public float SlideSpeed;

    [Export]
    public float FloorFriction;
    [Export]
    public float AirFriction;
    [Export]
    public float SlideFriction;


    //Node references
    public AnimatedSprite3D PlayerSprite;
    public Area ActorDetectorArea;
    public RayCast RayCast;

    //Player states
    public ActorState StandState, WalkState, RunState, LongRunState, JumpState, SpinJumpState, FallState, CrouchState, SlideState;

    //Speed limit handling
    private float SpeedLimit;
    private float SpeedLerpDuration = 0.5f;
    private float SpeedLerpStartTime;
    private float SpeedLerpStartVelocity;

    //Input manager for this player instance
    private InputManager InputManager;

    //Controls whether player facing is set automatically based on player input
    private bool autoPlayerFacing = true;

    public override ActorState GetDefaultState() {return StandState;}
    public override void AReady() {
        PlayerSprite = GetNodeOrNull(new NodePath("PlayerSprite")) as AnimatedSprite3D;
        ActorDetectorArea = GetNodeOrNull(new NodePath("ActorDetector")) as Area;
        RayCast = GetNodeOrNull(new NodePath("RayCast")) as RayCast;
        if (PlayerSprite == null || ActorDetectorArea == null || RayCast == null) GD.PrintErr("One or multiple required child nodes could not be found! Some features won't work!");

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
            CanCrouch();
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
            CanCrouch();

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
            CanCrouch();

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
            if(!InputManager.RunPressed && !InputManager.AltRunPressed || Mathf.Sign(Velocity.x) != Mathf.Sign(InputManager.DirectionalInput.x)) ChangeState(WalkState);
            CanFall();
            CanJump();
            CanCrouch();

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
            } else if(PreviousState == CrouchState) {
                ApplyForce2D(new Vector2(0, IdleJumpForce));
                PlayerSprite.SetAnimation(PlayerAnimation.CROUCH);
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

        SpinJumpState = new ActorState(() =>
        { //Enter State
            SnapToGround = false;
            PlayerSprite.SetAnimation(PlayerAnimation.SPIN_JUMP);

            float speed = Mathf.Abs(Velocity.x);
            if(speed > RunSpeed) {
                ApplyForce2D(new Vector2(0, LongRunJumpForce));
            } else if(speed > 0.5f) {
                ApplyForce2D(new Vector2(0, WalkJumpForce));
            } else if(PreviousState == CrouchState) {
                ApplyForce2D(new Vector2(0, IdleJumpForce));
            } else {
                ApplyForce2D(new Vector2(0, IdleJumpForce));
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
            if(InputManager.DirectionalInput.y < CrouchInputThreshold) {
                PlayerSprite.SetAnimation(PlayerAnimation.CROUCH);
            } else if (PreviousState == SpinJumpState) {
                PlayerSprite.SetAnimation(PlayerAnimation.SPIN_JUMP);
            } else {
                PlayerSprite.SetAnimation(PlayerAnimation.FALL);
            }
            SnapToGround = false;
        }, (float delta) =>
        { //Process State

        }, (float delta) =>
        { //State Physics Processing
            if(IsOnFloor()) {
                if(InputManager.DirectionalInput.y < CrouchInputThreshold) {
                    ChangeState(CrouchState);
                } else {
                    ChangeState(StandState);
                }
            }

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

        CrouchState = new ActorState(() =>
        { //Enter State
            SnapToGround = true;

            var angle = Mathf.Rad2Deg(Mathf.Acos(RayCast.GetCollisionNormal().Dot(FloorNormal)));
            if(angle >= SlideMinAngle){
                ChangeState(SlideState);
                return;
            }

            PlayerSprite.SetAnimation(PlayerAnimation.CROUCH);
        }, (float delta) =>
        { //Process State

        }, (float delta) =>
        { //State Physics Processing
            if(InputManager.DirectionalInput.y >= CrouchInputThreshold) ChangeState(StandState);
            CanFall();
            CanJump();
        }, () =>
        { //Exit State

        });

        SlideState = new ActorState(() =>
        { //Enter State
            SetSpeedLimit(SlideSpeed);
            SnapToGround = true;

            PlayerSprite.SetAnimation(PlayerAnimation.SLIDE);
            autoPlayerFacing = false;
        }, (float delta) =>
        { //Process State
            //Manually flip the sprite according to the movement direction
            if (Velocity.x > 0)
            {
                PlayerSprite.SetFlipH(false);
            }
            else if (Velocity.x < 0)
            {
                PlayerSprite.SetFlipH(true);
            }
        }, (float delta) =>
        { //State Physics Processing
            Vector3 normal = RayCast.GetCollisionNormal();
            float angle = Mathf.Rad2Deg(Mathf.Acos(normal.Dot(FloorNormal)));
            if(angle < SlideMinAngle) ChangeState(InputManager.DirectionalInput.y < CrouchInputThreshold ? CrouchState : StandState );
            CanFall();
            CanJump();

            float force = Mathf.Sign(normal.x) * SlideAcceleration;
            ApplyForce2D(new Vector2(force, 0));
        }, () =>
        { //Exit State
            autoPlayerFacing = true;
        });
    }

    private void CanJump(){
        if(InputManager.JumpJustPressed) {
            ChangeState(JumpState);
        } else if (InputManager.AltJumpJustPressed) {
            ChangeState(SpinJumpState);
        }
    }

    private void CanFall() {
        if(!IsOnFloor()) ChangeState(FallState);
    }

    private void CanCrouch() {
        if(IsOnFloor() && InputManager.DirectionalInput.y < CrouchInputThreshold) ChangeState(CrouchState);
    }

    public override void APhysicsProcess(float delta){
        InputManager.UpdateInputs();
    }

    public override void APhysicsPostProcess(float delta){
        //Apply friction
        if(CurrentState == SlideState){
            Velocity.x = Mathf.Max(Mathf.Abs(Velocity.x) - SlideFriction, 0) * Mathf.Sign(Velocity.x);
        } else {
            Velocity.x = Mathf.Max((Mathf.Abs(Velocity.x) - (IsOnFloor() ? FloorFriction : AirFriction)), 0) * Mathf.Sign(Velocity.x);
        }

        //Lerp to speed limit if above
        if(Mathf.Abs(Velocity.x) > SpeedLimit) { Velocity.x = ClampedInterpolation.Lerp(SpeedLerpStartVelocity, SpeedLimit, (Lifetime - SpeedLerpStartTime) * (1/SpeedLerpDuration)) * Mathf.Sign(Velocity.x); }

        //DebugText.Display("P" + PlayerNumber + "_SpeedLimit", "P" + PlayerNumber + " Speed Limit: " + SpeedLimit.ToString());

        MoveAndSlideWithSnap(new Vector3(Velocity.x, Velocity.y, 0), SnapVector, FloorNormal, false, floorMaxAngle: MaxFloorAngle);
    }

    public override void APostProcess(float delta) {
        //Flip the sprite correctly
        if(autoPlayerFacing){
            if (InputManager.DirectionalInput.x > 0)
            {
                PlayerSprite.SetFlipH(false);
            }
            else if (InputManager.DirectionalInput.x < 0)
            {
                PlayerSprite.SetFlipH(true);
            }
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
        public const string SPIN_JUMP = "SpinJump";
        public const string FALL = "Fall";
        public const string CROUCH = "Crouch";
        public const string SLIDE = "Slide";
    }
}
