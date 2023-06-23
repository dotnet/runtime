// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a base class for frozen sets that store their values in a dedicated array.</summary>
    internal abstract class ItemsFrozenSet<T, TThisWrapper> : FrozenSetInternalBase<T, TThisWrapper>
        where TThisWrapper : struct, FrozenSetInternalBase<T, TThisWrapper>.IGenericSpecializedWrapper
    {
        private protected readonly FrozenHashTable _hashTable;
        private protected readonly T[] _items;

        protected ItemsFrozenSet(HashSet<T> source) : base(source.Comparer)
        {
            Debug.Assert(source.Count != 0);

            T[] entries = new T[source.Count];
            source.CopyTo(entries);

            _items = new T[entries.Length];

            int[] arrayPoolHashCodes = ArrayPool<int>.Shared.Rent(entries.Length);
            Span<int> hashCodes = arrayPoolHashCodes.AsSpan(0, entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                hashCodes[i] = entries[i] is T t ? Comparer.GetHashCode(t) : 0;
            }

            _hashTable = FrozenHashTable.Create(hashCodes);

            for (int srcIndex = 0; srcIndex < hashCodes.Length; srcIndex++)
            {
                int destIndex = hashCodes[srcIndex];

                _items[destIndex] = entries[srcIndex];
            }

            ArrayPool<int>.Shared.Return(arrayPoolHashCodes);
        }

        /// <inheritdoc />
        private protected sealed override T[] ItemsCore => _items;

        /// <inheritdoc />
        private protected sealed override Enumerator GetEnumeratorCore() => new Enumerator(_items);

        /// <inheritdoc />
        private protected sealed override int CountCore => _hashTable.Count;
    }
}
