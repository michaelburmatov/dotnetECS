using System;
using System.Collections.Generic;
using System.Text;

namespace WindowsHost.States
{
    internal class StateECS : AppState
    {
        public override void OnUpdate(AppStateMachine stateMachine)
        {
            base.OnUpdate(stateMachine);
            stateMachine.Stop();
        }
    }
}
