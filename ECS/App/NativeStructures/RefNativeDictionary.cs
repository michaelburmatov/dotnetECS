using System;
using System.Collections.Generic;

namespace ECS.App.NativeStructures
{
    /// <summary>
    /// Ссылочная обёртка над NativeDictionary.
    /// Решает проблему boxing/unboxing struct при хранении в GCHandle.
    /// Все копии RefNativeDictionary указывают на один и тот же объект.
    /// </summary>
    public sealed class RefNativeDictionary<TKey, TValue> : IDisposable
        where TKey : unmanaged
        where TValue : unmanaged
    {
        private NativeDictionary<TKey, TValue> _dictionary;
        private bool _disposed;

        public RefNativeDictionary(int capacity)
        {
            _dictionary = new NativeDictionary<TKey, TValue>(capacity);
            _disposed = false;
        }

        public int Count => _dictionary.Count;
        public int Capacity => _dictionary.Capacity;
        public bool IsCreated => _dictionary.IsCreated && !_disposed;

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
        }

        public bool TryAdd(TKey key, TValue value)
        {
            return _dictionary.TryAdd(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public bool Remove(TKey key)
        {
            return _dictionary.Remove(key);
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            return _dictionary.TryRemove(key, out value);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public void EnsureCapacity(int capacity)
        {
            _dictionary.EnsureCapacity(capacity);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _dictionary.Dispose();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_dictionary);
        }

        public struct Enumerator : IDisposable
        {
            private NativeDictionary<TKey, TValue>.SnapshotEnumerator _enumerator;

            internal Enumerator(NativeDictionary<TKey, TValue> dictionary)
            {
                _enumerator = dictionary.GetEnumerator();
            }

            public KeyValuePair<TKey, TValue> Current => _enumerator.Current;

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }
        }
    }
}