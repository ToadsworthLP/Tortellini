using Godot;
using System.Collections.Generic;

public class PlayerSpawner : Spatial
{
    #region Static
            private static Dictionary<string, SpriteFrames> FrameCache = new Dictionary<string, SpriteFrames>();
        	private static Dictionary<PlayerForm, PackedScene> SceneCache = new Dictionary<PlayerForm, PackedScene>();
    #endregion

    [Export(PropertyHint.Range, "1,4")]
    public int PlayerNumber = 1;
    [Export]
    public PlayerForm InitialForm;
    [Export]
    public string SpriteFolderName;

    public enum PlayerForm {SMALL, BIG};

    private InputManager CurrentInputManager;

    private PlayerForm CurrentForm;
    private PackedScene CurrentScene;
    private SpriteFrames CurrentFrames;
    private Player CurrentPlayerScript;
    private KinematicBody CurrentKinematicBody;
    private AnimatedSprite3D CurrentSprite;

    private KinematicBody PlayerBody;
    private Player PlayerScript;

    public override void _Ready() {
        CurrentForm = InitialForm;
        CurrentInputManager = new InputManager(PlayerNumber);

        LoadForm(InitialForm);
    }

    public void SetForm(PlayerForm form) {
        if(form != CurrentForm) LoadForm(form);
    }

    //TODO remove this, this is just to test form changes until proper power-ups are implemented
    public override void _Process(float delta) {
        if(Input.IsActionJustPressed("p1_formchange")) {
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

    private void LoadForm(PlayerForm form) {
        GD.Print("Changing form to " + form.ToString());
        CurrentForm = form;

        if(!SceneCache.ContainsKey(CurrentForm)) {
            SceneCache.Add(CurrentForm, GD.Load<PackedScene>(Constants.FilePath.PLAYER_FORM_SCENES + CurrentForm.ToString() + ".tscn"));
        }
        CurrentScene = SceneCache[CurrentForm];

        string frameCacheKey = SpriteFolderName + "/" + CurrentForm.ToString();
        if(!FrameCache.ContainsKey(frameCacheKey)) {
            FrameCache.Add(frameCacheKey, GD.Load<SpriteFrames>(Constants.FilePath.PLAYER_FRAMES + SpriteFolderName + "/" + CurrentForm.ToString() + ".tres"));
        }
        CurrentFrames = FrameCache[frameCacheKey];

        string nodeName = "Player" + PlayerNumber;
        NodePath nodePath = new NodePath(nodeName);

        Node oldFormScene = GetChildCount() > 0 ? GetChildOrNull<Player>(0) : null;
        oldFormScene?.SetName("QueuedForDeletion");

        Node formScene = CurrentScene.Instance();
        formScene.SetName(nodeName);
        AddChild(formScene);

        Player newPlayerScript = GetNode<Player>(nodePath);
        KinematicBody newFormBody = GetNode<KinematicBody>(nodePath);

        if (CurrentKinematicBody == null || CurrentPlayerScript == null) {
            newFormBody.SetTransform(Transform);
        } else {
            newFormBody.SetTransform(CurrentKinematicBody.GetTransform());
            newPlayerScript.Velocity = CurrentPlayerScript.Velocity;
            //newPlayerScript.ChangeState(CurrentPlayerScript.CurrentState);
        }

        newPlayerScript.InputManager = CurrentInputManager;

        AnimatedSprite3D newSprite = GetNode<AnimatedSprite3D>(new NodePath(nodeName + "/PlayerSprite"));
        newSprite.Frames = FrameCache[frameCacheKey];
        if(CurrentSprite != null) newSprite.SetFlipH(CurrentSprite.IsFlippedH());

        CurrentKinematicBody = newFormBody;
        CurrentPlayerScript = newPlayerScript;
        CurrentSprite = newSprite;
        
        if(oldFormScene != null) {
            oldFormScene.SetProcess(false);
            oldFormScene.SetPhysicsProcess(false);
            oldFormScene.QueueFree();
        }
    }
}
