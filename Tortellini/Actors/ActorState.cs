using System;

public class ActorState {

    private readonly Action EnterState;
    private readonly Action<float> ProcessState;
    private readonly Action<float> PhysicsProcessState;
    private readonly Action ExitState;

    public ActorState(Action OnEnterState, Action<float> OnProcessState, Action<float> OnPhysicsProcessState, Action OnExitState){
        this.EnterState = OnEnterState;
        this.ProcessState = OnProcessState;
        this.PhysicsProcessState = OnPhysicsProcessState;
        this.ExitState = OnExitState;
    }

    public void OnEnterState() => EnterState();
    public void OnProcessState(float delta) => ProcessState(delta);
    public void OnPhysicsProcessState(float delta) => PhysicsProcessState(delta);
    public void OnExitState() => ExitState();
}