// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        internal abstract partial class Iterator<TSource>
        {
            /// <summary>
            /// Produce an array of the sequence through an optimized path.
            /// </summary>
            /// <returns>The array.</returns>
            public abstract TSource[] ToArray();

            /// <summary>
            /// Produce a <see cref="List{TSource}"/> of the sequence through an optimized path.
            /// </summary>
            /// <returns>The <see cref="List{TSource}"/>.</returns>
            public abstract List<TSource> ToList();

            /// <summary>
            /// Returns the count of elements in the sequence.
            /// </summary>
            /// <param name="onlyIfCheap">If true then the count should only be calculated if doing
            /// so is quick (sure or likely to be constant time), otherwise -1 should be returned.</param>
            /// <returns>The number of elements.</returns>
            public abstract int GetCount(bool onlyIfCheap);

            /// <summary>
            /// Creates a new iterator that skips the specified number of elements from this sequence.
            /// </summary>
            /// <param name="count">The number of elements to skip.</param>
            /// <returns>An <see cref="Iterator{TSource}"/> with the first <paramref name="count"/> items removed, or null if known empty.</returns>
            public virtual Iterator<TSource>? Skip(int count) => new IEnumerableSkipTakeIterator<TSource>(this, count, -1);

            /// <summary>
            /// Creates a new iterator that takes the specified number of elements from this sequence.
            /// </summary>
            /// <param name="count">The number of elements to take.</param>
            /// <returns>An <see cref="Iterator{TSource}"/> with only the first <paramref name="count"/> items, or null if known empty.</returns>
            public virtual Iterator<TSource>? Take(int count) => new IEnumerableSkipTakeIterator<TSource>(this, 0, count - 1);

            /// <summary>
            /// Gets the item associated with a 0-based index in this sequence.
            /// </summary>
            /// <param name="index">The 0-based index to access.</param>
            /// <param name="found"><c>true</c> if the sequence contains an element at that index, <c>false</c> otherwise.</param>
            /// <returns>The element if <paramref name="found"/> is <c>true</c>, otherwise, the default value of <typeparamref name="TSource"/>.</returns>
            public virtual TSource? TryGetElementAt(int index, out bool found) =>
                index == 0 ? TryGetFirst(out found) :
                TryGetElementAtNonIterator(this, index, out found);

            /// <summary>
            /// Gets the first item in this sequence.
            /// </summary>
            /// <param name="found"><c>true</c> if the sequence contains an element, <c>false</c> otherwise.</param>
            /// <returns>The element if <paramref name="found"/> is <c>true</c>, otherwise, the default value of <typeparamref name="TSource"/>.</returns>
            public virtual TSource? TryGetFirst(out bool found) => TryGetFirstNonIterator(this, out found);

            /// <summary>
            /// Gets the last item in this sequence.
            /// </summary>
            /// <param name="found"><c>true</c> if the sequence contains an element, <c>false</c> otherwise.</param>
            /// <returns>The element if <paramref name="found"/> is <c>true</c>, otherwise, the default value of <typeparamref name="TSource"/>.</returns>
            public virtual TSource? TryGetLast(out bool found) => TryGetLastNonIterator(this, out found);
        }
    }
}
