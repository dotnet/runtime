// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
    /// <summary>Provides a buffered channel of unbounded capacity.</summary>
    [DebuggerDisplay("Items={ItemsCountForDebugger}")]
    [DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
    internal sealed class UnboundedChannel<T> : Channel<T>, IDebugEnumerable<T>
    {
        /// <summary>Task that indicates the channel has completed.</summary>
        private readonly TaskCompletionSource<VoidResult> _completion;
        /// <summary>The items in the channel.</summary>
        private readonly ConcurrentQueue<T> _items = new ConcurrentQueue<T>();
        /// <summary>Whether to force continuations to be executed asynchronously from producer writes.</summary>
        private readonly bool _runContinuationsAsynchronously;

        /// <summary>Readers waiting for a notification that data is available.</summary>
        private ReaderInteractor<bool> _waitingReaders;
        /// <summary>Set to non-null once Complete has been called.</summary>
        private Exception _doneWriting;

        /// <summary>Initialize the channel.</summary>
        internal UnboundedChannel(bool runContinuationsAsynchronously)
        {
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
            _completion = new TaskCompletionSource<VoidResult>(runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);
            base.Reader = new UnboundedChannelReader(this);
            Writer = new UnboundedChannelWriter(this);
        }

        private sealed class UnboundedChannelReader : ChannelReader<T>
        {
            internal readonly UnboundedChannel<T> _parent;
            internal UnboundedChannelReader(UnboundedChannel<T> parent) => _parent = parent;

            public override Task Completion => _parent._completion.Task;

            public override bool TryRead(out T item)
            {
                UnboundedChannel<T> parent = _parent;

                // Dequeue an item if we can
                if (parent._items.TryDequeue(out item))
                {
                    if (parent._doneWriting != null && parent._items.IsEmpty)
                    {
                        // If we've now emptied the items queue and we're not getting any more, complete.
                        ChannelUtilities.Complete(parent._completion, parent._doneWriting);
                    }
                    return true;
                }

                item = default;
                return false;
            }

            public override Task<bool> WaitToReadAsync(CancellationToken cancellationToken)
            {
                // If there are any items, readers can try to get them.
                return !_parent._items.IsEmpty ?
                    ChannelUtilities.s_trueTask :
                    WaitToReadAsyncCore(cancellationToken);

                Task<bool> WaitToReadAsyncCore(CancellationToken ct)
                {
                    UnboundedChannel<T> parent = _parent;

                    lock (parent.SyncObj)
                    {
                        parent.AssertInvariants();

                        // Try again to read now that we're synchronized with writers.
                        if (!parent._items.IsEmpty)
                        {
                            return ChannelUtilities.s_trueTask;
                        }

                        // There are no items, so if we're done writing, there's never going to be data available.
                        if (parent._doneWriting != null)
                        {
                            return parent._doneWriting != ChannelUtilities.s_doneWritingSentinel ?
                                Task.FromException<bool>(parent._doneWriting) :
                                ChannelUtilities.s_falseTask;
                        }

                        // Queue the waiter
                        return ChannelUtilities.GetOrCreateWaiter(ref parent._waitingReaders, parent._runContinuationsAsynchronously, ct);
                    }
                }
            }
        }

        private sealed class UnboundedChannelWriter : ChannelWriter<T>
        {
            internal readonly UnboundedChannel<T> _parent;
            internal UnboundedChannelWriter(UnboundedChannel<T> parent) => _parent = parent;

            public override bool TryComplete(Exception error)
            {
                UnboundedChannel<T> parent = _parent;
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

                // At this point, _waitingReaders will not be mutated:
                // it's only mutated by readers while holding the lock, and only if _doneWriting is null.
                // We also know that only one thread (this one) will ever get here, as only that thread
                // will be the one to transition from _doneWriting false to true.  As such, we can
                // freely manipulate _waitingReaders without any concurrency concerns.
                ChannelUtilities.WakeUpWaiters(ref parent._waitingReaders, result: false, error: error);

                // Successfully transitioned to completed.
                return true;
            }

            public override bool TryWrite(T item)
            {
                UnboundedChannel<T> parent = _parent;
                while (true)
                {
                    ReaderInteractor<bool> waitingReaders = null;
                    lock (parent.SyncObj)
                    {
                        // If writing has already been marked as done, fail the write.
                        parent.AssertInvariants();
                        if (parent._doneWriting != null)
                        {
                            return false;
                        }

                        // Add the data to the queue, and let any waiting readers know that they should try to read it.
                        // We can only complete such waiters here under the lock if they run continuations asynchronously
                        // (otherwise the synchronous continuations could be invoked under the lock).  If we don't complete
                        // them here, we need to do so outside of the lock.
                        parent._items.Enqueue(item);
                        waitingReaders = parent._waitingReaders;
                        if (waitingReaders == null)
                        {
                            return true;
                        }
                        parent._waitingReaders = null;
                    }

                    // Wake up all of the waiters.  Since we've released the lock, it's possible
                    // we could cause some spurious wake-ups here, if we tell a waiter there's
                    // something available but all data has already been removed.  It's a benign
                    // race condition, though, as consumers already need to account for such things.
                    waitingReaders.Success(item: true);
                    return true;
                }
            }

            public override Task<bool> WaitToWriteAsync(CancellationToken cancellationToken)
            {
                Exception doneWriting = _parent._doneWriting;
                return
                    cancellationToken.IsCancellationRequested ? Task.FromCanceled<bool>(cancellationToken) :
                    doneWriting == null ? ChannelUtilities.s_trueTask : // unbounded writing can always be done if we haven't completed
                    doneWriting != ChannelUtilities.s_doneWritingSentinel ? Task.FromException<bool>(doneWriting) :
                    ChannelUtilities.s_falseTask;
            }

            public override Task WriteAsync(T item, CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) :
                TryWrite(item) ? ChannelUtilities.s_trueTask :
                Task.FromException(ChannelUtilities.CreateInvalidCompletionException(_parent._doneWriting));
        }

        /// <summary>Gets the object used to synchronize access to all state on this instance.</summary>
        private object SyncObj => _items;

        [Conditional("DEBUG")]
        private void AssertInvariants()
        {
            Debug.Assert(SyncObj != null, "The sync obj must not be null.");
            Debug.Assert(Monitor.IsEntered(SyncObj), "Invariants can only be validated while holding the lock.");

            if (!_items.IsEmpty)
            {
                if (_runContinuationsAsynchronously)
                {
                    Debug.Assert(_waitingReaders == null, "There's data available, so there shouldn't be any waiting readers.");
                }
                Debug.Assert(!_completion.Task.IsCompleted, "We still have data available, so shouldn't be completed.");
            }
            if (_waitingReaders != null && _runContinuationsAsynchronously)
            {
                Debug.Assert(_items.IsEmpty, "There are blocked/waiting readers, so there shouldn't be any data available.");
            }
            if (_completion.Task.IsCompleted)
            {
                Debug.Assert(_doneWriting != null, "We're completed, so we must be done writing.");
            }
        }  

        /// <summary>Gets the number of items in the channel.  This should only be used by the debugger.</summary>
        private int ItemsCountForDebugger => _items.Count;

        /// <summary>Gets an enumerator the debugger can use to show the contents of the channel.</summary>
        IEnumerator<T> IDebugEnumerable<T>.GetEnumerator() => _items.GetEnumerator();
    }
}
