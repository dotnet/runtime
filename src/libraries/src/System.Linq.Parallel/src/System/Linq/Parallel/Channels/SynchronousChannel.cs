// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// SynchronousChannel.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// The simplest channel is one that has no synchronization.  This is used for stop-
    /// and-go productions where we are guaranteed the consumer is not running
    /// concurrently. It just wraps a FIFO queue internally.
    ///
    /// Assumptions:
    ///     Producers and consumers never try to enqueue/dequeue concurrently.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class SynchronousChannel<T>
    {
        // We currently use the BCL FIFO queue internally, although any would do.
        private Queue<T> _queue;

#if DEBUG
        // In debug builds, we keep track of when the producer is done (for asserts).
        private bool _done;
#endif

        //-----------------------------------------------------------------------------------
        // Instantiates a new queue.
        //

        internal SynchronousChannel()
        {
        }

        //-----------------------------------------------------------------------------------
        // Initializes the queue for this channel.
        //

        internal void Init()
        {
            _queue = new Queue<T>();
        }

        //-----------------------------------------------------------------------------------
        // Enqueue a new item.
        //
        // Arguments:
        //     item                - the item to place into the queue
        //     timeoutMilliseconds - synchronous channels never wait, so this is unused
        //
        // Assumptions:
        //     The producer has not signaled that it's done yet.
        //
        // Return Value:
        //     Synchronous channels always return true for this function.  It can't timeout.
        //

        internal void Enqueue(T item)
        {
            Debug.Assert(_queue != null);
#if DEBUG
            Debug.Assert(!_done, "trying to enqueue into the queue after production is done");
#endif

            _queue.Enqueue(item);
        }

        //-----------------------------------------------------------------------------------
        // Dequeue the next item in the queue.
        //
        // Return Value:
        //     The item removed from the queue.
        //
        // Assumptions:
        //     The producer must be done producing. This queue is meant for synchronous
        //     production/consumption, therefore it's unsafe for the consumer to try and
        //     dequeue an item while a producer might be enqueueing one.
        //

        internal T Dequeue()
        {
            Debug.Assert(_queue != null);
#if DEBUG
            Debug.Assert(_done, "trying to dequeue before production is done -- this is not safe");
#endif
            return _queue.Dequeue();
        }

        //-----------------------------------------------------------------------------------
        // Signals that a producer will no longer be enqueueing items.
        //

        internal void SetDone()
        {
#if DEBUG
            // We only track this in DEBUG builds to aid in debugging. This ensures we
            // can assert dequeue-before-done and enqueue-after-done invariants above.
            _done = true;
#endif
        }

        //-----------------------------------------------------------------------------------
        // Copies the internal contents of this channel to an array.
        //

        internal void CopyTo(T[] array, int arrayIndex)
        {
            Debug.Assert(array != null);
#if DEBUG
            Debug.Assert(_done, "Can only copy from the channel after it's done being added to");
#endif
            _queue.CopyTo(array, arrayIndex);
        }

        //-----------------------------------------------------------------------------------
        // Retrieves the current count of items in the queue.
        //

        internal int Count
        {
            get
            {
                Debug.Assert(_queue != null);
                return _queue.Count;
            }
        }
    }
}
