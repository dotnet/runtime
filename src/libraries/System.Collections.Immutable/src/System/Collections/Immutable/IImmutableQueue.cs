// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable queue.
    /// </summary>
    /// <typeparam name="T">The type of elements in the queue.</typeparam>
    public interface IImmutableQueue<T> : IEnumerable<T>
    {
        /// <summary>
        /// Gets a value indicating whether this is the empty queue.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this queue is empty; otherwise, <c>false</c>.
        /// </value>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets an empty queue.
        /// </summary>
        IImmutableQueue<T> Clear();

        /// <summary>
        /// Gets the element at the front of the queue.
        /// </summary>
        /// <returns>
        /// The element at the front of the queue.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        T Peek();

        /// <summary>
        /// Adds an element to the back of the queue.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The new queue.
        /// </returns>
        IImmutableQueue<T> Enqueue(T value);

        /// <summary>
        /// Returns a queue that is missing the front element.
        /// </summary>
        /// <returns>A queue; never <c>null</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        IImmutableQueue<T> Dequeue();
    }
}
