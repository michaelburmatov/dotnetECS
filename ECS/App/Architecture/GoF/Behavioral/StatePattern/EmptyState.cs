using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.App.Architecture.GoF.Behavioral.StatePattern
{
    internal class EmptyState<T> : State<T> where T : StateMachine<T>
    {
    }
}
