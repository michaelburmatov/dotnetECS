using System;
using System.Runtime.CompilerServices;

namespace ECS.App.NativeStructures.Sparse
{
    public unsafe struct SparseSet<T> where T : unmanaged
    {
        public NativeList<uint> Sparse;
        public NativeList<uint> Dense;
        public NativeList<T> Data;

        private const uint EMPTY_VALUE = uint.MaxValue;

        public SparseSet(long initialCapacity = 16)
        {
            Sparse = new NativeList<uint>(initialCapacity, true, EMPTY_VALUE);
            Dense = new NativeList<uint>(initialCapacity, true, EMPTY_VALUE);
            Data = new NativeList<T>(initialCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(uint entityId, T data)
        {
            if (Sparse.Length <= entityId)
                Sparse.Resize(entityId + 1);

            var existingValue = Sparse[entityId];
            if (existingValue != EMPTY_VALUE)
            {
                Data[existingValue - 1] = data;
                return;
            }

            uint denseIndex = (uint)Data.Length;

            Sparse[entityId] = denseIndex + 1;

            Dense.Add(entityId);
            Data.Add(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(uint entityId)
        {
            if (entityId >= Sparse.Length)
                return;

            var denseIndexPlusOne = Sparse[entityId];

            if (denseIndexPlusOne == EMPTY_VALUE)
                return;

            uint denseIndex = denseIndexPlusOne - 1;
            uint lastDenseIndex = (uint)(Dense.Length - 1);

            if (denseIndex != lastDenseIndex)
            {
                uint lastEntityId = Dense[lastDenseIndex];

                Dense[denseIndex] = lastEntityId;
                Data[denseIndex] = Data[lastDenseIndex];

                Sparse[lastEntityId] = denseIndex + 1;
            }

            Sparse[entityId] = EMPTY_VALUE;

            Dense.Length--;
            Data.Length--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(uint entityId)
        {
#if DEBUG
            if (entityId >= Sparse.Length)
                throw new InvalidOperationException($"Entity {entityId} doesn't have component");
#endif
            var denseIndexPlusOne = Sparse[entityId];
#if DEBUG
            if (denseIndexPlusOne == EMPTY_VALUE)
                throw new InvalidOperationException($"Entity {entityId} doesn't have component");
#endif
            return ref Data.GetRef(denseIndexPlusOne - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(uint entityId)
        {
            return entityId < Sparse.Length && Sparse[entityId] != EMPTY_VALUE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(uint entityId, out T data)
        {
            if (Has(entityId))
            {
                data = Data[Sparse[entityId] - 1];
                return true;
            }
            data = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetRef(uint entityId, out T* dataPtr)
        {
            if (Has(entityId))
            {
                var denseIndexPlusOne = Sparse[entityId];
                dataPtr = Data.Buffer + (denseIndexPlusOne - 1);
                return true;
            }
            dataPtr = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetDataRef(out T* data, out long count)
        {
            if (Data.Buffer == null || Data.Length == 0)
            {
                data = null;
                count = 0;
                return false;
            }

            data = Data.Buffer;
            count = Data.Length;
            return true;
        }

        public void Dispose()
        {
            Sparse.Dispose();
            Dense.Dispose();
            Data.Dispose();
        }
    }
}