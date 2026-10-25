namespace WindowsHost.States
{
    internal class InitState : AppState
    {
        public override void OnEnter(AppStateMachine stateMachine)
        {
            base.OnEnter(stateMachine);
        }

        public override void OnUpdate(AppStateMachine stateMachine)
        {
            base.OnUpdate(stateMachine);
            stateMachine.SwitchState<StateECS>();
        }
    }
}
