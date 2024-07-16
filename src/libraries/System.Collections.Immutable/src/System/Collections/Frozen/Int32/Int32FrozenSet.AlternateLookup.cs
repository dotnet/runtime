// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    internal sealed partial class Int32FrozenSet
    {
        /// <inheritdoc />
        private protected override int FindItemIndex<TAlternate>(TAlternate item)
        {
            var comparer = GetAlternateEqualityComparer<TAlternate>();

            _hashTable.FindMatchingEntries(comparer.GetHashCode(item), out int index, out int endIndex);

            int[] hashCodes = _hashTable.HashCodes;
            while (index <= endIndex)
            {
                if (comparer.Equals(item, hashCodes[index]))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
    }
}
