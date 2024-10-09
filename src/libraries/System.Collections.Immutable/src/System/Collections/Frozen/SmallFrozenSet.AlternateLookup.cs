// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed partial class SmallFrozenSet<T>
    {

        private protected override AlternateLookupDelegate<TAlternateKey> GetAlternateLookupDelegate<TAlternateKey>()
            => AlternateKeyDelegateHolder<TAlternateKey>.Instance;

        private static class AlternateKeyDelegateHolder<TAlternateKey>
#if NET9_0_OR_GREATER
#pragma warning disable SA1001 // Commas should be spaced TAlternate
            where TAlternateKey : allows ref struct
#pragma warning restore SA1001
#endif
        {
            public static AlternateLookupDelegate<TAlternateKey> Instance = (set, item)
                => ((SmallFrozenSet<T>)set).FindItemIndexAlternate(item);
        }

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
