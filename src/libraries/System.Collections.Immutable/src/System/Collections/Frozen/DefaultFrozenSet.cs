// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    /// <summary>Provides the default <see cref="FrozenSet{T}"/> implementation to use when no other special-cases apply.</summary>
    /// <typeparam name="T">The type of the values in the set.</typeparam>
    internal sealed partial class DefaultFrozenSet<T> : ItemsFrozenSet<T, DefaultFrozenSet<T>.GSW>
    {
        internal DefaultFrozenSet(HashSet<T> source)
            : base(source)
        {
        }

        /// <inheritdoc />
        private protected override int FindItemIndex(T item)
        {
            IEqualityComparer<T> comparer = Comparer;

            int hashCode = item is null ? 0 : comparer.GetHashCode(item);
            _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (hashCode == _hashTable.HashCodes[index])
                {
                    if (comparer.Equals(item, _items[index]))
                    {
                        return index;
                    }
                }

                index++;
            }

            return -1;
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            private DefaultFrozenSet<T> _set;
            public void Store(FrozenSet<T> set) => _set = (DefaultFrozenSet<T>)set;

            public int Count => _set.Count;
            public IEqualityComparer<T> Comparer => _set.Comparer;
            public int FindItemIndex(T item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
