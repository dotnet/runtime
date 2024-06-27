// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace System.Collections.Frozen
{
    /// <summary>Provides a <see cref="FrozenSet{T}"/> implementation to use with small item counts.</summary>
    /// <typeparam name="T">The type of the items in the set.</typeparam>
    /// <remarks>
    /// No hashing here, just a straight-up linear scan through the items.
    /// </remarks>
    internal sealed partial class SmallFrozenSet<T> : FrozenSetInternalBase<T, SmallFrozenSet<T>.GSW>
    {
        private readonly T[] _items;

        internal SmallFrozenSet(HashSet<T> source) : base(source.Comparer)
        {
            _items = source.ToArray();
        }

        private protected override T[] ItemsCore => _items;
        private protected override int CountCore => _items.Length;

        private protected override int FindItemIndex(T item)
        {
            T[] items = _items;
            for (int i = 0; i < items.Length; i++)
            {
                if (Comparer.Equals(item, items[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);

        internal struct GSW : IGenericSpecializedWrapper
        {
            private SmallFrozenSet<T> _set;
            public void Store(FrozenSet<T> set) => _set = (SmallFrozenSet<T>)set;

            public int Count => _set.Count;
            public IEqualityComparer<T> Comparer => _set.Comparer;
            public int FindItemIndex(T item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
