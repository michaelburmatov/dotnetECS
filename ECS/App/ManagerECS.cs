using ECS.App.NativeStructures;
using ECS.App.NativeStructures.Sparse;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ECS.App
{
    public unsafe struct ManagerECS : IDisposable
    {
        private NativeDictionary<IntPtr, IntPtr> _containers;
        private NativeDictionary<FilterKey, IntPtr> _filters;

        public ManagerECS(uint initialCapacity = 16)
        {
            _containers = new NativeDictionary<IntPtr, IntPtr>((int)initialCapacity);
            _filters = new NativeDictionary<FilterKey, IntPtr>((int)initialCapacity);
        }

        private readonly struct FilterKey : IEquatable<FilterKey>
        {
            public readonly IntPtr First;
            public readonly IntPtr Second;

            public FilterKey(IntPtr first, IntPtr second)
            {
                First = first;
                Second = second;
            }

            public bool Equals(FilterKey other) => First == other.First && Second == other.Second;
            public override bool Equals(object obj) => obj is FilterKey k && Equals(k);
            public override int GetHashCode() => unchecked(First.GetHashCode() * 397 ^ Second.GetHashCode());
        }

        public void Dispose()
        {
            for (int s = 0; s < _filters.Capacity; s++)
                if (_filters.TryGetRawEntry(s, out _, out var p))
                { var f = (FilterState*)p; f->Dispose(); NativeMemory.Free(f); }
            _filters.Dispose();

            for (int s = 0; s < _containers.Capacity; s++)
                if (_containers.TryGetRawEntry(s, out _, out var p))
                { var c = (SparseSet*)p; c->Dispose(); NativeMemory.Free(c); }
            _containers.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IntPtr GetTypeHandle<T>() => typeof(T).TypeHandle.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetCount<T>(out uint count) where T : unmanaged
        {
            if (TryGetContainer<T>(out var container))
            {
                count = (uint)container->DataLength;
                return true;
            }
            count = 0;
            return false;
        }
        public DataFilter<T1, T2> GetFilter<T1, T2>()
            where T1 : unmanaged
            where T2 : unmanaged
        {
            var h1 = GetTypeHandle<T1>();
            var h2 = GetTypeHandle<T2>();
            var key = new FilterKey(h1, h2);

            if (!_filters.TryGetValue(key, out var p))
            {
                var c1 = GetOrCreateContainer<T1>();
                var c2 = GetOrCreateContainer<T2>();

                p = NativeMemory.Package(new FilterState(c1, c2, h1, h2));
                _filters[key] = p;
            }

            return new DataFilter<T1, T2> { _s = (FilterState*)p };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetFilter<T1, T2>(out DataFilter<T1, T2> filter)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            filter = GetFilter<T1, T2>();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(Entity e, T data) where T : unmanaged
        {
            var c = GetOrCreateContainer<T>();
            bool existed = c->Has(e.id);
            c->Add(e.id, data);
            if (!existed) NotifyAdded(GetTypeHandle<T>(), e.id);
        }

        private void NotifyAdded(IntPtr h, uint e)
        {
            for (int s = 0; s < _filters.Capacity; s++)
            {
                if (!_filters.TryGetRawEntry(s, out var key, out var p)) continue;
                if (key.First == h || key.Second == h)
                    ((FilterState*)p)->OnAdded(h, e);
            }
        }
        public bool RemoveComponent<T>(Entity entity) where T : unmanaged
        {
            if (!TryGetContainer<T>(out var container)) return false;
            if (!container->Has(entity.id)) return false;

            container->Remove(entity.id);
            NotifyRemoved(GetTypeHandle<T>(), entity.id);
            return true;
        }

        private void NotifyRemoved(IntPtr h, uint e)
        {
            for (int s = 0; s < _filters.Capacity; s++)
            {
                if (!_filters.TryGetRawEntry(s, out var key, out var p)) continue;
                if (key.First == h || key.Second == h)
                    ((FilterState*)p)->OnRemoved(h, e);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(Entity entity) where T : unmanaged
            => TryGetContainer<T>(out var container) && container->Has(entity.id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponentDataRef<T>(Entity entity) where T : unmanaged
        {
            if (!TryGetContainer<T>(out var container))
                throw new InvalidOperationException($"Container for type {typeof(T).Name} not found");
            return ref container->GetRef<T>(entity.id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetContainer<T>(out SparseSet* container) where T : unmanaged
        {
            var typeHandle = GetTypeHandle<T>();

            if (_containers.TryGetValue(typeHandle, out var handle))
            {
                container = (SparseSet*)handle;
                return true;
            }

            container = null;
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SparseSet* GetOrCreateContainer<T>() where T : unmanaged
        {
            var h = GetTypeHandle<T>();
            if (_containers.TryGetValue(h, out var p)) return (SparseSet*)p;
            var set = (SparseSet*)NativeMemory.Allocate<SparseSet>(1);
            *set = new SparseSet(sizeof(T), 16);
            _containers.Add(h, (IntPtr)set);
            return set;
        }
    }
}