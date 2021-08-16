// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
    /// <summary>Provides a channel with a bounded capacity.</summary>
    [DebuggerDisplay("Items={ItemsCountForDebugger}, Capacity={_bufferedCapacity}, Mode={_mode}, Closed={ChannelIsClosedForDebugger}")]
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
        /// <summary>Readers waiting to read from the channel.</summary>
        private readonly Deque<AsyncOperation<T>> _blockedReaders = new Deque<AsyncOperation<T>>();
        /// <summary>Writers waiting to write to the channel.</summary>
        private readonly Deque<VoidAsyncOperationWithData<T>> _blockedWriters = new Deque<VoidAsyncOperationWithData<T>>();
        /// <summary>Linked list of WaitToReadAsync waiters.</summary>
        private AsyncOperation<bool>? _waitingReadersTail;
        /// <summary>Linked list of WaitToWriteAsync waiters.</summary>
        private AsyncOperation<bool>? _waitingWritersTail;
        /// <summary>Whether to force continuations to be executed asynchronously from producer writes.</summary>
        private readonly bool _runContinuationsAsynchronously;
        /// <summary>Set to non-null once Complete has been called.</summary>
        private Exception? _doneWriting;
        /// <summary>Gets an object used to synchronize all state on the instance.</summary>
        private object SyncObj => _items;

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

        [DebuggerDisplay("Items={ItemsCountForDebugger}")]
        [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
        private sealed class BoundedChannelReader : ChannelReader<T>, IDebugEnumerable<T>
        {
            internal readonly BoundedChannel<T> _parent;
            private readonly AsyncOperation<T> _readerSingleton;
            private readonly AsyncOperation<bool> _waiterSingleton;

            internal BoundedChannelReader(BoundedChannel<T> parent)
            {
                _parent = parent;
                _readerSingleton = new AsyncOperation<T>(parent._runContinuationsAsynchronously, pooled: true);
                _waiterSingleton = new AsyncOperation<bool>(parent._runContinuationsAsynchronously, pooled: true);
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
                    if (parent._doneWriting != null)
                    {
                        return ChannelUtilities.GetInvalidCompletionValueTask<T>(parent._doneWriting);
                    }

                    // If we're able to use the singleton reader, do so.
                    if (!cancellationToken.CanBeCanceled)
                    {
                        AsyncOperation<T> singleton = _readerSingleton;
                        if (singleton.TryOwnAndReset())
                        {
                            parent._blockedReaders.EnqueueTail(singleton);
                            return singleton.ValueTaskOfT;
                        }
                    }

                    // Otherwise, queue a reader.  Note that in addition to checking whether synchronous continuations were requested,
                    // we also check whether the supplied cancellation token can be canceled.  The writer calls UnregisterCancellation
                    // while holding the lock, and if a callback needs to be unregistered and is currently running, it needs to wait
                    // for that callback to complete so that the subsequent code knows it won't be contending with another thread
                    // trying to complete the operation.  However, if we allowed a synchronous continuation from this operation, that
                    // cancellation callback could end up running arbitrary code, including code that called back into the reader or
                    // writer and tried to take the same lock held by the thread running UnregisterCancellation... deadlock.  As such,
                    // we only allow synchronous continuations here if both a) the caller requested it and the token isn't cancelable.
                    var reader = new AsyncOperation<T>(parent._runContinuationsAsynchronously | cancellationToken.CanBeCanceled, cancellationToken);
                    parent._blockedReaders.EnqueueTail(reader);
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
                    if (parent._doneWriting != null)
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
                        AsyncOperation<bool> singleton = _waiterSingleton;
                        if (singleton.TryOwnAndReset())
                        {
                            ChannelUtilities.QueueWaiter(ref parent._waitingReadersTail, singleton);
                            return singleton.ValueTaskOfT;
                        }
                    }

                    // Otherwise, queue a reader.  Note that in addition to checking whether synchronous continuations were requested,
                    // we also check whether the supplied cancellation token can be canceled.  The writer calls UnregisterCancellation
                    // while holding the lock, and if a callback needs to be unregistered and is currently running, it needs to wait
                    // for that callback to complete so that the subsequent code knows it won't be contending with another thread
                    // trying to complete the operation.  However, if we allowed a synchronous continuation from this operation, that
                    // cancellation callback could end up running arbitrary code, including code that called back into the reader or
                    // writer and tried to take the same lock held by the thread running UnregisterCancellation... deadlock.  As such,
                    // we only allow synchronous continuations here if both a) the caller requested it and the token isn't cancelable.
                    var waiter = new AsyncOperation<bool>(parent._runContinuationsAsynchronously | cancellationToken.CanBeCanceled, cancellationToken);
                    ChannelUtilities.QueueWaiter(ref _parent._waitingReadersTail, waiter);
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

                if (parent._doneWriting != null)
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
                    // remain in the physical queue).
                    //
                    // (It's possible for _doneWriting to be non-null due to Complete
                    // having been called but for there to still be blocked/waiting writers.
                    // This is a temporary condition, after which Complete has set _doneWriting
                    // and then exited the lock; at that point it'll proceed to clean this up,
                    // so we just ignore them.)

                    while (!parent._blockedWriters.IsEmpty)
                    {
                        VoidAsyncOperationWithData<T> w = parent._blockedWriters.DequeueHead();
                        if (w.TrySetResult(default))
                        {
                            parent._items.EnqueueTail(w.Item!);
                            return item;
                        }
                    }

                    // There was no blocked writer, so see if there's a WaitToWriteAsync
                    // we should wake up.
                    ChannelUtilities.WakeUpWaiters(ref parent._waitingWritersTail, result: true);
                }

                // Return the item
                return item;
            }

            /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
            IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _parent._items.GetEnumerator();
        }

        [DebuggerDisplay("Items={ItemsCountForDebugger}, Capacity={CapacityForDebugger}")]
        [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
        private sealed class BoundedChannelWriter : ChannelWriter<T>, IDebugEnumerable<T>
        {
            internal readonly BoundedChannel<T> _parent;
            private readonly VoidAsyncOperationWithData<T> _writerSingleton;
            private readonly AsyncOperation<bool> _waiterSingleton;

            internal BoundedChannelWriter(BoundedChannel<T> parent)
            {
                _parent = parent;
                _writerSingleton = new VoidAsyncOperationWithData<T>(runContinuationsAsynchronously: true, pooled: true);
                _waiterSingleton = new AsyncOperation<bool>(runContinuationsAsynchronously: true, pooled: true);
            }

            public override bool TryComplete(Exception? error)
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

                // At this point, _blockedReaders/Writers and _waitingReaders/Writers will not be mutated:
                // they're only mutated by readers/writers while holding the lock, and only if _doneWriting is null.
                // We also know that only one thread (this one) will ever get here, as only that thread
                // will be the one to transition from _doneWriting false to true.  As such, we can
                // freely manipulate them without any concurrency concerns.
                ChannelUtilities.FailOperations<AsyncOperation<T>, T>(parent._blockedReaders, ChannelUtilities.CreateInvalidCompletionException(error));
                ChannelUtilities.FailOperations<VoidAsyncOperationWithData<T>, VoidResult>(parent._blockedWriters, ChannelUtilities.CreateInvalidCompletionException(error));
                ChannelUtilities.WakeUpWaiters(ref parent._waitingReadersTail, result: false, error: error);
                ChannelUtilities.WakeUpWaiters(ref parent._waitingWritersTail, result: false, error: error);

                // Successfully transitioned to completed.
                return true;
            }

            public override bool TryWrite(T item)
            {
                AsyncOperation<T>? blockedReader = null;
                AsyncOperation<bool>? waitingReadersTail = null;

                BoundedChannel<T> parent = _parent;

                bool releaseLock = false;
                try
                {
                    Monitor.Enter(parent.SyncObj, ref releaseLock);

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
                        // There are no items in the channel, which means we may have blocked/waiting readers.

                        // If there are any blocked readers, find one that's not canceled
                        // and store it to complete outside of the lock, in case it has
                        // continuations that'll run synchronously
                        while (!parent._blockedReaders.IsEmpty)
                        {
                            AsyncOperation<T> r = parent._blockedReaders.DequeueHead();
                            if (r.UnregisterCancellation()) // ensure that once we grab it, we own its completion
                            {
                                blockedReader = r;
                                break;
                            }
                        }

                        if (blockedReader == null)
                        {
                            // If there wasn't a blocked reader, then store the item. If no one's waiting
                            // to be notified about a 0-to-1 transition, we're done.
                            parent._items.EnqueueTail(item);
                            waitingReadersTail = parent._waitingReadersTail;
                            if (waitingReadersTail == null)
                            {
                                return true;
                            }
                            parent._waitingReadersTail = null;
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

                // We either wrote the item already, or we're transferring it to the blocked reader we grabbed.
                if (blockedReader != null)
                {
                    Debug.Assert(waitingReadersTail == null, "Shouldn't have any waiters to wake up");

                    // Transfer the written item to the blocked reader.
                    bool success = blockedReader.TrySetResult(item);
                    Debug.Assert(success, "We should always be able to complete the reader.");
                }
                else
                {
                    // We stored an item bringing the count up from 0 to 1.  Alert
                    // any waiting readers that there may be something for them to consume.
                    // Since we're no longer holding the lock, it's possible we'll end up
                    // waking readers that have since come in.
                    ChannelUtilities.WakeUpWaiters(ref waitingReadersTail, result: true);
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
                    if (parent._doneWriting != null)
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
                        AsyncOperation<bool> singleton = _waiterSingleton;
                        if (singleton.TryOwnAndReset())
                        {
                            ChannelUtilities.QueueWaiter(ref parent._waitingWritersTail, singleton);
                            return singleton.ValueTaskOfT;
                        }
                    }

                    // Otherwise, queue a waiter.
                    var waiter = new AsyncOperation<bool>(runContinuationsAsynchronously: true, cancellationToken);
                    ChannelUtilities.QueueWaiter(ref parent._waitingWritersTail, waiter);
                    return waiter.ValueTaskOfT;
                }
            }

            public override ValueTask WriteAsync(T item, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask(Task.FromCanceled(cancellationToken));
                }

                AsyncOperation<T>? blockedReader = null;
                AsyncOperation<bool>? waitingReadersTail = null;

                BoundedChannel<T> parent = _parent;

                bool releaseLock = false;
                try
                {
                    Monitor.Enter(parent.SyncObj, ref releaseLock);

                    parent.AssertInvariants();

                    // If we're done writing, trying to write is an error.
                    if (parent._doneWriting != null)
                    {
                        return new ValueTask(Task.FromException(ChannelUtilities.CreateInvalidCompletionException(parent._doneWriting)));
                    }

                    // Get the number of items in the channel currently.
                    int count = parent._items.Count;

                    if (count == 0)
                    {
                        // There are no items in the channel, which means we may have blocked/waiting readers.

                        // If there are any blocked readers, find one that's not canceled
                        // and store it to complete outside of the lock, in case it has
                        // continuations that'll run synchronously
                        while (!parent._blockedReaders.IsEmpty)
                        {
                            AsyncOperation<T> r = parent._blockedReaders.DequeueHead();
                            if (r.UnregisterCancellation()) // ensure that once we grab it, we own its completion
                            {
                                blockedReader = r;
                                break;
                            }
                        }

                        if (blockedReader == null)
                        {
                            // If there wasn't a blocked reader, then store the item. If no one's waiting
                            // to be notified about a 0-to-1 transition, we're done.
                            parent._items.EnqueueTail(item);
                            waitingReadersTail = parent._waitingReadersTail;
                            if (waitingReadersTail == null)
                            {
                                return default;
                            }
                            parent._waitingReadersTail = null;
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
                            VoidAsyncOperationWithData<T> singleton = _writerSingleton;
                            if (singleton.TryOwnAndReset())
                            {
                                singleton.Item = item;
                                parent._blockedWriters.EnqueueTail(singleton);
                                return singleton.ValueTask;
                            }
                        }

                        // Otherwise, queue a new writer.
                        var writer = new VoidAsyncOperationWithData<T>(runContinuationsAsynchronously: true, cancellationToken);
                        writer.Item = item;
                        parent._blockedWriters.EnqueueTail(writer);
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

                // We either wrote the item already, or we're transfering it to the blocked reader we grabbed.
                if (blockedReader != null)
                {
                    // Transfer the written item to the blocked reader.
                    bool success = blockedReader.TrySetResult(item);
                    Debug.Assert(success, "We should always be able to complete the reader.");
                }
                else
                {
                    // We stored an item bringing the count up from 0 to 1.  Alert
                    // any waiting readers that there may be something for them to consume.
                    // Since we're no longer holding the lock, it's possible we'll end up
                    // waking readers that have since come in.
                    ChannelUtilities.WakeUpWaiters(ref waitingReadersTail, result: true);
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

        [Conditional("DEBUG")]
        private void AssertInvariants()
        {
            Debug.Assert(SyncObj != null, "The sync obj must not be null.");
            Debug.Assert(Monitor.IsEntered(SyncObj), "Invariants can only be validated while holding the lock.");

            if (!_items.IsEmpty)
            {
                Debug.Assert(_blockedReaders.IsEmpty, "There are items available, so there shouldn't be any blocked readers.");
                Debug.Assert(_waitingReadersTail == null, "There are items available, so there shouldn't be any waiting readers.");
            }
            if (_items.Count < _bufferedCapacity)
            {
                Debug.Assert(_blockedWriters.IsEmpty, "There's space available, so there shouldn't be any blocked writers.");
                Debug.Assert(_waitingWritersTail == null, "There's space available, so there shouldn't be any waiting writers.");
            }
            if (!_blockedReaders.IsEmpty)
            {
                Debug.Assert(_items.IsEmpty, "There shouldn't be queued items if there's a blocked reader.");
                Debug.Assert(_blockedWriters.IsEmpty, "There shouldn't be any blocked writer if there's a blocked reader.");
            }
            if (!_blockedWriters.IsEmpty)
            {
                Debug.Assert(_items.Count == _bufferedCapacity, "We should have a full buffer if there's a blocked writer.");
                Debug.Assert(_blockedReaders.IsEmpty, "There shouldn't be any blocked readers if there's a blocked writer.");
            }
            if (_completion.Task.IsCompleted)
            {
                Debug.Assert(_doneWriting != null, "We can only complete if we're done writing.");
            }
        }

        /// <summary>Gets the number of items in the channel.  This should only be used by the debugger.</summary>
        private int ItemsCountForDebugger => _items.Count;

        /// <summary>Report if the channel is closed or not. This should only be used by the debugger.</summary>
        private bool ChannelIsClosedForDebugger => _doneWriting != null;

        /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
        IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _items.GetEnumerator();
    }
}
