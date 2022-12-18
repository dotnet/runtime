// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the value is an <see cref="int"/> and the default comparer is used and the items are in a contiguous range.</summary>
    internal sealed class ClosedRangeInt32FrozenSet : FrozenSetInternalBase<int, ClosedRangeInt32FrozenSet.GSW>
    {
        private readonly int[] _items;
        private readonly int _min;

        internal ClosedRangeInt32FrozenSet(int[] entries)
            : base(EqualityComparer<int>.Default)
        {
            Debug.Assert(entries.Length != 0);

            _items = entries;
            _min = _items[0];
        }

        private protected override int[] ItemsCore => _items;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
        private protected override int CountCore => _items.Length;

        private protected override int FindItemIndex(int item)
        {
            if ((uint)(item - _min) < (uint)_items.Length)
            {
                return item - _min;
            }

            return -1;
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            private ClosedRangeInt32FrozenSet _set;
            public void Store(FrozenSet<int> set) => _set = (ClosedRangeInt32FrozenSet)set;

            public int Count => _set.Count;
            public IEqualityComparer<int> Comparer => _set.Comparer;
            public int FindItemIndex(int item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
