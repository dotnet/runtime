// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the item is a value type, the default comparer is used, and the item count is small.</summary>
    /// <remarks>
    /// While not constrained in this manner, the <typeparamref name="T"/> must be an <see cref="IComparable{T}"/>.
    /// This implementation is only used for a set of types that have a known-good <see cref="IComparable{T}"/> implementation; it's not
    /// used for an <see cref="IComparable{T}"/> as we can't know for sure whether it's valid, e.g. if the T is a ValueTuple`2, it itself
    /// is comparable, but its items might not be such that trying to compare it will result in exception.
    /// </remarks>
    internal sealed class SmallValueTypeComparableFrozenSet<T> : FrozenSetInternalBase<T, SmallValueTypeComparableFrozenSet<T>.GSW>
    {
        private readonly T[] _items;
        private readonly T _max;

        internal SmallValueTypeComparableFrozenSet(HashSet<T> source) : base(EqualityComparer<T>.Default)
        {
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
            private SmallValueTypeComparableFrozenSet<T> _set;
            public void Store(FrozenSet<T> set) => _set = (SmallValueTypeComparableFrozenSet<T>)set;

            public int Count => _set.Count;
            public IEqualityComparer<T> Comparer => _set.Comparer;
            public int FindItemIndex(T item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
