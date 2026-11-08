using System;
using System.Runtime.CompilerServices;

namespace ECS.App.NativeStructures.Sparse
{
    public unsafe struct SparseSet<T> where T : unmanaged
    {
        public SparseSetDictionary Sparse;
        public NativeList<uint> Dense;
        public NativeList<T> Data;

        public SparseSet(long initialCapacity = 16)
        {
            Sparse = new SparseSetDictionary(initialCapacity);
            Dense = new NativeList<uint>(initialCapacity);
            Data = new NativeList<T>(initialCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(uint entityId, T data)
        {
            if (Sparse.ContainsKey(entityId)) return;

            uint denseIndex = (uint)Data.Length;

            Sparse[entityId] = denseIndex + 1;

            Dense.Add(entityId);
            Data.Add(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(uint entityId)
        {
            if (!Sparse.TryGetValue(entityId, out uint denseIndexPlusOne)) return;

            uint denseIndex = denseIndexPlusOne - 1;
            uint lastDenseIndex = (uint)(Dense.Length - 1);

            if (denseIndex != lastDenseIndex)
            {
                uint lastEntityId = Dense[lastDenseIndex];

                Dense[denseIndex] = lastEntityId;
                Data[denseIndex] = Data[lastDenseIndex];

                Sparse[lastEntityId] = denseIndex + 1;
            }

            Sparse.Remove(entityId);

            Dense.Length--;
            Data.Length--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(uint entityId)
        {
            if (!Sparse.TryGetValue(entityId, out uint denseIndexPlusOne))
            {
                throw new InvalidOperationException($"Entity {entityId} doesn't have component");
            }
            return ref Data.GetRef(denseIndexPlusOne - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(uint entityId)
        {
            return Sparse.ContainsKey(entityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(uint entityId, out T data)
        {
            if (Sparse.TryGetValue(entityId, out uint denseIndexPlusOne))
            {
                data = Data[denseIndexPlusOne - 1];
                return true;
            }
            data = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetRef(uint entityId, out T* dataPtr)
        {
            if (Sparse.TryGetValue(entityId, out uint denseIndexPlusOne))
            {
                dataPtr = Data.Buffer + (denseIndexPlusOne - 1);
                return true;
            }
            dataPtr = null;
            return false;
        }

        public void Dispose()
        {
            Sparse.Dispose();
            Dense.Dispose();
            Data.Dispose();
        }
    }
}
