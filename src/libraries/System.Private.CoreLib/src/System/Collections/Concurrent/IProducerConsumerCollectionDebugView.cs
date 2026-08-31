// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// A debugger view of the IProducerConsumerCollection that makes it simple to browse the
    /// collection's contents at a point in time.
    /// </summary>
    /// <typeparam name="T">The type of elements stored within.</typeparam>
    internal sealed class IProducerConsumerCollectionDebugView<T>
    {
        private readonly IProducerConsumerCollection<T> _collection; // The collection being viewed.

        /// <summary>
        /// Constructs a new debugger view object for the provided collection object.
        /// </summary>
        /// <param name="collection">A collection to browse in the debugger.</param>
        public IProducerConsumerCollectionDebugView(IProducerConsumerCollection<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            _collection = collection;
        }

        /// <summary>
        /// Returns a snapshot of the underlying collection's elements.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _collection.ToArray();
    }
}
