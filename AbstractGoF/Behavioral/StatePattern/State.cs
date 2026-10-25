namespace AbstractGoF.Behavioral.StatePattern
{
    public abstract class State<T> where T : StateMachine<T>
    {
        public virtual void OnEnter(T stateMachine) { }
        public virtual void OnUpdate(T stateMachine) { }
        public virtual void OnExit(T stateMachine) { }
    }
}
