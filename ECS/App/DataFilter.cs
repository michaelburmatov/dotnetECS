using ECS.App.NativeStructures.Sparse;
using System;
using System.Runtime.CompilerServices;

namespace ECS.App
{
    public unsafe struct DataFilter<T1, T2>
        where T1 : unmanaged
        where T2 : unmanaged
    {
        private SparseSet<T1>* _set1;
        private SparseSet<T2>* _set2;
        private SparseSet<T1>* _smallerSet;  // Оптимизация: итерируем по меньшему
        private SparseSet<T2>* _largerSet;
        private bool _firstIsSmaller;
        private bool _isValid;

        public DataFilter()
        {
            _set1 = managerECS.GetSparseSetPtr<T1>();
            _set2 = managerECS.GetSparseSetPtr<T2>();

            _isValid = _set1 != null && _set2 != null;

            if (_isValid)
            {
                if (_set1->Data.Length <= _set2->Data.Length)
                {
                    _smallerSet = _set1;
                    _largerSet = _set2;
                    _firstIsSmaller = true;
                }
                else
                {
                    _smallerSet = (SparseSet<T1>*)_set2;
                    _largerSet = (SparseSet<T2>*)_set1;
                    _firstIsSmaller = false;
                }
            }
            else
            {
                _smallerSet = null;
                _largerSet = null;
                _firstIsSmaller = true;
            }
        }

        /// <summary>
        /// Возвращает количество сущностей, у которых есть ОБА компонента.
        /// Вызывать каждый раз дорого — лучше использовать Enumerator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetCount()
        {
            if (!_isValid) return 0;

            long count = 0;
            long smallerLen = _firstIsSmaller ? _set1->Data.Length : _set2->Data.Length;
            uint* smallerDense = _firstIsSmaller ? _set1->Dense.Buffer : _set2->Dense.Buffer;

            for (long i = 0; i < smallerLen; i++)
            {
                uint entityId = smallerDense[i];
                if (_firstIsSmaller ? _set2->Has(entityId) : _set1->Has(entityId))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Возвращает ref на первый компонент (T1) по индексу совпадения.
        /// Самый быстрый способ получить данные.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T1 GetFirst(long matchIndex)
        {
            uint entityId = GetEntityIdAt(matchIndex);
            return ref _set1->Get(entityId);
        }

        /// <summary>
        /// Возвращает ref на второй компонент (T2) по индексу совпадения.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T2 GetSecond(long matchIndex)
        {
            uint entityId = GetEntityIdAt(matchIndex);
            return ref _set2->Get(entityId);
        }

        /// <summary>
        /// Находит entityId по индексу совпадения (matchIndex).
        /// Пропускает сущности, у которых нет обоих компонентов.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetEntityIdAt(long matchIndex)
        {
            if (!_isValid)
                throw new InvalidOperationException("Filter is not valid");

            long smallerLen = _firstIsSmaller ? _set1->Data.Length : _set2->Data.Length;
            uint* smallerDense = _firstIsSmaller ? _set1->Dense.Buffer : _set2->Dense.Buffer;

            long currentMatch = 0;
            for (long i = 0; i < smallerLen; i++)
            {
                uint entityId = smallerDense[i];
                bool hasSecond = _firstIsSmaller ? _set2->Has(entityId) : _set1->Has(entityId);

                if (hasSecond)
                {
                    if (currentMatch == matchIndex)
                        return entityId;
                    currentMatch++;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(matchIndex));
        }

        /// <summary>
        /// Возвращает Enumerator для эффективной итерации через foreach.
        /// Это самый производительный способ работы с фильтром.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Struct Enumerator для эффективной итерации без аллокаций.
        /// Используется в foreach.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly DataFilter<T1, T2> _filter;
            private readonly uint* _smallerDense;
            private readonly long _smallerLen;
            private long _currentIndex;
            private uint _currentEntityId;

            internal Enumerator(DataFilter<T1, T2> filter)
            {
                _filter = filter;
                _currentIndex = -1;
                _currentEntityId = uint.MaxValue;

                if (!filter._isValid)
                {
                    _smallerDense = null;
                    _smallerLen = 0;
                    return;
                }

                _smallerDense = filter._firstIsSmaller
                    ? filter._set1->Dense.Buffer
                    : filter._set2->Dense.Buffer;
                _smallerLen = filter._firstIsSmaller
                    ? filter._set1->Data.Length
                    : filter._set2->Data.Length;
            }

            /// <summary>
            /// ID текущей сущности.
            /// </summary>
            public uint EntityId => _currentEntityId;

            /// <summary>
            /// Первый компонент (T1) текущей сущности.
            /// </summary>
            public ref T1 First
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _filter._set1->Get(_currentEntityId);
            }

            /// <summary>
            /// Второй компонент (T2) текущей сущности.
            /// </summary>
            public ref T2 Second
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _filter._set2->Get(_currentEntityId);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                for (long i = _currentIndex + 1; i < _smallerLen; i++)
                {
                    uint entityId = _smallerDense[i];
                    bool hasSecond = _filter._firstIsSmaller
                        ? _filter._set2->Has(entityId)
                        : _filter._set1->Has(entityId);

                    if (hasSecond)
                    {
                        _currentIndex = i;
                        _currentEntityId = entityId;
                        return true;
                    }
                }
                return false;
            }
        }
    }
}