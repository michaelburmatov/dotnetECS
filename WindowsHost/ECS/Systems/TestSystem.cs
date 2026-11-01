using ECS.App;
using ECS.App.ISystem;
using WindowsHost.ECS.DataComponent;

namespace WindowsHost.ECS.Systems
{
    internal struct TestSystem : ISystemUpdate, ISystemEnter
    {
        public void Enter(ref ManagerECS manager)
        {
            throw new NotImplementedException();
        }

        public void Update(ref ManagerECS manager)
        {
            uint count;
            if (manager.TryGetCount<TestData>(out count))
            {
                for (uint i = 0; i < count; i++)
                {
                    ref TestData data = ref manager.GetComponentDataRef<TestData>(i);
                    data.FloatData += (1+i) * 1.432f;
                    data.Integerdata += (int)((1+i) * 2);
                }
            }

            //Console.WriteLine("update => " + count);
        }
    }
}
