// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Concurrent
{
    /// <summary>Represents a producer/consumer queue.</summary>
    /// <typeparam name="T">Specifies the type of data contained in the queue.</typeparam>
    internal interface IProducerConsumerQueue<T> : IEnumerable<T>
    {
        /// <summary>Enqueues an item into the queue.</summary>
        /// <param name="item">The item to enqueue.</param>
        /// <remarks>This method is meant to be thread-safe subject to the particular nature of the implementation.</remarks>
        void Enqueue(T item);

        /// <summary>Attempts to dequeue an item from the queue.</summary>
        /// <param name="result">The dequeued item.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        /// <remarks>This method is meant to be thread-safe subject to the particular nature of the implementation.</remarks>
        bool TryDequeue([MaybeNullWhen(false)] out T result);

        /// <summary>Gets whether the collection is currently empty.</summary>
        /// <remarks>This method may or may not be thread-safe.</remarks>
        bool IsEmpty { get; }

        /// <summary>Gets the number of items in the collection.</summary>
        /// <remarks>In many implementations, this method will not be thread-safe.</remarks>
        int Count { get; }

        /// <summary>A thread-safe way to get the number of items in the collection. May synchronize access by locking the provided synchronization object.</summary>
        /// <param name="syncObj">The sync object used to lock</param>
        /// <returns>The collection count</returns>
        int GetCountSafe(object syncObj);
    }
}
