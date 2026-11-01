using ECS.App;
using WindowsHost.ECS.DataComponent;
using WindowsHost.ECS.Systems;

namespace WindowsHost.States
{
    internal class StateECS : AppState
    {
        private World mainWorld = new World();
        public override void OnEnter(AppStateMachine stateMachine)
        {
            base.OnEnter(stateMachine);
            mainWorld.TryAddSystem<TestSystem>();
            //var entity = new Entity();
            var entity = mainWorld.CreateEntity(new TestData()
            {
                FloatData = 1.0f,
                Integerdata = 2
            });
            mainWorld.AddComponent(entity, new TestAdjacentData()
            {
                FloatData = 5f
            });

            mainWorld.CreateEntity(new TestData()
            {
                FloatData = 4.0f,
                Integerdata = 3
            });

        }

        public override void OnUpdate(AppStateMachine stateMachine)
        {
            base.OnUpdate(stateMachine);
            mainWorld.Update();
            //stateMachine.Stop();
        }
    }
}
