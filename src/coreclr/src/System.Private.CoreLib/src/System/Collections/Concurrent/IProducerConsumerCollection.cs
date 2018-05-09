// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// A common interface for all concurrent collections.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Defines methods to manipulate thread-safe collections intended for producer/consumer usage.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the collection.</typeparam>
    /// <remarks>
    /// All implementations of this interface must enable all members of this interface
    /// to be used concurrently from multiple threads.
    /// </remarks>
    internal interface IProducerConsumerCollection<T> : IEnumerable<T>, ICollection
    {
        /// <summary>
        /// Copies the elements of the <see cref="IProducerConsumerCollection{T}"/> to
        /// an
        /// <see cref="T:System.Array"/>, starting at a specified index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of
        /// the elements copied from the <see cref="IProducerConsumerCollection{T}"/>.
        /// The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference (Nothing in
        /// Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> is equal to or greater than the
        /// length of the <paramref name="array"/>
        /// -or- The number of elements in the source <see cref="ConcurrentQueue{T}"/> is greater than the
        /// available space from <paramref name="index"/> to the end of the destination <paramref
        /// name="array"/>.
        /// </exception>
        void CopyTo(T[] array, int index);

        /// <summary>
        /// Copies the elements contained in the <see cref="IProducerConsumerCollection{T}"/> to a new array.
        /// </summary>
        /// <returns>A new array containing the elements copied from the <see cref="IProducerConsumerCollection{T}"/>.</returns>
        T[] ToArray();
    }

    /// <summary>
    /// A debugger view of the IProducerConsumerCollection that makes it simple to browse the
    /// collection's contents at a point in time.
    /// </summary>
    /// <typeparam name="T">The type of elements stored within.</typeparam>
    internal sealed class SystemCollectionsConcurrent_ProducerConsumerCollectionDebugView<T>
    {
        private IProducerConsumerCollection<T> m_collection; // The collection being viewed.
    }
}
