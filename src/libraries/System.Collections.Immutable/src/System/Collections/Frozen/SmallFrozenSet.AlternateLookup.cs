// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed partial class SmallFrozenSet<T>
    {
        /// <inheritdoc />
        private protected override int FindItemIndex<TAlternate>(TAlternate item)
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
