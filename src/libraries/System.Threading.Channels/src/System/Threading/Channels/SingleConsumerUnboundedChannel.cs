// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
    /// <summary>
    /// Provides a buffered channel of unbounded capacity for use by any number
    /// of writers but at most a single reader at a time.
    /// </summary>
    [DebuggerDisplay("Items={ItemsCountForDebugger}, Closed={ChannelIsClosedForDebugger}")]
    [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
    internal sealed class SingleConsumerUnboundedChannel<T> : Channel<T>, IDebugEnumerable<T>
    {
        /// <summary>Task that indicates the channel has completed.</summary>
        private readonly TaskCompletionSource<VoidResult> _completion;
        /// <summary>
        /// A concurrent queue to hold the items for this channel.  The queue itself supports at most
        /// one writer and one reader at a time; as a result, since this channel supports multiple writers,
        /// all write access to the queue must be synchronized by the channel.
        /// </summary>
        private readonly SingleProducerSingleConsumerQueue<T> _items = new SingleProducerSingleConsumerQueue<T>();
        /// <summary>Whether to force continuations to be executed asynchronously from producer writes.</summary>
        private readonly bool _runContinuationsAsynchronously;

        /// <summary>non-null if the channel has been marked as complete for writing.</summary>
        private volatile Exception? _doneWriting;

        /// <summary>An <see cref="AsyncOperation{T}"/> if there's a blocked reader.</summary>
        private AsyncOperation<T>? _blockedReader;

        /// <summary>A waiting reader (e.g. WaitForReadAsync) if there is one.</summary>
        private AsyncOperation<bool>? _waitingReader;

        /// <summary>Initialize the channel.</summary>
        /// <param name="runContinuationsAsynchronously">Whether to force continuations to be executed asynchronously.</param>
        internal SingleConsumerUnboundedChannel(bool runContinuationsAsynchronously)
        {
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
            _completion = new TaskCompletionSource<VoidResult>(runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);

            Reader = new UnboundedChannelReader(this);
            Writer = new UnboundedChannelWriter(this);
        }

        [DebuggerDisplay("Items={ItemsCountForDebugger}")]
        [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
        private sealed class UnboundedChannelReader : ChannelReader<T>, IDebugEnumerable<T>
        {
            internal readonly SingleConsumerUnboundedChannel<T> _parent;
            private readonly AsyncOperation<T> _readerSingleton;
            private readonly AsyncOperation<bool> _waiterSingleton;

            internal UnboundedChannelReader(SingleConsumerUnboundedChannel<T> parent)
            {
                _parent = parent;
                _readerSingleton = new AsyncOperation<T>(parent._runContinuationsAsynchronously, pooled: true);
                _waiterSingleton = new AsyncOperation<bool>(parent._runContinuationsAsynchronously, pooled: true);
            }

            public override Task Completion => _parent._completion.Task;

            public override ValueTask<T> ReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<T>(Task.FromCanceled<T>(cancellationToken));
                }

                if (TryRead(out T item))
                {
                    return new ValueTask<T>(item);
                }

                SingleConsumerUnboundedChannel<T> parent = _parent;

                AsyncOperation<T>? oldBlockedReader, newBlockedReader;
                lock (parent.SyncObj)
                {
                    // Now that we hold the lock, try reading again.
                    if (TryRead(out item))
                    {
                        return new ValueTask<T>(item);
                    }

                    // If no more items will be written, fail the read.
                    if (parent._doneWriting != null)
                    {
                        return ChannelUtilities.GetInvalidCompletionValueTask<T>(parent._doneWriting);
                    }

                    // Try to use the singleton reader.  If it's currently being used, then the channel
                    // is being used erroneously, and we cancel the outstanding operation.
                    oldBlockedReader = parent._blockedReader;
                    if (!cancellationToken.CanBeCanceled && _readerSingleton.TryOwnAndReset())
                    {
                        newBlockedReader = _readerSingleton;
                        if (newBlockedReader == oldBlockedReader)
                        {
                            // The previous operation completed, so null out the "old" reader
                            // so we don't end up canceling the new operation.
                            oldBlockedReader = null;
                        }
                    }
                    else
                    {
                        newBlockedReader = new AsyncOperation<T>(_parent._runContinuationsAsynchronously, cancellationToken);
                    }
                    parent._blockedReader = newBlockedReader;
                }

                oldBlockedReader?.TrySetCanceled(default);
                return newBlockedReader.ValueTaskOfT;
            }

            public override bool TryRead([MaybeNullWhen(false)] out T item)
            {
                SingleConsumerUnboundedChannel<T> parent = _parent;
                if (parent._items.TryDequeue(out item))
                {
                    if (parent._doneWriting != null && parent._items.IsEmpty)
                    {
                        ChannelUtilities.Complete(parent._completion, parent._doneWriting);
                    }
                    return true;
                }
                return false;
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
            {
                // Outside of the lock, check if there are any items waiting to be read.  If there are, we're done.
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
                }

                if (!_parent._items.IsEmpty)
                {
                    return new ValueTask<bool>(true);
                }

                SingleConsumerUnboundedChannel<T> parent = _parent;
                AsyncOperation<bool>? oldWaitingReader = null, newWaitingReader;
                lock (parent.SyncObj)
                {
                    // Again while holding the lock, check to see if there are any items available.
                    if (!parent._items.IsEmpty)
                    {
                        return new ValueTask<bool>(true);
                    }

                    // There aren't any items; if we're done writing, there never will be more items.
                    if (parent._doneWriting != null)
                    {
                        return parent._doneWriting != ChannelUtilities.s_doneWritingSentinel ?
                            new ValueTask<bool>(Task.FromException<bool>(parent._doneWriting)) :
                            default;
                    }

                    // Try to use the singleton waiter.  If it's currently being used, then the channel
                    // is being used erroneously, and we cancel the outstanding operation.
                    oldWaitingReader = parent._waitingReader;
                    if (!cancellationToken.CanBeCanceled && _waiterSingleton.TryOwnAndReset())
                    {
                        newWaitingReader = _waiterSingleton;
                        if (newWaitingReader == oldWaitingReader)
                        {
                            // The previous operation completed, so null out the "old" waiter
                            // so we don't end up canceling the new operation.
                            oldWaitingReader = null;
                        }
                    }
                    else
                    {
                        newWaitingReader = new AsyncOperation<bool>(_parent._runContinuationsAsynchronously, cancellationToken);
                    }
                    parent._waitingReader = newWaitingReader;
                }

                oldWaitingReader?.TrySetCanceled(default);
                return newWaitingReader.ValueTaskOfT;
            }

            /// <summary>Gets the number of items in the channel.  This should only be used by the debugger.</summary>
            private int ItemsCountForDebugger => _parent._items.Count;

            /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
            IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _parent._items.GetEnumerator();
        }

        [DebuggerDisplay("Items={ItemsCountForDebugger}")]
        [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
        private sealed class UnboundedChannelWriter : ChannelWriter<T>, IDebugEnumerable<T>
        {
            internal readonly SingleConsumerUnboundedChannel<T> _parent;
            internal UnboundedChannelWriter(SingleConsumerUnboundedChannel<T> parent) => _parent = parent;

            public override bool TryComplete(Exception? error)
            {
                AsyncOperation<T>? blockedReader = null;
                AsyncOperation<bool>? waitingReader = null;
                bool completeTask = false;

                SingleConsumerUnboundedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    // If we're already marked as complete, there's nothing more to do.
                    if (parent._doneWriting != null)
                    {
                        return false;
                    }

                    // Mark as complete for writing.
                    parent._doneWriting = error ?? ChannelUtilities.s_doneWritingSentinel;

                    // If we have no more items remaining, then the channel needs to be marked as completed
                    // and readers need to be informed they'll never get another item.  All of that needs
                    // to happen outside of the lock to avoid invoking continuations under the lock.
                    if (parent._items.IsEmpty)
                    {
                        completeTask = true;

                        if (parent._blockedReader != null)
                        {
                            blockedReader = parent._blockedReader;
                            parent._blockedReader = null;
                        }

                        if (parent._waitingReader != null)
                        {
                            waitingReader = parent._waitingReader;
                            parent._waitingReader = null;
                        }
                    }
                }

                // Complete the channel task if necessary
                if (completeTask)
                {
                    ChannelUtilities.Complete(parent._completion, error);
                }

                Debug.Assert(blockedReader == null || waitingReader == null, "There should only ever be at most one reader.");

                // Complete a blocked reader if necessary
                if (blockedReader != null)
                {
                    error = ChannelUtilities.CreateInvalidCompletionException(error);
                    blockedReader.TrySetException(error);
                }

                // Complete a waiting reader if necessary.  (We really shouldn't have both a blockedReader
                // and a waitingReader, but it's more expensive to prevent it than to just tolerate it.)
                if (waitingReader != null)
                {
                    if (error != null)
                    {
                        waitingReader.TrySetException(error);
                    }
                    else
                    {
                        waitingReader.TrySetResult(item: false);
                    }
                }

                // Successfully completed the channel
                return true;
            }

            public override bool TryWrite(T item)
            {
                SingleConsumerUnboundedChannel<T> parent = _parent;
                while (true) // in case a reader was canceled and we need to try again
                {
                    AsyncOperation<T>? blockedReader = null;
                    AsyncOperation<bool>? waitingReader = null;

                    lock (parent.SyncObj)
                    {
                        // If writing is completed, exit out without writing.
                        if (parent._doneWriting != null)
                        {
                            return false;
                        }

                        // If there's a blocked reader, store it into a local for completion outside of the lock.
                        // If there isn't a blocked reader, queue the item being written; then if there's a waiting
                        blockedReader = parent._blockedReader;
                        if (blockedReader != null)
                        {
                            parent._blockedReader = null;
                        }
                        else
                        {
                            parent._items.Enqueue(item);

                            waitingReader = parent._waitingReader;
                            if (waitingReader == null)
                            {
                                return true;
                            }
                            parent._waitingReader = null;
                        }
                    }

                    // If we get here, we grabbed a blocked or a waiting reader.
                    Debug.Assert((blockedReader != null) ^ (waitingReader != null), "Expected either a blocked or waiting reader, but not both");

                    // If we have a waiting reader, notify it that an item was written and exit.
                    if (waitingReader != null)
                    {
                        // If we get here, we grabbed a waiting reader.
                        waitingReader.TrySetResult(item: true);
                        return true;
                    }

                    // Otherwise we have a blocked reader: complete it with the item being written.
                    // In the case of a ReadAsync(CancellationToken), it's possible the reader could
                    // have been completed due to cancellation by the time we get here.  In that case,
                    // we'll loop around to try again so as not to lose the item being written.
                    Debug.Assert(blockedReader != null);
                    if (blockedReader.TrySetResult(item))
                    {
                        return true;
                    }
                }
            }

            public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken)
            {
                Exception? doneWriting = _parent._doneWriting;
                return
                    cancellationToken.IsCancellationRequested ? new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken)) :
                    doneWriting == null ? new ValueTask<bool>(true) :
                    doneWriting != ChannelUtilities.s_doneWritingSentinel ? new ValueTask<bool>(Task.FromException<bool>(doneWriting)) :
                    default;
            }

            public override ValueTask WriteAsync(T item, CancellationToken cancellationToken) =>
                // Writing always succeeds (unless we've already completed writing or cancellation has been requested),
                // so just TryWrite and return a completed task.
                cancellationToken.IsCancellationRequested ? new ValueTask(Task.FromCanceled(cancellationToken)) :
                TryWrite(item) ? default :
                new ValueTask(Task.FromException(ChannelUtilities.CreateInvalidCompletionException(_parent._doneWriting)));

            /// <summary>Gets the number of items in the channel. This should only be used by the debugger.</summary>
            private int ItemsCountForDebugger => _parent._items.Count;

            /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
            IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _parent._items.GetEnumerator();
        }

        private object SyncObj => _items;

        /// <summary>Gets the number of items in the channel.  This should only be used by the debugger.</summary>
        private int ItemsCountForDebugger => _items.Count;

        /// <summary>Report if the channel is closed or not. This should only be used by the debugger.</summary>
        private bool ChannelIsClosedForDebugger => _doneWriting != null;

        /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
        IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _items.GetEnumerator();
    }
}
