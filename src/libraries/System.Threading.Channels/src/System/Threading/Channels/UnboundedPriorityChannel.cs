// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

// This file is primarily a copy of UnboundedChannel, subsequently tweaked to account for differences
// between ConcurrentQueue<T> and PriorityQueue<bool, T>, e.g. that PQ isn't thread safe and so fast
// paths outside of locks need to be removed, that Enqueue/Dequeue methods take priorities, etc. Any
// changes made to this or that file should largely be kept in sync.

namespace System.Threading.Channels
{
    /// <summary>Provides a buffered channel of unbounded capacity.</summary>
    [DebuggerDisplay("Items = {ItemsCountForDebugger}, Closed = {ChannelIsClosedForDebugger}")]
    [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
    internal sealed class UnboundedPrioritizedChannel<T> : Channel<T>, IDebugEnumerable<T>
    {
        /// <summary>Task that indicates the channel has completed.</summary>
        private readonly TaskCompletionSource _completion;

        /// <summary>The items in the channel.</summary>
        /// <remarks>To avoid double storing of a potentially large struct T, the priority doubles as the element and the element is ignored.</remarks>
        private readonly PriorityQueue<bool, T> _items;

        /// <summary>Whether to force continuations to be executed asynchronously from producer writes.</summary>
        private readonly bool _runContinuationsAsynchronously;

        /// <summary>Readers blocked reading from the channel.</summary>
        private BlockedReadAsyncOperation<T>? _blockedReadersHead;

        /// <summary>Readers waiting for a notification that data is available.</summary>
        private WaitingReadAsyncOperation? _waitingReadersHead;

        /// <summary>Set to non-null once Complete has been called.</summary>
        private Exception? _doneWriting;

        /// <summary>Initialize the channel.</summary>
        internal UnboundedPrioritizedChannel(bool runContinuationsAsynchronously, IComparer<T>? comparer)
        {
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
            _completion = new TaskCompletionSource(runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);

            _items = new PriorityQueue<bool, T>(comparer);

            Reader = new UnboundedPrioritizedChannelReader(this);
            Writer = new UnboundedPrioritizedChannelWriter(this);
        }

        [DebuggerDisplay("Items = {Count}")]
        [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
        private sealed class UnboundedPrioritizedChannelReader : ChannelReader<T>, IDebugEnumerable<T>
        {
            internal readonly UnboundedPrioritizedChannel<T> _parent;
            private readonly BlockedReadAsyncOperation<T> _readerSingleton;
            private readonly WaitingReadAsyncOperation _waiterSingleton;

            internal UnboundedPrioritizedChannelReader(UnboundedPrioritizedChannel<T> parent)
            {
                _parent = parent;
                _readerSingleton = new BlockedReadAsyncOperation<T>(parent._runContinuationsAsynchronously, pooled: true);
                _waiterSingleton = new WaitingReadAsyncOperation(parent._runContinuationsAsynchronously, pooled: true);
            }

            public override Task Completion => _parent._completion.Task;

            public override bool CanCount => true;

            public override bool CanPeek => true;

            public override int Count => _parent._items.Count;

            public override ValueTask<T> ReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return ValueTask.FromCanceled<T>(cancellationToken);
                }

                // Dequeue an item if we can.
                UnboundedPrioritizedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // Try to dequeue again, now that we hold the lock.
                    if (parent._items.TryDequeue(out _, out T? item))
                    {
                        CompleteIfDone(parent);
                        return new ValueTask<T>(item);
                    }

                    // There are no items, so if we're done writing, fail.
                    if (parent._doneWriting is not null)
                    {
                        return ChannelUtilities.GetInvalidCompletionValueTask<T>(parent._doneWriting);
                    }

                    // If we're able to use the singleton reader, do so. Otherwise, create a new reader.
                    BlockedReadAsyncOperation<T> reader =
                        !cancellationToken.CanBeCanceled && _readerSingleton.TryOwnAndReset() ? _readerSingleton :
                        new BlockedReadAsyncOperation<T>(parent._runContinuationsAsynchronously, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
                    ChannelUtilities.Enqueue(ref parent._blockedReadersHead, reader);
                    return reader.ValueTaskOfT;
                }
            }

            public override bool TryRead([MaybeNullWhen(false)] out T item)
            {
                UnboundedPrioritizedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    // Dequeue an item if we can
                    if (parent._items.TryDequeue(out _, out item))
                    {
                        CompleteIfDone(parent);
                        return true;
                    }

                    item = default;
                    return false;
                }
            }

            public override bool TryPeek([MaybeNullWhen(false)] out T item)
            {
                UnboundedPrioritizedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    return parent._items.TryPeek(out _, out item);
                }
            }

