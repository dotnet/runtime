// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
    /// <summary>
    /// Provides a buffered channel of unbounded capacity for use by any number
    /// of writers but at most a single reader at a time.
    /// </summary>
    [DebuggerDisplay("Items={ItemsCountForDebugger}")]
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

        private sealed class UnboundedChannelReader : ChannelReader<T>
        {
            internal readonly SingleConsumerUnboundedChannel<T> _parent;
            internal UnboundedChannelReader(SingleConsumerUnboundedChannel<T> parent) => _parent = parent;

            public override Task Completion => _parent._completion.Task;

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
        }

        private sealed class UnboundedChannelWriter : ChannelWriter<T>
        {
            internal readonly SingleConsumerUnboundedChannel<T> _parent;
            internal UnboundedChannelWriter(SingleConsumerUnboundedChannel<T> parent) => _parent = parent;

            public override bool TryComplete(Exception error)
            {
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

                // Complete a waiting reader if necessary.
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
                    ReaderInteractor<bool> waitingReader = null;

                    lock (parent.SyncObj)
                    {
                        // If writing is completed, exit out without writing.
                        if (parent._doneWriting != null)
                        {
                            return false;
                        }

                        // Queue the item being written; then if there's a waiting
                        // reader, store it for notification outside of the lock.
                        parent._items.Enqueue(item);

                        waitingReader = parent._waitingReader;
                        if (waitingReader == null)
                        {
                            return true;
                        }
                        parent._waitingReader = null;
                    }

                    // If we get here, we grabbed a waiting reader.
                    // Notify it that an item was written and exit.
                    Debug.Assert(waitingReader != null, "Expected a waiting reader");
                    waitingReader.Success(item: true);
                    return true;
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
        }

        private object SyncObj => _items;

        /// <summary>Gets the number of items in the channel.  This should only be used by the debugger.</summary>
        private int ItemsCountForDebugger => _items.Count;

        /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
        IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _items.GetEnumerator();
    }
}
