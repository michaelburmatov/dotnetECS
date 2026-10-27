using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ECS.App.NativeStructures
{
    public unsafe struct NativeQueue<T> : IDisposable where T : unmanaged
    {
        private T* _buffer;
        private uint _head;
        private uint _tail;
        private uint _count;
        private uint _capacity;
        private bool _isCreated;

        public NativeQueue(uint capacity = 16)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            if (capacity == 0)
                capacity = 4;

            _buffer = NativeMemory.Allocate<T>((int)capacity);
            _head = 0;
            _tail = 0;
            _count = 0;
            _capacity = capacity;
            _isCreated = true;
        }

        public uint Count => _count;
        public uint Capacity => _capacity;
        public bool IsCreated => _isCreated;
        public bool IsEmpty => _count == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            CheckCreated();

            if (_count == _capacity)
                Grow(_capacity * 2);

            _buffer[_tail] = item;
            _tail = (_tail + 1) % _capacity;
            _count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            CheckCreated();

            if (_count == 0)
                throw new InvalidOperationException("Queue is empty.");

            T item = _buffer[_head];
            _buffer[_head] = default;
            _head = (_head + 1) % _capacity;
            _count--;

            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T item)
        {
            CheckCreated();

            if (_count == 0)
            {
                item = default;
                return false;
            }

            item = _buffer[_head];
            _buffer[_head] = default;
            _head = (_head + 1) % _capacity;
            _count--;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
            CheckCreated();

            if (_count == 0)
                throw new InvalidOperationException("Queue is empty.");

            return _buffer[_head];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out T item)
        {
            CheckCreated();

            if (_count == 0)
            {
                item = default;
                return false;
            }

            item = _buffer[_head];
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T PeekRef()
        {
            CheckCreated();

            if (_count == 0)
                throw new InvalidOperationException("Queue is empty.");

            return ref _buffer[_head];
        }

        public void Clear()
        {
            CheckCreated();

            if (_count == 0)
                return;

            if (_head < _tail)
            {
                NativeMemory.Zero(_buffer + _head, (int)_count);
            }
            else
            {
                var firstSegment = _capacity - _head;
                var secondSegment = _tail;

                NativeMemory.Zero(_buffer + _head, (int)firstSegment);
                if (secondSegment > 0)
                    NativeMemory.Zero(_buffer, (int)secondSegment);
            }

            _head = 0;
            _tail = 0;
            _count = 0;
        }

        public ReadOnlySpan<T> AsSpan()
        {
            CheckCreated();

            if (_count == 0)
                return ReadOnlySpan<T>.Empty;

            if (_head < _tail)
            {
                return new ReadOnlySpan<T>((void*)(_buffer + _head), (int)_count);
            }

            var firstSegment = _capacity - _head;
            return new ReadOnlySpan<T>((void*)(_buffer + _head), (int)firstSegment);
        }

        public void CopyTo(NativeList<T> destination)
        {
            CheckCreated();

            destination.EnsureCapacity(destination.Length + _count);

            if (_head < _tail)
            {
                for (uint i = _head; i < _tail; i++)
                    destination.Add(_buffer[i]);
            }
            else
            {
                for (uint i = _head; i < _capacity; i++)
                    destination.Add(_buffer[i]);

                for (uint i = 0; i < _tail; i++)
                    destination.Add(_buffer[i]);
            }
        }

        public void TrimExcess()
        {
            CheckCreated();

            if (_count == 0)
            {
                NativeMemory.Free(_buffer);
                _buffer = NativeMemory.Allocate<T>(4);
                _capacity = 4;
                _head = 0;
                _tail = 0;
                return;
            }

            uint newCapacity = Math.Max(4, _count);

            if (newCapacity < _capacity / 2)
            {
                T* newBuffer = NativeMemory.Allocate<T>((int)newCapacity);

                if (_head < _tail)
                {
                    NativeMemory.Copy(_buffer + _head, newBuffer, (int)_count);
                }
                else
                {
                    uint firstSegment = _capacity - _head;
                    NativeMemory.Copy(_buffer + _head, newBuffer, (int)firstSegment);
                    NativeMemory.Copy(_buffer, newBuffer + firstSegment, (int)_tail);
                }

                NativeMemory.Free(_buffer);

                _buffer = newBuffer;
                _capacity = newCapacity;
                _head = 0;
                _tail = _count % newCapacity;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public void Dispose()
        {
            if (!_isCreated)
                return;

            NativeMemory.Free(_buffer);

            _buffer = null;
            _head = 0;
            _tail = 0;
            _count = 0;
            _capacity = 0;
            _isCreated = false;
        }

        private void Grow(uint newCapacity)
        {
            if (newCapacity < _count)
                newCapacity = _count;

            T* newBuffer = NativeMemory.Allocate<T>((int)newCapacity);

            if (_count > 0)
            {
                if (_head < _tail)
                {
                    NativeMemory.Copy(_buffer + _head, newBuffer, (int)_count);
                }
                else
                {
                    uint firstSegment = _capacity - _head;
                    NativeMemory.Copy(_buffer + _head, newBuffer, (int)firstSegment);
                    NativeMemory.Copy(_buffer, newBuffer + firstSegment, (int)_tail);
                }
            }

            NativeMemory.Free(_buffer);

            _buffer = newBuffer;
            _capacity = newCapacity;
            _head = 0;
            _tail = _count % newCapacity;
        }

        private void CheckCreated()
        {
            if (!_isCreated)
                throw new InvalidOperationException("NativeQueue is not created or already disposed.");
        }

        public struct Enumerator : IDisposable
        {
            private readonly T* _buffer;
            private readonly uint _head;
            private readonly uint _capacity;
            private readonly uint _count;
            private int _index;

            internal Enumerator(NativeQueue<T> queue)
            {
                _buffer = queue._buffer;
                _head = queue._head;
                _capacity = queue._capacity;
                _count = queue._count;
                _index = -1;
            }

            public T Current => _buffer[(_head + _index) % _capacity];

            public bool MoveNext()
            {
                _index++;
                return _index < _count;
            }

            public void Reset()
            {
                _index = -1;
            }

            public void Dispose() { }
        }
    }
}
