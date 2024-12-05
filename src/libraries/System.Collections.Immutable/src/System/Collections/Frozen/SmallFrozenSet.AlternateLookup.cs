// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed partial class SmallFrozenSet<T>
    {
        /// <inheritdoc/>
        private protected override AlternateLookupDelegate<TAlternateKey> GetAlternateLookupDelegate<TAlternateKey>()
            => AlternateLookupDelegateHolder<TAlternateKey>.Instance;

        private static class AlternateLookupDelegateHolder<TAlternateKey>
#if NET9_0_OR_GREATER
#pragma warning disable SA1001 // Commas should be spaced TAlternate
            where TAlternateKey : allows ref struct
#pragma warning restore SA1001
#endif
        {
            /// <summary>
            /// Invokes <see cref="FindItemIndexAlternate{TAlternate}(TAlternate)"/>
            /// on instances known to be of type <see cref="SmallFrozenSet{T}"/>.
            /// </summary>
            public static readonly AlternateLookupDelegate<TAlternateKey> Instance = (set, item)
                => ((SmallFrozenSet<T>)set).FindItemIndexAlternate(item);
        }

        /// <inheritdoc cref="FindItemIndex(T)" />
        private int FindItemIndexAlternate<TAlternate>(TAlternate item)
#if NET9_0_OR_GREATER
#pragma warning disable SA1001 // Commas should be spaced TAlternate
            where TAlternate : allows ref struct
#pragma warning restore SA1001
#endif
        {
            IAlternateEqualityComparer<TAlternate, T> comparer = GetAlternateEqualityComparer<TAlternate>();

            T[] items = _items;
            for (int i = 0; i < items.Length; i++)
            {
                if (comparer.Equals(item, items[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
