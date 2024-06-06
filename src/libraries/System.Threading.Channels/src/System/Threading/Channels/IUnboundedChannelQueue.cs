// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Channels
{
    /// <summary>Representation of the queue data structure used by <see cref="UnboundedChannel{T, TQueue}"/>.</summary>
    internal interface IUnboundedChannelQueue<T> : IDebugEnumerable<T>
    {
        /// <summary>Gets whether the other members are safe to use concurrently with each other and themselves.</summary>
        bool IsThreadSafe { get; }

        /// <summary>Enqueues an item into the queue.</summary>
        /// <param name="item">The item to enqueue.</param>
        void Enqueue(T item);

        /// <summary>Dequeues an item from the queue, if possible.</summary>
        /// <param name="item">The dequeued item, or default if the queue was empty.</param>
        /// <returns>Whether an item was dequeued.</returns>
        bool TryDequeue([MaybeNullWhen(false)] out T item);

        /// <summary>Peeks at the next item from the queue that would be dequeued, if possible.</summary>
        /// <param name="item">The peeked item, or default if the queue was empty.</param>
        /// <returns>Whether an item was peeked.</returns>
        bool TryPeek([MaybeNullWhen(false)] out T item);

        /// <summary>Gets the number of elements in the queue.</summary>
        int Count { get; }

        /// <summary>Gets whether the queue is empty.</summary>
        bool IsEmpty { get; }
    }
}
