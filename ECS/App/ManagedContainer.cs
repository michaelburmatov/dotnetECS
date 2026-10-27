using ECS.App.NativeStructures;
using System;
using System.Runtime.CompilerServices;

namespace ECS.App
{
    internal sealed class ManagedContainer<T> : IDisposable where T : unmanaged
    {
        private NativeList<ComponentDataBucket<T>> _data;

        public ManagedContainer(uint initialCapacity = 16)
        {
            _data = new NativeList<ComponentDataBucket<T>>((int)initialCapacity);
        }

        public uint Count => (uint)_data.Length;

        public void Dispose()
        {
            _data.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetData(out T data, uint index)
        {
            if (index >= _data.Length)
            {
                data = default;
                return false;
            }

            ref var activator = ref _data[(int)index];
            if (activator.isActive)
            {
                data = activator.componentData;
                return true;
            }

            data = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetEntityParentIndex(uint index)
        {
            if (index >= _data.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _data[(int)index].entityParentIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetDataRef(uint index)
        {
            if (index >= _data.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return ref _data[(int)index].componentData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetData(T data, uint index)
        {
            if (index >= _data.Length)
                return false;

            ref var activator = ref _data[(int)index];
            if (activator.isActive)
            {
                activator.componentData = data;
                return true;
            }

            return false;
        }

        public uint AddComponent(uint entityParentIndex, T data)
        {
            var activator = new ComponentDataBucket<T>
            {
                isActive = true,
                componentData = data,
                entityParentIndex = entityParentIndex
            };
            _data.Add(activator);
            return (uint)_data.Length - 1;
        }

        public ReadOnlySpan<ComponentDataBucket<T>> GetAllData()
        {
            return _data.AsSpan();
        }
    }
}
