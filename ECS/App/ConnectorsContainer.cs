using ECS.App.NativeStructures;
using System;
using System.Runtime.InteropServices;

namespace ECS.App
{
    internal class ConnectorsContainer : IDisposable
    {
        private NativeDictionary<IntPtr, IntPtr> _containersConnections;

        public ConnectorsContainer()
        {
            _containersConnections = new NativeDictionary<IntPtr, IntPtr>(16);
        }

        public void AddComponent<T>(uint entityIndex, uint componentDataIndex) where T : unmanaged
        {
            var container = GetOrCreateContainer<T>();
            container[entityIndex] = componentDataIndex;
        }
        public bool TryGetComponent<T>(uint entityIndex, out uint componentDataIndex) where T : unmanaged
        {
            var container = GetOrCreateContainer<T>();
            return container.TryGetValue(entityIndex, out componentDataIndex);
        }

        private static IntPtr GetTypeHandle<T>() => typeof(T).TypeHandle.Value;

        private NativeDictionary<uint, uint> GetOrCreateContainer<T>() where T : unmanaged
        {
            var typeHandle = GetTypeHandle<T>();

            if (_containersConnections.TryGetValue(typeHandle, out var ptr))
            {
                var handleFinded = GCHandle.FromIntPtr(ptr);
                return (NativeDictionary<uint, uint>)handleFinded.Target;
            }

            var container = new NativeDictionary<uint, uint>(16);
            var handle = GCHandle.Alloc(container, GCHandleType.Normal);
            var containerPtr = GCHandle.ToIntPtr(handle);

            _containersConnections.Add(typeHandle, containerPtr);

            return container;
        }

        public void Dispose()
        {
            _containersConnections.Dispose();
        }
    }
}
