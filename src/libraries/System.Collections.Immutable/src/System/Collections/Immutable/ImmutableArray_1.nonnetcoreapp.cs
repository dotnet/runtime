// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    public readonly partial struct ImmutableArray<T> : IReadOnlyList<T>, IList<T>, IEquatable<ImmutableArray<T>>, IList, IImmutableArray, IStructuralComparable, IStructuralEquatable, IImmutableList<T>
    {
        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="items">The items to remove if matches are found in this list.</param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// </param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        public ImmutableArray<T> RemoveRange(ReadOnlySpan<T> items, IEqualityComparer<T>? equalityComparer = null)
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();

            if (items.IsEmpty || self.IsEmpty)
            {
                return self;
            }

            if (items.Length == 1)
            {
                return self.Remove(items[0], equalityComparer);
            }

#nullable disable
            var itemsMultiSet = new Dictionary<T, int>(equalityComparer);
#nullable restore
            int nullValueCount = 0;
            foreach (ref readonly T item in items)
            {
                if (item == null)
                {
                    nullValueCount++;
                }
                else if (itemsMultiSet.TryGetValue(item, out int count))
                {
                    itemsMultiSet[item] = count + 1;
                }
                else
                {
                    itemsMultiSet[item] = 1;
                }
            }

            List<int>? indicesToRemove = null;
            T[] selfArray = self.array!;
            for (int i = 0; i < selfArray.Length; i++)
            {
                bool found = false;
                if (selfArray[i] == null)
                {
                    if (nullValueCount > 0)
                    {
                        found = true;
                        nullValueCount--;
                    }
                }
                else if (itemsMultiSet.TryGetValue(selfArray[i], out int count))
                {
                    found = true;
                    if (count == 1)
                    {
                        itemsMultiSet.Remove(selfArray[i]);
                    }
                    else
                    {
                        Debug.Assert(count > 1);
                        itemsMultiSet[selfArray[i]] = count - 1;
                    }
                }

                if (found)
                {
                    indicesToRemove ??= new();
                    indicesToRemove.Add(i);
                }
            }

            return indicesToRemove == null ? self : self.RemoveAtRange(indicesToRemove);
        }
    }
}
