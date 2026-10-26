using AbstractGoF.Behavioral.Stratagy;
using System;
using System.Collections.Generic;

namespace AbstractGoF.Behavioral.StatePattern
{
    public abstract class StratagyState<TMachine, TResult> : State<TMachine>
        where TMachine : StateMachine<TMachine>
    {
        protected abstract Dictionary<Type, object> strateges { get; }
        protected bool TryDoStratagy<TContext>(TResult result, TContext context)
        {
            if (strateges.TryGetValue(typeof(TContext), out var stratagy))
                if (stratagy is ITryStratagy<TResult, TContext> converted)
                    return converted.TryDoStratagy(out result, context);

            result = default;
            return false;
        }
    }
}
