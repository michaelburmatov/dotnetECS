using System;
using System.Collections.Generic;

namespace ECS.App.NativeStructures
{
    public unsafe struct NativeList<T> : IDisposable where T : unmanaged
    {
        private T* _ptr;
        private int _length;
        private int _capacity;
        private bool _isCreated;

        public NativeList(int initialCapacity = 0)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            _ptr = NativeMemory.Allocate<T>(initialCapacity);
            _length = 0;
            _capacity = initialCapacity;
            _isCreated = true;
        }

        public int Length
        {
            get => _length;
            set => Resize(value);
        }

        public int Capacity
        {
            get => _capacity;
            set => SetCapacity(value);
        }

        public bool IsCreated => _isCreated;

        public ref T this[int index]
        {
            get
            {
                CheckCreated();

                if ((uint)index >= (uint)_length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return ref _ptr[index];
            }
        }

        public void Add(T value)
        {
            CheckCreated();

            if (_length == int.MaxValue)
                throw new OutOfMemoryException();

            EnsureCapacity(_length + 1);
            _ptr[_length++] = value;
        }

        public void EnsureCapacity(int requiredCapacity)
        {
            CheckCreated();

            if (requiredCapacity < 0)
                throw new OutOfMemoryException();

            if (requiredCapacity <= _capacity)
                return;

            int newCapacity = _capacity == 0 ? 4 : _capacity;

            while (newCapacity < requiredCapacity)
            {
                if (newCapacity >= HashHelpers.MaxCapacity)
                    throw new OutOfMemoryException();

                newCapacity = newCapacity > HashHelpers.MaxCapacity / 2
                    ? HashHelpers.MaxCapacity
                    : newCapacity * 2;
            }

            SetCapacityInternal(newCapacity);
        }

        public void SetCapacity(int newCapacity)
        {
            CheckCreated();

            if (newCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(newCapacity));

            if (newCapacity < _length)
                throw new InvalidOperationException("Capacity cannot be less than Length.");

            if (newCapacity == _capacity)
                return;

            SetCapacityInternal(newCapacity);
        }

        public void Resize(int newLength)
        {
            CheckCreated();

            if (newLength < 0)
                throw new ArgumentOutOfRangeException(nameof(newLength));

            if (newLength < _length)
            {
                _length = newLength;
                return;
            }

            if (newLength == _length)
                return;

            EnsureCapacity(newLength);

            if (newLength > _length)
                NativeMemory.Zero(_ptr + _length, newLength - _length);

            _length = newLength;
        }

        public void RemoveAtSwapBack(int index)
        {
            CheckCreated();

            if ((uint)index >= (uint)_length)
                throw new ArgumentOutOfRangeException(nameof(index));

            _length--;

            if (index != _length)
                _ptr[index] = _ptr[_length];
        }

        public void RemoveAt(int index)
        {
            CheckCreated();

            if ((uint)index >= (uint)_length)
                throw new ArgumentOutOfRangeException(nameof(index));

            for (int i = index; i < _length - 1; i++)
                _ptr[i] = _ptr[i + 1];

            _length--;
        }

        public int IndexOf(T value)
        {
            CheckCreated();

            var comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < _length; i++)
            {
                if (comparer.Equals(_ptr[i], value))
                    return i;
            }

            return -1;
        }

        public bool Contains(T value) => IndexOf(value) >= 0;

        public bool Remove(T value)
        {
            int index = IndexOf(value);
            if (index < 0)
                return false;

            RemoveAtSwapBack(index);
            return true;
        }

        public void Clear()
        {
            CheckCreated();
            _length = 0;
        }

        public Span<T> AsSpan()
        {
            CheckCreated();

            if (_length == 0)
                return Span<T>.Empty;

            return new Span<T>((void*)_ptr, _length);
        }

        public void Dispose()
        {
            if (!_isCreated)
                return;

            NativeMemory.Free(_ptr);

            _ptr = null;
            _length = 0;
            _capacity = 0;
            _isCreated = false;
        }

        private void SetCapacityInternal(int newCapacity)
        {
            if (newCapacity == 0)
            {
                NativeMemory.Free(_ptr);
                _ptr = null;
                _capacity = 0;
                return;
            }

            _ptr = _ptr == null
                ? NativeMemory.Allocate<T>(newCapacity)
                : NativeMemory.Reallocate<T>(_ptr, newCapacity);

            _capacity = newCapacity;
        }

        private void CheckCreated()
        {
            if (!_isCreated)
                throw new InvalidOperationException("Native collection is not created or already disposed.");
        }
    }
}
