using ECS.App.NativeStructures;
using System;
using System.Runtime.CompilerServices;

public unsafe struct NativeList<T> : IDisposable where T : unmanaged
{
    public T* Buffer;
    public long Length;
    public long Capacity;
    public T DefaultValue;
    private bool needFeelBuffer;

    public NativeList(long initialCapacity = 16, bool useClearBuffer = false, T defaultValue = default)
    {
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        DefaultValue = defaultValue;
        needFeelBuffer = useClearBuffer;

        if (initialCapacity == 0)
        {
            Buffer = null;
            Length = 0;
            Capacity = 0;
        }
        else
        {
            Buffer = NativeMemory.Allocate<T>(initialCapacity);
            Length = 0;
            Capacity = initialCapacity;

            FillBuffer(Buffer, initialCapacity, defaultValue);
        }
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

        long emptyCount = newCapacity - Length;
        if (emptyCount > 0)
            FillBuffer(newBuffer + Length, emptyCount, DefaultValue);

        Buffer = newBuffer;
        Capacity = newCapacity;
    }

    public void EnsureCapacity(long requiredCapacity)
    {
        if (requiredCapacity < 0)
            throw new OutOfMemoryException("Capacity cannot be negative.");

        if (requiredCapacity <= Capacity)
            return;

        long newCapacity = Capacity == 0 ? 4 : Capacity;

        long maxCapacity = HashHelpers.MaxCapacity;

        while (newCapacity < requiredCapacity)
        {
            if (newCapacity >= maxCapacity)
            {
                newCapacity = maxCapacity;
                break;
            }

            if (newCapacity > maxCapacity / 2)
                newCapacity = maxCapacity;
            else
                newCapacity *= 2;
        }

        if (newCapacity < requiredCapacity)
            throw new OutOfMemoryException($"Unable to allocate memory. Required: {requiredCapacity}, Max: {maxCapacity}");

        Resize(newCapacity);
    }

    public ref T this[long index]
    {
        get
        {
            if (index < 0 || index >= Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref Buffer[index];
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

        Buffer[Length - 1] = DefaultValue;
        Length--;
    }

    public void RemoveAtSwapBack(long index)
    {
        if (index < 0 || index >= Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        Buffer[index] = Buffer[Length - 1];

        Buffer[Length - 1] = DefaultValue;
        Length--;
    }

    public void Clear()
    {
        if (Length > 0 && Buffer != null)
            FillBuffer(Buffer, Length, DefaultValue);
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

    /// <summary>
    /// Быстрое заполнение буфера через Span.Fill с поддержкой long размеров.
    /// Span.Fill использует SIMD-инструкции процессора для максимальной производительности.
    /// </summary>
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FillBuffer(T* buffer, long count, T value)
    {
        if (!needFeelBuffer || count <= 0 || buffer == null) return;

        const long maxChunkSize = int.MaxValue;
        long remaining = count;
        T* currentPtr = buffer;

        while (remaining > 0)
        {
            int chunkSize = (int)Math.Min(remaining, maxChunkSize);
            new Span<T>((void*)currentPtr, chunkSize).Fill(value);

            currentPtr += chunkSize;
            remaining -= chunkSize;
        }
    }
}