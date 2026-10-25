using ECS.App.Architecture.GoF.Behavioral.StatePattern;
using System;
using System.Collections.Generic;
using System.Text;

namespace WindowsHost.States
{
    internal class AppState : State<AppStateMachine>
    {
        private Type stateType;
        public override void OnEnter(AppStateMachine stateMachine)
        {
            base.OnEnter(stateMachine);
            stateType = GetType();
            Console.WriteLine("Enter state: " + stateType);
        }

        public override void OnUpdate(AppStateMachine stateMachine)
        {
            base.OnEnter(stateMachine);
            Console.WriteLine("Update state: " + stateType);
        }

        public override void OnExit(AppStateMachine stateMachine)
        {
            base.OnEnter(stateMachine);
            Console.WriteLine("Exit state: " + stateType);
        }
    }
}
