using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ECS.App.NativeStructures
{
    /// <summary>
    /// Высокопроизводительный словарь для неуправляемых типов (unmanaged),
    /// использующий открытую адресацию с цепочками (separate chaining).
    /// 
    /// ⚠️ НЕ ЯВЛЯЕТСЯ ПОТОКОБЕЗОПАСНЫМ!
    /// Параллельные операции записи (Add/Remove/set) из разных потоков
    /// приведут к повреждению внутренней структуры (циклы в _next)
    /// и бесконечным циклам при поиске.
    /// </summary>
    public struct NativeDictionary<TKey, TValue> : IDisposable
        where TKey : unmanaged
        where TValue : unmanaged
    {
        private NativeArray<int> _buckets;
        private NativeArray<int> _hashes;
        private NativeArray<int> _next;
        private NativeArray<TKey> _keys;
        private NativeArray<TValue> _values;
        private int _count;
        private int _freeList;
        private int _version;
        private bool _isDisposed;

        public NativeDictionary(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");

            _isDisposed = false;
            _version = 0;

            int cap = HashHelpers.GetHashCapacity(capacity);

            if (cap == 0)
            {
                _buckets = default;
                _hashes = default;
                _next = default;
                _keys = default;
                _values = default;
                _count = 0;
                _freeList = -1;
            }
            else
            {
                _buckets = new NativeArray<int>(cap);
                _hashes = new NativeArray<int>(cap);
                _next = new NativeArray<int>(cap);
                _keys = new NativeArray<TKey>(cap);
                _values = new NativeArray<TValue>(cap);

                _count = 0;
                _freeList = -1;

                Initialize(cap);
            }
        }

        public int Count => _count;
        public int Capacity => _buckets.IsCreated ? _buckets.Length : 0;
        public bool IsCreated => _buckets.IsCreated && !_isDisposed;

        public TValue this[TKey key]
        {
            get
            {
                CheckCreated();

                int index = FindIndex(key);
                if (index < 0)
                    throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");

                return _values[index];
            }

            set
            {
                CheckCreated();

                int index = FindIndex(key);
                if (index >= 0)
                {
                    _values[index] = value;
                    _version++;
                    return;
                }

                AddInternal(key, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, TValue value)
        {
            if (!TryAdd(key, value))
                throw new ArgumentException($"An item with the same key '{key}' has already been added.", nameof(key));
        }

        public bool TryAdd(TKey key, TValue value)
        {
            CheckCreated();

            int hash = GetHashCode(key);

            if (_buckets.Length > 0)
            {
                int bucket = hash & (_buckets.Length - 1);
                var comparer = EqualityComparer<TKey>.Default;

                for (int i = _buckets[bucket]; i >= 0; i = _next[i])
                {
                    if (_hashes[i] == hash && comparer.Equals(_keys[i], key))
                        return false;
                }

                if (!HashHelpers.ShouldGrow(_count, _buckets.Length))
                {
                    InsertNew(key, value, hash, bucket);
                    return true;
                }
            }

            AddWithGrow(key, value, hash);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddInternal(TKey key, TValue value)
        {
            int hash = GetHashCode(key);

            if (_buckets.Length > 0)
            {
                int bucket = hash & (_buckets.Length - 1);
                var comparer = EqualityComparer<TKey>.Default;

                for (int i = _buckets[bucket]; i >= 0; i = _next[i])
                {
                    if (_hashes[i] == hash && comparer.Equals(_keys[i], key))
                    {
                        _values[i] = value;
                        _version++;
                        return;
                    }
                }

                if (!HashHelpers.ShouldGrow(_count, _buckets.Length))
                {
                    InsertNew(key, value, hash, bucket);
                    return;
                }
            }

            AddWithGrow(key, value, hash);
        }

        private void AddWithGrow(TKey key, TValue value, int hash)
        {
            Grow();
            int newBucket = hash & (_buckets.Length - 1);
            InsertNew(key, value, hash, newBucket);
        }

        public bool ContainsKey(TKey key)
        {
            CheckCreated();
            return FindIndex(key) >= 0;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            CheckCreated();

            int index = FindIndex(key);
            if (index >= 0)
            {
                value = _values[index];
                return true;
            }

            value = default;
            return false;
        }

        public bool Remove(TKey key)
        {
            CheckCreated();

            if (_buckets.Length == 0 || _count == 0)
                return false;

            int hash = GetHashCode(key);
            int bucket = hash & (_buckets.Length - 1);
            var comparer = EqualityComparer<TKey>.Default;

            int previous = -1;

            for (int i = _buckets[bucket]; i >= 0; i = _next[i])
            {
                if (_hashes[i] == hash && comparer.Equals(_keys[i], key))
                {
                    RemoveEntry(i, previous, bucket);
                    return true;
                }

                previous = i;
            }

            return false;
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            CheckCreated();

            if (_buckets.Length == 0 || _count == 0)
            {
                value = default;
                return false;
            }

            int hash = GetHashCode(key);
            int bucket = hash & (_buckets.Length - 1);
            var comparer = EqualityComparer<TKey>.Default;

            int previous = -1;

            for (int i = _buckets[bucket]; i >= 0; i = _next[i])
            {
                if (_hashes[i] == hash && comparer.Equals(_keys[i], key))
                {
                    value = _values[i];
                    RemoveEntry(i, previous, bucket);
                    return true;
                }

                previous = i;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveEntry(int index, int previous, int bucket)
        {
            if (previous < 0)
                _buckets[bucket] = _next[index];
            else
                _next[previous] = _next[index];

            _hashes[index] = -1;
            _keys[index] = default;
            _values[index] = default;
            _next[index] = _freeList;
            _freeList = index;

            _count--;
            _version++;
        }

        public void Clear()
        {
            CheckCreated();

            int capacity = _buckets.Length;

            for (int i = 0; i < capacity; i++)
                _buckets[i] = -1;

            int free = -1;

            for (int i = capacity - 1; i >= 0; i--)
            {
                _hashes[i] = -1;
                _keys[i] = default;
                _values[i] = default;
                _next[i] = free;
                free = i;
            }

            _count = 0;
            _freeList = free;
            _version++;
        }

        public void EnsureCapacity(int capacity)
        {
            CheckCreated();

            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            int requiredCap = HashHelpers.GetHashCapacity(capacity);
            if (requiredCap > _buckets.Length)
            {
                GrowTo(requiredCap);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (_buckets.IsCreated) _buckets.Dispose();
            if (_hashes.IsCreated) _hashes.Dispose();
            if (_next.IsCreated) _next.Dispose();
            if (_keys.IsCreated) _keys.Dispose();
            if (_values.IsCreated) _values.Dispose();

            _count = 0;
            _freeList = -1;
            _version = 0;
        }

        private void Initialize(int capacity)
        {
            for (int i = 0; i < capacity; i++)
                _buckets[i] = -1;

            int free = -1;

            for (int i = capacity - 1; i >= 0; i--)
            {
                _hashes[i] = -1;
                _next[i] = free;
                free = i;
            }

            _freeList = free;
        }

        private int FindIndex(TKey key)
        {
            if (_buckets.Length == 0)
                return -1;

            int hash = GetHashCode(key);
            int bucket = hash & (_buckets.Length - 1);
            var comparer = EqualityComparer<TKey>.Default;

            int i = _buckets[bucket];

#if DEBUG
            // ⚠️ КРИТИЧЕСКИ ВАЖНО для отладки:
            // Если структура повреждена (из-за race condition), здесь будет обнаружен цикл
            // ДО того, как приложение зависнет.
            int maxIterations = _count + 2;
            int iterations = 0;
#endif

            while (i >= 0)
            {
#if DEBUG
                if (++iterations > maxIterations)
                {
                    throw new InvalidOperationException(
                        $"CYCLE DETECTED in bucket chain! Bucket={bucket}, Hash={hash}. " +
                        "This is almost always caused by CONCURRENT ACCESS from multiple threads. " +
                        "NativeDictionary is NOT thread-safe! Use synchronization or " +
                        "ConcurrentDictionary for multi-threaded scenarios.");
                }
#endif

                if (_hashes[i] == hash && comparer.Equals(_keys[i], key))
                    return i;

                i = _next[i];
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AllocateEntry()
        {
            if (_freeList >= 0)
            {
                int index = _freeList;
                _freeList = _next[index];
                return index;
            }

            // ⚠️ Защита от повреждения структуры данных
            if (_count >= _buckets.Length)
            {
                throw new InvalidOperationException(
                    $"INTERNAL ERROR: No free entries. Count={_count}, Capacity={_buckets.Length}. " +
                    "Structure is corrupted. Check for race conditions!");
            }

            return _count;
        }

        private void InsertNew(TKey key, TValue value, int hash, int bucket)
        {
            int index = AllocateEntry();

            // Защита от выхода за границы массива (защита от повреждения памяти)
            if ((uint)index >= (uint)_buckets.Length)
            {
                throw new IndexOutOfRangeException(
                    $"Index {index} out of bounds for capacity {_buckets.Length}. " +
                    "Internal structure is corrupted.");
            }

            _hashes[index] = hash;
            _keys[index] = key;
            _values[index] = value;
            _next[index] = _buckets[bucket];
            _buckets[bucket] = index;

            _count++;
            _version++;
        }

        private void Grow()
        {
            int oldCapacity = _buckets.Length;
            int newCapacity = HashHelpers.GrowHashCapacity(oldCapacity);
            GrowTo(newCapacity);
        }

        private void GrowTo(int newCapacity)
        {
            int oldCapacity = _buckets.Length;

            var newBuckets = new NativeArray<int>(newCapacity);
            var newHashes = new NativeArray<int>(newCapacity);
            var newNext = new NativeArray<int>(newCapacity);
            var newKeys = new NativeArray<TKey>(newCapacity);
            var newValues = new NativeArray<TValue>(newCapacity);

            for (int i = 0; i < newCapacity; i++)
                newBuckets[i] = -1;

            int newCount = 0;

            for (int i = 0; i < oldCapacity; i++)
            {
                int hash = _hashes[i];
                if (hash < 0) // -1 = пустой или удалённый
                    continue;

                int bucket = hash & (newCapacity - 1);

                newHashes[newCount] = hash;
                newKeys[newCount] = _keys[i];
                newValues[newCount] = _values[i];
                newNext[newCount] = newBuckets[bucket];
                newBuckets[bucket] = newCount;

                newCount++;
            }

            int free = -1;

            for (int i = newCapacity - 1; i >= newCount; i--)
            {
                newHashes[i] = -1;
                newNext[i] = free;
                free = i;
            }

            // Освобождаем старые массивы
            if (_buckets.IsCreated) _buckets.Dispose();
            if (_hashes.IsCreated) _hashes.Dispose();
            if (_next.IsCreated) _next.Dispose();
            if (_keys.IsCreated) _keys.Dispose();
            if (_values.IsCreated) _values.Dispose();

            _buckets = newBuckets;
            _hashes = newHashes;
            _next = newNext;
            _keys = newKeys;
            _values = newValues;

            _count = newCount;
            _freeList = free;
            _version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetHashCode(TKey key)
            => EqualityComparer<TKey>.Default.GetHashCode(key) & 0x7FFFFFFF;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            if (_isDisposed || !_buckets.IsCreated)
                throw new InvalidOperationException("NativeDictionary is not created or has been disposed.");
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public struct Enumerator : IDisposable
        {
            private readonly NativeArray<int> _hashes;
            private readonly NativeArray<TKey> _keys;
            private readonly NativeArray<TValue> _values;
            private readonly int _version;
            private int _index;
            private KeyValuePair<TKey, TValue> _current;
            private bool _disposed;

            internal Enumerator(NativeDictionary<TKey, TValue> dictionary)
            {
                _hashes = dictionary._hashes;
                _keys = dictionary._keys;
                _values = dictionary._values;
                _version = dictionary._version;
                _index = 0;
                _current = default;
                _disposed = dictionary._isDisposed;
            }

            public KeyValuePair<TKey, TValue> Current => _current;

            public bool MoveNext()
            {
                if (_disposed || !_hashes.IsCreated)
                    throw new InvalidOperationException(
                        "Collection was modified or disposed during enumeration. " +
                        "Do not modify the dictionary while iterating.");

                int capacity = _hashes.Length;

                while (_index < capacity)
                {
                    if (_hashes[_index] >= 0) // Живой элемент
                    {
                        _current = new KeyValuePair<TKey, TValue>(
                            _keys[_index],
                            _values[_index]
                        );
                        _index++;
                        return true;
                    }
                    _index++;
                }

                return false;
            }

            public void Dispose() { }

            public void Reset()
            {
                _index = 0;
                _current = default;
            }
        }
    }
}