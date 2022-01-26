// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Collections.Immutable
{
    public readonly partial struct ImmutableArray<T> : IReadOnlyList<T>, IList<T>, IEquatable<ImmutableArray<T>>, IList, IImmutableArray, IStructuralComparable, IStructuralEquatable, IImmutableList<T>
    {
        /// <summary>
        /// Creates a <see cref="ReadOnlySpan{T}"/> over the portion of current <see cref="ImmutableArray{T}"/> based on specified <paramref name="range"/>
        /// </summary>
        /// <param name="range">Range in current <see cref="ImmutableArray{T}"/>.</param>
        /// <returns>The <see cref="ReadOnlySpan{T}"/> representation of the <see cref="ImmutableArray{T}"/></returns>
        public ReadOnlySpan<T> AsSpan(Range range)
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();

            (int start, int length) = range.GetOffsetAndLength(self.Length);
            return new ReadOnlySpan<T>(self.array, start, length);
        }

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
                else
                {
#nullable disable
                    ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(itemsMultiSet, item, out _);
#nullable restore
                    count++;
                }
            }

            List<int>? indicesToRemove = null;
            T[] selfArray = self.array!;
            for (int i = 0; i < selfArray.Length; i++)
            {
                if (selfArray[i] == null)
                {
                    if (nullValueCount == 0)
                    {
                        continue;
                    }
                    nullValueCount--;
                }
                else
                {
#nullable disable
                    ref int count = ref CollectionsMarshal.GetValueRefOrNullRef(itemsMultiSet, selfArray[i]);
#nullable restore
                    if (Unsafe.IsNullRef(ref count) || count == 0)
                    {
                        continue;
                    }
                    count--;
                }

                indicesToRemove ??= new();
                indicesToRemove.Add(i);
            }

            return indicesToRemove == null ? self : self.RemoveAtRange(indicesToRemove);
        }
    }
}