            private static void CompleteIfDone(UnboundedPrioritizedChannel<T> parent)
            {
                Debug.Assert(Monitor.IsEntered(parent.SyncObj));

                if (parent._doneWriting is not null && parent._items.Count == 0)
                {
                    // If we've now emptied the items queue and we're not getting any more, complete.
                    ChannelUtilities.Complete(parent._completion, parent._doneWriting);
                }
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return ValueTask.FromCanceled<bool>(cancellationToken);
                }

                UnboundedPrioritizedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // Try again to read now that we're synchronized with writers.
                    if (parent._items.Count != 0)
                    {
                        return new ValueTask<bool>(true);
                    }

                    // There are no items, so if we're done writing, there's never going to be data available.
                    if (parent._doneWriting is not null)
                    {
                        return parent._doneWriting != ChannelUtilities.s_doneWritingSentinel ?
                            ValueTask.FromException<bool>(parent._doneWriting) :
                            default;
                    }

                    // If we're able to use the singleton waiter, do so. Otherwise, create a new waiter.
                    WaitingReadAsyncOperation waiter =
                        !cancellationToken.CanBeCanceled && _waiterSingleton.TryOwnAndReset() ? _waiterSingleton :
                        new(parent._runContinuationsAsynchronously, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
                    ChannelUtilities.Enqueue(ref parent._waitingReadersHead, waiter);
                    return waiter.ValueTaskOfT;
                }
            }

            /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
            IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _parent.GetEnumerator();
        }

        [DebuggerDisplay("Items = {ItemsCountForDebugger}")]
        [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
        private sealed class UnboundedPrioritizedChannelWriter : ChannelWriter<T>, IDebugEnumerable<T>
        {
            internal readonly UnboundedPrioritizedChannel<T> _parent;

            internal UnboundedPrioritizedChannelWriter(UnboundedPrioritizedChannel<T> parent) => _parent = parent;

            public override bool TryComplete(Exception? error)
            {
                UnboundedPrioritizedChannel<T> parent = _parent;

                BlockedReadAsyncOperation<T>? blockedReadersHead;
                WaitingReadAsyncOperation? waitingReadersHead;

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
                    completeTask = parent._items.Count == 0;

                    // Snag the queues while holding the lock, so that we don't need to worry
                    // about concurrent mutation, such as from cancellation of pending operations.
                    blockedReadersHead = parent._blockedReadersHead;
                    waitingReadersHead = parent._waitingReadersHead;
                    parent._blockedReadersHead = null;
                    parent._waitingReadersHead = null;
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
                ChannelUtilities.SetOrFailOperations(waitingReadersHead, result: false, error: error);

                // Successfully transitioned to completed.
                return true;
            }

            public override bool TryWrite(T item)
            {
                UnboundedPrioritizedChannel<T> parent = _parent;

                BlockedReadAsyncOperation<T>? blockedReader = null;
                WaitingReadAsyncOperation? waitingReadersHead = null;
                lock (parent.SyncObj)
                {
                    // If writing has already been marked as done, fail the write.
                    parent.AssertInvariants();
                    if (parent._doneWriting is not null)
                    {
                        return false;
                    }

                    // Try to get a blocked reader that we can transfer the item to.
                    blockedReader = ChannelUtilities.TryDequeueAndReserveCompletionIfCancelable(ref parent._blockedReadersHead);

                    // If we weren't able to get a reader, instead queue the item and get any waiters that need to be notified.
                    if (blockedReader is null)
                    {
                        parent._items.Enqueue(true, item);
                        waitingReadersHead = ChannelUtilities.TryReserveCompletionIfCancelable(ref parent._waitingReadersHead);
                    }
                }

                // Now that we're outside of the lock, if we successfully got any tasks to complete and reserved their completion, do so.
                if (blockedReader is not null)
                {
                    blockedReader.DangerousSetResult(item);
                }
                else if (waitingReadersHead is not null)
                {
                    ChannelUtilities.DangerousSetOperations(waitingReadersHead, result: true);
                }

                return true;
            }

            public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken)
            {
                Exception? doneWriting = _parent._doneWriting;
                return
                    cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<bool>(cancellationToken) :
                    doneWriting is null ? new ValueTask<bool>(true) : // unbounded writing can always be done if we haven't completed
                    doneWriting != ChannelUtilities.s_doneWritingSentinel ? ValueTask.FromException<bool>(doneWriting) :
                    default;
            }

            public override ValueTask WriteAsync(T item, CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled(cancellationToken) :
                TryWrite(item) ? default :
                ValueTask.FromException(ChannelUtilities.CreateInvalidCompletionException(_parent._doneWriting));

            /// <summary>Gets the number of items in the channel. This should only be used by the debugger.</summary>
            private int ItemsCountForDebugger => _parent._items.Count;

            /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
            IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _parent.GetEnumerator();
        }

        /// <summary>Gets the object used to synchronize access to all state on this instance.</summary>
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

                                case WaitingReadAsyncOperation waitingReader:
                                    ChannelUtilities.Remove(ref state.Key._waitingReadersHead, waitingReader);
                                    break;

                                default:
                                    Debug.Fail($"Unexpected operation: {state.Value}");
                                    break;
                            }
                        }
                    }, new KeyValuePair<UnboundedPrioritizedChannel<T>, AsyncOperation>(this, op));
                }
            };

        [Conditional("DEBUG")]
        private void AssertInvariants()
        {
            Debug.Assert(SyncObj is not null, "The sync obj must not be null.");
            Debug.Assert(Monitor.IsEntered(SyncObj), "Invariants can only be validated while holding the lock.");

            if (_items.Count != 0)
            {
                if (_runContinuationsAsynchronously)
                {
                    Debug.Assert(_blockedReadersHead is null, "There's data available, so there shouldn't be any blocked readers.");
                    Debug.Assert(_waitingReadersHead is null, "There's data available, so there shouldn't be any waiting readers.");
                }
                Debug.Assert(!_completion.Task.IsCompleted, "We still have data available, so shouldn't be completed.");
            }

            if ((_blockedReadersHead is not null || _waitingReadersHead is not null) && _runContinuationsAsynchronously)
            {
                Debug.Assert(_items.Count == 0, "There are blocked/waiting readers, so there shouldn't be any data available.");
            }

            if (_completion.Task.IsCompleted)
            {
                Debug.Assert(_doneWriting is not null, "We're completed, so we must be done writing.");
            }
        }

        /// <summary>Gets the number of items in the channel.  This should only be used by the debugger.</summary>
        private int ItemsCountForDebugger => _items.Count;

        /// <summary>Report if the channel is closed or not. This should only be used by the debugger.</summary>
        private bool ChannelIsClosedForDebugger => _doneWriting is not null;

        /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
        public IEnumerator<T> GetEnumerator()
        {
            List<T> list = [];
            foreach ((bool _, T Priority) item in _items.UnorderedItems)
            {
                list.Add(item.Priority);
            }

            list.Sort(_items.Comparer);

            return list.GetEnumerator();
        }
    }
}
