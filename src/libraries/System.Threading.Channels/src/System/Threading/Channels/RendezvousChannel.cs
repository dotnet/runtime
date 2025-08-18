// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
    /// <summary>Provides an unbuffered channel where readers and writers must directly hand off to each other.</summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class RendezvousChannel<T> : Channel<T>
    {
        /// <summary>Whether to suceed writes immediately even when there's no rendezvousing reader.</summary>
        private readonly bool _dropWrites;

        /// <summary>The delegate that will be invoked when the channel hits its bound and an item is dropped from the channel.</summary>
        private readonly Action<T>? _itemDropped;

        /// <summary>Task signaled when the channel has completed.</summary>
        private readonly TaskCompletionSource _completion;

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

        /// <summary>Initializes the <see cref="RendezvousChannel{T}"/>.</summary>
        /// <param name="mode">The mode used when writing to a full channel.</param>
        /// <param name="runContinuationsAsynchronously">Whether to force continuations to be executed asynchronously.</param>
        /// <param name="itemDropped">Delegate that will be invoked when an item is dropped from the channel. See <see cref="BoundedChannelFullMode"/>.</param>
        internal RendezvousChannel(BoundedChannelFullMode mode, bool runContinuationsAsynchronously, Action<T>? itemDropped)
        {
            _dropWrites = mode is not BoundedChannelFullMode.Wait;

            _runContinuationsAsynchronously = runContinuationsAsynchronously;
            _itemDropped = itemDropped;
            _completion = new TaskCompletionSource(runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);

            Reader = new RendezvousChannelReader(this);
            Writer = new RendezvousChannelWriter(this);
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        private sealed class RendezvousChannelReader : ChannelReader<T>
        {
            internal readonly RendezvousChannel<T> _parent;
            private readonly BlockedReadAsyncOperation<T> _readerSingleton;
            private readonly WaitingReadAsyncOperation _waiterSingleton;

            internal RendezvousChannelReader(RendezvousChannel<T> parent)
            {
                _parent = parent;
                _readerSingleton = new BlockedReadAsyncOperation<T>(parent._runContinuationsAsynchronously, pooled: true);
                _waiterSingleton = new WaitingReadAsyncOperation(parent._runContinuationsAsynchronously, pooled: true);
            }

            public override Task Completion => _parent._completion.Task;

            public override bool CanCount => true;

            public override bool CanPeek => true;

            public override int Count => 0;

            public override bool TryRead([MaybeNullWhen(false)] out T item)
            {
                RendezvousChannel<T> parent = _parent;

                // Reserve a blocked writer if one is available.
                BlockedWriteAsyncOperation<T>? blockedWriter = null;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    if (parent._doneWriting is null)
                    {
                        blockedWriter = ChannelUtilities.TryDequeueAndReserveCompletionIfCancelable(ref parent._blockedWritersHead);
                    }
                }

                // If we got one, transfer its item to the read and complete successfully.
                if (blockedWriter is not null)
                {
                    item = blockedWriter.Item!;
                    blockedWriter.DangerousSetResult(default);
                    return true;
                }

                item = default;
                return false;
            }

            public override bool TryPeek([MaybeNullWhen(false)] out T item)
            {
                RendezvousChannel<T> parent = _parent;

                // Peek at a blocked writer if one is available.
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    if (parent._doneWriting is null &&
                        parent._blockedWritersHead is { } blockedWriter)
                    {
                        item = blockedWriter.Item!;
                        return true;
                    }
                }

                item = default;
                return false;
            }

            public override ValueTask<T> ReadAsync(CancellationToken cancellationToken)
            {
                RendezvousChannel<T> parent = _parent;

                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<T>(Task.FromCanceled<T>(cancellationToken));
                }

                BlockedReadAsyncOperation<T>? reader = null;
                WaitingWriteAsyncOperation? waitingWriters = null;
                BlockedWriteAsyncOperation<T>? blockedWriter = null;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If we're done writing so that there will never be more items, fail.
                    if (parent._doneWriting is not null)
                    {
                        return ChannelUtilities.GetInvalidCompletionValueTask<T>(parent._doneWriting);
                    }

                    // Reserve a blocked writer if one is available.
                    blockedWriter = ChannelUtilities.TryDequeueAndReserveCompletionIfCancelable(ref parent._blockedWritersHead);

                    // If we couldn't get one, create a waiting reader, and reserve any waiting writers to alert.
                    if (blockedWriter is null)
                    {
                        reader =
                            !cancellationToken.CanBeCanceled && _readerSingleton.TryOwnAndReset() ? _readerSingleton :
                            new(parent._runContinuationsAsynchronously, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
                        ChannelUtilities.Enqueue(ref parent._blockedReadersHead, reader);

                        waitingWriters = ChannelUtilities.TryReserveCompletionIfCancelable(ref parent._waitingWritersHead);
                    }
                }

                // Either complete the reserved blocked writer, transferring its item to the read,
                // or return the waiting reader task, also alerting any waiting writers.
                ValueTask<T> result;
                if (blockedWriter is not null)
                {
                    Debug.Assert(reader is null);
                    Debug.Assert(waitingWriters is null);
                    result = new(blockedWriter.Item!);
                    blockedWriter.DangerousSetResult(default);
                }
                else
                {
                    Debug.Assert(reader is not null);
                    ChannelUtilities.DangerousSetOperations(waitingWriters, result: true);
                    result = reader.ValueTaskOfT;
                }

                return result;
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
                }

                RendezvousChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If we're done writing, a read will never be possible.
                    if (parent._doneWriting is not null)
                    {
                        return parent._doneWriting != ChannelUtilities.s_doneWritingSentinel ?
                            new ValueTask<bool>(Task.FromException<bool>(parent._doneWriting)) :
                            default;
                    }

                    // If there are any writers waiting, a read is possible.
                    if (parent._blockedWritersHead is not null)
                    {
                        return new ValueTask<bool>(true);
                    }

                    // Register a waiting reader task.
                    WaitingReadAsyncOperation waiter =
                        !cancellationToken.CanBeCanceled && _waiterSingleton.TryOwnAndReset() ? _waiterSingleton :
                        new(parent._runContinuationsAsynchronously, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
                    ChannelUtilities.Enqueue(ref parent._waitingReadersHead, waiter);
                    return waiter.ValueTaskOfT;
                }
            }

            internal string DebuggerDisplay
            {
                get
                {
                    long blockedReaderCount, waitingReaderCount;
                    lock (_parent.SyncObj)
                    {
                        blockedReaderCount = ChannelUtilities.CountOperations(_parent._blockedReadersHead);
                        waitingReaderCount = ChannelUtilities.CountOperations(_parent._waitingReadersHead);
                    }

                    return $"ReadAsync={blockedReaderCount}, WaitToReadAsync={waitingReaderCount}";
                }
            }
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        private sealed class RendezvousChannelWriter : ChannelWriter<T>
        {
            internal readonly RendezvousChannel<T> _parent;
            private readonly BlockedWriteAsyncOperation<T> _writerSingleton;
            private readonly WaitingWriteAsyncOperation _waiterSingleton;

            internal RendezvousChannelWriter(RendezvousChannel<T> parent)
            {
                _parent = parent;
                _writerSingleton = new BlockedWriteAsyncOperation<T>(runContinuationsAsynchronously: true, pooled: true);
                _waiterSingleton = new WaitingWriteAsyncOperation(runContinuationsAsynchronously: true, pooled: true);
            }

            public override bool TryComplete(Exception? error)
            {
                RendezvousChannel<T> parent = _parent;

                BlockedReadAsyncOperation<T>? blockedReadersHead;
                BlockedWriteAsyncOperation<T>? blockedWritersHead;
                WaitingReadAsyncOperation? waitingReadersHead;
                WaitingWriteAsyncOperation? waitingWritersHead;
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

                // Complete the channel's task, as no more data can possibly arrive at this point.  We do this outside
                // of the lock in case we'll be running synchronous completions, and we
                // do it before completing blocked/waiting readers, so that when they
                // wake up they'll see the task as being completed.
                ChannelUtilities.Complete(parent._completion, error);

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
                RendezvousChannel<T> parent = _parent;

                // Reserve a blocked reader if one is available.
                BlockedReadAsyncOperation<T>? blockedReader = null;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    if (parent._doneWriting is null)
                    {
                        blockedReader = ChannelUtilities.TryDequeueAndReserveCompletionIfCancelable(ref parent._blockedReadersHead);
                    }
                }

                // If we got one, transfer its item to the read and complete successfully.
                if (blockedReader is not null)
                {
                    blockedReader.DangerousSetResult(item);
                    return true;
                }

                // There's no concurrent reader, but if we're configured to drop writes, we can succeed immediately.
                if (parent._dropWrites)
                {
                    parent._itemDropped?.Invoke(item);
                    return true;
                }

                return false;
            }

            public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
                }

                RendezvousChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If we're done writing, a read will never be possible.
                    if (parent._doneWriting is not null)
                    {
                        return parent._doneWriting != ChannelUtilities.s_doneWritingSentinel ?
                            new ValueTask<bool>(Task.FromException<bool>(parent._doneWriting)) :
                            default;
                    }

                    // If there are any readers waiting, a write is possible.
                    if (parent._blockedReadersHead is not null || parent._dropWrites)
                    {
                        return new ValueTask<bool>(true);
                    }

                    // There were no readers available, but there could be in the future, so ensure
                    // there's a waiting writer task and return it.
                    WaitingWriteAsyncOperation waiter =
                        !cancellationToken.CanBeCanceled && _waiterSingleton.TryOwnAndReset() ? _waiterSingleton :
                        new(parent._runContinuationsAsynchronously, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
                    ChannelUtilities.Enqueue(ref parent._waitingWritersHead, waiter);
                    return waiter.ValueTaskOfT;
                }
            }

            public override ValueTask WriteAsync(T item, CancellationToken cancellationToken)
            {
                RendezvousChannel<T> parent = _parent;

                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask(Task.FromCanceled<T>(cancellationToken));
                }

                BlockedWriteAsyncOperation<T>? writer = null;
                WaitingReadAsyncOperation? waitingReaders = null;
                BlockedReadAsyncOperation<T>? blockedReader = null;
                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If we've already been marked as done for writing, we shouldn't be writing.
                    if (parent._doneWriting is not null)
                    {
                        return new ValueTask(Task.FromException(ChannelUtilities.CreateInvalidCompletionException(parent._doneWriting)));
                    }

                    // Reserve a blocked reader if one is available.
                    blockedReader = ChannelUtilities.TryDequeueAndReserveCompletionIfCancelable(ref parent._blockedReadersHead);

                    // If we couldn't get one, create a blocked writer, and reserve any waiting readers to alert.
                    if (blockedReader is null && !parent._dropWrites)
                    {
                        writer =
                            !cancellationToken.CanBeCanceled && _writerSingleton.TryOwnAndReset() ? _writerSingleton :
                            new(parent._runContinuationsAsynchronously, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
                        writer.Item = item;
                        ChannelUtilities.Enqueue(ref parent._blockedWritersHead, writer);

                        waitingReaders = ChannelUtilities.TryReserveCompletionIfCancelable(ref parent._waitingReadersHead);
                    }
                }

                if (writer is not null)
                {
                    Debug.Assert(blockedReader is null);
                    ChannelUtilities.DangerousSetOperations(waitingReaders, result: true);
                    return writer.ValueTask;
                }

                if (blockedReader is not null)
                {
                    blockedReader.DangerousSetResult(item);
                }
                else
                {
                    Debug.Assert(parent._dropWrites);
                    parent._itemDropped?.Invoke(item);
                }

                return default;
            }

            internal string DebuggerDisplay
            {
                get
                {
                    long blockedWriterCount, waitingWriterCount;
                    lock (_parent.SyncObj)
                    {
                        blockedWriterCount = ChannelUtilities.CountOperations(_parent._blockedWritersHead);
                        waitingWriterCount = ChannelUtilities.CountOperations(_parent._waitingWritersHead);
                    }

                    return $"WriteAsync={blockedWriterCount}, WaitToWriteAsync={waitingWriterCount}";
                }
            }
        }

        /// <summary>Gets an object used to synchronize all state on the instance.</summary>
        private object SyncObj => _completion;

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
                    }, new KeyValuePair<RendezvousChannel<T>, AsyncOperation>(this, op));
                }
            };

        private string DebuggerDisplay =>
            $"{((RendezvousChannelReader)Reader).DebuggerDisplay}, {((RendezvousChannelWriter)Writer).DebuggerDisplay}";

        [Conditional("DEBUG")]
        private void AssertInvariants()
        {
            Debug.Assert(SyncObj is not null, "The sync obj must not be null.");
            Debug.Assert(Monitor.IsEntered(SyncObj), "Invariants can only be validated while holding the lock.");

            if (_blockedReadersHead is not null)
            {
                Debug.Assert(_blockedWritersHead is null, "There shouldn't be any blocked writer if there's a blocked reader.");
                Debug.Assert(_waitingWritersHead is null, "There shouldn't be any waiting writers if there's a blocked reader.");
            }

            if (_blockedWritersHead is not null)
            {
                Debug.Assert(_blockedReadersHead is null, "There shouldn't be any blocked readers if there's a blocked writer.");
                Debug.Assert(_waitingReadersHead is null, "There shouldn't be any waiting readers if there's a blocked writer.");
            }

            if (_completion.Task.IsCompleted)
            {
                Debug.Assert(_doneWriting is not null, "We can only complete if we're done writing.");
            }
        }
    }
}
