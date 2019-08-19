using Godot;
using System;

public class InputManager
{
    public Vector2 DirectionalInput {get; private set;}

    public bool JumpPressed {get; private set;}
    public bool JumpJustPressed {get; private set;}

    public bool AltJumpPressed {get; private set;}
    public bool AltJumpJustPressed {get; private set;}

    public bool RunPressed {get; private set;}
    public bool RunJustPressed {get; private set;}

    public bool AltRunPressed {get; private set;}
    public bool AltRunJustPressed {get; private set;}


    public readonly string UP_AXIS, DOWN_AXIS, LEFT_AXIS, RIGHT_AXIS, JUMP_AXIS, ALTJUMP_AXIS, RUN_AXIS, ALTRUN_AXIS;

    private const string UP_AXIS_SUFFIX = "_up";
    private const string DOWN_AXIS_SUFFIX = "_down";
    private const string LEFT_AXIS_SUFFIX = "_left";
    private const string RIGHT_AXIS_SUFFIX = "_right";
    private const string JUMP_AXIS_SUFFIX = "_jump";
    private const string ALTJUMP_AXIS_SUFFIX = "_altjump"; 
    private const string RUN_AXIS_SUFFIX = "_run"; 
    private const string ALTRUN_AXIS_SUFFIX = "_altrun"; 


    /// <summary>
    /// Sets up this input manager using the given axis names, which should correspond to valid input actions set in Godot's input map
    /// </summary>
    public InputManager(string upAxis, string downAxis, string leftAxis, string rightAxis, string jump, string altjump, string run, string altrun) {
        UP_AXIS = upAxis;
        DOWN_AXIS = downAxis;
        LEFT_AXIS = leftAxis;
        RIGHT_AXIS = rightAxis;
        JUMP_AXIS = jump;
        ALTJUMP_AXIS = altjump;
        RUN_AXIS = run;
        ALTRUN_AXIS = altrun;
    }

    /// <summary>
    /// Sets up this input manager using just a player number and generates the action names, for example: p1_jump where 1 is the player number.
    /// Make sure that the generated names have corresponding input actions assigned in Godot's input map!
    /// </summary>
    /// <param name="playerNum"></param>
    public InputManager(int playerNum) {
        UP_AXIS = "p" + playerNum + UP_AXIS_SUFFIX;
        DOWN_AXIS = "p" + playerNum + DOWN_AXIS_SUFFIX;
        LEFT_AXIS = "p" + playerNum + LEFT_AXIS_SUFFIX;
        RIGHT_AXIS = "p" + playerNum + RIGHT_AXIS_SUFFIX;
        JUMP_AXIS = "p" + playerNum + JUMP_AXIS_SUFFIX;
        ALTJUMP_AXIS = "p" + playerNum + ALTJUMP_AXIS_SUFFIX;
        RUN_AXIS = "p" + playerNum + RUN_AXIS_SUFFIX;
        ALTRUN_AXIS = "p" + playerNum + ALTRUN_AXIS_SUFFIX;

    }

    public void UpdateInputs(){
        DirectionalInput = new Vector2(
            Input.GetActionStrength(RIGHT_AXIS) - Input.GetActionStrength(LEFT_AXIS),
            Input.GetActionStrength(UP_AXIS) - Input.GetActionStrength(DOWN_AXIS)
        );

        JumpPressed = Input.IsActionPressed(JUMP_AXIS);
        JumpJustPressed = Input.IsActionJustPressed(JUMP_AXIS);

        AltJumpPressed = Input.IsActionPressed(ALTJUMP_AXIS);
        AltJumpJustPressed = Input.IsActionJustPressed(ALTJUMP_AXIS);
        
        RunPressed = Input.IsActionPressed(RUN_AXIS);
        RunJustPressed = Input.IsActionJustPressed(RUN_AXIS);

        AltRunPressed = Input.IsActionPressed(ALTRUN_AXIS);
        AltRunJustPressed = Input.IsActionJustPressed(ALTRUN_AXIS);
    }
}
