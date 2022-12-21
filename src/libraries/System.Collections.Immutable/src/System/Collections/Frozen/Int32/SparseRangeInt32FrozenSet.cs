// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the value is an <see cref="int"/>, the default comparer is used, and the items are not in a contiguous range.</summary>
    /// <remarks>
    /// No hashing here, just a direct lookup into a bit vector.
    /// </remarks>
    internal sealed class SparseRangeInt32FrozenSet : FrozenSetInternalBase<int, SparseRangeInt32FrozenSet.GSW>
    {
        private readonly int[] _items;
        private readonly ulong[] _bits;
        private readonly int _min;
        private readonly uint _numBits;

        // assumes the items are in sorted order
        internal SparseRangeInt32FrozenSet(int[] entries)
            : base(EqualityComparer<int>.Default)
        {
            _items = entries;
            _min = _items[0];
            int max = _items[_items.Length - 1];
            _numBits = (uint)(max - _min + 1);

            _bits = new ulong[(_numBits + 63) / 64];
            for (int i = 0; i < entries.Length; i++)
            {
                uint bit = (uint)(entries[i] - _min);
                uint index = bit / 64;
                ulong mask = 1UL << (int)(bit % 64);

                _bits[index] |= mask;
            }
        }

        private protected override int[] ItemsCore => _items;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
        private protected override int CountCore => _items.Length;

        private protected override int FindItemIndex(int item)
        {
            uint bit = (uint)(item - _min);
            if (bit < _numBits)
            {
                uint index = bit / 64;
                ulong mask = 1UL << (int)(bit % 64);

                if ((_bits[index] & mask) != 0UL)
                {
                    return (int)bit;
                }
            }

            return -1;
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            private SparseRangeInt32FrozenSet _set;
            public void Store(FrozenSet<int> set) => _set = (SparseRangeInt32FrozenSet)set;

            public int Count => _set.Count;
            public IEqualityComparer<int> Comparer => _set.Comparer;
            public int FindItemIndex(int item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
