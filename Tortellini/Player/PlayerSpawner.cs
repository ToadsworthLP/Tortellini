using Godot;
using System.Collections.Generic;

public class PlayerSpawner : Spatial
{
    #region Static
            public static List<PlayerSpawner> PlayerSpawners = new List<PlayerSpawner>();
            private static Dictionary<string, SpriteFrames> FrameCache = new Dictionary<string, SpriteFrames>();
        	private static Dictionary<PlayerForm, PackedScene> SceneCache = new Dictionary<PlayerForm, PackedScene>();
    #endregion

    [Export(PropertyHint.Range, "1,4")]
    public int PlayerNumber = 1;
    [Export]
    public PlayerForm InitialForm;
    [Export]
    public string SpriteFolderName;
    [Export]
    public float FormChangeDelay = 1;
    public Player CurrentPlayer {get; private set;}
    public Vector3 CurrentPlayerPosition { get{
        return CurrentPlayer == null ? PreviousTransform.origin : CurrentPlayer.GlobalTransform.origin;
    }}

    public enum PlayerForm {SMALL, BIG};

    private InputManager PlayerInputManager;

    private PlayerForm CurrentForm;
    private PackedScene CurrentScene;
    private SpriteFrames CurrentFrames;

    private bool IsChangingForm;
    private float FormChangeDelayTimer;

    private Transform PreviousTransform;
    private Vector3 PreviousVelocity;
    private bool PreviousSpriteFlipped = false;

    private AnimatedSprite3D TransitionSprite;
    private SpriteFrames TransitionFrames;

    public override void _EnterTree() {
        PlayerSpawners.Add(this);

        PreviousTransform = GlobalTransform;
        PreviousVelocity = Vector3.Zero;

        TransitionFrames = new SpriteFrames();
        TransitionFrames.AddAnimation("Transition");
        TransitionFrames.SetAnimationSpeed("Transition", 10);

        TransitionSprite = new AnimatedSprite3D();
        TransitionSprite.Name = "TransitionSprite";
        TransitionSprite.Frames = TransitionFrames;
        TransitionSprite.CastShadow = GeometryInstance.ShadowCastingSetting.On;
        TransitionSprite.Transparent = true;
        TransitionSprite.AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass;

        CurrentForm = InitialForm;
        PlayerInputManager = new InputManager(PlayerNumber);

        ChangeForm(InitialForm, true);
    }

    public override void _ExitTree() {
        PlayerSpawners.Remove(this);
    }

    public void SetForm(PlayerForm form) {
        if(form != CurrentForm && !IsChangingForm) ChangeForm(form);
    }

    public override void _Process(float delta) {
        ForceChangeForm();

        if(IsChangingForm) FormChangeDelayTimer += delta;
        if(IsChangingForm && FormChangeDelayTimer >= FormChangeDelay) FinishFormChange(CurrentForm);
    }

    //TODO remove this, this is just to test form changes until proper power-ups are implemented
    private void ForceChangeForm(){
        if(Input.IsActionJustPressed("p" + PlayerNumber + "_formchange")) {
            switch (CurrentForm) {
                case PlayerForm.BIG:
                SetForm(PlayerForm.SMALL);
                break;

                case PlayerForm.SMALL:
                SetForm(PlayerForm.BIG);
                break;
            }
        }
    }

    private void ChangeForm(PlayerForm form, bool instant = false) {
        IsChangingForm = true;
        CurrentForm = form;

        GD.Print("Changing form to " + form.ToString());

        if(!SceneCache.ContainsKey(CurrentForm)) {
            SceneCache.Add(CurrentForm, GD.Load<PackedScene>(Constants.FilePath.PLAYER_FORM_SCENES + CurrentForm.ToString() + ".tscn"));
        }
        CurrentScene = SceneCache[CurrentForm];

        string frameCacheKey = SpriteFolderName + "/" + CurrentForm.ToString();
        if(!FrameCache.ContainsKey(frameCacheKey)) {
            FrameCache.Add(frameCacheKey, GD.Load<SpriteFrames>(Constants.FilePath.PLAYER_FRAMES + SpriteFolderName + "/" + CurrentForm.ToString() + ".tres"));
        }
        CurrentFrames = FrameCache[frameCacheKey];

        Player oldFormScene = GetChildCount() > 0 ? GetChildOrNull<Player>(0) : null;
        if(oldFormScene != null) oldFormScene.Name = "QueuedForDeletion";

        if (oldFormScene != null) {
            PreviousTransform = oldFormScene.GlobalTransform;
            PreviousVelocity = oldFormScene.Velocity;

            AnimatedSprite3D sprite = GetNode<AnimatedSprite3D>(new NodePath("QueuedForDeletion/PlayerSprite"));
            PreviousSpriteFlipped = sprite.FlipH;

            if(!instant){
                //Create blinking state transition
                TransitionFrames.Clear("Transition");

                TransitionFrames.AddFrame("Transition", sprite.Frames.GetFrame(Player.PlayerAnimation.IDLE, 0), 0);
                TransitionFrames.AddFrame("Transition", CurrentFrames.GetFrame(Player.PlayerAnimation.IDLE, 0), 1);

                TransitionSprite.GlobalTransform = sprite.GlobalTransform.Translated(-Transform.origin);
                TransitionSprite.FlipH = sprite.FlipH;
                TransitionSprite.PixelSize = sprite.PixelSize;
                TransitionSprite.Play("Transition");
            }

            oldFormScene.SetProcess(false);
            oldFormScene.SetPhysicsProcess(false);
            oldFormScene.QueueFree();
        }

        if(instant) {
            FinishFormChange(form);
            return;
        }

        AddChild(TransitionSprite);
    }

    private void FinishFormChange(PlayerForm form){
        if(TransitionSprite.IsInsideTree()) RemoveChild(TransitionSprite);

        string nodeName = "Player" + PlayerNumber;
        NodePath nodePath = new NodePath(nodeName);

        Node formScene = CurrentScene.Instance();
        formScene.Name = nodeName;
        AddChild(formScene);

        Player newPlayerScript = GetNode<Player>(nodePath);
        
        FormChangeDelayTimer = 0;
        IsChangingForm = false;

        string frameCacheKey = SpriteFolderName + "/" + CurrentForm.ToString();
        newPlayerScript.SetupPlayer(PlayerInputManager, PreviousTransform, PreviousVelocity, FrameCache[frameCacheKey], PreviousSpriteFlipped);
        CurrentPlayer = newPlayerScript;
    }
}
