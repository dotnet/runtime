// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the item is a comparable value type, the default comparer is used, and the item count is small.</summary>
    /// <remarks>
    /// No hashing involved, just a linear scan through the items.  This implementation is close in nature to that of <see cref="ValueTypeDefaultComparerFrozenSet{T}"/>,
    /// except that this implementation sorts the values in order to a) extract a max that it can compare against at the beginning of each match in order to
    /// immediately rule out values too large to be contained, and b) early-exits from the linear scan when a comparison determines the value is too
    /// small to be contained.
    /// </remarks>
    internal sealed class SmallComparableValueTypeFrozenSet<T> : FrozenSetInternalBase<T, SmallComparableValueTypeFrozenSet<T>.GSW>
    {
        private readonly T[] _items;
        private readonly T _max;

        internal SmallComparableValueTypeFrozenSet(HashSet<T> source) : base(EqualityComparer<T>.Default)
        {
            // T is logically constrained to `where T : struct, IComparable<T>`, but we can't actually write that
            // constraint currently and still have this be used from the calling context that has an unconstrained T.
            // So, we assert it here instead. The implementation relies on {Equality}Comparer<T>.Default to sort things out.
            Debug.Assert(default(T) is IComparable<T>);
            Debug.Assert(default(T) is not null);
            Debug.Assert(typeof(T).IsValueType);

            Debug.Assert(source.Count != 0);
            Debug.Assert(ReferenceEquals(source.Comparer, EqualityComparer<T>.Default));

            _items = source.ToArray();
            Array.Sort(_items);

            _max = _items[_items.Length - 1];
        }

        private protected override T[] ItemsCore => _items;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
        private protected override int CountCore => _items.Length;

        private protected override int FindItemIndex(T item)
        {
            if (Comparer<T>.Default.Compare(item, _max) <= 0)
            {
                T[] items = _items;
                for (int i = 0; i < items.Length; i++)
                {
                    int c = Comparer<T>.Default.Compare(item, items[i]);
                    if (c <= 0)
                    {
                        if (c == 0)
                        {
                            return i;
                        }

                        break;
                    }
                }
            }

            return -1;
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            private SmallComparableValueTypeFrozenSet<T> _set;
            public void Store(FrozenSet<T> set) => _set = (SmallComparableValueTypeFrozenSet<T>)set;

            public int Count => _set.Count;
            public IEqualityComparer<T> Comparer => _set.Comparer;
            public int FindItemIndex(T item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
