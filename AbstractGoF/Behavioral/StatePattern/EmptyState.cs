using System;
using System.Collections.Generic;
using System.Text;

namespace AbstractGoF.Behavioral.StatePattern
{
    internal class EmptyState<T> : State<T> where T : StateMachine<T>
    {
    }
}
