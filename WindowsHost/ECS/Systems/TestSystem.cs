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
            
        }
    }
}
