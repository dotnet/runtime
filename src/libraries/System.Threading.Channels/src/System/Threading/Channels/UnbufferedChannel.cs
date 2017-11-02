// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
    /// <summary>Provides an unbuffered channel, such that a reader and a writer must rendezvous to succeed.</summary>
    [DebuggerDisplay("Writers Waiting/Blocked: {WaitingWritersForDebugger}/{BlockedWritersCountForDebugger}, Readers Waiting/Blocked: {WaitingReadersForDebugger}/{BlockedReadersCountForDebugger}")]
    [DebuggerTypeProxy(typeof(UnbufferedChannel<>.DebugView))]
    internal sealed class UnbufferedChannel<T> : Channel<T>
    {
        /// <summary>Task that represents the completion of the channel.</summary>
        private readonly TaskCompletionSource<VoidResult> _completion = new TaskCompletionSource<VoidResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>A queue of readers blocked waiting to be matched with a writer.</summary>		
        private readonly Dequeue<ReaderInteractor<T>> _blockedReaders = new Dequeue<ReaderInteractor<T>>();
        /// <summary>A queue of writers blocked waiting to be matched with a reader.</summary>
        private readonly Dequeue<WriterInteractor<T>> _blockedWriters = new Dequeue<WriterInteractor<T>>();

        /// <summary>Task signaled when any WaitToReadAsync waiters should be woken up.</summary>
        private ReaderInteractor<bool> _waitingReaders;
        /// <summary>Task signaled when any WaitToReadAsync waiters should be woken up.</summary>		
        private ReaderInteractor<bool> _waitingWriters;

        private sealed class UnbufferedChannelReader : ChannelReader<T>
        {
            internal readonly UnbufferedChannel<T> _parent;
            internal UnbufferedChannelReader(UnbufferedChannel<T> parent) => _parent = parent;

            public override Task Completion => _parent._completion.Task;

            public override bool TryRead(out T item)
            {
                UnbufferedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // Try to find a writer to pair with
                    while (!parent._blockedWriters.IsEmpty)
                    {
                        WriterInteractor<T> w = parent._blockedWriters.DequeueHead();
                        if (w.Success(default))
                        {
                            item = w.Item;
                            return true;
                        }
                    }
                }

                // None found
                item = default;
                return false;
            }

            public override ValueTask<T> ReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<T>(Task.FromCanceled<T>(cancellationToken));
                }

                UnbufferedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If we're already completed, nothing to read.		
                    if (parent._completion.Task.IsCompleted)
                    {
                        return new ValueTask<T>(
                            parent._completion.Task.IsCanceled ? Task.FromCanceled<T>(new CancellationToken(true)) :
                            Task.FromException<T>(
                                parent._completion.Task.IsFaulted ?
                                ChannelUtilities.CreateInvalidCompletionException(parent._completion.Task.Exception.InnerException) :
                                ChannelUtilities.CreateInvalidCompletionException()));
                    }

                    // If there are any blocked writers, find one to pair up with		
                    // and get its data.  Writers that got canceled will remain in the queue,		
                    // so we need to loop to skip past them.		
                    while (!parent._blockedWriters.IsEmpty)
                    {
                        WriterInteractor<T> w = parent._blockedWriters.DequeueHead();
                        if (w.Success(default(VoidResult)))
                        {
                            return new ValueTask<T>(w.Item);
                        }
                    }

                    // No writer found to pair with.  Queue the reader.		
                    var r = ReaderInteractor<T>.Create(true, cancellationToken);
                    parent._blockedReaders.EnqueueTail(r);

                    // And let any waiting writers know it's their lucky day.		
                    ChannelUtilities.WakeUpWaiters(ref parent._waitingWriters, result: true);

                    return new ValueTask<T>(r.Task);
                }
            }

            public override Task<bool> WaitToReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled<bool>(cancellationToken);
                }

                UnbufferedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    // If we're done writing, fail.
                    if (parent._completion.Task.IsCompleted)
                    {
                        return parent._completion.Task.IsFaulted ?
                            Task.FromException<bool>(parent._completion.Task.Exception.InnerException) :
                            ChannelUtilities.s_falseTask;
                    }

                    // If there's a blocked writer, we can read.
                    if (!parent._blockedWriters.IsEmpty)
                    {
                        return ChannelUtilities.s_trueTask;
                    }

                    // Otherwise, queue the waiter.
                    return ChannelUtilities.GetOrCreateWaiter(ref parent._waitingReaders, runContinuationsAsynchronously: true, cancellationToken);
                }
            }
        }

        private sealed class UnbufferedChannelWriter : ChannelWriter<T>
        {
            internal readonly UnbufferedChannel<T> _parent;
            internal UnbufferedChannelWriter(UnbufferedChannel<T> parent) => _parent = parent;

            public override bool TryComplete(Exception error)
            {
                UnbufferedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // Mark the channel as being done. Since there's no buffered data, we can complete immediately.
                    if (parent._completion.Task.IsCompleted)
                    {
                        return false;
                    }
                    ChannelUtilities.Complete(parent._completion, error);

                    // Fail any blocked readers/writers, as there will be no writers/readers to pair them with.
                    ChannelUtilities.FailInteractors<ReaderInteractor<T>, T>(parent._blockedReaders, ChannelUtilities.CreateInvalidCompletionException(error));
                    ChannelUtilities.FailInteractors<WriterInteractor<T>, VoidResult>(parent._blockedWriters, ChannelUtilities.CreateInvalidCompletionException(error));

                    // Let any waiting readers and writers know there won't be any more data
                    ChannelUtilities.WakeUpWaiters(ref parent._waitingReaders, result: false, error: error);
                    ChannelUtilities.WakeUpWaiters(ref parent._waitingWriters, result: false, error: error);
                }

                return true;
            }

            public override bool TryWrite(T item)
            {
                UnbufferedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // Try to find a reader to pair with
                    while (!parent._blockedReaders.IsEmpty)
                    {
                        ReaderInteractor<T> r = parent._blockedReaders.DequeueHead();
                        if (r.Success(item))
                        {
                            return true;
                        }
                    }
                }

                // None found
                return false;
            }

            public override Task<bool> WaitToWriteAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled<bool>(cancellationToken);
                }

                UnbufferedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    // If we're done writing, fail.
                    if (parent._completion.Task.IsCompleted)
                    {
                        return parent._completion.Task.IsFaulted ?
                            Task.FromException<bool>(parent._completion.Task.Exception.InnerException) :
                            ChannelUtilities.s_falseTask;
                    }

                    // If there's a blocked reader, we can write
                    if (!parent._blockedReaders.IsEmpty)
                    {
                        return ChannelUtilities.s_trueTask;
                    }

                    // Otherwise, queue the writer
                    return ChannelUtilities.GetOrCreateWaiter(ref parent._waitingWriters, true, cancellationToken);
                }
            }

            public override Task WriteAsync(T item, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled(cancellationToken);
                }

                UnbufferedChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    // Fail if we've already completed
                    if (parent._completion.Task.IsCompleted)
                    {
                        return
                            parent._completion.Task.IsCanceled ? Task.FromCanceled<T>(new CancellationToken(true)) :
                            Task.FromException<T>(
                                parent._completion.Task.IsFaulted ?
                                ChannelUtilities.CreateInvalidCompletionException(parent._completion.Task.Exception.InnerException) :
                                ChannelUtilities.CreateInvalidCompletionException());
                    }

                    // Try to find a reader to pair with.  Canceled readers remain in the queue,
                    // so we need to loop until we find one.
                    while (!parent._blockedReaders.IsEmpty)
                    {
                        ReaderInteractor<T> r = parent._blockedReaders.DequeueHead();
                        if (r.Success(item))
                        {
                            return Task.CompletedTask;
                        }
                    }

                    // No reader was available.  Queue the writer.
                    var w = WriterInteractor<T>.Create(true, item, cancellationToken);
                    parent._blockedWriters.EnqueueTail(w);

                    // And let any waiting readers know it's their lucky day.
                    ChannelUtilities.WakeUpWaiters(ref parent._waitingReaders, result: true);

                    return w.Task;
                }
            }
        }

        /// <summary>Initialize the channel.</summary>
        internal UnbufferedChannel()
        {
            base.Reader = new UnbufferedChannelReader(this);
            Writer = new UnbufferedChannelWriter(this);
        }

        /// <summary>Gets an object used to synchronize all state on the instance.</summary>
        private object SyncObj => _completion;

        [Conditional("DEBUG")]
        private void AssertInvariants()
        {
            Debug.Assert(SyncObj != null, "The sync obj must not be null.");
            Debug.Assert(Monitor.IsEntered(SyncObj), "Invariants can only be validated while holding the lock.");

            if (!_blockedReaders.IsEmpty)
            {
                Debug.Assert(_blockedWriters.IsEmpty, "If there are blocked readers, there can't be blocked writers.");
            }
            if (!_blockedWriters.IsEmpty)
            {
                Debug.Assert(_blockedReaders.IsEmpty, "If there are blocked writers, there can't be blocked readers.");
            }
            if (_completion.Task.IsCompleted)
            {
                Debug.Assert(_blockedReaders.IsEmpty, "No readers can be blocked after we've completed.");
                Debug.Assert(_blockedWriters.IsEmpty, "No writers can be blocked after we've completed.");
            }
        }

        /// <summary>Gets whether there are any waiting writers.  This should only be used by the debugger.</summary>
        private bool WaitingWritersForDebugger => _waitingWriters != null;
        /// <summary>Gets whether there are any waiting readers.  This should only be used by the debugger.</summary>
        private bool WaitingReadersForDebugger => _waitingReaders != null;
        /// <summary>Gets the number of blocked writers.  This should only be used by the debugger.</summary>
        private int BlockedWritersCountForDebugger => _blockedWriters.Count;
        /// <summary>Gets the number of blocked readers.  This should only be used by the debugger.</summary>
        private int BlockedReadersCountForDebugger => _blockedReaders.Count;

        private sealed class DebugView
        {
            private readonly UnbufferedChannel<T> _channel;

            public DebugView(UnbufferedChannel<T> channel) => _channel = channel;

            public bool WaitingReaders => _channel._waitingReaders != null;
            public bool WaitingWriters => _channel._waitingWriters != null;
            public int BlockedReaders => _channel._blockedReaders.Count;
            public T[] BlockedWriters
            {
                get
                {
                    var items = new List<T>();
                    foreach (WriterInteractor<T> blockedWriter in _channel._blockedWriters)
                    {
                        items.Add(blockedWriter.Item);
                    }
                    return items.ToArray();
                }
            }
        }
    }
}
