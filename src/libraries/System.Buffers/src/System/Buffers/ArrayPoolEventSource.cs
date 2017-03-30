// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;

namespace System.Buffers
{
    [EventSource(Name = "System.Buffers.ArrayPoolEventSource")]
    internal sealed class ArrayPoolEventSource : EventSource
    {
        internal static readonly ArrayPoolEventSource Log = new ArrayPoolEventSource();

        /// <summary>The reason for a BufferAllocated event.</summary>
        internal enum BufferAllocatedReason : int
        {
            /// <summary>The pool is allocating a buffer to be pooled in a bucket.</summary>
            Pooled,
            /// <summary>The requested buffer size was too large to be pooled.</summary>
            OverMaximumSize,
            /// <summary>The pool has already allocated for pooling as many buffers of a particular size as it's allowed.</summary>
            PoolExhausted
        }

        /// <summary>
        /// Event for when a buffer is rented.  This is invoked once for every successful call to Rent,
        /// regardless of whether a buffer is allocated or a buffer is taken from the pool.  In a
        /// perfect situation where all rented buffers are returned, we expect to see the number
        /// of BufferRented events exactly match the number of BuferReturned events, with the number
        /// of BufferAllocated events being less than or equal to those numbers (ideally significantly
        /// less than).
        /// </summary>
        [Event(1, Level = EventLevel.Verbose)]
        internal unsafe void BufferRented(int bufferId, int bufferSize, int poolId, int bucketId)
        {
            EventData* payload = stackalloc EventData[4];
            payload[0].Size = sizeof(int);
            payload[0].DataPointer = ((IntPtr)(&bufferId));
            payload[1].Size = sizeof(int);
            payload[1].DataPointer = ((IntPtr)(&bufferSize));
            payload[2].Size = sizeof(int);
            payload[2].DataPointer = ((IntPtr)(&poolId));
            payload[3].Size = sizeof(int);
            payload[3].DataPointer = ((IntPtr)(&bucketId));
            WriteEventCore(1, 4, payload);
        }

        /// <summary>
        /// Event for when a buffer is allocated by the pool.  In an ideal situation, the number
        /// of BufferAllocated events is significantly smaller than the number of BufferRented and
        /// BufferReturned events.
        /// </summary>
        [Event(2, Level = EventLevel.Informational)]
        internal unsafe void BufferAllocated(int bufferId, int bufferSize, int poolId, int bucketId, BufferAllocatedReason reason)
        {
            EventData* payload = stackalloc EventData[5];
            payload[0].Size = sizeof(int);
            payload[0].DataPointer = ((IntPtr)(&bufferId));
            payload[1].Size = sizeof(int);
            payload[1].DataPointer = ((IntPtr)(&bufferSize));
            payload[2].Size = sizeof(int);
            payload[2].DataPointer = ((IntPtr)(&poolId));
            payload[3].Size = sizeof(int);
            payload[3].DataPointer = ((IntPtr)(&bucketId));
            payload[4].Size = sizeof(BufferAllocatedReason);
            payload[4].DataPointer = ((IntPtr)(&reason));
            WriteEventCore(2, 5, payload);
        }

        /// <summary>
        /// Event raised when a buffer is returned to the pool.  This event is raised regardless of whether
        /// the returned buffer is stored or dropped.  In an ideal situation, the number of BufferReturned
        /// events exactly matches the number of BufferRented events.
        /// </summary>
        [Event(3, Level = EventLevel.Verbose)]
        internal void BufferReturned(int bufferId, int bufferSize, int poolId) => WriteEvent(3, bufferId, bufferSize, poolId);
    }
}
