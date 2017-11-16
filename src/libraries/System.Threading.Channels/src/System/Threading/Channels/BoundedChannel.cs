// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
    /// <summary>Provides a channel with a bounded capacity.</summary>
    [DebuggerDisplay("Items={ItemsCountForDebugger}, Capacity={_bufferedCapacity}")]
    [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
    internal sealed class BoundedChannel<T> : Channel<T>, IDebugEnumerable<T>
    {
        /// <summary>The mode used when the channel hits its bound.</summary>
        private readonly BoundedChannelFullMode _mode;
        /// <summary>Task signaled when the channel has completed.</summary>
        private readonly TaskCompletionSource<VoidResult> _completion;
        /// <summary>The maximum capacity of the channel.</summary>
        private readonly int _bufferedCapacity;
        /// <summary>Items currently stored in the channel waiting to be read.</summary>
        private readonly Dequeue<T> _items = new Dequeue<T>();
        /// <summary>Writers waiting to write to the channel.</summary>
        private readonly Dequeue<WriterInteractor<T>> _blockedWriters = new Dequeue<WriterInteractor<T>>();
        /// <summary>Task signaled when any WaitToReadAsync waiters should be woken up.</summary>
        private ReaderInteractor<bool> _waitingReaders;
        /// <summary>Task signaled when any WaitToWriteAsync waiters should be woken up.</summary>
        private ReaderInteractor<bool> _waitingWriters;
        /// <summary>Whether to force continuations to be executed asynchronously from producer writes.</summary>
        private readonly bool _runContinuationsAsynchronously;
        /// <summary>Set to non-null once Complete has been called.</summary>
        private Exception _doneWriting;
        /// <summary>Gets an object used to synchronize all state on the instance.</summary>
        private object SyncObj => _items;

        /// <summary>Initializes the <see cref="BoundedChannel{T}"/>.</summary>
        /// <param name="bufferedCapacity">The positive bounded capacity for the channel.</param>
        /// <param name="mode">The mode used when writing to a full channel.</param>
        /// <param name="runContinuationsAsynchronously">Whether to force continuations to be executed asynchronously.</param>
        internal BoundedChannel(int bufferedCapacity, BoundedChannelFullMode mode, bool runContinuationsAsynchronously)
        {
            Debug.Assert(bufferedCapacity > 0);
            _bufferedCapacity = bufferedCapacity;
            _mode = mode;
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
            _completion = new TaskCompletionSource<VoidResult>(runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);
            Reader = new BoundedChannelReader(this);
            Writer = new BoundedChannelWriter(this);
        }

        private sealed class BoundedChannelReader : ChannelReader<T>
        {
            internal readonly BoundedChannel<T> _parent;
            internal BoundedChannelReader(BoundedChannel<T> parent) => _parent = parent;

            public override Task Completion => _parent._completion.Task;

            public override bool TryRead(out T item)
            {
                BoundedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // Get an item if there is one.
                    if (!parent._items.IsEmpty)
                    {
                        item = DequeueItemAndPostProcess();
                        return true;
                    }
                }

                item = default;
                return false;
            }

            public override Task<bool> WaitToReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled<bool>(cancellationToken);
                }

                BoundedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If there are any items available, a read is possible.
                    if (!parent._items.IsEmpty)
                    {
                        return ChannelUtilities.s_trueTask;
                    }

                    // There were no items available, so if we're done writing, a read will never be possible.
                    if (parent._doneWriting != null)
                    {
                        return parent._doneWriting != ChannelUtilities.s_doneWritingSentinel ?
                            Task.FromException<bool>(parent._doneWriting) :
                            ChannelUtilities.s_falseTask;
                    }

                    // There were no items available, but there could be in the future, so ensure
                    // there's a blocked reader task and return it.
                    return ChannelUtilities.GetOrCreateWaiter(ref parent._waitingReaders, parent._runContinuationsAsynchronously, cancellationToken);
                }
            }

            /// <summary>Dequeues an item, and then fixes up our state around writers and completion.</summary>
            /// <returns>The dequeued item.</returns>
            private T DequeueItemAndPostProcess()
            {
                BoundedChannel<T> parent = _parent;
                Debug.Assert(Monitor.IsEntered(parent.SyncObj));

                // Dequeue an item.
                T item = parent._items.DequeueHead();

                // If we're now empty and we're done writing, complete the channel.
                if (parent._doneWriting != null && parent._items.IsEmpty)
                {
                    ChannelUtilities.Complete(parent._completion, parent._doneWriting);
                }

                // If there are any writers blocked, there's now room for at least one
                // to be promoted to have its item moved into the items queue.  We need
                // to loop while trying to complete the writer in order to find one that
                // hasn't yet been canceled (canceled writers transition to canceled but
                // remain in the physical queue).
                while (!parent._blockedWriters.IsEmpty)
                {
                    WriterInteractor<T> w = parent._blockedWriters.DequeueHead();
                    if (w.Success(default))
                    {
                        parent._items.EnqueueTail(w.Item);
                        return item;
                    }
                }

                // There was no blocked writer, so see if there's a WaitToWriteAsync
                // we should wake up.
                ChannelUtilities.WakeUpWaiters(ref parent._waitingWriters, result: true);

                // Return the item
                return item;
            }
        }

        private sealed class BoundedChannelWriter : ChannelWriter<T>
        {
            internal readonly BoundedChannel<T> _parent;
            internal BoundedChannelWriter(BoundedChannel<T> parent) => _parent = parent;

            public override bool TryComplete(Exception error)
            {
                BoundedChannel<T> parent = _parent;
                bool completeTask;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If we've already marked the channel as completed, bail.
                    if (parent._doneWriting != null)
                    {
                        return false;
                    }

                    // Mark that we're done writing.
                    parent._doneWriting = error ?? ChannelUtilities.s_doneWritingSentinel;
                    completeTask = parent._items.IsEmpty;
                }

                // If there are no items in the queue, complete the channel's task,
                // as no more data can possibly arrive at this point.  We do this outside
                // of the lock in case we'll be running synchronous completions, and we
                // do it before completing blocked/waiting readers, so that when they
                // wake up they'll see the task as being completed.
                if (completeTask)
                {
                    ChannelUtilities.Complete(parent._completion, error);
                }

                // At this point, _blockedWriters and _waitingReaders/Writers will not be mutated:
                // they're only mutated by readers/writers while holding the lock, and only if _doneWriting is null.
                // We also know that only one thread (this one) will ever get here, as only that thread
                // will be the one to transition from _doneWriting false to true.  As such, we can
                // freely manipulate them without any concurrency concerns.
                ChannelUtilities.FailInteractors<WriterInteractor<T>, VoidResult>(parent._blockedWriters, ChannelUtilities.CreateInvalidCompletionException(error));
                ChannelUtilities.WakeUpWaiters(ref parent._waitingReaders, result: false, error: error);
                ChannelUtilities.WakeUpWaiters(ref parent._waitingWriters, result: false, error: error);

                // Successfully transitioned to completed.
                return true;
            }

            public override bool TryWrite(T item)
            {
                ReaderInteractor<bool> waitingReaders = null;

                BoundedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If we're done writing, nothing more to do.
                    if (parent._doneWriting != null)
                    {
                        return false;
                    }

                    // Get the number of items in the channel currently.
                    int count = parent._items.Count;

                    if (count == 0)
                    {
                        // There are no items in the channel, which means we may have waiting readers.
                        // Store the item.
                        parent._items.EnqueueTail(item);
                        waitingReaders = parent._waitingReaders;
                        if (waitingReaders == null)
                        {
                            // If no one's waiting to be notified about a 0-to-1 transition, we're done.
                            return true;
                        }
                        parent._waitingReaders = null;
                    }
                    else if (count < parent._bufferedCapacity)
                    {
                        // There's room in the channel.  Since we're not transitioning from 0-to-1 and
                        // since there's room, we can simply store the item and exit without having to
                        // worry about blocked/waiting readers.
                        parent._items.EnqueueTail(item);
                        return true;
                    }
                    else if (parent._mode == BoundedChannelFullMode.Wait)
                    {
                        // The channel is full and we're in a wait mode.
                        // Simply exit and let the caller know we didn't write the data.
                        return false;
                    }
                    else if (parent._mode == BoundedChannelFullMode.DropWrite)
                    {
                        // The channel is full.  Just ignore the item being added
                        // but say we added it.
                        return true;
                    }
                    else
                    {
                        // The channel is full, and we're in a dropping mode.
                        // Drop either the oldest or the newest and write the new item.
                        T droppedItem = parent._mode == BoundedChannelFullMode.DropNewest ?
                            parent._items.DequeueTail() :
                            parent._items.DequeueHead();
                        parent._items.EnqueueTail(item);
                        return true;
                    }
                }

                // We stored an item bringing the count up from 0 to 1.  Alert
                // any waiting readers that there may be something for them to consume.
                // Since we're no longer holding the lock, it's possible we'll end up
                // waking readers that have since come in.
                waitingReaders.Success(item: true);
                return true;
            }

            public override Task<bool> WaitToWriteAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled<bool>(cancellationToken);
                }

                BoundedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If we're done writing, no writes will ever succeed.
                    if (parent._doneWriting != null)
                    {
                        return parent._doneWriting != ChannelUtilities.s_doneWritingSentinel ?
                            Task.FromException<bool>(parent._doneWriting) :
                            ChannelUtilities.s_falseTask;
                    }

                    // If there's space to write, a write is possible.
                    // And if the mode involves dropping/ignoring, we can always write, as even if it's
                    // full we'll just drop an element to make room.
                    if (parent._items.Count < parent._bufferedCapacity || parent._mode != BoundedChannelFullMode.Wait)
                    {
                        return ChannelUtilities.s_trueTask;
                    }

                    // We're still allowed to write, but there's no space, so ensure a waiter is queued and return it.
                    return ChannelUtilities.GetOrCreateWaiter(ref parent._waitingWriters, runContinuationsAsynchronously: true, cancellationToken);
                }
            }

            public override Task WriteAsync(T item, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled(cancellationToken);
                }

                ReaderInteractor<bool> waitingReaders = null;

                BoundedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If we're done writing, trying to write is an error.
                    if (parent._doneWriting != null)
                    {
                        return Task.FromException(ChannelUtilities.CreateInvalidCompletionException(parent._doneWriting));
                    }

                    // Get the number of items in the channel currently.
                    int count = parent._items.Count;

                    if (count == 0)
                    {
                        // There are no items in the channel, which means we may have waiting readers.
                        // Store the item.
                        parent._items.EnqueueTail(item);
                        waitingReaders = parent._waitingReaders;
                        if (waitingReaders == null)
                        {
                            // If no one's waiting to be notified about a 0-to-1 transition, we're done.
                            return ChannelUtilities.s_trueTask;
                        }
                        parent._waitingReaders = null;
                    }
                    else if (count < parent._bufferedCapacity)
                    {
                        // There's room in the channel.  Since we're not transitioning from 0-to-1 and
                        // since there's room, we can simply store the item and exit without having to
                        // worry about blocked/waiting readers.
                        parent._items.EnqueueTail(item);
                        return ChannelUtilities.s_trueTask;
                    }
                    else if (parent._mode == BoundedChannelFullMode.Wait)
                    {
                        // The channel is full and we're in a wait mode.
                        // Queue the writer.
                        var writer = WriterInteractor<T>.Create(runContinuationsAsynchronously: true, item, cancellationToken);
                        parent._blockedWriters.EnqueueTail(writer);
                        return writer.Task;
                    }
                    else if (parent._mode == BoundedChannelFullMode.DropWrite)
                    {
                        // The channel is full and we're in ignore mode.
                        // Ignore the item but say we accepted it.
                        return ChannelUtilities.s_trueTask;
                    }
                    else
                    {
                        // The channel is full, and we're in a dropping mode.
                        // Drop either the oldest or the newest and write the new item.
                        T droppedItem = parent._mode == BoundedChannelFullMode.DropNewest ?
                            parent._items.DequeueTail() :
                            parent._items.DequeueHead();
                        parent._items.EnqueueTail(item);
                        return ChannelUtilities.s_trueTask;
                    }
                }

                // We stored an item bringing the count up from 0 to 1.  Alert
                // any waiting readers that there may be something for them to consume.
                // Since we're no longer holding the lock, it's possible we'll end up
                // waking readers that have since come in.
                waitingReaders.Success(item: true);
                return ChannelUtilities.s_trueTask;
            }
        }

        [Conditional("DEBUG")]
        private void AssertInvariants()
        {
            Debug.Assert(SyncObj != null, "The sync obj must not be null.");
            Debug.Assert(Monitor.IsEntered(SyncObj), "Invariants can only be validated while holding the lock.");

            if (!_items.IsEmpty)
            {
                Debug.Assert(_waitingReaders == null, "There are items available, so there shouldn't be any waiting readers.");
            }
            if (_items.Count < _bufferedCapacity)
            {
                Debug.Assert(_blockedWriters.IsEmpty, "There's space available, so there shouldn't be any blocked writers.");
                Debug.Assert(_waitingWriters == null, "There's space available, so there shouldn't be any waiting writers.");
            }
            if (!_blockedWriters.IsEmpty)
            {
                Debug.Assert(_items.Count == _bufferedCapacity, "We should have a full buffer if there's a blocked writer.");
            }
            if (_completion.Task.IsCompleted)
            {
                Debug.Assert(_doneWriting != null, "We can only complete if we're done writing.");
            }
        }

        /// <summary>Gets the number of items in the channel.  This should only be used by the debugger.</summary>
        private int ItemsCountForDebugger => _items.Count;

        /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
        IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _items.GetEnumerator();
    }
}
