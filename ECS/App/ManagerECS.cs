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
        private NativeDictionary<IntPtr, SparseSetHandle> _containers;

        // Холодный путь: типобезопасное освобождение контейнеров вместо type-punning
        //private NativeDictionary<IntPtr, Action<IntPtr>> _disposers;

        // Кэш фильтров: (T1,T2) -> материализованный список совпадений
        private NativeDictionary<FilterKey, DataFilterHandle> _filters;

        // Подписчики на структурные изменения конкретного типа компонента
        private NativeDictionary<IntPtr, ListObserversHandle> _observers;

        public ManagerECS(uint initialCapacity = 16)
        {
            _containers = new NativeDictionary<IntPtr, SparseSetHandle>((int)initialCapacity);
            //_disposers = new NativeDictionary<IntPtr, Action<IntPtr>>();
            _filters = new NativeDictionary<FilterKey, DataFilterHandle>((int)initialCapacity);
            _observers = new NativeDictionary<IntPtr, ListObserversHandle>((int)initialCapacity);
        }

        /// <summary>
        /// Ключ кэша фильтров. Порядок важен: DataFilter&lt;A,B&gt; и DataFilter&lt;B,A&gt; —
        /// разные кэши, т.к. GetFirst возвращает первый тип параметра.
        /// IEquatable реализован явно, чтобы Dictionary не боксировал ключ.
        /// </summary>
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

        private static class ContainerDisposer<T> where T : unmanaged
        {
            public static readonly Action<IntPtr> Dispose = ptr =>
            {
                var typed = (SparseSet<T>*)ptr;
                typed->Dispose();
                NativeMemory.Free(typed);
            };
        }

        internal struct SparseSetHandle
        {
            public IntPtr Ptr;
        }
        internal struct DataFilterHandle
        {
            public IntPtr Ptr;
        }

        internal struct ListObserversHandle
        {
            public IntPtr Ptr;
        }



        public void Dispose()
        {
            //foreach (var kvp in _filters)
            //    ((IDisposable)kvp.Value).Dispose();
            _filters.Clear();
            _observers.Clear();

            //foreach (var kvp in _disposers)
            //    kvp.Value(kvp.Key);
            //_disposers.Clear();
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

        /// <summary>
        /// Возвращает кэшированный фильтр «есть и T1, и T2».
        /// Фильтр создаётся один раз на пару типов и дальше поддерживается инкрементально,
        /// поэтому его можно (и нужно) сохранять в поле системы.
        ///
        /// Контейнеры создаются при необходимости, так что фильтр валиден даже если
        /// данных ещё нет: он наполнится по мере добавления компонентов.
        ///
        /// Сложность: O(1) при повторном запросе, O(min(n,m)) при первом создании.
        /// </summary>
        public DataFilter<T1, T2> GetFilter<T1, T2>()
            where T1 : unmanaged
            where T2 : unmanaged
        {
            var h1 = GetTypeHandle<T1>();
            var h2 = GetTypeHandle<T2>();
            var key = new FilterKey(h1, h2);

            if (_filters.TryGetValue(key, out var cached))
                return ((DataFilter<T1, T2>*)cached.Ptr)[0];

            var container1 = GetOrCreateContainer<T1>();
            var container2 = GetOrCreateContainer<T2>();

            var state = new DataFilter<T1, T2>(container1, container2, h1, h2);
            var fataFilter = new DataFilterHandle()
            {
                Ptr = Package(state)
            };

            _filters[key] = fataFilter;

            return ((DataFilter<T1, T2>*)fataFilter.Ptr)[0];
        }

        /// <summary>
        /// Совместимая обёртка над <see cref="GetFilter{T1,T2}"/>.
        /// Всегда возвращает true и валидный фильтр (пустой, если совпадений нет).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetFilter<T1, T2>(out DataFilter<T1, T2> filter)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            filter = GetFilter<T1, T2>();
            return true;
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

            // перезапись существующего компонента не меняет состав набора -> фильтры не трогаем
            bool existed = container->Has(entity.id);
            container->Add(entity.id, componentData);
        }

        /// <summary>
        /// Удаляет компонент у сущности и обновляет затронутые фильтры. O(1) на фильтр.
        /// Возвращает false, если компонента не было.
        /// </summary>
        public bool RemoveComponent<T>(Entity entity) where T : unmanaged
        {
            if (!TryGetContainer<T>(out var container))
                return false;
            if (!container->Has(entity.id))
                return false;

            container->Remove(entity.id);
            return true;
        }

        /// <summary>Есть ли у сущности компонент T. O(1).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(Entity entity) where T : unmanaged
            => TryGetContainer<T>(out var container) && container->Has(entity.id);

        /// <summary>ref на компонент сущности (не по dense-индексу). O(1).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponentDataRef<T>(Entity entity) where T : unmanaged
        {
            if (!TryGetContainer<T>(out var container))
                throw new InvalidOperationException($"Container for type {typeof(T).Name} not found");
            return ref container->Get(entity.id);
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

            SparseSetHandle newHandle = new SparseSetHandle
            {
                Ptr = Package(new SparseSet<T>(16))
            };

            _containers.Add(typeHandle, newHandle);
            return (SparseSet<T>*)newHandle.Ptr;
        }

        private IntPtr Package<T>(T element)
            where T : unmanaged
        {
            var ptr = NativeMemory.Allocate<T>(1);
            *ptr = element;
            return (IntPtr)ptr;
        }
    }
}