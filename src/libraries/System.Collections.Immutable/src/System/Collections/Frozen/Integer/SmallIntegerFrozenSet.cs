// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the value is an integer, the default comparer is used, and the item count is small.</summary>
    /// <remarks>
    /// No hashing here, just a straight-up linear scan through the items.
    /// </remarks>
    internal sealed class SmallIntegerFrozenSet<T> : FrozenSetInternalBase<T, SmallIntegerFrozenSet<T>.GSW>
        where T : struct, IBinaryInteger<T>
    {
        private readonly T[] _items;
        private readonly T _max;

        // this assumes the entries are sorted
        internal SmallIntegerFrozenSet(T[] entries)
            : base(EqualityComparer<T>.Default)
        {
            Debug.Assert(entries.Length != 0);

            _items = entries;
            _max = _items[^1];
        }

        private protected override T[] ItemsCore => _items;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
        private protected override int CountCore => _items.Length;

        private protected override int FindItemIndex(T item)
        {
            if (item <= _max)
            {
                T[] items = _items;
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
            private SmallIntegerFrozenSet<T> _set;
            public void Store(FrozenSet<T> set) => _set = (SmallIntegerFrozenSet<T>)set;

            public int Count => _set.Count;
            public IEqualityComparer<T> Comparer => _set.Comparer;
            public int FindItemIndex(T item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
