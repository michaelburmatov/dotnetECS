using System;
using System.Collections.Generic;

namespace ECS.App.NativeStructures
{
    public struct NativeHashSet<T> : IDisposable where T : unmanaged
    {
        private NativeArray<int> _buckets;
        private NativeArray<int> _hashes;
        private NativeArray<int> _next;
        private NativeArray<T> _values;
        private int _count;
        private int _freeList;

        public NativeHashSet(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            int cap = HashHelpers.GetHashCapacity(capacity);

            _buckets = new NativeArray<int>(cap);
            _hashes = new NativeArray<int>(cap);
            _next = new NativeArray<int>(cap);
            _values = new NativeArray<T>(cap);

            _count = 0;
            _freeList = -1;

            Initialize(cap);
        }

        public int Count => _count;
        public bool IsCreated => _buckets.IsCreated;

        public bool Add(T value)
        {
            CheckCreated();

            int hash = GetHashCode(value);

            if (_buckets.Length != 0)
            {
                int bucket = hash & (_buckets.Length - 1);
                var comparer = EqualityComparer<T>.Default;

                for (int i = _buckets[bucket]; i >= 0; i = _next[i])
                {
                    if (_hashes[i] == hash && comparer.Equals(_values[i], value))
                        return false;
                }

                if (!HashHelpers.ShouldGrow(_count, _buckets.Length))
                {
                    InsertNew(value, hash, bucket);
                    return true;
                }
            }

            Grow();

            int newBucket = hash & (_buckets.Length - 1);
            InsertNew(value, hash, newBucket);

            return true;
        }

        public bool Contains(T value)
        {
            CheckCreated();

            if (_buckets.Length == 0)
                return false;

            int hash = GetHashCode(value);
            int bucket = hash & (_buckets.Length - 1);
            var comparer = EqualityComparer<T>.Default;

            for (int i = _buckets[bucket]; i >= 0; i = _next[i])
            {
                if (_hashes[i] == hash && comparer.Equals(_values[i], value))
                    return true;
            }

            return false;
        }

        public bool Remove(T value)
        {
            CheckCreated();

            if (_buckets.Length == 0)
                return false;

            int hash = GetHashCode(value);
            int bucket = hash & (_buckets.Length - 1);
            var comparer = EqualityComparer<T>.Default;

            int previous = -1;

            for (int i = _buckets[bucket]; i >= 0; i = _next[i])
            {
                if (_hashes[i] == hash && comparer.Equals(_values[i], value))
                {
                    if (previous < 0)
                        _buckets[bucket] = _next[i];
                    else
                        _next[previous] = _next[i];

                    _hashes[i] = -1;
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

        private void InsertNew(T value, int hash, int bucket)
        {
            int index = AllocateEntry();

            _hashes[index] = hash;
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
            var newValues = new NativeArray<T>(newCapacity);

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
            _values.Dispose();

            _buckets = newBuckets;
            _hashes = newHashes;
            _next = newNext;
            _values = newValues;

            _count = newCount;
            _freeList = free;
        }

        private static int GetHashCode(T value)
            => EqualityComparer<T>.Default.GetHashCode(value) & 0x7FFFFFFF;

        private void CheckCreated()
        {
            if (!_buckets.IsCreated)
                throw new InvalidOperationException("Native collection is not created or already disposed.");
        }
    }
}
