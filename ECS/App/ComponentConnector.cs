namespace ECS.App
{
    internal struct ComponentConnector<T> where T : unmanaged
    {
        public uint entityID;
        public uint componentDataID;
    }
}
