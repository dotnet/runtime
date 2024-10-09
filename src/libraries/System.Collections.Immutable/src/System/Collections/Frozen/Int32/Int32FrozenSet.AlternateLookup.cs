// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    internal sealed partial class Int32FrozenSet
    {

        private protected override AlternateLookupDelegate<TAlternate> GetAlternateLookupDelegate<TAlternate>()
            => AlternateKeyDelegateHolder<TAlternate>.Instance;

        private static class AlternateKeyDelegateHolder<TAlternate>
#if NET9_0_OR_GREATER
#pragma warning disable SA1001 // Commas should be spaced correctly
            where TAlternate : allows ref struct
#pragma warning restore SA1001
#endif
        {
            public static AlternateLookupDelegate<TAlternate> Instance = (set, item)
                => ((Int32FrozenSet)set).FindItemIndexAlternate(item);
        }

        private int FindItemIndexAlternate<TAlternate>(TAlternate item)
#if NET9_0_OR_GREATER
#pragma warning disable SA1001 // Commas should be spaced correctly
            where TAlternate : allows ref struct
#pragma warning restore SA1001
#endif
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
