using System;
using System.Collections.Generic;

namespace ECS.App.NativeStructures
{
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

        public NativeDictionary(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            int cap = HashHelpers.GetHashCapacity(capacity);

            _buckets = new NativeArray<int>(cap);
            _hashes = new NativeArray<int>(cap);
            _next = new NativeArray<int>(cap);
            _keys = new NativeArray<TKey>(cap);
            _values = new NativeArray<TValue>(cap);

            _count = 0;
            _freeList = -1;

            Initialize(cap);
        }

        public int Count => _count;
        public bool IsCreated => _buckets.IsCreated;

        public TValue this[TKey key]
        {
            get
            {
                CheckCreated();

                int index = FindIndex(key);
                if (index < 0)
                    throw new KeyNotFoundException();

                return _values[index];
            }

            set
            {
                CheckCreated();

                int index = FindIndex(key);
                if (index >= 0)
                {
                    _values[index] = value;
                    return;
                }

                int hash = GetHashCode(key);

                if (_buckets.Length != 0 && !HashHelpers.ShouldGrow(_count, _buckets.Length))
                {
                    int bucket = hash & (_buckets.Length - 1);
                    InsertNew(key, value, hash, bucket);
                    return;
                }

                Grow();

                int newBucket = hash & (_buckets.Length - 1);
                InsertNew(key, value, hash, newBucket);
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (!TryAdd(key, value))
                throw new ArgumentException("An item with the same key has already been added.", nameof(key));
        }

        public bool TryAdd(TKey key, TValue value)
        {
            CheckCreated();

            int hash = GetHashCode(key);

            if (_buckets.Length != 0)
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

            Grow();

            int newBucket = hash & (_buckets.Length - 1);
            InsertNew(key, value, hash, newBucket);

            return true;
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

            if (_buckets.Length == 0)
                return false;

            int hash = GetHashCode(key);
            int bucket = hash & (_buckets.Length - 1);
            var comparer = EqualityComparer<TKey>.Default;

            int previous = -1;

            for (int i = _buckets[bucket]; i >= 0; i = _next[i])
            {
                if (_hashes[i] == hash && comparer.Equals(_keys[i], key))
                {
                    if (previous < 0)
                        _buckets[bucket] = _next[i];
                    else
                        _next[previous] = _next[i];

                    _hashes[i] = -1;
                    _keys[i] = default;
                    _values[i] = default;
                    _next[i] = _freeList;
                    _freeList = i;

                    _count--;
                    return true;
                }

                previous = i;
            }

            return false;
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
        }

        public void Dispose()
        {
            _buckets.Dispose();
            _hashes.Dispose();
            _next.Dispose();
            _keys.Dispose();
            _values.Dispose();

            _count = 0;
            _freeList = -1;
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

            for (int i = _buckets[bucket]; i >= 0; i = _next[i])
            {
                if (_hashes[i] == hash && comparer.Equals(_keys[i], key))
                    return i;
            }

            return -1;
        }

        private int AllocateEntry()
        {
            if (_freeList >= 0)
            {
                int index = _freeList;
                _freeList = _next[index];
                return index;
            }

            return _count;
        }

        private void InsertNew(TKey key, TValue value, int hash, int bucket)
        {
            int index = AllocateEntry();

            _hashes[index] = hash;
            _keys[index] = key;
            _values[index] = value;
            _next[index] = _buckets[bucket];
            _buckets[bucket] = index;

            _count++;
        }

        private void Grow()
        {
            int oldCapacity = _buckets.Length;
            int newCapacity = HashHelpers.GrowHashCapacity(oldCapacity);

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
                if (hash == -1)
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

            _buckets.Dispose();
            _hashes.Dispose();
            _next.Dispose();
            _keys.Dispose();
            _values.Dispose();

            _buckets = newBuckets;
            _hashes = newHashes;
            _next = newNext;
            _keys = newKeys;
            _values = newValues;

            _count = newCount;
            _freeList = free;
        }

        private static int GetHashCode(TKey key)
            => EqualityComparer<TKey>.Default.GetHashCode(key) & 0x7FFFFFFF;

        private void CheckCreated()
        {
            if (!_buckets.IsCreated)
                throw new InvalidOperationException("Native collection is not created or already disposed.");
        }
    }
}
