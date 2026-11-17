using ECS.App.NativeStructures;
using ECS.App.NativeStructures.Sparse;
using System;
using System.Runtime.CompilerServices;

namespace ECS.App
{
    /// <summary>
    /// Фильтр «есть и T1, и T2». Список совпадений МАТЕРИАЛИЗОВАН:
    /// он строится один раз при создании фильтра и дальше поддерживается
    /// инкрементально через уведомления менеджера о добавлении/удалении компонентов.
    ///
    /// Сложность:
    ///   GetCount()            — O(1)
    ///   GetFirst(i)/GetSecond(i) — O(1)
    ///   GetEntityId(i)        — O(1)
    ///   полный обход          — O(k), k = число совпадений (без проверок Has)
    ///   AddComponent/RemoveComponent — O(1) на затронутый фильтр (амортизированно)
    ///   создание фильтра      — O(min(n, m)) однократно
    ///
    /// Это struct-хэндл: все копии разделяют одно состояние (как RefNativeDictionary).
    /// Кэшем владеет ManagerECS и освобождает его в Dispose.
    ///
    /// ВАЖНО: matchIndex — позиция в плотном списке совпадений. Он стабилен только
    /// до следующего структурного изменения (удаление делает swap-back).
    /// </summary>
    public unsafe struct DataFilter<T1, T2>
        where T1 : unmanaged
        where T2 : unmanaged
    {

        private readonly SparseSet<T1>* _set1;
        private readonly SparseSet<T2>* _set2;
        private readonly IntPtr _h1;
        private readonly IntPtr _h2;

        private NativeList<uint>* _matches;   // плотный список entityId с обоими компонентами
        private SparseSet<uint>* _position;   // entityId -> индекс в _matches (O(1) удаление)
        private bool _disposed;

        /// <summary>Счётчик структурных изменений (для DEBUG-проверки итерации).</summary>
        public int Version;

        public DataFilter(SparseSet<T1>* set1, SparseSet<T2>* set2, IntPtr h1, IntPtr h2)
        {
            _set1 = set1;
            _set2 = set2;
            _h1 = h1;
            _h2 = h2;
            _matches = (NativeList<uint>*)NativeMemory.Package(new NativeList<uint>(16));
            _position = (SparseSet<uint>*)NativeMemory.Package(new SparseSet<uint>(16));
            _disposed = false;
            Version = 0;
            Rebuild();
        }

        public long Count => _matches->Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint EntityAt(long matchIndex) => _matches[0][matchIndex];

        // O(1): одна загрузка entityId + sparse-lookup (Sparse[e] -> Data[idx-1])
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T1 GetFirst(long matchIndex) => ref _set1->Get(_matches[0][matchIndex]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T2 GetSecond(long matchIndex) => ref _set2->Get(_matches[0][matchIndex]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T1 FirstByEntity(uint entityId) => ref _set1->Get(entityId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T2 SecondByEntity(uint entityId) => ref _set2->Get(entityId);

        /// <summary>
        /// Полная перестройка кэша. O(min(n, m)). Вызывается при создании фильтра
        /// (и может быть вызвана вручную как «аварийное» восстановление инварианта).
        /// </summary>
        public void Rebuild()
        {
            _matches->Length = 0;
            _position->Clear();

            if (_set1->Data.Length <= _set2->Data.Length)
            {
                ref var dense = ref _set1->Dense;
                for (long i = 0; i < dense.Length; i++)
                {
                    uint e = dense[i];
                    if (_set2->Has(e))
                        Append(e);
                }
            }
            else
            {
                ref var dense = ref _set2->Dense;
                for (long i = 0; i < dense.Length; i++)
                {
                    uint e = dense[i];
                    if (_set1->Has(e))
                        Append(e);
                }
            }

            Version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Append(uint entityId)
        {
            _position->Add(entityId, (uint)_matches->Length);
            _matches->Add(entityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryAddMatch(uint entityId)
        {
            if (_position->Has(entityId))
                return;

            Append(entityId);
            Version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveMatch(uint entityId)
        {
            if (!_position->TryGet(entityId, out uint index))
                return;

            long last = _matches->Length - 1;
            if (index != last)
            {
                uint moved = _matches[0][last];
                _matches[0][index] = moved;
                _position->Add(moved, index);   // upsert: правим позицию переехавшей сущности
            }

            _matches->Length = last;
            _position->Remove(entityId);
            Version++;
        }

        public void OnComponentAdded(IntPtr typeHandle, uint entityId)
        {
            // добавили один из двух компонентов — совпадение есть, если второй уже на месте
            bool hasOther = typeHandle == _h1 ? _set2->Has(entityId) : _set1->Has(entityId);
            if (hasOther)
                TryAddMatch(entityId);
        }

        public void OnComponentRemoved(IntPtr typeHandle, uint entityId)
        {
            // любого из двух компонентов больше нет — совпадение обязано исчезнуть
            RemoveMatch(entityId);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _matches->Dispose();
            NativeMemory.Free(_matches);
            _position->Dispose();
            NativeMemory.Free(_position);
            Version++;

        }
    }
}