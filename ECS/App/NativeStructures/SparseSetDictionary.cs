using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ECS.App.NativeStructures
{
    public unsafe struct SparseSetDictionary : IDisposable
    {
        public NativeList<uint> Keys;
        public NativeList<uint> Values;

        public const uint EMPTY_KEY = uint.MaxValue;
        public const uint REMOVED_KEY = uint.MaxValue - 1;

        public long Capacity;
        public long Count;

        public SparseSetDictionary(long initialCapacity = 16)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            // Минимальная ёмкость — 16
            if (initialCapacity < 16) initialCapacity = 16;

            Capacity = initialCapacity;
            Count = 0;

            Keys = new NativeList<uint>(initialCapacity);
            Values = new NativeList<uint>(initialCapacity);

            Keys.Resize(initialCapacity);
            Values.Resize(initialCapacity);

            // Безопасное обнуление чанками для больших объёмов
            FillWithConstant(Keys.Buffer, EMPTY_KEY, initialCapacity);
            FillWithConstant(Values.Buffer, 0, initialCapacity);
        }

        /// <summary>
        /// Безопасное заполнение массива константой через чанки
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FillWithConstant(uint* buffer, uint value, long count)
        {
            if (count <= 0 || buffer == null) return;

            const long maxChunkSize = int.MaxValue;
            long remaining = count;
            uint* currentPtr = buffer;

            while (remaining > 0)
            {
                int chunkSize = (int)Math.Min(remaining, maxChunkSize);
                new Span<uint>((void*)currentPtr, chunkSize).Fill(value);

                currentPtr += chunkSize;
                remaining -= chunkSize;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long Hash(uint key, long capacity)
        {
            // Murmur-inspired hash для uint
            key ^= key >> 16;
            key *= 0x7feb352d;
            key ^= key >> 15;
            key *= 0x846ca68b;
            key ^= key >> 16;

            // Модуль для long capacity
            return (long)(key % (ulong)capacity);
        }

        // === ИНДЕКСАТОР ===
        public uint this[uint key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!TryGetValue(key, out uint value))
                    throw new KeyNotFoundException($"Key {key} not found in dictionary");
                return value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (!TryUpdate(key, value))
                {
                    Add(key, value);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(uint key, out uint value)
        {
            if (Count == 0)
            {
                value = 0;
                return false;
            }

            long index = Hash(key, Capacity);

            for (long i = 0; i < Capacity; i++)
            {
                long probeIndex = (index + i) % Capacity;
                uint currentKey = Keys[probeIndex];

                if (currentKey == key)
                {
                    value = Values[probeIndex];
                    return true;
                }

                if (currentKey == EMPTY_KEY)
                {
                    value = 0;
                    return false;
                }

                // REMOVED_KEY — продолжаем пробирование
            }

            value = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(uint key)
        {
            return TryGetValue(key, out _);
        }

        public bool TryUpdate(uint key, uint value)
        {
            if (Count == 0) return false;

            long index = Hash(key, Capacity);

            for (long i = 0; i < Capacity; i++)
            {
                long probeIndex = (index + i) % Capacity;
                uint currentKey = Keys[probeIndex];

                if (currentKey == key)
                {
                    Values[probeIndex] = value;
                    return true;
                }

                if (currentKey == EMPTY_KEY)
                    return false;
            }

            return false;
        }

        public void Add(uint key, uint value)
        {
            if (key == EMPTY_KEY || key == REMOVED_KEY)
                throw new ArgumentException($"Key cannot be {EMPTY_KEY} or {REMOVED_KEY}");

            // Проверка коэффициента загрузки (< 70%)
            // Используем double для избежания переполнения при очень больших числах
            if (Count * 10 >= Capacity * 7)
            {
                Resize(Capacity * 2);
            }

            long index = Hash(key, Capacity);

            for (long i = 0; i < Capacity; i++)
            {
                long probeIndex = (index + i) % Capacity;
                uint currentKey = Keys[probeIndex];

                if (currentKey == EMPTY_KEY || currentKey == REMOVED_KEY)
                {
                    Keys[probeIndex] = key;
                    Values[probeIndex] = value;
                    Count++;
                    return;
                }

                if (currentKey == key)
                {
                    Values[probeIndex] = value;
                    return;
                }
            }

            throw new InvalidOperationException("Dictionary is full despite resize check");
        }

        public bool Remove(uint key)
        {
            if (Count == 0) return false;

            long index = Hash(key, Capacity);

            for (long i = 0; i < Capacity; i++)
            {
                long probeIndex = (index + i) % Capacity;
                uint currentKey = Keys[probeIndex];

                if (currentKey == key)
                {
                    Keys[probeIndex] = REMOVED_KEY;
                    Values[probeIndex] = 0;
                    Count--;
                    return true;
                }

                if (currentKey == EMPTY_KEY)
                    return false;
            }

            return false;
        }

        private void Resize(long newCapacity)
        {
            if (newCapacity <= Capacity) return;

            var oldKeys = Keys;
            var oldValues = Values;
            long oldCapacity = Capacity;

            // Инициализируем новую таблицу
            Capacity = newCapacity;
            Count = 0;
            Keys = new NativeList<uint>(newCapacity);
            Values = new NativeList<uint>(newCapacity);

            Keys.Resize(newCapacity);
            Values.Resize(newCapacity);

            // Безопасное заполнение чанками
            FillWithConstant(Keys.Buffer, EMPTY_KEY, newCapacity);
            FillWithConstant(Values.Buffer, 0, newCapacity);

            // Переносим только валидные записи
            for (long i = 0; i < oldCapacity; i++)
            {
                uint key = oldKeys[i];
                if (key != EMPTY_KEY && key != REMOVED_KEY)
                {
                    Add(key, oldValues[i]);
                }
            }

            oldKeys.Dispose();
            oldValues.Dispose();
        }

        public void Clear()
        {
            if (Capacity > 0)
            {
                FillWithConstant(Keys.Buffer, EMPTY_KEY, Capacity);
                FillWithConstant(Values.Buffer, 0, Capacity);
            }
            Count = 0;
        }

        public void Dispose()
        {
            Keys.Dispose();
            Values.Dispose();
            Capacity = 0;
            Count = 0;
        }
    }
}