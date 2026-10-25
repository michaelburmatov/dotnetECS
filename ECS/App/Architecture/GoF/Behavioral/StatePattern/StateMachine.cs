using System;
using System.Collections.Generic;

namespace ECS.App.Architecture.GoF.Behavioral.StatePattern
{
    public abstract class StateMachine<T> where T : StateMachine<T>
    {
        private readonly Dictionary<Type, State<T>> _states = new Dictionary<Type, State<T>>();
        private State<T>? _activeState;
        private readonly T _context;

        public StateMachine(State<T> initialState, params State<T>[] additionalStates)
        {
            _context = (T)this;

            TryAddState(initialState);
            foreach (var state in additionalStates)
            {
                TryAddState(state);
            }

            _activeState = initialState;
            _activeState?.OnEnter(_context);
            IsRunning = true;
        }

        public bool IsRunning { get; private set; }

        public void SwitchState<TState>() where TState : State<T>
        {
            var stateType = typeof(TState);
            if (_states.TryGetValue(stateType, out var state))
            {
                if (state == _activeState) return;

                _activeState?.OnExit(_context);
                _activeState = state;
                _activeState?.OnEnter(_context);
            }
            else
            {
                throw new InvalidOperationException($"State {stateType.Name} is not registered in the StateMachine.");
            }
        }
        
        public void Update()
        {
            _activeState?.OnUpdate(_context);
        }

        public void Stop()
        {
            _activeState?.OnExit(_context);
            _activeState = null;
            IsRunning = false;
        }

        protected bool TryAddState(State<T> state)
        {
            return _states.TryAdd(state.GetType(), state);
        }
    }
}