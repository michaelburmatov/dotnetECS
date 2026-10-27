using ECS.App.NativeStructures;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ECS.App
{
    public sealed class ManagerECS : IDisposable
    {
        private readonly NativeDictionary<IntPtr, IntPtr> _containers;
        private readonly ConnectorsContainer _connectorsContainer;

        public ManagerECS(uint initialCapacity = 16)
        {
            _containers = new NativeDictionary<IntPtr, IntPtr>((int)initialCapacity);
            _connectorsContainer = new ConnectorsContainer();
        }

        public void Dispose()
        {
            foreach (var kvp in _containers)
            {
                var ptr = kvp.Value;

                if (ptr != IntPtr.Zero)
                {
                    var handle = GCHandle.FromIntPtr(ptr);
                    handle.Free();
                }
            }

            _containers.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IntPtr GetTypeHandle<T>() => typeof(T).TypeHandle.Value;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetCount<T>(out uint count) where T : unmanaged
        {
            if (TryGetContainer<T>(out var container))
            {
                count = container.Count;
                return true;
            }
            count = 0;
            return false;
        }

        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponentData<T>(out T componentData, uint index) where T : unmanaged
        {
            if (TryGetContainer<T>(out var container))
            {
                return container.TryGetData(out componentData, index);
            }
            componentData = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponentDataRef<T>(uint index) where T : unmanaged
        {
            if (!TryGetContainer<T>(out var container))
                throw new InvalidOperationException($"Container for type {typeof(T).Name} not found");

            return ref container.GetDataRef(index);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetContainer<T>(out ManagedContainer<T> container) where T : unmanaged
        {
            var typeHandle = GetTypeHandle<T>();

            if (_containers.TryGetValue(typeHandle, out var ptr))
            {
                var handle = GCHandle.FromIntPtr(ptr);
                container = (ManagedContainer<T>)handle.Target;
                return true;
            }

            container = null;
            return false;
        }

        private ManagedContainer<T> GetOrCreateContainer<T>() where T : unmanaged
        {
            var typeHandle = GetTypeHandle<T>();

            if (_containers.TryGetValue(typeHandle, out var ptr))
            {
                var handleFinded = GCHandle.FromIntPtr(ptr);
                return (ManagedContainer<T>)handleFinded.Target;
            }

            var container = new ManagedContainer<T>();
            var handle = GCHandle.Alloc(container, GCHandleType.Normal);
            var containerPtr = GCHandle.ToIntPtr(handle);

            _containers.Add(typeHandle, containerPtr);

            return container;
        }
    }
}
