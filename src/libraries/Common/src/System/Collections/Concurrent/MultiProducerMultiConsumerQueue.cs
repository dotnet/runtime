// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Provides a producer/consumer queue safe to be used by any number of producers and consumers concurrently.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data contained in the queue.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class MultiProducerMultiConsumerQueue<T> : ConcurrentQueue<T>, IProducerConsumerQueue<T>
    {
        /// <summary>Enqueues an item into the queue.</summary>
        /// <param name="item">The item to enqueue.</param>
        void IProducerConsumerQueue<T>.Enqueue(T item) { base.Enqueue(item); }

        /// <summary>Attempts to dequeue an item from the queue.</summary>
        /// <param name="result">The dequeued item.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        bool IProducerConsumerQueue<T>.TryDequeue([MaybeNullWhen(false)] out T result) { return base.TryDequeue(out result); }

        /// <summary>Gets whether the collection is currently empty.</summary>
        bool IProducerConsumerQueue<T>.IsEmpty => base.IsEmpty;

        /// <summary>Gets the number of items in the collection.</summary>
        int IProducerConsumerQueue<T>.Count => base.Count;

        /// <summary>A thread-safe way to get the number of items in the collection. May synchronize access by locking the provided synchronization object.</summary>
        /// <remarks>ConcurrentQueue.Count is thread safe, no need to acquire the lock.</remarks>
        int IProducerConsumerQueue<T>.GetCountSafe(object syncObj) => base.Count;
    }
}
