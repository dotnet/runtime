// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the value is an <see cref="int"/>, the default comparer is used, and the item count is small.</summary>
    /// <remarks>
    /// No hashing here, just a straight-up linear scan through the items.
    /// </remarks>
    internal sealed class SmallInt32FrozenSet : FrozenSetInternalBase<int, SmallInt32FrozenSet.GSW>
    {
        private readonly int[] _items;
        private readonly int _max;

        internal SmallInt32FrozenSet(HashSet<int> source) : base(EqualityComparer<int>.Default)
        {
            Debug.Assert(source.Count != 0);
            Debug.Assert(ReferenceEquals(source.Comparer, EqualityComparer<int>.Default));

            int[] items = source.ToArray();
            Array.Sort(items);

            _items = items;
            _max = _items[_items.Length - 1];
        }

        private protected override int[] ItemsCore => _items;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
        private protected override int CountCore => _items.Length;

        private protected override int FindItemIndex(int item)
        {
            if (item <= _max)
            {
                int[] items = _items;
                for (int i = 0; i < items.Length; i++)
                {
                    if (item <= items[i])
                    {
                        if (item < items[i])
                        {
                            break;
                        }

                        return i;
                    }
                }
            }

            return -1;
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            private SmallInt32FrozenSet _set;
            public void Store(FrozenSet<int> set) => _set = (SmallInt32FrozenSet)set;

            public int Count => _set.Count;
            public IEqualityComparer<int> Comparer => _set.Comparer;
            public int FindItemIndex(int item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
