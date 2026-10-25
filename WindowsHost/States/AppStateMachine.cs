using ECS.App.Architecture.GoF.Behavioral.StatePattern;

namespace WindowsHost.States
{
    internal class AppStateMachine : StateMachine<AppStateMachine>
    {
        public AppStateMachine() : base(new InitState())
        {
            TryAddState(new StateECS());
        }

    }
}
