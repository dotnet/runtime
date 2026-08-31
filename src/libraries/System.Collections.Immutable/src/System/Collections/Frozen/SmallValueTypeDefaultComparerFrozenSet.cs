// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the item is a value type, the default comparer is used, and the item count is small.</summary>
    internal sealed class SmallValueTypeDefaultComparerFrozenSet<T> : FrozenSetInternalBase<T, SmallValueTypeDefaultComparerFrozenSet<T>.GSW>
    {
        private readonly T[] _items;

        internal SmallValueTypeDefaultComparerFrozenSet(HashSet<T> source) : base(EqualityComparer<T>.Default)
        {
            Debug.Assert(default(T) is not null);
            Debug.Assert(typeof(T).IsValueType);

            Debug.Assert(source.Count != 0);
            Debug.Assert(ReferenceEquals(source.Comparer, EqualityComparer<T>.Default));

            _items = source.ToArray();
        }

        private protected override T[] ItemsCore => _items;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
        private protected override int CountCore => _items.Length;

        private protected override int FindItemIndex(T item)
        {
            T[] items = _items;
            for (int i = 0; i < items.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(item, items[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            private SmallValueTypeDefaultComparerFrozenSet<T> _set;
            public void Store(FrozenSet<T> set) => _set = (SmallValueTypeDefaultComparerFrozenSet<T>)set;

            public int Count => _set.Count;
            public IEqualityComparer<T> Comparer => _set.Comparer;
            public int FindItemIndex(T item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
