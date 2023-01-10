// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the value is an integer, the default comparer is used, and the items are not in a contiguous range.</summary>
    /// <remarks>
    /// No hashing here, just a direct lookup into a bit vector.
    /// </remarks>
    internal sealed class SparseRangeIntegerFrozenSet<T> : FrozenSetInternalBase<T, SparseRangeIntegerFrozenSet<T>.GSW>
        where T : struct, IBinaryInteger<T>
    {
        private readonly T[] _items;
        private readonly ulong[] _bits;
        private readonly T _min;
        private readonly uint _numBits;

        // assumes the items are in sorted order
        internal SparseRangeIntegerFrozenSet(T[] entries)
            : base(EqualityComparer<T>.Default)
        {
            _items = entries;
            _min = _items[0];
            T max = _items[^1];
            _numBits = (uint)(ulong.CreateTruncating(max - _min) + 1);

            _bits = new ulong[(_numBits + 63) / 64];
            for (int i = 0; i < entries.Length; i++)
            {
                uint bit = uint.CreateTruncating(entries[i] - _min);
                uint index = bit / 64;
                ulong mask = 1UL << (int)(bit % 64);

                _bits[index] |= mask;
            }
        }

        private protected override T[] ItemsCore => _items;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
        private protected override int CountCore => _items.Length;

        private protected override int FindItemIndex(T item)
        {
            uint bit = uint.CreateTruncating(item - _min);
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
            private SparseRangeIntegerFrozenSet<T> _set;
            public void Store(FrozenSet<T> set) => _set = (SparseRangeIntegerFrozenSet<T>)set;

            public int Count => _set.Count;
            public IEqualityComparer<T> Comparer => _set.Comparer;
            public int FindItemIndex(T item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
