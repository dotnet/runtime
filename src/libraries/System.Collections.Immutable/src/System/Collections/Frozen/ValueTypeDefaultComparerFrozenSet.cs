// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set optimized for value types using the default comparer.</summary>
    /// <typeparam name="T">The type of values in the dictionary.</typeparam>
    internal sealed class ValueTypeDefaultComparerFrozenSet<T> : ItemsFrozenSet<T, ValueTypeDefaultComparerFrozenSet<T>.GSW>
    {
        internal ValueTypeDefaultComparerFrozenSet(HashSet<T> source) :
            base(source, EqualityComparer<T>.Default)
        {
            Debug.Assert(typeof(T).IsValueType);
        }

        /// <inheritdoc />
        private protected override int FindItemIndex(T item)
        {
            int hashCode = EqualityComparer<T>.Default.GetHashCode(item!);
            _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (hashCode == _hashTable.HashCodes[index])
                {
                    if (EqualityComparer<T>.Default.Equals(item, _items[index]))
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
            private ValueTypeDefaultComparerFrozenSet<T> _set;
            public void Store(FrozenSet<T> set) => _set = (ValueTypeDefaultComparerFrozenSet<T>)set;

            public int Count => _set.Count;
            public IEqualityComparer<T> Comparer => _set.Comparer;
            public int FindItemIndex(T item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
