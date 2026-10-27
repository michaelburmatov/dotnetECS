using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ECS.App.NativeStructures
{
    public unsafe struct NativeStack<T> : IDisposable where T : unmanaged
    {
        private T* _buffer;
        private uint _count;
        private uint _capacity;
        private bool _isCreated;

        public NativeStack(uint capacity = 16)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            if (capacity == 0)
                capacity = 4;

            _buffer = NativeMemory.Allocate<T>((int)capacity);
            _count = 0;
            _capacity = capacity;
            _isCreated = true;
        }

        public uint Count => _count;
        public uint Capacity => _capacity;
        public bool IsCreated => _isCreated;
        public bool IsEmpty => _count == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T item)
        {
            CheckCreated();

            if (_count == _capacity)
                Grow(_capacity * 2);

            _buffer[_count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            CheckCreated();

            if (_count == 0)
                throw new InvalidOperationException("Stack is empty.");

            T item = _buffer[--_count];
            _buffer[_count] = default;

            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T item)
        {
            CheckCreated();

            if (_count == 0)
            {
                item = default;
                return false;
            }

            item = _buffer[--_count];
            _buffer[_count] = default;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
            CheckCreated();

            if (_count == 0)
                throw new InvalidOperationException("Stack is empty.");

            return _buffer[_count - 1];
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

            item = _buffer[_count - 1];
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T PeekRef()
        {
            CheckCreated();

            if (_count == 0)
                throw new InvalidOperationException("Stack is empty.");

            return ref _buffer[_count - 1];
        }

        public void Clear()
        {
            CheckCreated();

            if (_count > 0)
            {
                NativeMemory.Zero(_buffer, (int)_count);
                _count = 0;
            }
        }

        public ReadOnlySpan<T> AsSpan()
        {
            CheckCreated();

            if (_count == 0)
                return ReadOnlySpan<T>.Empty;

            return new ReadOnlySpan<T>((void*)_buffer, (int)_count);
        }

        public void CopyTo(NativeList<T> destination)
        {
            CheckCreated();

            destination.EnsureCapacity(destination.Length + _count);

            for (int i = 0; i < _count; i++)
                destination.Add(_buffer[i]);
        }

        public void TrimExcess()
        {
            CheckCreated();

            if (_count == 0)
            {
                NativeMemory.Free(_buffer);
                _buffer = NativeMemory.Allocate<T>(4);
                _capacity = 4;
                return;
            }

            var newCapacity = Math.Max(4, _count);

            if (newCapacity < _capacity / 2)
            {
                T* newBuffer = NativeMemory.Allocate<T>((int)newCapacity);
                NativeMemory.Copy(_buffer, newBuffer, (int)_count);
                NativeMemory.Free(_buffer);

                _buffer = newBuffer;
                _capacity = newCapacity;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public void Dispose()
        {
            if (!_isCreated)
                return;

            NativeMemory.Free(_buffer);

            _buffer = null;
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
                NativeMemory.Copy(_buffer, newBuffer, (int)_count);

            NativeMemory.Free(_buffer);

            _buffer = newBuffer;
            _capacity = newCapacity;
        }

        private void CheckCreated()
        {
            if (!_isCreated)
                throw new InvalidOperationException("NativeStack is not created or already disposed.");
        }

        public struct Enumerator : IDisposable
        {
            private readonly T* _buffer;
            private readonly uint _count;
            private uint _index;

            internal Enumerator(NativeStack<T> stack)
            {
                _buffer = stack._buffer;
                _count = stack._count;
                _index = _count; // Начинаем с вершины стека
            }

            public T Current => _buffer[_index];

            public bool MoveNext()
            {
                _index--;
                return _index >= 0;
            }

            public void Reset()
            {
                _index = _count;
            }

            public void Dispose() { }
        }
    }
}
