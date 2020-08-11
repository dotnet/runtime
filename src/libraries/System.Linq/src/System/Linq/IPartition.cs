// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    /// <summary>
    /// An iterator that supports random access and can produce a partial sequence of its items through an optimized path.
    /// </summary>
    internal interface IPartition<TElement> : IIListProvider<TElement>
    {
        /// <summary>
        /// Creates a new partition that skips the specified number of elements from this sequence.
        /// </summary>
        /// <param name="count">The number of elements to skip.</param>
        /// <returns>An <see cref="IPartition{TElement}"/> with the first <paramref name="count"/> items removed.</returns>
        IPartition<TElement> Skip(int count);

        /// <summary>
        /// Creates a new partition that takes the specified number of elements from this sequence.
        /// </summary>
        /// <param name="count">The number of elements to take.</param>
        /// <returns>An <see cref="IPartition{TElement}"/> with only the first <paramref name="count"/> items.</returns>
        IPartition<TElement> Take(int count);

        /// <summary>
        /// Gets the item associated with a 0-based index in this sequence.
        /// </summary>
        /// <param name="index">The 0-based index to access.</param>
        /// <param name="found"><c>true</c> if the sequence contains an element at that index, <c>false</c> otherwise.</param>
        /// <returns>The element if <paramref name="found"/> is <c>true</c>, otherwise, the default value of <typeparamref name="TElement"/>.</returns>
        TElement? TryGetElementAt(int index, out bool found);

        /// <summary>
        /// Gets the first item in this sequence.
        /// </summary>
        /// <param name="found"><c>true</c> if the sequence contains an element, <c>false</c> otherwise.</param>
        /// <returns>The element if <paramref name="found"/> is <c>true</c>, otherwise, the default value of <typeparamref name="TElement"/>.</returns>
        TElement? TryGetFirst(out bool found);

        /// <summary>
        /// Gets the last item in this sequence.
        /// </summary>
        /// <param name="found"><c>true</c> if the sequence contains an element, <c>false</c> otherwise.</param>
        /// <returns>The element if <paramref name="found"/> is <c>true</c>, otherwise, the default value of <typeparamref name="TElement"/>.</returns>
        TElement? TryGetLast(out bool found);
    }
}
