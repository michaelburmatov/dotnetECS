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

                    //Console.WriteLine(data.ToString());

                    if (manager.HaveAdjacentComponentData<TestData, TestAdjacentData>(i))
                    {
                        ref var adjacent = ref manager.GetAdjacentComponentDataRef<TestData, TestAdjacentData>(i);
                        adjacent.FloatData += (1 + i) * 0.5f;

                        //Console.WriteLine(adjacent.ToString());
                    }
                }
            }

            //Console.WriteLine("update => " + count);
        }
    }
}
