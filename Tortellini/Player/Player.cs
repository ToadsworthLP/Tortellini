using Godot;

public class Player : PhysicsActor
{
    //Input manager for this player instance
    public InputManager InputManager;

    //Movement parameters
    [Export]
    public float WalkAcceleration = 2;
    [Export]
    public float WalkSpeed = 7.4f;
    [Export]
    public float RunAcceleration = 2;
    [Export]
    public float RunSpeed = 12.8f;
    [Export]
    public float LongRunAcceleration = 2;
    [Export]
    public float LongRunSpeed = 18.2f;
    [Export]
    public float LongRunTime = 1;
    [Export]
    public float IdleJumpForce = 25;
    [Export]
    public float WalkJumpForce = 27.6f;
    [Export]
    public float LongRunJumpForce = 30;
    [Export]
    public float MaxJumpSustainTime = 0.45f;
    [Export]
    public float JumpSustainGravityMultiplier = 0.55f;
    [Export]
    public float AirHorizontalAcceleration = 1.5f;
    [Export(PropertyHint.Range, "-1,1")]
    public float CrouchInputThreshold = -0.5f;
    [Export(PropertyHint.Range, "0, 180")]
    public float SlideMinAngle = 5;
    [Export]
    public float SlideAcceleration = 1.4f;
    [Export]
    public float SlideSpeed = 12.8f;
    [Export]
    public Vector2 CrouchBoostForce = new Vector2(6,6);

    [Export]
    public float FloorFriction = 1;
    [Export]
    public float AirFriction = 0.4f;
    [Export]
    public float SlideFriction = 0.4f;


    //Node references
    public AnimatedSprite3D PlayerSprite;
    public Area ActorDetectorArea;
    public RayCast FloorRayCast;

    //Player states
    public ActorState StandState, WalkState, RunState, LongRunState, JumpState, SpinJumpState, FallState, CrouchState, SlideState;

    //Speed limit handling
    private float SpeedLimit;
    private float SpeedLerpDuration = 0.5f;
    private float SpeedLerpStartTime;
    private float SpeedLerpStartVelocity;

    //Controls whether player facing is set automatically based on player input
    private bool autoPlayerFacing = true;

    //Controls which PlayerCollisionShape is the default for the current form
    protected virtual PlayerCollisionShape GetDefaultCollisionShape() {
        return PlayerCollisionShape.BIG;
    }

    public override ActorState GetDefaultState() {return StandState;}
    
