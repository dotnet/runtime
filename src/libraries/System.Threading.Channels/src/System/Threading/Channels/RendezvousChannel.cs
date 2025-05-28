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

                while (true)
                {
                    BlockedWriteAsyncOperation<T>? blockedWriter;

                    lock (parent.SyncObj)
                    {
                        parent.AssertInvariants();

                        if (parent._doneWriting is not null ||
                            !ChannelUtilities.TryDequeue(ref parent._blockedWritersHead, out blockedWriter))
                        {
                            item = default;
                            return false;
                        }
                    }

                    T writerItem = blockedWriter.Item!; // once TrySetResult is successful, a pooled instance may be reused
                    if (blockedWriter.TrySetResult(default) is true)
                    {
                        item = writerItem;
                        return true;
                    }
                }
            }

            public override bool TryPeek([MaybeNullWhen(false)] out T item)
            {
                RendezvousChannel<T> parent = _parent;

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
                while (true)
                {
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

                        if (!ChannelUtilities.TryDequeue(ref parent._blockedWritersHead, out blockedWriter))
                        {
                            // If we're able to use the singleton reader, do so.
                            // Otherwise, create a new reader. Note that in addition to checking whether synchronous continuations were requested,
                            // we also check whether the supplied cancellation token can be canceled.  The writer calls UnregisterCancellation
                            // while holding the lock, and if a callback needs to be unregistered and is currently running, it needs to wait
                            // for that callback to complete so that the subsequent code knows it won't be contending with another thread
                            // trying to complete the operation.  However, if we allowed a synchronous continuation from this operation, that
                            // cancellation callback could end up running arbitrary code, including code that called back into the reader or
                            // writer and tried to take the same lock held by the thread running UnregisterCancellation... deadlock.  As such,
                            // we only allow synchronous continuations here if both a) the caller requested it and the token isn't cancelable.
                            reader =
                                !cancellationToken.CanBeCanceled && _readerSingleton.TryOwnAndReset() ? _readerSingleton :
                                new(parent._runContinuationsAsynchronously || cancellationToken.CanBeCanceled, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
                            ChannelUtilities.Enqueue(ref parent._blockedReadersHead, reader);

                            waitingWriters = parent._waitingWritersHead;
                            parent._waitingWritersHead = null;
                        }
                    }

                    if (reader is not null)
                    {
                        ChannelUtilities.SetOperations(ref waitingWriters, result: true);
                        return reader.ValueTaskOfT;
                    }

                    Debug.Assert(blockedWriter is not null);
                    T writerItem = blockedWriter.Item!; // once TrySetResult is successful, a pooled instance may be reused
                    if (blockedWriter.TrySetResult(default))
                    {
                        return new(writerItem);
                    }
                }
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

                    // There were no items available, but there could be in the future, so ensure
                    // there's a blocked reader task and return it.
                    WaitingReadAsyncOperation waiter =
                        !cancellationToken.CanBeCanceled && _waiterSingleton.TryOwnAndReset() ? _waiterSingleton :
                        new(parent._runContinuationsAsynchronously || cancellationToken.CanBeCanceled, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
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
                }

                // Complete the channel's task, as no more data can possibly arrive at this point.  We do this outside
                // of the lock in case we'll be running synchronous completions, and we
                // do it before completing blocked/waiting readers, so that when they
                // wake up they'll see the task as being completed.
                ChannelUtilities.Complete(parent._completion, error);

                // At this point, _blockedReaders/Writers and _waitingReaders/Writers will not be mutated:
                // they're only mutated by readers/writers while holding the lock, and only if _doneWriting is null.
                // We also know that only one thread (this one) will ever get here, as only that thread
                // will be the one to transition from _doneWriting false to true.  As such, we can
                // freely manipulate them without any concurrency concerns.
                ChannelUtilities.FailOperations(ref parent._blockedReadersHead, ChannelUtilities.CreateInvalidCompletionException(error));
                ChannelUtilities.FailOperations(ref parent._blockedWritersHead, ChannelUtilities.CreateInvalidCompletionException(error));
                ChannelUtilities.SetOrFailOperations(ref parent._waitingReadersHead, result: false, error: error);
                ChannelUtilities.SetOrFailOperations(ref parent._waitingWritersHead, result: false, error: error);

                // Successfully transitioned to completed.
                return true;
            }

            public override bool TryWrite(T item)
            {
                RendezvousChannel<T> parent = _parent;

                while (true)
                {
                    BlockedReadAsyncOperation<T>? blockedReader;

                    lock (parent.SyncObj)
                    {
                        parent.AssertInvariants();

                        if (parent._doneWriting is not null ||
                            (!ChannelUtilities.TryDequeue(ref parent._blockedReadersHead, out blockedReader) && !parent._dropWrites))
                        {
                            return false;
                        }
                    }

                    if (blockedReader is null)
                    {
                        Debug.Assert(parent._dropWrites);
                        parent._itemDropped?.Invoke(item);
                        return true;
                    }

                    if (blockedReader.TrySetResult(item))
                    {
                        return true;
                    }
                }
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

                    WaitingWriteAsyncOperation waiter =
                        !cancellationToken.CanBeCanceled && _waiterSingleton.TryOwnAndReset() ? _waiterSingleton :
                        new(parent._runContinuationsAsynchronously || cancellationToken.CanBeCanceled, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
                    ChannelUtilities.Enqueue(ref parent._waitingWritersHead, waiter);
                    return waiter.ValueTaskOfT;
                }
            }

            public override ValueTask WriteAsync(T item, CancellationToken cancellationToken)
            {
                RendezvousChannel<T> parent = _parent;
                while (true)
                {
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

                        if (parent._doneWriting is not null)
                        {
                            return new ValueTask(Task.FromException(ChannelUtilities.CreateInvalidCompletionException(parent._doneWriting)));
                        }

                        if (!ChannelUtilities.TryDequeue(ref parent._blockedReadersHead, out blockedReader) &&
                            !parent._dropWrites)
                        {
                            Debug.Assert(blockedReader is null);

                            writer =
                                !cancellationToken.CanBeCanceled && _writerSingleton.TryOwnAndReset() ? _writerSingleton :
                                new(parent._runContinuationsAsynchronously || cancellationToken.CanBeCanceled, cancellationToken, cancellationCallback: _parent.CancellationCallbackDelegate);
                            writer.Item = item;
                            ChannelUtilities.Enqueue(ref parent._blockedWritersHead, writer);

                            waitingReaders = parent._waitingReadersHead;
                            parent._waitingReadersHead = null;
                        }
                    }

                    if (writer is not null)
                    {
                        ChannelUtilities.SetOperations(ref waitingReaders, result: true);
                        return writer.ValueTask;
                    }

                    if (blockedReader is null)
                    {
                        Debug.Assert(parent._dropWrites);
                        parent._itemDropped?.Invoke(item);
                        return default;
                    }

                    if (blockedReader.TrySetResult(item))
                    {
                        return default;
                    }
                }
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
