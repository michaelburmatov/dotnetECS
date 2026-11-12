using ECS.App.NativeStructures;
using ECS.App.NativeStructures.Sparse;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ECS.App
{
    public unsafe struct ManagerECS : IDisposable
    {
        private NativeDictionary<IntPtr, SparseSetHandle> _containers;

        public ManagerECS(uint initialCapacity = 16)
        {
            _containers = new NativeDictionary<IntPtr, SparseSetHandle>((int)initialCapacity);
        }

        internal struct SparseSetHandle
        {
            public IntPtr Ptr;
        }

        public void Dispose()
        {
            foreach (var kvp in _containers)
            {
                var handle = kvp.Value;
                if (handle.Ptr != IntPtr.Zero)
                {
                    var header = (SparseSetHeader*)handle.Ptr;
                    header->Dispose();
                    Marshal.FreeHGlobal(handle.Ptr);
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
                count = (uint)container->Data.Length;
                return true;
            }
            count = 0;
            return false;
        }

        //TODO 
        // Переделать
        // Закешировать все возможные DataFilter
        // Выдавать их и изменять при изменении используемых данных
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetFilter<T1, T2>(out DataFilter<T1, T2> filter)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            if (TryGetContainer<T1>(out var container1))
            {
                if (TryGetContainer<T2>(out var container2))
                {
                    filter = new DataFilter<T1, T2>(container1, container2);
                    return true;
                }
            }

            filter = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponentData<T>(out T componentData, uint denseIndex) where T : unmanaged
        {
            if (TryGetContainer<T>(out var container))
            {
                if (denseIndex < container->Data.Length)
                {
                    componentData = container->Data[(int)denseIndex];
                    return true;
                }
            }
            componentData = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HaveAdjacentComponentData<TOriginal, TAdjacent>(uint denseIndexOriginal)
            where TOriginal : unmanaged
            where TAdjacent : unmanaged
        {
            if (!TryGetContainer<TOriginal>(out var containerOriginal))
                throw new InvalidOperationException($"Container for type {typeof(TOriginal).Name} not found");
            if (!TryGetContainer<TAdjacent>(out var containerAdjacent))
                throw new InvalidOperationException($"Container for type {typeof(TAdjacent).Name} not found");

            if (denseIndexOriginal >= containerOriginal->Data.Length)
                throw new ArgumentOutOfRangeException(nameof(denseIndexOriginal));

            uint entityId = containerOriginal->Dense[denseIndexOriginal];
            return containerAdjacent->Has(entityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TAdjacent GetAdjacentComponentDataRef<TOriginal, TAdjacent>(uint denseIndexOriginal)
            where TOriginal : unmanaged
            where TAdjacent : unmanaged
        {
            if (!TryGetContainer<TOriginal>(out var containerOriginal))
                throw new InvalidOperationException($"Container for type {typeof(TOriginal).Name} not found");
            if (!TryGetContainer<TAdjacent>(out var containerAdjacent))
                throw new InvalidOperationException($"Container for type {typeof(TAdjacent).Name} not found");

            if (denseIndexOriginal >= containerOriginal->Data.Length)
                throw new ArgumentOutOfRangeException(nameof(denseIndexOriginal));

            uint entityId = containerOriginal->Dense[denseIndexOriginal];
            if (containerAdjacent->TryGetRef(entityId, out TAdjacent* dataPtr))
                return ref *dataPtr;

            throw new InvalidOperationException($"{typeof(TOriginal).Name} dont have {typeof(TAdjacent).Name} as adjacent");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponentDataRef<T>(uint denseIndex) where T : unmanaged
        {
            if (!TryGetContainer<T>(out var container))
                throw new InvalidOperationException($"Container for type {typeof(T).Name} not found");

            if (denseIndex >= container->Data.Length)
                throw new ArgumentOutOfRangeException(nameof(denseIndex));

            return ref container->Data.GetRef((int)denseIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(Entity entity, T componentData) where T : unmanaged
        {
            var container = GetOrCreateContainer<T>();
            container->Add(entity.id, componentData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetContainer<T>(out SparseSet<T>* container) where T : unmanaged
        {
            var typeHandle = GetTypeHandle<T>();

            if (_containers.TryGetValue(typeHandle, out var handle))
            {
                container = (SparseSet<T>*)handle.Ptr;
                return true;
            }

            container = null;
            return false;
        }

        private SparseSet<T>* GetOrCreateContainer<T>() where T : unmanaged
        {
            var typeHandle = GetTypeHandle<T>();

            if (_containers.TryGetValue(typeHandle, out var handle))
            {
                return (SparseSet<T>*)handle.Ptr;
            }


            var ptr = NativeMemory.Allocate<SparseSet<T>>(1);
            *ptr = new SparseSet<T>(16);

            SparseSetHandle newHandle = new SparseSetHandle
            {
                Ptr = (IntPtr)ptr
            };

            _containers.Add(typeHandle, newHandle);

            return ptr;
        }

        private unsafe struct SparseSetHeader
        {
            public SparseSetDictionaryHeader Sparse;
            public NativeListHeader Dense;
            public NativeListHeader Data;

            public void Dispose()
            {
                Sparse.Dispose();
                Dense.Dispose();
                Data.Dispose();
            }
        }

        private unsafe struct SparseSetDictionaryHeader
        {
            public NativeListHeader Keys;
            public NativeListHeader Values;
            public long Capacity;
            public long Count;

            public void Dispose()
            {
                Keys.Dispose();
                Values.Dispose();
            }
        }

        private unsafe struct NativeListHeader
        {
            public void* Buffer;
            public long Length;
            public long Capacity;

            public void Dispose()
            {
                if (Buffer != null)
                {
                    Marshal.FreeHGlobal((IntPtr)Buffer);
                    Buffer = null;
                }
                Length = 0;
                Capacity = 0;
            }
        }
    }
}