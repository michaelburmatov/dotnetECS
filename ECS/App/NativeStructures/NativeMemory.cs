using System;
using System.Runtime.InteropServices;

namespace ECS.App.NativeStructures
{
    internal static unsafe class NativeMemory
    {
        // Используем long для размеров
        public static T* Allocate<T>(long count) where T : unmanaged
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return null;

            long bytes = count * sizeof(T);
            if (bytes < 0)
                throw new OutOfMemoryException();

            // Marshal.AllocHGlobal принимает IntPtr (64-bit в x64)
            return (T*)(void*)Marshal.AllocHGlobal(new IntPtr(bytes));
        }

        public static T* Reallocate<T>(T* ptr, long count) where T : unmanaged
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

            long bytes = count * sizeof(T);
            if (bytes < 0)
                throw new OutOfMemoryException();

            return (T*)(void*)Marshal.ReAllocHGlobal(
                (IntPtr)(void*)ptr,
                new IntPtr(bytes)
            );
        }

        public static void Free<T>(T* ptr) where T : unmanaged
        {
            if (ptr != null)
                Marshal.FreeHGlobal((IntPtr)(void*)ptr);
        }

        // Используем long для count в копировании
        public static void Copy<T>(T* source, T* destination, long count) where T : unmanaged
        {
            if (count <= 0) return;

            long bytes = count * sizeof(T);

            // Buffer.MemoryCopy принимает long для размеров
            Buffer.MemoryCopy(
                (void*)source,
                (void*)destination,
                bytes,
                bytes
            );
        }

        public static void Zero<T>(T* ptr, long count) where T : unmanaged
        {
            for (long i = 0; i < count; i++)
                ptr[i] = default;
        }
    }
}
