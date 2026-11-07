using ECS.App.NativeStructures;
using System;

public unsafe struct NativeList<T> : IDisposable where T : unmanaged
{
    public T* Buffer;
    public long Length;
    public long Capacity;

    public NativeList(long initialCapacity = 16)
    {
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        Buffer = initialCapacity == 0 ? null : NativeMemory.Allocate<T>(initialCapacity);
        Length = 0;
        Capacity = initialCapacity;
    }

    public void Add(T item)
    {
        if (Length >= Capacity)
        {
            long newCapacity = Capacity == 0 ? 16 : Capacity * 2;
            Resize(newCapacity);
        }
        Buffer[Length++] = item;
    }

    public void Resize(long newCapacity)
    {
        if (newCapacity < Length)
            throw new ArgumentOutOfRangeException(nameof(newCapacity));

        T* newBuffer = NativeMemory.Allocate<T>(newCapacity);

        if (Buffer != null && Length > 0)
        {
            NativeMemory.Copy(Buffer, newBuffer, Length);
            NativeMemory.Free(Buffer);
        }

        Buffer = newBuffer;
        Capacity = newCapacity;
    }

    public T this[long index]
    {
        get
        {
            if (index < 0 || index >= Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return Buffer[index];
        }
        set
        {
            if (index < 0 || index >= Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            Buffer[index] = value;
        }
    }

    public ref T GetRef(long index)
    {
        if (index < 0 || index >= Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        return ref Buffer[index];
    }

    public void RemoveAt(long index)
    {
        if (index < 0 || index >= Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        for (long i = index; i < Length - 1; i++)
        {
            Buffer[i] = Buffer[i + 1];
        }
        Length--;
    }

    public void RemoveAtSwapBack(long index)
    {
        if (index < 0 || index >= Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        Buffer[index] = Buffer[Length - 1];
        Length--;
    }

    public void Clear()
    {
        Length = 0;
    }

    public void Dispose()
    {
        if (Buffer != null)
        {
            NativeMemory.Free(Buffer);
            Buffer = null;
        }
        Length = 0;
        Capacity = 0;
    }
}