using ECS.App.NativeStructures;
using ECS.App.NativeStructures.Sparse;
using System;
using System.Runtime.CompilerServices;

namespace ECS.App
{
    public unsafe struct DataFilter<T1, T2>
    where T1 : unmanaged where T2 : unmanaged
    {
        internal FilterState* _s;
        public long Count => _s->Matches.Length;
        public ref T1 GetFirst(long i) => ref _s->Set1->GetRef<T1>(_s->Matches[i]);
        public ref T2 GetSecond(long i) => ref _s->Set2->GetRef<T2>(_s->Matches[i]);
    }
}