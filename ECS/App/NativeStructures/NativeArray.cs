using System;

namespace ECS.App.NativeStructures
{
    public unsafe struct NativeArray<T> : IDisposable where T : unmanaged
    {
        private T* _ptr;
        private int _length;
        private bool _isCreated;

        public NativeArray(int length, bool clearMemory = false)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            _ptr = NativeMemory.Allocate<T>(length);
            _length = length;
            _isCreated = true;

            if (clearMemory && length > 0)
                NativeMemory.Zero(_ptr, length);
        }

        public int Length => _length;
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

        public T* GetUnsafePtr()
        {
            CheckCreated();
            return _ptr;
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
            _isCreated = false;
        }

        private void CheckCreated()
        {
            if (!_isCreated)
                throw new InvalidOperationException("Native collection is not created or already disposed.");
        }
    }
}
