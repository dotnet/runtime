// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

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
        private volatile Exception _doneWriting;

        /// <summary>A <see cref="ReaderInteractor{T}"/> if there's a blocked reader.</summary>
        private ReaderInteractor<T> _blockedReader;

        /// <summary>A waiting reader (e.g. WaitForReadAsync) if there is one.</summary>
        private ReaderInteractor<bool> _waitingReader;

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
            internal UnboundedChannelReader(SingleConsumerUnboundedChannel<T> parent) => _parent = parent;

            public override Task Completion => _parent._completion.Task;

            public override ValueTask<T> ReadAsync(CancellationToken cancellationToken)
            {
                {
                    return TryRead(out T item) ?
                        new ValueTask<T>(item) :
                        ReadAsyncCore(cancellationToken);
                }

                ValueTask<T> ReadAsyncCore(CancellationToken ct)
                {
                    SingleConsumerUnboundedChannel<T> parent = _parent;
                    if (ct.IsCancellationRequested)
                    {
                        return new ValueTask<T>(Task.FromCanceled<T>(ct));
                    }

                    lock (parent.SyncObj)
                    {
                        // Now that we hold the lock, try reading again.
                        if (TryRead(out T item))
                        {
                            return new ValueTask<T>(item);
                        }

                        // If no more items will be written, fail the read.
                        if (parent._doneWriting != null)
                        {
                            return ChannelUtilities.GetInvalidCompletionValueTask<T>(parent._doneWriting);
                        }

                        Debug.Assert(parent._blockedReader == null || parent._blockedReader.Task.IsCanceled,
                            "Incorrect usage; multiple outstanding reads were issued against this single-consumer channel");

                        // Store the reader to be completed by a writer.
                        var reader = ReaderInteractor<T>.Create(parent._runContinuationsAsynchronously, ct);
                        parent._blockedReader = reader;
                        return new ValueTask<T>(reader.Task);
                    }
                }
            }

            public override bool TryRead(out T item)
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

            public override Task<bool> WaitToReadAsync(CancellationToken cancellationToken)
            {
                // Outside of the lock, check if there are any items waiting to be read.  If there are, we're done.
                return
                    cancellationToken.IsCancellationRequested ? Task.FromCanceled<bool>(cancellationToken) :
                    !_parent._items.IsEmpty ? ChannelUtilities.s_trueTask :
                    WaitToReadAsyncCore(cancellationToken);

                Task<bool> WaitToReadAsyncCore(CancellationToken ct)
                {
                    SingleConsumerUnboundedChannel<T> parent = _parent;
                    ReaderInteractor<bool> oldWaiter = null, newWaiter;
                    lock (parent.SyncObj)
                    {
                        // Again while holding the lock, check to see if there are any items available.
                        if (!parent._items.IsEmpty)
                        {
                            return ChannelUtilities.s_trueTask;
                        }

                        // There aren't any items; if we're done writing, there never will be more items.
                        if (parent._doneWriting != null)
                        {
                            return parent._doneWriting != ChannelUtilities.s_doneWritingSentinel ?
                                Task.FromException<bool>(parent._doneWriting) :
                                ChannelUtilities.s_falseTask;
                        }

                        // Create the new waiter.  We're a bit more tolerant of a stray waiting reader
                        // than we are of a blocked reader, as with usage patterns it's easier to leave one
                        // behind, so we just cancel any that may have been waiting around.
                        oldWaiter = parent._waitingReader;
                        parent._waitingReader = newWaiter = ReaderInteractor<bool>.Create(parent._runContinuationsAsynchronously, ct);
                    }

                    oldWaiter?.TrySetCanceled();
                    return newWaiter.Task;
                }
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

            public override bool TryComplete(Exception error)
            {
                ReaderInteractor<T> blockedReader = null;
                ReaderInteractor<bool> waitingReader = null;
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
                    blockedReader.Fail(error);
                }

                // Complete a waiting reader if necessary.  (We really shouldn't have both a blockedReader
                // and a waitingReader, but it's more expensive to prevent it than to just tolerate it.)
                if (waitingReader != null)
                {
                    if (error != null)
                    {
                        waitingReader.Fail(error);
                    }
                    else
                    {
                        waitingReader.Success(item: false);
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
                    ReaderInteractor<T> blockedReader = null;
                    ReaderInteractor<bool> waitingReader = null;

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
                    {                // If we get here, we grabbed a waiting reader.
                        waitingReader.Success(item: true);
                        return true;
                    }

                    // Otherwise we have a blocked reader: complete it with the item being written.
                    // In the case of a ReadAsync(CancellationToken), it's possible the reader could
                    // have been completed due to cancellation by the time we get here.  In that case,
                    // we'll loop around to try again so as not to lose the item being written.
                    Debug.Assert(blockedReader != null);
                    if (blockedReader.Success(item))
                    {
                        return true;
                    }
                }
            }

            public override Task<bool> WaitToWriteAsync(CancellationToken cancellationToken)
            {
                Exception doneWriting = _parent._doneWriting;
                return
                    cancellationToken.IsCancellationRequested ? Task.FromCanceled<bool>(cancellationToken) :
                    doneWriting == null ? ChannelUtilities.s_trueTask :
                    doneWriting != ChannelUtilities.s_doneWritingSentinel ? Task.FromException<bool>(doneWriting) :
                    ChannelUtilities.s_falseTask;
            }

            public override Task WriteAsync(T item, CancellationToken cancellationToken) =>
                // Writing always succeeds (unless we've already completed writing or cancellation has been requested),
                // so just TryWrite and return a completed task.
                cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) :
                TryWrite(item) ? Task.CompletedTask :
                Task.FromException(ChannelUtilities.CreateInvalidCompletionException(_parent._doneWriting));

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
