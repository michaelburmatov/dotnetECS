using ECS.App.NativeStructures.Sparse;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.App
{
    internal unsafe struct FilterState : IDisposable
    {
        public SparseSet* Set1;
        public SparseSet* Set2;
        public IntPtr H1, H2;
        public NativeList<uint> Matches;
        public SparseSet Position;
        public int Version;
        public bool Disposed;

        public FilterState(SparseSet* set1, SparseSet* set2, IntPtr h1, IntPtr h2)
        {
            Set1 = set1; Set2 = set2;
            H1 = h1; H2 = h2;
            Matches = new NativeList<uint>(16);
            Position = new SparseSet(sizeof(uint), 16);
            Version = 0;
            Disposed = false;
            Rebuild();
        }

        public long Count => Matches.Length;

        public uint EntityAt(long matchIndex) => Matches[matchIndex];

        public void Rebuild()
        {
            Matches.Length = 0;
            Position.Clear();

            if (Set1->DataLength <= Set2->DataLength)
            {
                ref var dense = ref Set1->Dense;
                long n = dense.Length;
                for (long i = 0; i < n; i++)
                {
                    uint e = dense[i];
                    if (Set2->Has(e)) Append(e);
                }
            }
            else
            {
                ref var dense = ref Set2->Dense;
                long n = dense.Length;
                for (long i = 0; i < n; i++)
                {
                    uint e = dense[i];
                    if (Set1->Has(e)) Append(e);
                }
            }
            Version++;
        }

        public void OnAdded(IntPtr h, uint e)
        {
            if (h != H1 && h != H2) return;
            bool hasOther = h == H1 ? Set2->Has(e) : Set1->Has(e);
            if (hasOther) TryAddMatch(e);
        }

        public void OnRemoved(IntPtr h, uint e)
        {
            if (h != H1 && h != H2) return;
            RemoveMatch(e);
        }

        private void Append(uint e)
        {
            Position.Add<uint>(e, (uint)Matches.Length);
            Matches.Add(e);
        }

        private void TryAddMatch(uint e)
        {
            if (Position.Has(e)) return;
            Append(e);
            Version++;
        }

        private void RemoveMatch(uint e)
        {
            if (!Position.TryGet<uint>(e, out uint index)) return;

            long last = Matches.Length - 1;
            if (index != last)
            {
                uint moved = Matches[last];
                Matches[index] = moved;
                Position.Add<uint>(moved, index);
            }
            Matches.Length = last;
            Position.Remove(e);
            Version++;
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Matches.Dispose();
            Position.Dispose();
            Version++;
        }
    }
}
