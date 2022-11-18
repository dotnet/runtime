// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a base class for frozen sets that store their values in a dedicated array.</summary>
    internal abstract class ItemsFrozenSet<T, TThisWrapper> : FrozenSetInternalBase<T, TThisWrapper>
        where TThisWrapper : struct, FrozenSetInternalBase<T, TThisWrapper>.IGenericSpecializedWrapper
    {
        private protected readonly FrozenHashTable _hashTable;
        private protected readonly T[] _items;

        protected ItemsFrozenSet(HashSet<T> source, IEqualityComparer<T> comparer) : base(comparer)
        {
            Debug.Assert(source.Count != 0);

            T[] entries = new T[source.Count];
            source.CopyTo(entries);

            _items = new T[entries.Length];

            _hashTable = FrozenHashTable.Create(
                entries,
                o => o is null ? 0 : comparer.GetHashCode(o),
                (index, item) => _items[index] = item);
        }

        /// <inheritdoc />
        private protected sealed override ImmutableArray<T> ItemsCore => new ImmutableArray<T>(_items);

        /// <inheritdoc />
        private protected sealed override Enumerator GetEnumeratorCore() => new Enumerator(_items);

        /// <inheritdoc />
        private protected sealed override int CountCore => _hashTable.Count;
    }
}
