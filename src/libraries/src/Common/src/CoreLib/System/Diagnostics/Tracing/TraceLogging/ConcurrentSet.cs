// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Interlocked = System.Threading.Interlocked;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: A very simple lock-free add-only dictionary.
    /// Warning: this is a copy-by-value type. Copying performs a snapshot.
    /// Accessing a readonly field always makes a copy of the field, so the
    /// GetOrAdd method will not work as expected if called on a readonly field.
    /// </summary>
    /// <typeparam name="KeyType">
    /// The type of the key, used for TryGet.
    /// </typeparam>
    /// <typeparam name="ItemType">
    /// The type of the item, used for GetOrAdd.
    /// </typeparam>
    internal struct ConcurrentSet<KeyType, ItemType>
        where ItemType : ConcurrentSetItem<KeyType, ItemType>
    {
        private ItemType[]? items;

        public ItemType? TryGet(KeyType key)
        {
            ItemType? item;
            var oldItems = this.items;

            if (oldItems != null)
            {
                var lo = 0;
                var hi = oldItems.Length;
                do
                {
                    int i = (lo + hi) / 2;
                    item = oldItems[i];

                    int cmp = item.Compare(key);
                    if (cmp == 0)
                    {
                        goto Done;
                    }
                    else if (cmp < 0)
                    {
                        lo = i + 1;
                    }
                    else
                    {
                        hi = i;
                    }
                }
                while (lo != hi);
            }

            item = null;

        Done:

            return item;
        }

        public ItemType GetOrAdd(ItemType newItem)
        {
            ItemType item;
            var oldItems = this.items;
            ItemType[] newItems;

        Retry:

            if (oldItems == null)
            {
                newItems = new ItemType[] { newItem };
            }
            else
            {
                var lo = 0;
                var hi = oldItems.Length;
                do
                {
                    int i = (lo + hi) / 2;
                    item = oldItems[i];

                    int cmp = item.Compare(newItem);
                    if (cmp == 0)
                    {
                        goto Done;
                    }
                    else if (cmp < 0)
                    {
                        lo = i + 1;
                    }
                    else
                    {
                        hi = i;
                    }
                }
                while (lo != hi);

                int oldLength = oldItems.Length;
                newItems = new ItemType[oldLength + 1];
                Array.Copy(oldItems, 0, newItems, 0, lo);
                newItems[lo] = newItem;
                Array.Copy(oldItems, lo, newItems, lo + 1, oldLength - lo);
            }

            newItems = Interlocked.CompareExchange(ref this.items, newItems, oldItems)!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/34901
            if (oldItems != newItems)
            {
                oldItems = newItems;
                goto Retry;
            }

            item = newItem;

        Done:

            return item;
        }
    }
}
