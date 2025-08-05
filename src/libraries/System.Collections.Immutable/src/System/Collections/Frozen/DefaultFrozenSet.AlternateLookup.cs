// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed partial class DefaultFrozenSet<T>
    {
        /// <inheritdoc/>
        private protected override AlternateLookupDelegate<TAlternate> GetAlternateLookupDelegate<TAlternate>()
            => AlternateLookupDelegateHolder<TAlternate>.Instance;

        private static class AlternateLookupDelegateHolder<TAlternate>
#if NET9_0_OR_GREATER
#pragma warning disable SA1001 // Commas should be spaced correctly
            where TAlternate : allows ref struct
#pragma warning restore SA1001
#endif
        {
            /// <summary>
            /// Invokes <see cref="FindItemIndexAlternate{TAlternate}(TAlternate)"/>
            /// on instances known to be of type <see cref="DefaultFrozenSet{TValue}"/>.
            /// </summary>
            public static readonly AlternateLookupDelegate<TAlternate> Instance = (set, item)
                => ((DefaultFrozenSet<T>)set).FindItemIndexAlternate(item);
        }

        /// <inheritdoc cref="FindItemIndex(T)" />
        private int FindItemIndexAlternate<TAlternate>(TAlternate item)
#if NET9_0_OR_GREATER
#pragma warning disable SA1001 // Commas should be spaced correctly
            where TAlternate : allows ref struct
#pragma warning restore SA1001
#endif
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
