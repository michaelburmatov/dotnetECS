namespace ECS.App.ISystem
{
    public interface ISystemUpdate
    {
        void Update(ref ManagerECS manager);
    }
}
