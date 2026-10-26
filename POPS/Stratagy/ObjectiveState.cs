using AbstractGoF.Behavioral.StatePattern;

namespace POPS.Stratagy
{
    public class ObjectiveState<TObjective, TMachine> : State<TMachine>
        where TMachine : StateMachine<TMachine>
        //TODO: where TObjective : IObjective
    {
    }
}