    public override void AEnterTree() {
        PlayerSprite = GetNodeOrNull<AnimatedSprite3D>(new NodePath("PlayerSprite"));
        ActorDetectorArea = GetNodeOrNull<Area>(new NodePath("ActorDetector"));
        FloorRayCast = GetNodeOrNull<RayCast>(new NodePath("FloorRayCast"));

        if (PlayerSprite == null || ActorDetectorArea == null || FloorRayCast == null) GD.Print("One or multiple required child nodes could not be found! Some features won't work!");

        SetPlayerCollisionShape(GetDefaultCollisionShape());

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
                SetPlayerCollisionShape(PlayerCollisionShape.SMALL);
            } else {
                ApplyForce2D(new Vector2(0, IdleJumpForce));
                PlayerSprite.SetAnimation(PlayerAnimation.JUMP);
            }
        }, (float delta) =>
        { //Process State
            //DebugText.Display("P" + PlayerNumber + "_JumpSusTime", "P" + PlayerNumber + " Jump Sustain Time: " + (MaxJumpSustainTime - GetElapsedTimeInState()).ToString());
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
            //DebugText.Remove("P" + PlayerNumber + "_JumpSusTime");
            SetPlayerCollisionShape(GetDefaultCollisionShape());
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
            //DebugText.Display("P" + PlayerNumber + "_JumpSusTime", "P" + PlayerNumber + " Jump Sustain Time: " + (MaxJumpSustainTime - GetElapsedTimeInState()).ToString());
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
            //DebugText.Remove("P" + PlayerNumber + "_JumpSusTime");
        });

        FallState = new ActorState(() =>
        { //Enter State
            if(InputManager.DirectionalInput.y < CrouchInputThreshold) {
                PlayerSprite.SetAnimation(PlayerAnimation.CROUCH);
                SetPlayerCollisionShape(PlayerCollisionShape.SMALL);
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
            SetPlayerCollisionShape(GetDefaultCollisionShape());
        });

        CrouchState = new ActorState(() =>
        { //Enter State
            SnapToGround = true;

            var angle = Mathf.Rad2Deg(Mathf.Acos(FloorRayCast.GetCollisionNormal().Dot(FloorNormal)));
            if(angle >= SlideMinAngle){
                ChangeState(SlideState);
                return;
            }

            PlayerSprite.SetAnimation(PlayerAnimation.CROUCH);
            SetPlayerCollisionShape(PlayerCollisionShape.SMALL);
        }, (float delta) =>
        { //Process State

        }, (float delta) =>
        { //State Physics Processing
            //Check if we're on a slope. If yes, start sliding.
            var angle = Mathf.Rad2Deg(Mathf.Acos(FloorRayCast.GetCollisionNormal().Dot(FloorNormal)));
            if(angle >= SlideMinAngle){
                ChangeState(SlideState);
                return;
            }

            Transform slightlyRight;
            slightlyRight.origin = GlobalTransform.origin;
            slightlyRight.basis = GlobalTransform.basis;
            slightlyRight.origin.x += 0.4f;

            Transform slightlyLeft;
            slightlyLeft.origin = GlobalTransform.origin;
            slightlyLeft.basis = GlobalTransform.basis;
            slightlyLeft.origin.x -= 0.4f;

            if(GetDefaultCollisionShape() == PlayerCollisionShape.SMALL || !TestMove(GlobalTransform, new Vector3(0, 0.5f, 0))){
                if(InputManager.DirectionalInput.y >= CrouchInputThreshold) ChangeState(StandState);
                CanJump();
                CanFall();
            } else if(!TestMove(slightlyRight, new Vector3(0, 0.5f, 0))) {
                ApplyForce2D(new Vector2(0.4f, 0));
            } else if(!TestMove(slightlyLeft, new Vector3(0, 0.5f, 0))) {
                ApplyForce2D(new Vector2(-0.4f, 0));
            } else {
                if(InputManager.JumpJustPressed || InputManager.AltJumpJustPressed) { ApplyForce2D(new Vector2(CrouchBoostForce.x * InputManager.DirectionalInput.x, CrouchBoostForce.y)); }
            }
            
        }, () =>
        { //Exit State
            SetPlayerCollisionShape(GetDefaultCollisionShape());
        });

        SlideState = new ActorState(() =>
        { //Enter State
            SetSpeedLimit(SlideSpeed);
            SnapToGround = true;

            PlayerSprite.SetAnimation(PlayerAnimation.SLIDE);
            SetPlayerCollisionShape(PlayerCollisionShape.SMALL);
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
            Vector3 normal = FloorRayCast.GetCollisionNormal();
            float angle = Mathf.Rad2Deg(Mathf.Acos(normal.Dot(FloorNormal)));
            if(angle < SlideMinAngle) ChangeState(InputManager.DirectionalInput.y < CrouchInputThreshold ? CrouchState : StandState );
            CanFall();
            CanJump();

            float force = Mathf.Sign(normal.x) * SlideAcceleration;
            ApplyForce2D(new Vector2(force, 0));
        }, () =>
        { //Exit State
            autoPlayerFacing = true;
            SetPlayerCollisionShape(GetDefaultCollisionShape());
        });
    }

    protected void CanJump(){
        if(InputManager.JumpJustPressed) {
            ChangeState(JumpState);
        } else if (InputManager.AltJumpJustPressed) {
            ChangeState(SpinJumpState);
        }
    }

    protected void CanFall() {
        if(!IsOnFloor()) ChangeState(FallState);
    }

    protected void CanCrouch() {
        if(IsOnFloor() && InputManager.DirectionalInput.y < CrouchInputThreshold) ChangeState(CrouchState);
    }

    public override void APhysicsPreProcess(float delta){
        InputManager.UpdateInputs();
    }

    public override void APhysicsPostProcess(float delta){
        //Apply friction
        if(CurrentState == CrouchState || CurrentState == SlideState){
            Velocity.x = Mathf.Max(Mathf.Abs(Velocity.x) - SlideFriction, 0) * Mathf.Sign(Velocity.x);
        } else {
            Velocity.x = Mathf.Max((Mathf.Abs(Velocity.x) - (IsOnFloor() ? FloorFriction : AirFriction)), 0) * Mathf.Sign(Velocity.x);
        }

        //Lerp to speed limit if above
        if(Mathf.Abs(Velocity.x) > SpeedLimit) { Velocity.x = ClampedInterpolation.Lerp(SpeedLerpStartVelocity, SpeedLimit, (Lifetime - SpeedLerpStartTime) * (1/SpeedLerpDuration)) * Mathf.Sign(Velocity.x); }

        //DebugText.Display("P" + PlayerNumber + "_SpeedLimit", "P" + PlayerNumber + " Speed Limit: " + SpeedLimit.ToString());
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
        
        //DebugText.Display("P1_Position", "P1 Position: " + GlobalTransform.origin.ToString());
        //DebugText.Display("P" + PlayerNumber + "_Velocity", "P" + PlayerNumber + " Velocity: " + Velocity.ToString());
        //DebugText.Display("P" + PlayerNumber + "_StateTime", "P" + PlayerNumber + " Time in State: " + GetElapsedTimeInState().ToString());
    }

    protected void SetSpeedLimit(float limit){
        if(limit == SpeedLimit) return;

        SpeedLerpStartTime = Lifetime;
        SpeedLerpStartVelocity = Mathf.Abs(Velocity.x);
        SpeedLimit = limit;
    }

    protected enum PlayerCollisionShape {SMALL, BIG}
    protected void SetPlayerCollisionShape(PlayerCollisionShape shape) {
        switch (shape) {
            case PlayerCollisionShape.SMALL:
            ShapeOwnerSetDisabled(0, false);
            ShapeOwnerSetDisabled(1, true);
            break;

            case PlayerCollisionShape.BIG:
            ShapeOwnerSetDisabled(0, true);
            ShapeOwnerSetDisabled(1, false);
            break;
        }
    }

    public struct PlayerAnimation {
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
