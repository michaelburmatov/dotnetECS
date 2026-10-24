using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.App.NativeStructures
{
    internal static class HashHelpers
    {
        public const int MaxCapacity = 1 << 30;

        public static bool ShouldGrow(int count, int capacity)
            => capacity == 0 || (long)count * 4 >= (long)capacity * 3;

        public static int GetHashCapacity(int requiredCount)
        {
            if (requiredCount < 0)
                throw new ArgumentOutOfRangeException(nameof(requiredCount));

            if (requiredCount == 0)
                return 0;

            long needed = (long)requiredCount * 4 / 3 + 1;
            if (needed > MaxCapacity)
                throw new OutOfMemoryException();

            int capacity = NextPowerOfTwo((int)needed);
            return capacity < 4 ? 4 : capacity;
        }

        public static int GrowHashCapacity(int currentCapacity)
        {
            if (currentCapacity == 0)
                return 4;

            if (currentCapacity >= MaxCapacity)
                throw new OutOfMemoryException();

            int doubled = currentCapacity > MaxCapacity / 2
                ? MaxCapacity
                : currentCapacity * 2;

            return NextPowerOfTwo(doubled);
        }

        public static int NextPowerOfTwo(int value)
        {
            if (value <= 0)
                return 0;

            int result = 1;
            while (result < value)
            {
                if (result >= MaxCapacity)
                    return MaxCapacity;

                result <<= 1;
            }

            return result;
        }
    }
}
