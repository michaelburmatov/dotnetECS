using System;
using System.Runtime.InteropServices;

namespace ECS.App.NativeStructures
{
    internal static unsafe class NativeMemory
    {
        public static T* Allocate<T>(int count) where T : unmanaged
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0)
                return null;

            long bytes = (long)count * sizeof(T);
            if (bytes < 0)
                throw new OutOfMemoryException();

            return (T*)(void*)Marshal.AllocHGlobal(new IntPtr(bytes));
        }

        public static T* Reallocate<T>(T* ptr, int count) where T : unmanaged
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0)
            {
                Free(ptr);
                return null;
            }

            if (ptr == null)
                return Allocate<T>(count);

            long bytes = (long)count * sizeof(T);
            if (bytes < 0)
                throw new OutOfMemoryException();

            return (T*)(void*)Marshal.ReAllocHGlobal((IntPtr)(void*)ptr, new IntPtr(bytes));
        }

        public static void Free<T>(T* ptr) where T : unmanaged
        {
            if (ptr != null)
                Marshal.FreeHGlobal((IntPtr)(void*)ptr);
        }

        public static void Copy<T>(T* source, T* destination, int count) where T : unmanaged
        {
            if (count <= 0)
                return;

            long bytes = (long)count * sizeof(T);
            Buffer.MemoryCopy((void*)source, (void*)destination, bytes, bytes);
        }

        public static void Zero<T>(T* ptr, int count) where T : unmanaged
        {
            for (int i = 0; i < count; i++)
                ptr[i] = default;
        }
    }
}
