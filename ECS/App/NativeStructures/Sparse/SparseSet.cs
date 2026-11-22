using System;
using System.Runtime.CompilerServices;

namespace ECS.App.NativeStructures.Sparse
{
    public unsafe struct SparseSet : IDisposable
    {
        public NativeList<uint> Sparse;
        public NativeList<uint> Dense;
        public byte* Data;
        public long DataLength;
        public long DataCapacity;
        public int ElementSize;
        private const uint EMPTY_VALUE = uint.MaxValue;
        public SparseSet(int elementSize, long cap = 16)
        {
            ElementSize = elementSize;
            Sparse = new NativeList<uint>(cap, true, EMPTY_VALUE);
            Dense = new NativeList<uint>(cap, true, EMPTY_VALUE);
            Data = (byte*)NativeMemory.Allocate<byte>(cap * elementSize);
            DataLength = 0; DataCapacity = cap;
        }

        public void Add<T>(uint e, T v) where T : unmanaged
        {
            if (Sparse.Length <= e) { Sparse.EnsureCapacity(e + 1); Sparse.Length = e + 1; }
            uint slot = Sparse[e];
            if (slot != uint.MaxValue) { *(T*)(Data + (slot - 1) * ElementSize) = v; return; }
            if (DataLength >= DataCapacity) Grow();
            Sparse[e] = (uint)DataLength + 1;
            Dense.Add(e);
            *(T*)(Data + DataLength * ElementSize) = v;
            DataLength++;
        }

        private void Grow()
        {
            long nc = DataCapacity == 0 ? 16 : DataCapacity * 2;
            Data = (byte*)NativeMemory.Reallocate(Data, nc * ElementSize);
            DataCapacity = nc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetRef<T>(uint e) where T : unmanaged
            => ref *(T*)(Data + (Sparse[e] - 1) * ElementSize);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(uint e) => e < Sparse.Length && Sparse[e] != EMPTY_VALUE;

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
        }

        public bool TryGet<T>(uint e, out T value) where T : unmanaged
        {
            if (Has(e)) { value = GetRef<T>(e); return true; }
            value = default;
            return false;
        }

        public void Dispose()
        { 
            Sparse.Dispose();
            Dense.Dispose();
            NativeMemory.Free(Data);
            Data = null;
        }
        public void Clear()
        {
            for (long i = 0; i < Dense.Length; i++)
                Sparse[Dense[i]] = uint.MaxValue;
            Dense.Length = 0;
            DataLength = 0;
            Sparse.Length = 0;
        }
    }
}