// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
    /// <summary>Provides a channel with a bounded capacity.</summary>
    [DebuggerDisplay("Items = {ItemsCountForDebugger}, Capacity = {_bufferedCapacity}, Mode = {_mode}, Closed = {ChannelIsClosedForDebugger}")]
    [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
    internal sealed class BoundedChannel<T> : Channel<T>, IDebugEnumerable<T>
    {
        /// <summary>The mode used when the channel hits its bound.</summary>
        private readonly BoundedChannelFullMode _mode;

        /// <summary>The delegate that will be invoked when the channel hits its bound and an item is dropped from the channel.</summary>
        private readonly Action<T>? _itemDropped;

        /// <summary>Task signaled when the channel has completed.</summary>
        private readonly TaskCompletionSource _completion;

        /// <summary>The maximum capacity of the channel.</summary>
        private readonly int _bufferedCapacity;

        /// <summary>Items currently stored in the channel waiting to be read.</summary>
        private readonly Deque<T> _items = new Deque<T>();

        /// <summary>Head of linked list of blocked ReadAsync calls.</summary>
        private BlockedReadAsyncOperation<T>? _blockedReadersHead;

        /// <summary>Head of linked list of blocked WriteAsync calls.</summary>
        private BlockedWriteAsyncOperation<T>? _blockedWritersHead;

        /// <summary>Head of linked list of waiting WaitToReadAsync calls.</summary>
        private WaitingReadAsyncOperation? _waitingReadersHead;

        /// <summary>Head of linked list of waiting WaitToWriteAsync calls.</summary>
        private WaitingWriteAsyncOperation? _waitingWritersHead;

        /// <summary>Whether to force continuations to be executed asynchronously from producer writes.</summary>
        private readonly bool _runContinuationsAsynchronously;

        /// <summary>Set to non-null once Complete has been called.</summary>
        private Exception? _doneWriting;

        /// <summary>Initializes the <see cref="BoundedChannel{T}"/>.</summary>
        /// <param name="bufferedCapacity">The positive bounded capacity for the channel.</param>
        /// <param name="mode">The mode used when writing to a full channel.</param>
        /// <param name="runContinuationsAsynchronously">Whether to force continuations to be executed asynchronously.</param>
        /// <param name="itemDropped">Delegate that will be invoked when an item is dropped from the channel. See <see cref="BoundedChannelFullMode"/>.</param>
        internal BoundedChannel(int bufferedCapacity, BoundedChannelFullMode mode, bool runContinuationsAsynchronously, Action<T>? itemDropped)
        {
            Debug.Assert(bufferedCapacity > 0);

            _bufferedCapacity = bufferedCapacity;
            _mode = mode;
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
            _itemDropped = itemDropped;
            _completion = new TaskCompletionSource(runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);

            Reader = new BoundedChannelReader(this);
            Writer = new BoundedChannelWriter(this);
        }

        [DebuggerDisplay("Items = {ItemsCountForDebugger}")]
        [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
        private sealed class BoundedChannelReader : ChannelReader<T>, IDebugEnumerable<T>
        {
            internal readonly BoundedChannel<T> _parent;
            private readonly BlockedReadAsyncOperation<T> _readerSingleton;
            private readonly WaitingReadAsyncOperation _waiterSingleton;

            internal BoundedChannelReader(BoundedChannel<T> parent)
            {
                _parent = parent;
                _readerSingleton = new BlockedReadAsyncOperation<T>(parent._runContinuationsAsynchronously, pooled: true);
                _waiterSingleton = new WaitingReadAsyncOperation(parent._runContinuationsAsynchronously, pooled: true);
            }

            public override Task Completion => _parent._completion.Task;

            public override bool CanCount => true;

            public override bool CanPeek => true;

            public override int Count
            {
                get
                {
                    BoundedChannel<T> parent = _parent;
                    lock (parent.SyncObj)
                    {
                        parent.AssertInvariants();
                        return parent._items.Count;
                    }
                }
            }

            /// <summary>Gets the number of items in the channel. This should only be used by the debugger.</summary>
            /// <remarks>
            /// Unlike <see cref="Count"/>, avoids locking so as to not block the debugger if another suspended thread is holding the lock.
            /// Hence, this must only be used from the debugger in a serialized context.
            /// </remarks>
            private int ItemsCountForDebugger => _parent._items.Count;

            public override bool TryRead([MaybeNullWhen(false)] out T item)
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

            public override bool TryPeek([MaybeNullWhen(false)] out T item)
            {
                BoundedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // Peek at an item if there is one.
                    if (!parent._items.IsEmpty)
                    {
                        item = parent._items.PeekHead();
                        return true;
                    }
                }

                item = default;
                return false;
            }

            public override ValueTask<T> ReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<T>(Task.FromCanceled<T>(cancellationToken));
                }

                BoundedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If there are any items, hand one back.
                    if (!parent._items.IsEmpty)
                    {
                        return new ValueTask<T>(DequeueItemAndPostProcess());
                    }

                    // There weren't any items.  If we're done writing so that there
                    // will never be more items, fail.
                    if (parent._doneWriting is not null)
                    {
                        return ChannelUtilities.GetInvalidCompletionValueTask<T>(parent._doneWriting);
                    }

                    // If we're able to use the singleton reader, do so.
                    if (!cancellationToken.CanBeCanceled)
                    {
                        BlockedReadAsyncOperation<T> singleton = _readerSingleton;
                        if (singleton.TryOwnAndReset())
                        {
                            ChannelUtilities.Enqueue(ref parent._blockedReadersHead, singleton);
                            return singleton.ValueTaskOfT;
                        }
                    }

                    // Otherwise, queue a reader.
                    var reader = new BlockedReadAsyncOperation<T>(parent._runContinuationsAsynchronously, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
                    ChannelUtilities.Enqueue(ref parent._blockedReadersHead, reader);
                    return reader.ValueTaskOfT;
                }
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
                }

                BoundedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If there are any items available, a read is possible.
                    if (!parent._items.IsEmpty)
                    {
                        return new ValueTask<bool>(true);
                    }

                    // There were no items available, so if we're done writing, a read will never be possible.
                    if (parent._doneWriting is not null)
                    {
                        return parent._doneWriting != ChannelUtilities.s_doneWritingSentinel ?
                            new ValueTask<bool>(Task.FromException<bool>(parent._doneWriting)) :
                            default;
                    }

                    // There were no items available, but there could be in the future, so ensure
                    // there's a blocked reader task and return it.

                    // If we're able to use the singleton waiter, do so.
                    if (!cancellationToken.CanBeCanceled)
                    {
                        WaitingReadAsyncOperation singleton = _waiterSingleton;
                        if (singleton.TryOwnAndReset())
                        {
                            ChannelUtilities.Enqueue(ref parent._waitingReadersHead, singleton);
                            return singleton.ValueTaskOfT;
                        }
                    }

                    // Otherwise, queue a reader.
                    var waiter = new WaitingReadAsyncOperation(parent._runContinuationsAsynchronously, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
                    ChannelUtilities.Enqueue(ref parent._waitingReadersHead, waiter);
                    return waiter.ValueTaskOfT;
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

                if (parent._doneWriting is not null)
                {
                    // We're done writing, so if we're now empty, complete the channel.
                    if (parent._items.IsEmpty)
                    {
                        ChannelUtilities.Complete(parent._completion, parent._doneWriting);
                    }
                }
                else
                {
                    // If there are any writers blocked, there's now room for at least one
                    // to be promoted to have its item moved into the items queue.  We need
                    // to loop while trying to complete the writer in order to find one that
                    // hasn't yet been canceled (canceled writers transition to canceled but
                    // may temporarily remain in the physical queue).
                    //
                    // (It's possible for _doneWriting to be non-null due to Complete
                    // having been called but for there to still be blocked/waiting writers.
                    // This is a temporary condition, after which Complete has set _doneWriting
                    // and then exited the lock; at that point it'll proceed to clean this up,
                    // so we just ignore them.)
                    while (ChannelUtilities.TryDequeue(ref parent._blockedWritersHead, out BlockedWriteAsyncOperation<T>? w))
                    {
                        if (w.TrySetResult(default))
                        {
                            parent._items.EnqueueTail(w.Item!);
                            return item;
                        }
                    }

                    // There was no blocked writer; alert any WaitToWriteAsync waiters that they may be able to write now.
                    ChannelUtilities.AssertAll(parent._waitingWritersHead, static writer => writer.RunContinuationsAsynchronously, "All WaitToWriteAsync waiters should have been asynchronous.");
                    ChannelUtilities.SetOperations(ref parent._waitingWritersHead, result: true);
                }

                // Return the item
                return item;
            }

            /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
            IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _parent._items.GetEnumerator();
        }

        [DebuggerDisplay("Items = {ItemsCountForDebugger}, Capacity = {CapacityForDebugger}")]
        [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
        private sealed class BoundedChannelWriter : ChannelWriter<T>, IDebugEnumerable<T>
        {
            internal readonly BoundedChannel<T> _parent;
            private readonly BlockedWriteAsyncOperation<T> _writerSingleton;
            private readonly WaitingWriteAsyncOperation _waiterSingleton;

            internal BoundedChannelWriter(BoundedChannel<T> parent)
            {
                _parent = parent;
                _writerSingleton = new BlockedWriteAsyncOperation<T>(runContinuationsAsynchronously: true, pooled: true);
                _waiterSingleton = new WaitingWriteAsyncOperation(runContinuationsAsynchronously: true, pooled: true);
            }

            public override bool TryComplete(Exception? error)
            {
                BoundedChannel<T> parent = _parent;

                BlockedReadAsyncOperation<T>? blockedReadersHead;
                BlockedWriteAsyncOperation<T>? blockedWritersHead;
                WaitingReadAsyncOperation? waitingReadersHead;
                WaitingWriteAsyncOperation? waitingWritersHead;

                bool completeTask;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If we've already marked the channel as completed, bail.
                    if (parent._doneWriting is not null)
                    {
                        return false;
                    }

                    // Mark that we're done writing.
                    parent._doneWriting = error ?? ChannelUtilities.s_doneWritingSentinel;
                    completeTask = parent._items.IsEmpty;

                    // Snag the queues while holding the lock, so that we don't need to worry
                    // about concurrent mutation, such as from cancellation of pending operations.
                    blockedReadersHead = parent._blockedReadersHead;
                    blockedWritersHead = parent._blockedWritersHead;
                    waitingReadersHead = parent._waitingReadersHead;
                    waitingWritersHead = parent._waitingWritersHead;
                    parent._blockedReadersHead = null;
                    parent._blockedWritersHead = null;
                    parent._waitingReadersHead = null;
                    parent._waitingWritersHead = null;
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

                // Complete all pending operations. We don't need to worry about concurrent mutation here:
                // No other writers or readers will be able to register operations, and any cancellation callbacks
                // will see the queues as being null and exit immediately.
                ChannelUtilities.FailOperations(blockedReadersHead, ChannelUtilities.CreateInvalidCompletionException(error));
                ChannelUtilities.FailOperations(blockedWritersHead, ChannelUtilities.CreateInvalidCompletionException(error));
                ChannelUtilities.SetOrFailOperations(waitingReadersHead, result: false, error: error);
                ChannelUtilities.SetOrFailOperations(waitingWritersHead, result: false, error: error);

                // Successfully transitioned to completed.
                return true;
            }

            public override bool TryWrite(T item)
            {
                BlockedReadAsyncOperation<T>? blockedReader = null;
                WaitingReadAsyncOperation? waitingReadersHead = null;

                BoundedChannel<T> parent = _parent;

                bool releaseLock = false;
                try
                {
                    Monitor.Enter(parent.SyncObj, ref releaseLock);

                    parent.AssertInvariants();

                    // If we're done writing, nothing more to do.
                    if (parent._doneWriting is not null)
                    {
                        return false;
                    }

                    // Get the number of items in the channel currently.
                    int count = parent._items.Count;

                    if (count == 0)
                    {
                        // There are no items in the channel, which means we may have blocked/waiting readers.

                        // Try to get a blocked reader that we can transfer the item to.
                        blockedReader = ChannelUtilities.TryDequeueAndReserveCompletionIfCancelable(ref parent._blockedReadersHead);

                        // If we weren't able to get a reader, instead queue the item and get any waiters that need to be notified.
                        if (blockedReader is null)
                        {
                            parent._items.EnqueueTail(item);
                            waitingReadersHead = ChannelUtilities.TryReserveCompletionIfCancelable(ref parent._waitingReadersHead);
                            if (waitingReadersHead is null)
                            {
                                return true;
                            }
                        }
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
                        Monitor.Exit(parent.SyncObj);
                        releaseLock = false;
                        parent._itemDropped?.Invoke(item);
                        return true;
                    }
                    else
                    {
                        // The channel is full, and we're in a dropping mode.
                        // Drop either the oldest or the newest
                        T droppedItem = parent._mode == BoundedChannelFullMode.DropNewest ?
                            parent._items.DequeueTail() :
                            parent._items.DequeueHead();

                        parent._items.EnqueueTail(item);

                        Monitor.Exit(parent.SyncObj);
                        releaseLock = false;
                        parent._itemDropped?.Invoke(droppedItem);

                        return true;
                    }
                }
                finally
                {
                    if (releaseLock)
                    {
                        Monitor.Exit(parent.SyncObj);
                    }
                }

                // Now that we're outside of the lock, if we successfully got any tasks to complete and reserved their completion, do so.
                if (blockedReader is not null)
                {
                    Debug.Assert(waitingReadersHead is null);
                    blockedReader.DangerousSetResult(item);
                }
                else
                {
                    Debug.Assert(waitingReadersHead is not null);
                    ChannelUtilities.DangerousSetOperations(waitingReadersHead, result: true);
                }

                return true;
            }

            public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
                }

                BoundedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If we're done writing, no writes will ever succeed.
                    if (parent._doneWriting is not null)
                    {
                        return parent._doneWriting != ChannelUtilities.s_doneWritingSentinel ?
                            new ValueTask<bool>(Task.FromException<bool>(parent._doneWriting)) :
                            default;
                    }

                    // If there's space to write, a write is possible.
                    // And if the mode involves dropping/ignoring, we can always write, as even if it's
                    // full we'll just drop an element to make room.
                    if (parent._items.Count < parent._bufferedCapacity || parent._mode != BoundedChannelFullMode.Wait)
                    {
                        return new ValueTask<bool>(true);
                    }

                    // We're still allowed to write, but there's no space, so ensure a waiter is queued and return it.

                    // If we're able to use the singleton waiter, do so.
                    if (!cancellationToken.CanBeCanceled)
                    {
                        WaitingWriteAsyncOperation singleton = _waiterSingleton;
                        if (singleton.TryOwnAndReset())
                        {
                            ChannelUtilities.Enqueue(ref parent._waitingWritersHead, singleton);
                            return singleton.ValueTaskOfT;
                        }
                    }

                    // Otherwise, queue a waiter.
                    var waiter = new WaitingWriteAsyncOperation(runContinuationsAsynchronously: true, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
                    ChannelUtilities.Enqueue(ref parent._waitingWritersHead, waiter);
                    return waiter.ValueTaskOfT;
                }
            }

            public override ValueTask WriteAsync(T item, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask(Task.FromCanceled(cancellationToken));
                }

                BlockedReadAsyncOperation<T>? blockedReader = null;
                WaitingReadAsyncOperation? waitingReadersHead = null;

                BoundedChannel<T> parent = _parent;

                bool releaseLock = false;
                try
                {
                    Monitor.Enter(parent.SyncObj, ref releaseLock);

                    parent.AssertInvariants();

                    // If we're done writing, trying to write is an error.
                    if (parent._doneWriting is not null)
                    {
                        return new ValueTask(Task.FromException(ChannelUtilities.CreateInvalidCompletionException(parent._doneWriting)));
                    }

                    // Get the number of items in the channel currently.
                    int count = parent._items.Count;

                    if (count == 0)
                    {
                        // There are no items in the channel, which means we may have blocked/waiting readers.

                        // Try to get a blocked reader that we can transfer the item to.
                        blockedReader = ChannelUtilities.TryDequeueAndReserveCompletionIfCancelable(ref parent._blockedReadersHead);

                        // If we weren't able to get a reader, instead queue the item and get any waiters that need to be notified.
                        if (blockedReader is null)
                        {
                            parent._items.EnqueueTail(item);
                            waitingReadersHead = ChannelUtilities.TryReserveCompletionIfCancelable(ref parent._waitingReadersHead);
                            if (waitingReadersHead is null)
                            {
                                return default;
                            }
                        }
                    }
                    else if (count < parent._bufferedCapacity)
                    {
                        // There's room in the channel.  Since we're not transitioning from 0-to-1 and
                        // since there's room, we can simply store the item and exit without having to
                        // worry about blocked/waiting readers.
                        parent._items.EnqueueTail(item);
                        return default;
                    }
                    else if (parent._mode == BoundedChannelFullMode.Wait)
                    {
                        // The channel is full and we're in a wait mode.  We need to queue a writer.

                        // If we're able to use the singleton writer, do so.
                        if (!cancellationToken.CanBeCanceled)
                        {
                            BlockedWriteAsyncOperation<T> singleton = _writerSingleton;
                            if (singleton.TryOwnAndReset())
                            {
                                singleton.Item = item;
                                ChannelUtilities.Enqueue(ref parent._blockedWritersHead, singleton);
                                return singleton.ValueTask;
                            }
                        }

                        // Otherwise, queue a new writer.
                        var writer = new BlockedWriteAsyncOperation<T>(runContinuationsAsynchronously: true, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate)
                        {
                            Item = item
                        };
                        ChannelUtilities.Enqueue(ref parent._blockedWritersHead, writer);
                        return writer.ValueTask;
                    }
                    else if (parent._mode == BoundedChannelFullMode.DropWrite)
                    {
                        // The channel is full and we're in ignore mode.
                        // Ignore the item but say we accepted it.
                        Monitor.Exit(parent.SyncObj);
                        releaseLock = false;
                        parent._itemDropped?.Invoke(item);
                        return default;
                    }
                    else
                    {
                        // The channel is full, and we're in a dropping mode.
                        // Drop either the oldest or the newest and write the new item.
                        T droppedItem = parent._mode == BoundedChannelFullMode.DropNewest ?
                            parent._items.DequeueTail() :
                            parent._items.DequeueHead();

                        parent._items.EnqueueTail(item);

                        Monitor.Exit(parent.SyncObj);
                        releaseLock = false;
                        parent._itemDropped?.Invoke(droppedItem);

                        return default;
                    }
                }
                finally
                {
                    if (releaseLock)
                    {
                        Monitor.Exit(parent.SyncObj);
                    }
                }

                // Now that we're outside of the lock, if we successfully got any tasks to complete and reserved their completion, do so.
                if (blockedReader is not null)
                {
                    Debug.Assert(waitingReadersHead is null);
                    blockedReader.DangerousSetResult(item);
                }
                else
                {
                    Debug.Assert(waitingReadersHead is not null);
                    ChannelUtilities.DangerousSetOperations(waitingReadersHead, result: true);
                }

                return default;
            }

            /// <summary>Gets the number of items in the channel. This should only be used by the debugger.</summary>
            private int ItemsCountForDebugger => _parent._items.Count;

            /// <summary>Gets the capacity of the channel. This should only be used by the debugger.</summary>
            private int CapacityForDebugger => _parent._bufferedCapacity;

            /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
            IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _parent._items.GetEnumerator();
        }

        /// <summary>Gets an object used to synchronize all state on the instance.</summary>
        private object SyncObj => _items;

        private Action<object?, CancellationToken> CancellationCallbackDelegate =>
            field ??= (state, cancellationToken) =>
            {
                AsyncOperation op = (AsyncOperation)state!;
                if (op.TrySetCanceled(cancellationToken))
                {
                    ChannelUtilities.UnsafeQueueUserWorkItem(static state => // escape cancellation callback
                    {
                        lock (state.Key.SyncObj)
                        {
                            switch (state.Value)
                            {
                                case BlockedReadAsyncOperation<T> blockedReader:
                                    ChannelUtilities.Remove(ref state.Key._blockedReadersHead, blockedReader);
                                    break;

                                case BlockedWriteAsyncOperation<T> blockedWriter:
                                    ChannelUtilities.Remove(ref state.Key._blockedWritersHead, blockedWriter);
                                    break;

                                case WaitingReadAsyncOperation waitingReader:
                                    ChannelUtilities.Remove(ref state.Key._waitingReadersHead, waitingReader);
                                    break;

                                case WaitingWriteAsyncOperation waitingWriter:
                                    ChannelUtilities.Remove(ref state.Key._waitingWritersHead, waitingWriter);
                                    break;

                                default:
                                    Debug.Fail($"Unexpected operation: {state.Value}");
                                    break;
                            }
                        }
                    }, new KeyValuePair<BoundedChannel<T>, AsyncOperation>(this, op));
                }
            };

        [Conditional("DEBUG")]
        private void AssertInvariants()
        {
            Debug.Assert(SyncObj is not null, "The sync obj must not be null.");
            Debug.Assert(Monitor.IsEntered(SyncObj), "Invariants can only be validated while holding the lock.");

            if (!_items.IsEmpty)
            {
                Debug.Assert(_blockedReadersHead is null, "There are items available, so there shouldn't be any blocked readers.");
                Debug.Assert(_waitingReadersHead is null, "There are items available, so there shouldn't be any waiting readers.");
            }

            if (_items.Count < _bufferedCapacity)
            {
                Debug.Assert(_blockedWritersHead is null, "There's space available, so there shouldn't be any blocked writers.");
                Debug.Assert(_waitingWritersHead is null, "There's space available, so there shouldn't be any waiting writers.");
            }

            if (_blockedReadersHead is not null)
            {
                Debug.Assert(_items.IsEmpty, "There shouldn't be queued items if there's a blocked reader.");
                Debug.Assert(_blockedWritersHead is null, "There shouldn't be any blocked writer if there's a blocked reader.");
            }

            if (_blockedWritersHead is not null)
            {
                Debug.Assert(_items.Count == _bufferedCapacity, "We should have a full buffer if there's a blocked writer.");
                Debug.Assert(_blockedReadersHead is null, "There shouldn't be any blocked readers if there's a blocked writer.");
            }

            if (_completion.Task.IsCompleted)
            {
                Debug.Assert(_doneWriting is not null, "We can only complete if we're done writing.");
            }
        }

        /// <summary>Gets the number of items in the channel.  This should only be used by the debugger.</summary>
        private int ItemsCountForDebugger => _items.Count;

        /// <summary>Report if the channel is closed or not. This should only be used by the debugger.</summary>
        private bool ChannelIsClosedForDebugger => _doneWriting is not null;

        /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
        IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _items.GetEnumerator();
    }
}
