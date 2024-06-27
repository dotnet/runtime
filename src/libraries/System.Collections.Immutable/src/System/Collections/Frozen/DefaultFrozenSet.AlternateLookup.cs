// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed partial class DefaultFrozenSet<T>
    {
        /// <inheritdoc />
        private protected override int FindItemIndex<TAlternate>(TAlternate item)
        {
            IAlternateEqualityComparer<TAlternate, T> comparer = GetAlternateEqualityComparer<TAlternate>();

            int hashCode = item is null ? 0 : comparer.GetHashCode(item);
            _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (hashCode == _hashTable.HashCodes[index] && comparer.Equals(item, _items[index]))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
    }
}
