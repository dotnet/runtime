// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable queue.
    /// </summary>
    /// <typeparam name="T">The type of elements in the queue.</typeparam>    
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Ignored")]
    public interface IImmutableQueue<T> : IEnumerable<T>
    {
        /// <summary>
        /// Gets a value indicating whether this is the empty queue.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this queue is empty; otherwise, <c>false</c>.
        /// </value>
        [Pure]
        bool IsEmpty { get; }

        /// <summary>
        /// Gets an empty queue.
        /// </summary>
        [Pure]
        IImmutableQueue<T> Clear();

        /// <summary>
        /// Gets the element at the front of the queue.
        /// </summary>
        /// <returns>
        /// The element on the top of the stack. 
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        [Pure]
        T Peek();

        /// <summary>
        /// Adds an element to the back of the queue.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The new queue.
        /// </returns>
        [Pure]
        IImmutableQueue<T> Enqueue(T value);

        /// <summary>
        /// Returns a queue that is missing the front element.
        /// </summary>
        /// <returns>A queue; never <c>null</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        [Pure]
        IImmutableQueue<T> Dequeue();
    }
}