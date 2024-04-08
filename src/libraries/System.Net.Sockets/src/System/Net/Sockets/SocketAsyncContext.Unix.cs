// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Sockets
{
    // Note on asynchronous behavior here:

    // The asynchronous socket operations here generally do the following:
    // (1) If the operation queue is Ready (queue is empty), try to perform the operation immediately, non-blocking.
    // If this completes (i.e. does not return EWOULDBLOCK), then we return the results immediately
    // for both success (SocketError.Success) or failure.
    // No callback will happen; callers are expected to handle these synchronous completions themselves.
    // (2) If EWOULDBLOCK is returned, or the queue is not empty, then we enqueue an operation to the
    // appropriate queue and return SocketError.IOPending.
    // Enqueuing itself may fail because the socket is closed before the operation can be enqueued;
    // in this case, we return SocketError.OperationAborted (which matches what Winsock would return in this case).
    // (3) When we receive an epoll notification for the socket, we post a work item to the threadpool
    // to perform the I/O and invoke the callback with the I/O result.

    // Synchronous operations generally do the same, except that instead of returning IOPending,
    // they block on an event handle until the operation is processed by the queue.

    // See comments on OperationQueue below for more details of how the queue coordination works.

    internal sealed class SocketAsyncContext
    {
        // Cached operation instances for operations commonly repeated on the same socket instance,
        // e.g. async accepts, sends/receives with single and multiple buffers.  More can be
        // added in the future if necessary, at the expense of extra fields here.  With a larger
        // refactoring, these could also potentially be moved to SocketAsyncEventArgs, which
        // would be more invasive but which would allow them to be reused across socket instances
        // and also eliminate the interlocked necessary to rent the instances.
        private AcceptOperation? _cachedAcceptOperation;
        private BufferMemoryReceiveOperation? _cachedBufferMemoryReceiveOperation;
        private BufferListReceiveOperation? _cachedBufferListReceiveOperation;
        private BufferMemorySendOperation? _cachedBufferMemorySendOperation;
        private BufferListSendOperation? _cachedBufferListSendOperation;

        private void ReturnOperation(AcceptOperation operation)
        {
            operation.Reset();
            operation.Callback = null;
            operation.SocketAddress = default;
            Volatile.Write(ref _cachedAcceptOperation, operation); // benign race condition
        }

        private void ReturnOperation(BufferMemoryReceiveOperation operation)
        {
            operation.Reset();
            operation.Buffer = default;
            operation.Callback = null;
            operation.SocketAddress = default;
            Volatile.Write(ref _cachedBufferMemoryReceiveOperation, operation); // benign race condition
        }

        private void ReturnOperation(BufferListReceiveOperation operation)
        {
            operation.Reset();
            operation.Buffers = null;
            operation.Callback = null;
            operation.SocketAddress = default;
            Volatile.Write(ref _cachedBufferListReceiveOperation, operation); // benign race condition
        }

        private void ReturnOperation(BufferMemorySendOperation operation)
        {
            operation.Reset();
            operation.Buffer = default;
            operation.Callback = null;
            operation.SocketAddress = default;
            Volatile.Write(ref _cachedBufferMemorySendOperation, operation); // benign race condition
        }

        private void ReturnOperation(BufferListSendOperation operation)
        {
            operation.Reset();
            operation.Buffers = null;
            operation.Callback = null;
            operation.SocketAddress = default;
            Volatile.Write(ref _cachedBufferListSendOperation, operation); // benign race condition
        }

        private AcceptOperation RentAcceptOperation() =>
            Interlocked.Exchange(ref _cachedAcceptOperation, null) ??
            new AcceptOperation(this);

        private BufferMemoryReceiveOperation RentBufferMemoryReceiveOperation() =>
            Interlocked.Exchange(ref _cachedBufferMemoryReceiveOperation, null) ??
            new BufferMemoryReceiveOperation(this);

        private BufferListReceiveOperation RentBufferListReceiveOperation() =>
            Interlocked.Exchange(ref _cachedBufferListReceiveOperation, null) ??
            new BufferListReceiveOperation(this);

        private BufferMemorySendOperation RentBufferMemorySendOperation() =>
            Interlocked.Exchange(ref _cachedBufferMemorySendOperation, null) ??
            new BufferMemorySendOperation(this);

        private BufferListSendOperation RentBufferListSendOperation() =>
            Interlocked.Exchange(ref _cachedBufferListSendOperation, null) ??
            new BufferListSendOperation(this);

        private abstract class AsyncOperation : IThreadPoolWorkItem
        {
            private enum State
            {
                Waiting = 0,
                Running,
                RunningWithPendingCancellation,
                Complete,
                Canceled
            }

            private int _state; // Actually AsyncOperation.State.

#if DEBUG
            private int _callbackQueued; // When non-zero, the callback has been queued.
#endif

            public readonly SocketAsyncContext AssociatedContext;
            public AsyncOperation Next = null!; // initialized by helper called from ctor
            public SocketError ErrorCode;
            public Memory<byte> SocketAddress;
            public CancellationTokenRegistration CancellationRegistration;

            public ManualResetEventSlim? Event { get; set; }

            public AsyncOperation(SocketAsyncContext context)
            {
                AssociatedContext = context;
                Reset();
            }

            public void Reset()
            {
                _state = (int)State.Waiting;
                Event = null;
                Next = this;
#if DEBUG
                _callbackQueued = 0;
#endif
            }

            public OperationResult TryComplete(SocketAsyncContext context)
            {
                TraceWithContext(context, "Enter");

                // Set state to Running, unless we've been canceled
                int oldState = Interlocked.CompareExchange(ref _state, (int)State.Running, (int)State.Waiting);
                if (oldState == (int)State.Canceled)
                {
                    TraceWithContext(context, "Exit, Previously canceled");
                    return OperationResult.Cancelled;
                }

                Debug.Assert(oldState == (int)State.Waiting, $"Unexpected operation state: {(State)oldState}");

                // Try to perform the IO
                if (DoTryComplete(context))
                {
                    Debug.Assert((State)Volatile.Read(ref _state) is State.Running or State.RunningWithPendingCancellation, "Unexpected operation state");

                    Volatile.Write(ref _state, (int)State.Complete);

                    TraceWithContext(context, "Exit, Completed");
                    return OperationResult.Completed;
                }

                // Set state back to Waiting, unless we were canceled, in which case we have to process cancellation now
                int newState;
                while (true)
                {
                    int state = Volatile.Read(ref _state);
                    Debug.Assert(state is (int)State.Running or (int)State.RunningWithPendingCancellation, $"Unexpected operation state: {(State)state}");

                    newState = (state == (int)State.Running ? (int)State.Waiting : (int)State.Canceled);
                    if (state == Interlocked.CompareExchange(ref _state, newState, state))
                    {
                        break;
                    }

                    // Race to update the state. Loop and try again.
                }

                if (newState == (int)State.Canceled)
                {
                    ProcessCancellation();
                    TraceWithContext(context, "Exit, Newly cancelled");
                    return OperationResult.Cancelled;
                }

                TraceWithContext(context, "Exit, Pending");
                return OperationResult.Pending;
            }

            public bool TryCancel()
            {
                Trace("Enter");

                // Note we could be cancelling because of socket close. Regardless, we don't need the registration anymore.
                CancellationRegistration.Dispose();

                int newState;
                while (true)
                {
                    int state = Volatile.Read(ref _state);
                    if (state is (int)State.Complete or (int)State.Canceled or (int)State.RunningWithPendingCancellation)
                    {
                        return false;
                    }

                    newState = (state == (int)State.Waiting ? (int)State.Canceled : (int)State.RunningWithPendingCancellation);
                    if (state == Interlocked.CompareExchange(ref _state, newState, state))
                    {
                        break;
                    }

                    // Race to update the state. Loop and try again.
                }

                if (newState == (int)State.RunningWithPendingCancellation)
                {
                    // TryComplete will either succeed, or it will see the pending cancellation and deal with it.
                    return false;
                }

                ProcessCancellation();

                // Note, we leave the operation in the OperationQueue.
                // When we get around to processing it, we'll see it's cancelled and skip it.
                return true;
            }

            public void ProcessCancellation()
            {
                Trace("Enter");

                Debug.Assert(_state == (int)State.Canceled);

                ErrorCode = SocketError.OperationAborted;

                ManualResetEventSlim? e = Event;
                if (e != null)
                {
                    e.Set();
                }
                else
                {
#if DEBUG
                    Debug.Assert(Interlocked.CompareExchange(ref _callbackQueued, 1, 0) == 0, $"Unexpected _callbackQueued: {_callbackQueued}");
#endif
                    // We've marked the operation as canceled, and so should invoke the callback, but
                    // we can't pool the object, as ProcessQueue may still have a reference to it, due to
                    // using a pattern whereby it takes the lock to grab an item, but then releases the lock
                    // to do further processing on the item that's still in the list.
                    ThreadPool.UnsafeQueueUserWorkItem(o => ((AsyncOperation)o!).InvokeCallback(allowPooling: false), this);
                }
            }

            public void Dispatch()
            {
                ManualResetEventSlim? e = Event;
                if (e != null)
                {
                    // Sync operation.  Signal waiting thread to continue processing.
                    e.Set();
                }
                else
                {
                    // Async operation.
                    Schedule();
                }
            }

            public void Schedule()
            {
                Debug.Assert(Event == null);

                // Async operation.  Process the IO on the threadpool.
                ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
            }

            public void Process() => ((IThreadPoolWorkItem)this).Execute();

            void IThreadPoolWorkItem.Execute()
            {
                // ReadOperation and WriteOperation, the only two types derived from
                // AsyncOperation, implement IThreadPoolWorkItem.Execute to call
                // ProcessAsyncOperation(this) on the appropriate receive or send queue.
                // However, this base class needs to be able to queue them without
                // additional allocation, so it also implements the interface in order
                // to pass the compiler's static checking for the interface, but then
                // when the runtime queries for the interface, it'll use the derived
                // type's interface implementation.  We could instead just make this
                // an abstract and have the derived types override it, but that adds
                // "Execute" as a public method, which could easily be misunderstood.
                // We could also add an abstract method that the base interface implementation
                // invokes, but that adds an extra virtual dispatch.
                Debug.Fail("Expected derived type to implement IThreadPoolWorkItem");
                throw new InvalidOperationException();
            }

            // Called when op is not in the queue yet, so can't be otherwise executing
            public void DoAbort()
            {
                ErrorCode = SocketError.OperationAborted;
            }

            protected abstract bool DoTryComplete(SocketAsyncContext context);

            public abstract void InvokeCallback(bool allowPooling);

            [Conditional("SOCKETASYNCCONTEXT_TRACE")]
            public void Trace(string message, [CallerMemberName] string? memberName = null)
            {
                OutputTrace($"{IdOf(this)}.{memberName}: {message}");
            }

            [Conditional("SOCKETASYNCCONTEXT_TRACE")]
            public void TraceWithContext(SocketAsyncContext context, string message, [CallerMemberName] string? memberName = null)
            {
                OutputTrace($"{IdOf(context)}, {IdOf(this)}.{memberName}: {message}");
            }
        }

        // These two abstract classes differentiate the operations that go in the
        // read queue vs the ones that go in the write queue.
        private abstract class ReadOperation : AsyncOperation, IThreadPoolWorkItem
        {
            public ReadOperation(SocketAsyncContext context) : base(context) { }

            void IThreadPoolWorkItem.Execute() => AssociatedContext.ProcessAsyncReadOperation(this);
        }

        private abstract class WriteOperation : AsyncOperation, IThreadPoolWorkItem
        {
            public WriteOperation(SocketAsyncContext context) : base(context) { }

            void IThreadPoolWorkItem.Execute() => AssociatedContext.ProcessAsyncWriteOperation(this);
        }

        private abstract unsafe class SendOperation : WriteOperation
        {
            public SocketFlags Flags;
            public int BytesTransferred;
            public int Offset;
            public int Count;

            public SendOperation(SocketAsyncContext context) : base(context) { }

            public Action<int, Memory<byte>, SocketFlags, SocketError>? Callback { get; set; }

            public override unsafe void InvokeCallback(bool allowPooling) =>
                Callback!(BytesTransferred, SocketAddress, SocketFlags.None, ErrorCode);
        }

        private sealed class BufferMemorySendOperation : SendOperation
        {
            public Memory<byte> Buffer;

            public BufferMemorySendOperation(SocketAsyncContext context) : base(context) { }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                int bufferIndex = 0;
                return SocketPal.TryCompleteSendTo(context._socket, Buffer.Span, null, ref bufferIndex, ref Offset, ref Count, Flags, SocketAddress.Span, ref BytesTransferred, out ErrorCode);
            }

            public override unsafe void InvokeCallback(bool allowPooling)
            {
                var cb = Callback!;
                int bt = BytesTransferred;
                Memory<byte> sa = SocketAddress;
                SocketError ec = ErrorCode;

                if (allowPooling)
                {
                    AssociatedContext.ReturnOperation(this);
                }

                cb(bt, sa, SocketFlags.None, ec);
            }
        }

        private sealed class BufferListSendOperation : SendOperation
        {
            public IList<ArraySegment<byte>>? Buffers;
            public int BufferIndex;

            public BufferListSendOperation(SocketAsyncContext context) : base(context) { }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                return SocketPal.TryCompleteSendTo(context._socket, default(ReadOnlySpan<byte>), Buffers, ref BufferIndex, ref Offset, ref Count, Flags, SocketAddress.Span, ref BytesTransferred, out ErrorCode);
            }

            public override void InvokeCallback(bool allowPooling)
            {
                var cb = Callback!;
                int bt = BytesTransferred;
                Memory<byte> sa = SocketAddress;
                SocketError ec = ErrorCode;

                if (allowPooling)
                {
                    AssociatedContext.ReturnOperation(this);
                }

                cb(bt, sa, SocketFlags.None, ec);
            }
        }

        private sealed unsafe class BufferPtrSendOperation : SendOperation
        {
            public byte* BufferPtr;

            public BufferPtrSendOperation(SocketAsyncContext context) : base(context) { }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                int bufferIndex = 0;
                int bufferLength = Offset + Count; // TryCompleteSendTo expects the entire buffer, which it then indexes into with the ref Offset and ref Count arguments
                return SocketPal.TryCompleteSendTo(context._socket, new ReadOnlySpan<byte>(BufferPtr, bufferLength), null, ref bufferIndex, ref Offset, ref Count, Flags, SocketAddress.Span, ref BytesTransferred, out ErrorCode);
            }
        }

        private abstract class ReceiveOperation : ReadOperation
        {
            public SocketFlags Flags;
            public SocketFlags ReceivedFlags;
            public int BytesTransferred;

            public ReceiveOperation(SocketAsyncContext context) : base(context) { }

            public Action<int, Memory<byte>, SocketFlags, SocketError>? Callback { get; set; }

            public override void InvokeCallback(bool allowPooling) =>
                Callback!(BytesTransferred, SocketAddress, ReceivedFlags, ErrorCode);
        }

        private sealed class BufferMemoryReceiveOperation : ReceiveOperation
        {
            public Memory<byte> Buffer;
            public bool SetReceivedFlags;

            public BufferMemoryReceiveOperation(SocketAsyncContext context) : base(context) { }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                // Zero byte read is performed to know when data is available.
                // We don't have to call receive, our caller is interested in the event.
                if (Buffer.Length == 0 && Flags == SocketFlags.None && SocketAddress.Length == 0)
                {
                    BytesTransferred = 0;
                    ReceivedFlags = SocketFlags.None;
                    ErrorCode = SocketError.Success;
                    return true;
                }
                else
                {
                    if (!SetReceivedFlags)
                    {
                        Debug.Assert(SocketAddress.Length == 0);

                        ReceivedFlags = SocketFlags.None;
                        return SocketPal.TryCompleteReceive(context._socket, Buffer.Span, Flags, out BytesTransferred, out ErrorCode);
                    }
                    else
                    {
                        bool completed = SocketPal.TryCompleteReceiveFrom(context._socket, Buffer.Span, null, Flags, SocketAddress.Span, out int socketAddressLen, out BytesTransferred, out ReceivedFlags, out ErrorCode);
                        if (completed && ErrorCode == SocketError.Success)
                        {
                            SocketAddress = SocketAddress.Slice(0, socketAddressLen);
                        }
                        return completed;
                    }
                }
            }

            public override void InvokeCallback(bool allowPooling)
            {
                var cb = Callback!;
                int bt = BytesTransferred;
                Memory<byte> sa = SocketAddress;
                SocketFlags rf = ReceivedFlags;
                SocketError ec = ErrorCode;

                if (allowPooling)
                {
                    AssociatedContext.ReturnOperation(this);
                }

                cb(bt, sa, rf, ec);
            }
        }

        private sealed class BufferListReceiveOperation : ReceiveOperation
        {
            public IList<ArraySegment<byte>>? Buffers;

            public BufferListReceiveOperation(SocketAsyncContext context) : base(context) { }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                bool completed = SocketPal.TryCompleteReceiveFrom(context._socket, default(Span<byte>), Buffers, Flags, SocketAddress.Span, out int socketAddressLen, out BytesTransferred, out ReceivedFlags, out ErrorCode);
                if (completed && ErrorCode == SocketError.Success)
                {
                    SocketAddress = SocketAddress.Slice(0, socketAddressLen);
                }
                return completed;
            }

            public override void InvokeCallback(bool allowPooling)
            {
                var cb = Callback!;
                int bt = BytesTransferred;
                Memory<byte> sa = SocketAddress;
                SocketFlags rf = ReceivedFlags;
                SocketError ec = ErrorCode;

                if (allowPooling)
                {
                    AssociatedContext.ReturnOperation(this);
                }

                cb(bt, sa, rf, ec);
            }
        }

        private sealed unsafe class BufferPtrReceiveOperation : ReceiveOperation
        {
            public byte* BufferPtr;
            public int Length;

            public BufferPtrReceiveOperation(SocketAsyncContext context) : base(context) { }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                bool completed = SocketPal.TryCompleteReceiveFrom(context._socket, new Span<byte>(BufferPtr, Length), null, Flags, SocketAddress.Span, out int socketAddressLen, out BytesTransferred, out ReceivedFlags, out ErrorCode);
                if (completed && ErrorCode == SocketError.Success)
                {
                    SocketAddress = SocketAddress.Slice(0, socketAddressLen);
                }
                return completed;
            }
        }

        private sealed class ReceiveMessageFromOperation : ReadOperation
        {
            public Memory<byte> Buffer;
            public SocketFlags Flags;
            public int BytesTransferred;
            public SocketFlags ReceivedFlags;
            public IList<ArraySegment<byte>>? Buffers;

            public bool IsIPv4;
            public bool IsIPv6;
            public IPPacketInformation IPPacketInformation;

            public ReceiveMessageFromOperation(SocketAsyncContext context) : base(context) { }

            public Action<int, Memory<byte>, SocketFlags, IPPacketInformation, SocketError>? Callback { get; set; }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                bool completed = SocketPal.TryCompleteReceiveMessageFrom(context._socket, Buffer.Span, Buffers, Flags, SocketAddress, out int socketAddressLen, IsIPv4, IsIPv6, out BytesTransferred, out ReceivedFlags, out IPPacketInformation, out ErrorCode);
                if (completed && ErrorCode == SocketError.Success)
                {
                    SocketAddress = SocketAddress.Slice(0, socketAddressLen);
                }
                return completed;
            }

            public override void InvokeCallback(bool allowPooling) =>
                Callback!(BytesTransferred, SocketAddress, ReceivedFlags, IPPacketInformation, ErrorCode);
        }

        private sealed unsafe class BufferPtrReceiveMessageFromOperation : ReadOperation
        {
            public byte* BufferPtr;
            public int Length;
            public SocketFlags Flags;
            public int BytesTransferred;
            public SocketFlags ReceivedFlags;

            public bool IsIPv4;
            public bool IsIPv6;
            public IPPacketInformation IPPacketInformation;

            public BufferPtrReceiveMessageFromOperation(SocketAsyncContext context) : base(context) { }

            public Action<int, Memory<byte>, SocketFlags, IPPacketInformation, SocketError>? Callback { get; set; }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                bool completed = SocketPal.TryCompleteReceiveMessageFrom(context._socket, new Span<byte>(BufferPtr, Length), null, Flags, SocketAddress!, out int socketAddressLen, IsIPv4, IsIPv6, out BytesTransferred, out ReceivedFlags, out IPPacketInformation, out ErrorCode);
                if (completed && ErrorCode == SocketError.Success)
                {
                    SocketAddress = SocketAddress.Slice(0, socketAddressLen);
                }
                return completed;
            }

            public override void InvokeCallback(bool allowPooling) =>
                Callback!(BytesTransferred, SocketAddress, ReceivedFlags, IPPacketInformation, ErrorCode);
        }

        private sealed class AcceptOperation : ReadOperation
        {
            public IntPtr AcceptedFileDescriptor;

            public AcceptOperation(SocketAsyncContext context) : base(context) { }

            public Action<IntPtr, Memory<byte>, SocketError>? Callback { get; set; }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                bool completed = SocketPal.TryCompleteAccept(context._socket, SocketAddress, out int socketAddressLen, out AcceptedFileDescriptor, out ErrorCode);
                Debug.Assert(ErrorCode == SocketError.Success || AcceptedFileDescriptor == (IntPtr)(-1), $"Unexpected values: ErrorCode={ErrorCode}, AcceptedFileDescriptor={AcceptedFileDescriptor}");
                if (ErrorCode == SocketError.Success)
                {
                    SocketAddress = SocketAddress.Slice(0, socketAddressLen);
                }
                return completed;
            }

            public override void InvokeCallback(bool allowPooling)
            {
                var cb = Callback!;
                IntPtr fd = AcceptedFileDescriptor;
                Memory<byte> sa = SocketAddress;
                SocketError ec = ErrorCode;

                if (allowPooling)
                {
                    AssociatedContext.ReturnOperation(this);
                }

                cb(fd, sa, ec);
            }
        }

        private sealed class ConnectOperation : WriteOperation
        {
            public ConnectOperation(SocketAsyncContext context) : base(context) { }

            public Action<SocketError>? Callback { get; set; }

            protected override bool DoTryComplete(SocketAsyncContext context)
            {
                bool result = SocketPal.TryCompleteConnect(context._socket, out ErrorCode);
                context._socket.RegisterConnectResult(ErrorCode);
                return result;
            }

            public override void InvokeCallback(bool allowPooling) =>
                Callback!(ErrorCode);
        }

        private sealed class SendFileOperation : WriteOperation
        {
            public SafeFileHandle FileHandle = null!; // always set when constructed
            public long Offset;
            public long Count;
            public long BytesTransferred;

            public SendFileOperation(SocketAsyncContext context) : base(context) { }

            public Action<long, SocketError>? Callback { get; set; }

            public override void InvokeCallback(bool allowPooling) =>
                Callback!(BytesTransferred, ErrorCode);

            protected override bool DoTryComplete(SocketAsyncContext context) =>
                SocketPal.TryCompleteSendFile(context._socket, FileHandle, ref Offset, ref Count, ref BytesTransferred, out ErrorCode);
        }

        // In debug builds, this struct guards against:
        // (1) Unexpected lock reentrancy, which should never happen
        // (2) Deadlock, by setting a reasonably large timeout
        private readonly struct LockToken : IDisposable
        {
            private readonly object _lockObject;

            public LockToken(object lockObject)
            {
                Debug.Assert(lockObject != null);

                _lockObject = lockObject;

                Debug.Assert(!Monitor.IsEntered(_lockObject));

#if DEBUG
                bool success = Monitor.TryEnter(_lockObject, 10000);
                Debug.Assert(success, "Timed out waiting for queue lock");
#else
                Monitor.Enter(_lockObject);
#endif
            }

            public void Dispose()
            {
                Debug.Assert(Monitor.IsEntered(_lockObject));
                Monitor.Exit(_lockObject);
            }
        }

        public enum OperationResult
        {
            Pending = 0,
            Completed = 1,
            Cancelled = 2
        }

        private struct OperationQueue<TOperation>
            where TOperation : AsyncOperation
        {
            // Quick overview:
            //
            // When attempting to perform an IO operation, the caller first checks IsReady,
            // and if true, attempts to perform the operation itself.
            // If this returns EWOULDBLOCK, or if the queue was not ready, then the operation
            // is enqueued by calling StartAsyncOperation and the state becomes Waiting.
            // When an epoll notification is received, we check if the state is Waiting,
            // and if so, change the state to Processing and enqueue a workitem to the threadpool
            // to try to perform the enqueued operations.
            // If an operation is successfully performed, we remove it from the queue,
            // enqueue another threadpool workitem to process the next item in the queue (if any),
            // and call the user's completion callback.
            // If we successfully process all enqueued operations, then the state becomes Ready;
            // otherwise, the state becomes Waiting and we wait for another epoll notification.

            private enum QueueState : int
            {
                Ready = 0,          // Indicates that data MAY be available on the socket.
                                    // Queue must be empty.
                Waiting = 1,        // Indicates that data is definitely not available on the socket.
                                    // Queue must not be empty.
                Processing = 2,     // Indicates that a thread pool item has been scheduled (and may
                                    // be executing) to process the IO operations in the queue.
                                    // Queue must not be empty.
                Stopped = 3,        // Indicates that the queue has been stopped because the
                                    // socket has been closed.
                                    // Queue must be empty.
            }

            // These fields define the queue state.

            private QueueState _state;      // See above
            private bool _isNextOperationSynchronous;
            private int _sequenceNumber;    // This sequence number is updated when we receive an epoll notification.
                                            // It allows us to detect when a new epoll notification has arrived
                                            // since the last time we checked the state of the queue.
                                            // If this happens, we MUST retry the operation, otherwise we risk
                                            // "losing" the notification and causing the operation to pend indefinitely.
            private AsyncOperation? _tail;   // Queue of pending IO operations to process when data becomes available.

            // The _queueLock is used to ensure atomic access to the queue state above.
            // The lock is only ever held briefly, to read and/or update queue state, and
            // never around any external call, e.g. OS call or user code invocation.
            private object _queueLock;

            private LockToken Lock() => new LockToken(_queueLock);

            public bool IsNextOperationSynchronous_Speculative => _isNextOperationSynchronous;

            public void Init()
            {
                Debug.Assert(_queueLock == null);
                _queueLock = new object();

                _state = QueueState.Ready;
                _sequenceNumber = 0;
            }

            // IsReady returns whether an operation can be executed immediately.
            // observedSequenceNumber must be passed to StartAsyncOperation.
            public bool IsReady(SocketAsyncContext context, out int observedSequenceNumber)
            {
                // It is safe to read _state and _sequence without using Lock.
                // - The return value is soley based on Volatile.Read of _state.
                // - The Volatile.Read of _sequenceNumber ensures we read a value before executing the operation.
                //   This is needed to retry the operation in StartAsyncOperation in case the _sequenceNumber incremented.
                // - Because no Lock is taken, it is possible we observe a sequence number increment before the state
                //   becomes Ready. When that happens, observedSequenceNumber is decremented, and StartAsyncOperation will
                //   execute the operation because the sequence number won't match.

                Debug.Assert(sizeof(QueueState) == sizeof(int));
                QueueState state = (QueueState)Volatile.Read(ref Unsafe.As<QueueState, int>(ref _state));
                observedSequenceNumber = Volatile.Read(ref _sequenceNumber);

                bool isReady = state == QueueState.Ready || state == QueueState.Stopped;
                if (!isReady)
                {
                    observedSequenceNumber--;
                }

                Trace(context, $"{isReady}");

                return isReady;
            }

            // Return true for pending, false for completed synchronously (including failure and abort)
            public bool StartAsyncOperation(SocketAsyncContext context, TOperation operation, int observedSequenceNumber, CancellationToken cancellationToken = default)
            {
                Trace(context, $"Enter");

                if (!context.IsRegistered && !context.TryRegister(out Interop.Error error))
                {
                    HandleFailedRegistration(context, operation, error);

                    Trace(context, "Leave, not registered");
                    return false;
                }

                while (true)
                {
                    bool doAbort = false;
                    using (Lock())
                    {
                        switch (_state)
                        {
                            case QueueState.Ready:
                                if (observedSequenceNumber != _sequenceNumber)
                                {
                                    // The queue has become ready again since we previously checked it.
                                    // So, we need to retry the operation before we enqueue it.
                                    Debug.Assert(observedSequenceNumber - _sequenceNumber < 10000, "Very large sequence number increase???");
                                    observedSequenceNumber = _sequenceNumber;
                                    break;
                                }

                                // Caller tried the operation and got an EWOULDBLOCK, so we need to transition.
                                _state = QueueState.Waiting;
                                goto case QueueState.Waiting;

                            case QueueState.Waiting:
                            case QueueState.Processing:
                                // Enqueue the operation.
                                Debug.Assert(operation.Next == operation, "Expected operation.Next == operation");

                                if (_tail == null)
                                {
                                    Debug.Assert(!_isNextOperationSynchronous);
                                    _isNextOperationSynchronous = operation.Event != null;
                                }
                                else
                                {
                                    operation.Next = _tail.Next;
                                    _tail.Next = operation;
                                }

                                _tail = operation;
                                Trace(context, $"Leave, enqueued {IdOf(operation)}");

                                // Now that the object is enqueued, hook up cancellation.
                                // Note that it's possible the call to register itself could
                                // call TryCancel, so we do this after the op is fully enqueued.
                                if (cancellationToken.CanBeCanceled)
                                {
                                    operation.CancellationRegistration = cancellationToken.UnsafeRegister(s => ((TOperation)s!).TryCancel(), operation);
                                }

                                return true;

                            case QueueState.Stopped:
                                Debug.Assert(_tail == null);
                                doAbort = true;
                                break;

                            default:
                                Environment.FailFast("unexpected queue state");
                                break;
                        }
                    }

                    if (doAbort)
                    {
                        operation.DoAbort();
                        Trace(context, $"Leave, queue stopped");
                        return false;
                    }

                    // Retry the operation.
                    if (operation.TryComplete(context) != OperationResult.Pending)
                    {
                        Trace(context, $"Leave, retry succeeded");
                        return false;
                    }
                }

                static void HandleFailedRegistration(SocketAsyncContext context, TOperation operation, Interop.Error error)
                {
                    Debug.Assert(error != Interop.Error.SUCCESS);

                    // macOS: kevent returns EPIPE when adding pipe fd for which the other end is closed.
                    if (error == Interop.Error.EPIPE)
                    {
                        // Because the other end close, we expect the operation to complete when we retry it.
                        // If it doesn't, we fall through and throw an Exception.
                        if (operation.TryComplete(context) != OperationResult.Pending)
                        {
                            return;
                        }
                    }

                    if (error == Interop.Error.ENOMEM || error == Interop.Error.ENOSPC)
                    {
                        throw new OutOfMemoryException();
                    }
                    else
                    {
                        throw new InternalException(error);
                    }
                }
            }

            public AsyncOperation? ProcessSyncEventOrGetAsyncEvent(SocketAsyncContext context, bool skipAsyncEvents = false)
            {
                AsyncOperation op;
                using (Lock())
                {
                    Trace(context, $"Enter");

                    switch (_state)
                    {
                        case QueueState.Ready:
                            Debug.Assert(_tail == null, "State == Ready but queue is not empty!");
                            _sequenceNumber++;
                            Trace(context, $"Exit (previously ready)");
                            return null;

                        case QueueState.Waiting:
                            Debug.Assert(_tail != null, "State == Waiting but queue is empty!");
                            op = _tail.Next;
                            Debug.Assert(_isNextOperationSynchronous == (op.Event != null));
                            if (skipAsyncEvents && !_isNextOperationSynchronous)
                            {
                                // Return the operation to indicate that the async operation was not processed, without making
                                // any state changes because async operations are being skipped
                                return op;
                            }

                            _state = QueueState.Processing;
                            // Break out and release lock
                            break;

                        case QueueState.Processing:
                            Debug.Assert(_tail != null, "State == Processing but queue is empty!");
                            _sequenceNumber++;
                            Trace(context, $"Exit (currently processing)");
                            return null;

                        case QueueState.Stopped:
                            Debug.Assert(_tail == null);
                            Trace(context, $"Exit (stopped)");
                            return null;

                        default:
                            Environment.FailFast("unexpected queue state");
                            return null;
                    }
                }

                ManualResetEventSlim? e = op.Event;
                if (e != null)
                {
                    // Sync operation.  Signal waiting thread to continue processing.
                    e.Set();
                    return null;
                }
                else
                {
                    // Async operation.  The caller will figure out how to process the IO.
                    Debug.Assert(!skipAsyncEvents);
                    return op;
                }
            }

            internal void ProcessAsyncOperation(TOperation op)
            {
                OperationResult result = ProcessQueuedOperation(op);

                Debug.Assert(op.Event == null, "Sync operation encountered in ProcessAsyncOperation");

                if (result == OperationResult.Completed)
                {
                    // At this point, the operation has completed and it's no longer
                    // in the queue / no one else has a reference to it.  We can invoke
                    // the callback and let it pool the object if appropriate. This is
                    // also a good time to unregister from cancellation; we must do
                    // so before the object is returned to the pool (or else a cancellation
                    // request for a previous operation could affect a subsequent one)
                    // and here we know the operation has completed.
                    op.CancellationRegistration.Dispose();
                    op.InvokeCallback(allowPooling: true);
                }
            }

            public OperationResult ProcessQueuedOperation(TOperation op)
            {
                SocketAsyncContext context = op.AssociatedContext;

                int observedSequenceNumber;
                using (Lock())
                {
                    Trace(context, $"Enter");

                    if (_state == QueueState.Stopped)
                    {
                        Debug.Assert(_tail == null);
                        Trace(context, $"Exit (stopped)");
                        return OperationResult.Cancelled;
                    }
                    else
                    {
                        Debug.Assert(_state == QueueState.Processing, $"_state={_state} while processing queue!");
                        Debug.Assert(_tail != null, "Unexpected empty queue while processing I/O");
                        Debug.Assert(op == _tail.Next, "Operation is not at head of queue???");
                        observedSequenceNumber = _sequenceNumber;
                    }
                }

                OperationResult result;
                while (true)
                {
                    result = op.TryComplete(context);
                    if (result != OperationResult.Pending)
                    {
                        break;
                    }

                    // Check for retry and reset queue state.

                    using (Lock())
                    {
                        if (_state == QueueState.Stopped)
                        {
                            Debug.Assert(_tail == null);
                            Trace(context, $"Exit (stopped)");
                            return OperationResult.Cancelled;
                        }
                        else
                        {
                            Debug.Assert(_state == QueueState.Processing, $"_state={_state} while processing queue!");

                            if (observedSequenceNumber != _sequenceNumber)
                            {
                                // We received another epoll notification since we previously checked it.
                                // So, we need to retry the operation.
                                Debug.Assert(observedSequenceNumber - _sequenceNumber < 10000, "Very large sequence number increase???");
                                observedSequenceNumber = _sequenceNumber;
                            }
                            else
                            {
                                _state = QueueState.Waiting;
                                Trace(context, $"Exit (received EAGAIN)");
                                return OperationResult.Pending;
                            }
                        }
                    }
                }

                // Remove the op from the queue and see if there's more to process.

                AsyncOperation? nextOp = null;
                using (Lock())
                {
                    if (_state == QueueState.Stopped)
                    {
                        Debug.Assert(_tail == null);
                        Trace(context, $"Exit (stopped)");
                    }
                    else
                    {
                        Debug.Assert(_state == QueueState.Processing, $"_state={_state} while processing queue!");
                        Debug.Assert(_tail.Next == op, "Queue modified while processing queue");

                        if (op == _tail)
                        {
                            // No more operations to process
                            _tail = null;
                            _isNextOperationSynchronous = false;
                            _state = QueueState.Ready;
                            _sequenceNumber++;
                            Trace(context, $"Exit (finished queue)");
                        }
                        else
                        {
                            // Pop current operation and advance to next
                            nextOp = _tail.Next = op.Next;
                            _isNextOperationSynchronous = nextOp.Event != null;
                        }
                    }
                }

                nextOp?.Dispatch();

                Debug.Assert(result != OperationResult.Pending);
                return result;
            }

            public void CancelAndContinueProcessing(TOperation op)
            {
                // Note, only sync operations use this method.
                Debug.Assert(op.Event != null);

                // Remove operation from queue.
                // Note it must be there since it can only be processed and removed by the caller.
                AsyncOperation? nextOp = null;
                using (Lock())
                {
                    if (_state == QueueState.Stopped)
                    {
                        Debug.Assert(_tail == null);
                    }
                    else
                    {
                        Debug.Assert(_tail != null, "Unexpected empty queue in CancelAndContinueProcessing");

                        if (_tail.Next == op)
                        {
                            // We're the head of the queue
                            if (op == _tail)
                            {
                                // No more operations
                                _tail = null;
                                _isNextOperationSynchronous = false;
                            }
                            else
                            {
                                // Pop current operation and advance to next
                                _tail.Next = op.Next;
                                _isNextOperationSynchronous = op.Next.Event != null;
                            }

                            // We're the first op in the queue.
                            if (_state == QueueState.Processing)
                            {
                                // The queue has already handed off execution responsibility to us.
                                // We need to dispatch to the next op.
                                if (_tail == null)
                                {
                                    _state = QueueState.Ready;
                                    _sequenceNumber++;
                                }
                                else
                                {
                                    nextOp = _tail.Next;
                                }
                            }
                            else if (_state == QueueState.Waiting)
                            {
                                if (_tail == null)
                                {
                                    _state = QueueState.Ready;
                                    _sequenceNumber++;
                                }
                            }
                        }
                        else
                        {
                            // We're not the head of the queue.
                            // Just find this op and remove it.
                            AsyncOperation current = _tail.Next;
                            while (current.Next != op)
                            {
                                current = current.Next;
                            }

                            if (current.Next == _tail)
                            {
                                _tail = current;
                            }
                            current.Next = current.Next.Next;
                        }
                    }
                }

                nextOp?.Dispatch();
            }

            // Called when the socket is closed.
            public bool StopAndAbort(SocketAsyncContext context)
            {
                bool aborted = false;

                // We should be called exactly once, by SafeSocketHandle.
                Debug.Assert(_state != QueueState.Stopped);

                using (Lock())
                {
                    Trace(context, $"Enter");

                    Debug.Assert(_state != QueueState.Stopped);

                    _state = QueueState.Stopped;

                    if (_tail != null)
                    {
                        AsyncOperation op = _tail;
                        do
                        {
                            aborted |= op.TryCancel();
                            op = op.Next;
                        } while (op != _tail);
                    }

                    _tail = null;
                    _isNextOperationSynchronous = false;

                    Trace(context, $"Exit");
                }

                return aborted;
            }

            [Conditional("SOCKETASYNCCONTEXT_TRACE")]
            public void Trace(SocketAsyncContext context, string message, [CallerMemberName] string? memberName = null)
            {
                string queueType =
                    typeof(TOperation) == typeof(ReadOperation) ? "recv" :
                    typeof(TOperation) == typeof(WriteOperation) ? "send" :
                    "???";

                OutputTrace($"{IdOf(context)}-{queueType}.{memberName}: {message}, {_state}-{_sequenceNumber}, {((_tail == null) ? "empty" : "not empty")}");
            }
        }

        private readonly SafeSocketHandle _socket;
        private OperationQueue<ReadOperation> _receiveQueue;
        private OperationQueue<WriteOperation> _sendQueue;
        private SocketAsyncEngine? _asyncEngine;
        private bool IsRegistered => _asyncEngine != null;
        private bool _isHandleNonBlocking;

        private readonly object _registerLock = new object();

        public SocketAsyncContext(SafeSocketHandle socket)
        {
            _socket = socket;

            _receiveQueue.Init();
            _sendQueue.Init();
        }

        public bool PreferInlineCompletions
        {
            // Socket.PreferInlineCompletions is an experimental API with internal access modifier.
            // DynamicDependency ensures the setter is available externally using reflection.
            [DynamicDependency("set_PreferInlineCompletions", typeof(Socket))]
            get => _socket.PreferInlineCompletions;
        }

        private bool TryRegister(out Interop.Error error)
        {
            Debug.Assert(_isHandleNonBlocking);
            lock (_registerLock)
            {
                if (_asyncEngine == null)
                {
                    bool addedRef = false;
                    try
                    {
                        _socket.DangerousAddRef(ref addedRef);
                        IntPtr handle = _socket.DangerousGetHandle();
                        if (SocketAsyncEngine.TryRegisterSocket(handle, this, out SocketAsyncEngine? engine, out error))
                        {
                            Volatile.Write(ref _asyncEngine, engine);

                            Trace("Registered");
                            return true;
                        }
                        else
                        {
                            Trace("Registration failed");
                            return false;
                        }
                    }
                    finally
                    {
                        if (addedRef)
                        {
                            _socket.DangerousRelease();
                        }
                    }
                }
                error = Interop.Error.SUCCESS;
                return true;
            }
        }

        public bool StopAndAbort()
        {
            bool aborted = false;

            // Drain queues
            aborted |= _sendQueue.StopAndAbort(this);
            aborted |= _receiveQueue.StopAndAbort(this);

            // We don't need to synchronize with Register.
            // This method is called when the handle gets released.
            // The Register method will throw ODE when it tries to use the handle at this point.
            _asyncEngine?.UnregisterSocket(_socket.DangerousGetHandle());

            return aborted;
        }

        public void SetHandleNonBlocking()
        {
            //
            // Our sockets may start as blocking, and later transition to non-blocking, either because the user
            // explicitly requested non-blocking mode, or because we need non-blocking mode to support async
            // operations.  We never transition back to blocking mode, to avoid problems synchronizing that
            // transition with the async infrastructure.
            //
            // Note that there's no synchronization here, so we may set the non-blocking option multiple times
            // in a race.  This should be fine.
            //
            if (!_isHandleNonBlocking)
            {
                if (Interop.Sys.Fcntl.SetIsNonBlocking(_socket, 1) != 0)
                {
                    throw new SocketException((int)SocketPal.GetSocketErrorForErrorCode(Interop.Sys.GetLastError()));
                }

                _isHandleNonBlocking = true;
            }
        }

        public bool IsHandleNonBlocking => _isHandleNonBlocking;

        private void PerformSyncOperation<TOperation>(ref OperationQueue<TOperation> queue, TOperation operation, int timeout, int observedSequenceNumber)
            where TOperation : AsyncOperation
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            using (var e = new ManualResetEventSlim(false, 0))
            {
                operation.Event = e;

                if (!queue.StartAsyncOperation(this, operation, observedSequenceNumber))
                {
                    // Completed synchronously
                    return;
                }

                bool timeoutExpired = false;
                while (true)
                {
                    DateTime waitStart = DateTime.UtcNow;

                    if (!e.Wait(timeout))
                    {
                        timeoutExpired = true;
                        break;
                    }

                    // Reset the event now to avoid lost notifications if the processing is unsuccessful.
                    e.Reset();

                    // We've been signalled to try to process the operation.
                    OperationResult result = queue.ProcessQueuedOperation(operation);
                    if (result == OperationResult.Completed ||
                        result == OperationResult.Cancelled)
                    {
                        break;
                    }

                    // Couldn't process the operation.
                    // Adjust timeout and try again.
                    if (timeout > 0)
                    {
                        timeout -= (DateTime.UtcNow - waitStart).Milliseconds;

                        if (timeout <= 0)
                        {
                            timeoutExpired = true;
                            break;
                        }
                    }
                }

                if (timeoutExpired)
                {
                    queue.CancelAndContinueProcessing(operation);
                    operation.ErrorCode = SocketError.TimedOut;
                }
            }
        }

        private bool ShouldRetrySyncOperation(out SocketError errorCode)
        {
            if (_isHandleNonBlocking)
            {
                errorCode = SocketError.Success;    // Will be ignored
                return true;
            }

            // We are in blocking mode, so the EAGAIN we received indicates a timeout.
            errorCode = SocketError.TimedOut;
            return false;
        }

        private void ProcessAsyncReadOperation(ReadOperation op) => _receiveQueue.ProcessAsyncOperation(op);

        private void ProcessAsyncWriteOperation(WriteOperation op) => _sendQueue.ProcessAsyncOperation(op);

        public SocketError Accept(Memory<byte> socketAddress, out int socketAddressLen, out IntPtr acceptedFd)
        {
            Debug.Assert(socketAddress.Length > 0, $"Unexpected socketAddressLen: {socketAddress.Length}");

            SocketError errorCode;
            int observedSequenceNumber;
            if (_receiveQueue.IsReady(this, out observedSequenceNumber) &&
                SocketPal.TryCompleteAccept(_socket, socketAddress, out socketAddressLen, out acceptedFd, out errorCode))
            {
                Debug.Assert(errorCode == SocketError.Success || acceptedFd == (IntPtr)(-1), $"Unexpected values: errorCode={errorCode}, acceptedFd={acceptedFd}");
                return errorCode;
            }

            var operation = new AcceptOperation(this)
            {
                SocketAddress = socketAddress,
            };

            PerformSyncOperation(ref _receiveQueue, operation, -1, observedSequenceNumber);

            socketAddressLen = operation.SocketAddress.Length;
            acceptedFd = operation.AcceptedFileDescriptor;
            return operation.ErrorCode;
        }

        public SocketError AcceptAsync(Memory<byte> socketAddress, out int socketAddressLen, out IntPtr acceptedFd, Action<IntPtr, Memory<byte>, SocketError> callback, CancellationToken cancellationToken)
        {
            Debug.Assert(socketAddress.Length > 0, $"Unexpected socketAddressLen: {socketAddress.Length}");
            Debug.Assert(callback != null, "Expected non-null callback");

            SetHandleNonBlocking();

            SocketError errorCode;
            int observedSequenceNumber;
            if (_receiveQueue.IsReady(this, out observedSequenceNumber) &&
                SocketPal.TryCompleteAccept(_socket, socketAddress, out socketAddressLen, out acceptedFd, out errorCode))
            {
                Debug.Assert(errorCode == SocketError.Success || acceptedFd == (IntPtr)(-1), $"Unexpected values: errorCode={errorCode}, acceptedFd={acceptedFd}");

                return errorCode;
            }

            AcceptOperation operation = RentAcceptOperation();
            operation.Callback = callback;
            operation.SocketAddress = socketAddress;

            if (!_receiveQueue.StartAsyncOperation(this, operation, observedSequenceNumber, cancellationToken))
            {
                socketAddressLen = operation.SocketAddress.Length;
                acceptedFd = operation.AcceptedFileDescriptor;
                errorCode = operation.ErrorCode;

                ReturnOperation(operation);
                return errorCode;
            }

            acceptedFd = (IntPtr)(-1);
            socketAddressLen = 0;
            return SocketError.IOPending;
        }

        public SocketError Connect(Memory<byte> socketAddress)
        {
            Debug.Assert(socketAddress.Length > 0, $"Unexpected socketAddressLen: {socketAddress.Length}");

            // Connect is different than the usual "readiness" pattern of other operations.
            // We need to call TryStartConnect to initiate the connect with the OS,
            // before we try to complete it via epoll notification.
            // Thus, always call TryStartConnect regardless of readiness.
            SocketError errorCode;
            int observedSequenceNumber;
            _sendQueue.IsReady(this, out observedSequenceNumber);
            if (SocketPal.TryStartConnect(_socket, socketAddress, out errorCode) ||
                !ShouldRetrySyncOperation(out errorCode))
            {
                _socket.RegisterConnectResult(errorCode);
                return errorCode;
            }

            var operation = new ConnectOperation(this)
            {
                SocketAddress = socketAddress,
            };

            PerformSyncOperation(ref _sendQueue, operation, -1, observedSequenceNumber);

            return operation.ErrorCode;
        }

        public SocketError ConnectAsync(Memory<byte> socketAddress, Action<SocketError> callback)
        {
            Debug.Assert(socketAddress.Length > 0, $"Unexpected socketAddressLen: {socketAddress.Length}");
            Debug.Assert(callback != null, "Expected non-null callback");

            SetHandleNonBlocking();

            // Connect is different than the usual "readiness" pattern of other operations.
            // We need to initiate the connect before we try to complete it.
            // Thus, always call TryStartConnect regardless of readiness.
            SocketError errorCode;
            int observedSequenceNumber;
            _sendQueue.IsReady(this, out observedSequenceNumber);
            if (SocketPal.TryStartConnect(_socket, socketAddress, out errorCode))
            {
                _socket.RegisterConnectResult(errorCode);
                return errorCode;
            }

            var operation = new ConnectOperation(this)
            {
                Callback = callback,
                SocketAddress = socketAddress,
            };

            if (!_sendQueue.StartAsyncOperation(this, operation, observedSequenceNumber))
            {
                return operation.ErrorCode;
            }

            return SocketError.IOPending;
        }

        public SocketError Receive(Memory<byte> buffer, SocketFlags flags, int timeout, out int bytesReceived)
        {
            return ReceiveFrom(buffer, ref flags, Memory<byte>.Empty, out int _, timeout, out bytesReceived);
        }

        public SocketError Receive(Span<byte> buffer, SocketFlags flags, int timeout, out int bytesReceived)
        {
            return ReceiveFrom(buffer, ref flags, Memory<byte>.Empty, out int _, timeout, out bytesReceived);
        }

        public SocketError ReceiveAsync(Memory<byte> buffer, SocketFlags flags, out int bytesReceived, out SocketFlags receivedFlags, Action<int, Memory<byte>, SocketFlags, SocketError> callback, CancellationToken cancellationToken)
        {
            return ReceiveFromAsync(buffer, flags, Memory<byte>.Empty, out int _, out bytesReceived, out receivedFlags, callback, cancellationToken);
        }

        public unsafe SocketError ReceiveFrom(Memory<byte> buffer, ref SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, int timeout, out int bytesReceived)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SocketFlags receivedFlags;
            SocketError errorCode;
            int observedSequenceNumber;
            if (_receiveQueue.IsReady(this, out observedSequenceNumber) &&
                (SocketPal.TryCompleteReceiveFrom(_socket, buffer.Span, flags, socketAddress.Span, out socketAddressLen, out bytesReceived, out receivedFlags, out errorCode) ||
                !ShouldRetrySyncOperation(out errorCode)))
            {
                flags = receivedFlags;
                return errorCode;
            }

            var operation = new BufferMemoryReceiveOperation(this)
            {
                Buffer = buffer,
                Flags = flags,
                SetReceivedFlags = true,
                SocketAddress = socketAddress,
            };

            PerformSyncOperation(ref _receiveQueue, operation, timeout, observedSequenceNumber);

            flags = operation.ReceivedFlags;
            bytesReceived = operation.BytesTransferred;
            socketAddressLen = operation.SocketAddress.Length;
            return operation.ErrorCode;
        }

        public unsafe SocketError ReceiveFrom(Span<byte> buffer, ref SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, int timeout, out int bytesReceived)
        {
            SocketFlags receivedFlags;
            SocketError errorCode;
            int observedSequenceNumber;
            if (_receiveQueue.IsReady(this, out observedSequenceNumber) &&
                (SocketPal.TryCompleteReceiveFrom(_socket, buffer, flags, socketAddress.Span, out socketAddressLen, out bytesReceived, out receivedFlags, out errorCode) ||
                !ShouldRetrySyncOperation(out errorCode)))
            {
                flags = receivedFlags;
                return errorCode;
            }

            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer))
            {
                var operation = new BufferPtrReceiveOperation(this)
                {
                    BufferPtr = bufferPtr,
                    Length = buffer.Length,
                    Flags = flags,
                    SocketAddress = socketAddress,
                };

                PerformSyncOperation(ref _receiveQueue, operation, timeout, observedSequenceNumber);

                flags = operation.ReceivedFlags;
                bytesReceived = operation.BytesTransferred;
                socketAddressLen = operation.SocketAddress.Length;
                return operation.ErrorCode;
            }
        }

        public SocketError ReceiveAsync(Memory<byte> buffer, SocketFlags flags, out int bytesReceived, Action<int, Memory<byte>, SocketFlags, SocketError> callback, CancellationToken cancellationToken = default)
        {
            SetHandleNonBlocking();

            SocketError errorCode;
            int observedSequenceNumber;
            if (_receiveQueue.IsReady(this, out observedSequenceNumber) &&
                SocketPal.TryCompleteReceive(_socket, buffer.Span, flags, out bytesReceived, out errorCode))
            {
                return errorCode;
            }

            BufferMemoryReceiveOperation operation = RentBufferMemoryReceiveOperation();
            operation.SetReceivedFlags = false;
            operation.Callback = callback;
            operation.Buffer = buffer;
            operation.Flags = flags;
            operation.SocketAddress = default;

            if (!_receiveQueue.StartAsyncOperation(this, operation, observedSequenceNumber, cancellationToken))
            {
                bytesReceived = operation.BytesTransferred;
                errorCode = operation.ErrorCode;

                ReturnOperation(operation);
                return errorCode;
            }

            bytesReceived = 0;
            return SocketError.IOPending;
        }

        public SocketError ReceiveFromAsync(Memory<byte> buffer, SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, out int bytesReceived, out SocketFlags receivedFlags, Action<int, Memory<byte>, SocketFlags, SocketError> callback, CancellationToken cancellationToken = default)
        {
            SetHandleNonBlocking();

            SocketError errorCode;
            int observedSequenceNumber;
            if (_receiveQueue.IsReady(this, out observedSequenceNumber) &&
                SocketPal.TryCompleteReceiveFrom(_socket, buffer.Span, flags, socketAddress.Span, out socketAddressLen, out bytesReceived, out receivedFlags, out errorCode))
            {
                return errorCode;
            }

            BufferMemoryReceiveOperation operation = RentBufferMemoryReceiveOperation();
            operation.SetReceivedFlags = true;
            operation.Callback = callback;
            operation.Buffer = buffer;
            operation.Flags = flags;
            operation.SocketAddress = socketAddress;

            if (!_receiveQueue.StartAsyncOperation(this, operation, observedSequenceNumber, cancellationToken))
            {
                receivedFlags = operation.ReceivedFlags;
                bytesReceived = operation.BytesTransferred;
                errorCode = operation.ErrorCode;
                socketAddressLen = operation.SocketAddress.Length;

                ReturnOperation(operation);
                return errorCode;
            }

            bytesReceived = 0;
            socketAddressLen = 0;
            receivedFlags = SocketFlags.None;
            return SocketError.IOPending;
        }

        public SocketError Receive(IList<ArraySegment<byte>> buffers, SocketFlags flags, int timeout, out int bytesReceived)
        {
            return ReceiveFrom(buffers, ref flags, Memory<byte>.Empty, out int _, timeout, out bytesReceived);
        }

        public SocketError ReceiveAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags, out int bytesReceived, out SocketFlags receivedFlags, Action<int, Memory<byte>, SocketFlags, SocketError> callback)
        {
            return ReceiveFromAsync(buffers, flags, Memory<byte>.Empty, out int _, out bytesReceived, out receivedFlags, callback);
        }

        public unsafe SocketError ReceiveFrom(IList<ArraySegment<byte>> buffers, ref SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, int timeout, out int bytesReceived)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SocketFlags receivedFlags;
            SocketError errorCode;
            int observedSequenceNumber;
            if (_receiveQueue.IsReady(this, out observedSequenceNumber) &&
                (SocketPal.TryCompleteReceiveFrom(_socket, buffers, flags, socketAddress.Span, out socketAddressLen, out bytesReceived, out receivedFlags, out errorCode) ||
                !ShouldRetrySyncOperation(out errorCode)))
            {
                flags = receivedFlags;
                return errorCode;
            }

            var operation = new BufferListReceiveOperation(this)
            {
                Buffers = buffers,
                Flags = flags,
                SocketAddress = socketAddress,
            };

            PerformSyncOperation(ref _receiveQueue, operation, timeout, observedSequenceNumber);

            socketAddressLen = operation.SocketAddress.Length;
            flags = operation.ReceivedFlags;
            bytesReceived = operation.BytesTransferred;
            return operation.ErrorCode;
        }

        public SocketError ReceiveFromAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, out int bytesReceived, out SocketFlags receivedFlags, Action<int, Memory<byte>, SocketFlags, SocketError> callback)
        {
            SetHandleNonBlocking();

            SocketError errorCode;
            int observedSequenceNumber;
            if (_receiveQueue.IsReady(this, out observedSequenceNumber) &&
                SocketPal.TryCompleteReceiveFrom(_socket, buffers, flags, socketAddress.Span, out socketAddressLen, out bytesReceived, out receivedFlags, out errorCode))
            {
                // Synchronous success or failure
                return errorCode;
            }

            BufferListReceiveOperation operation = RentBufferListReceiveOperation();
            operation.Callback = callback;
            operation.Buffers = buffers;
            operation.Flags = flags;
            operation.SocketAddress = socketAddress;

            if (!_receiveQueue.StartAsyncOperation(this, operation, observedSequenceNumber))
            {
                socketAddressLen = operation.SocketAddress.Length;
                receivedFlags = operation.ReceivedFlags;
                bytesReceived = operation.BytesTransferred;
                errorCode = operation.ErrorCode;

                ReturnOperation(operation);
                return errorCode;
            }

            receivedFlags = SocketFlags.None;
            socketAddressLen = 0;
            bytesReceived = 0;
            return SocketError.IOPending;
        }

        public SocketError ReceiveMessageFrom(
            Memory<byte> buffer, ref SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, bool isIPv4, bool isIPv6, int timeout, out IPPacketInformation ipPacketInformation, out int bytesReceived)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SocketFlags receivedFlags;
            SocketError errorCode;
            int observedSequenceNumber;
            if (_receiveQueue.IsReady(this, out observedSequenceNumber) &&
                (SocketPal.TryCompleteReceiveMessageFrom(_socket, buffer.Span, null, flags, socketAddress, out socketAddressLen, isIPv4, isIPv6, out bytesReceived, out receivedFlags, out ipPacketInformation, out errorCode) ||
                !ShouldRetrySyncOperation(out errorCode)))
            {
                flags = receivedFlags;
                return errorCode;
            }

            var operation = new ReceiveMessageFromOperation(this)
            {
                Buffer = buffer,
                Buffers = null,
                Flags = flags,
                SocketAddress = socketAddress,
                IsIPv4 = isIPv4,
                IsIPv6 = isIPv6,
            };

            PerformSyncOperation(ref _receiveQueue, operation, timeout, observedSequenceNumber);

            socketAddressLen = operation.SocketAddress.Length;
            flags = operation.ReceivedFlags;
            ipPacketInformation = operation.IPPacketInformation;
            bytesReceived = operation.BytesTransferred;
            return operation.ErrorCode;
        }

        public unsafe SocketError ReceiveMessageFrom(
            Span<byte> buffer, ref SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, bool isIPv4, bool isIPv6, int timeout, out IPPacketInformation ipPacketInformation, out int bytesReceived)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SocketFlags receivedFlags;
            SocketError errorCode;
            int observedSequenceNumber;
            if (_receiveQueue.IsReady(this, out observedSequenceNumber) &&
                (SocketPal.TryCompleteReceiveMessageFrom(_socket, buffer, null, flags, socketAddress, out socketAddressLen, isIPv4, isIPv6, out bytesReceived, out receivedFlags, out ipPacketInformation, out errorCode) ||
                !ShouldRetrySyncOperation(out errorCode)))
            {
                flags = receivedFlags;
                return errorCode;
            }

            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer))
            {
                var operation = new BufferPtrReceiveMessageFromOperation(this)
                {
                    BufferPtr = bufferPtr,
                    Length = buffer.Length,
                    Flags = flags,
                    SocketAddress = socketAddress,
                    IsIPv4 = isIPv4,
                    IsIPv6 = isIPv6,
                };

                PerformSyncOperation(ref _receiveQueue, operation, timeout, observedSequenceNumber);

                socketAddressLen = operation.SocketAddress.Length;
                flags = operation.ReceivedFlags;
                ipPacketInformation = operation.IPPacketInformation;
                bytesReceived = operation.BytesTransferred;
                return operation.ErrorCode;
            }
        }

        public SocketError ReceiveMessageFromAsync(Memory<byte> buffer, IList<ArraySegment<byte>>? buffers, SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, bool isIPv4, bool isIPv6, out int bytesReceived, out SocketFlags receivedFlags, out IPPacketInformation ipPacketInformation, Action<int, Memory<byte>, SocketFlags, IPPacketInformation, SocketError> callback, CancellationToken cancellationToken = default)
        {
            SetHandleNonBlocking();

            SocketError errorCode;
            int observedSequenceNumber;
            if (_receiveQueue.IsReady(this, out observedSequenceNumber) &&
                SocketPal.TryCompleteReceiveMessageFrom(_socket, buffer.Span, buffers, flags, socketAddress, out socketAddressLen, isIPv4, isIPv6, out bytesReceived, out receivedFlags, out ipPacketInformation, out errorCode))
            {
                return errorCode;
            }

            var operation = new ReceiveMessageFromOperation(this)
            {
                Callback = callback,
                Buffer = buffer,
                Buffers = buffers,
                Flags = flags,
                SocketAddress = socketAddress,
                IsIPv4 = isIPv4,
                IsIPv6 = isIPv6,
            };

            if (!_receiveQueue.StartAsyncOperation(this, operation, observedSequenceNumber, cancellationToken))
            {
                socketAddressLen = operation.SocketAddress.Length;
                receivedFlags = operation.ReceivedFlags;
                ipPacketInformation = operation.IPPacketInformation;
                bytesReceived = operation.BytesTransferred;
                return operation.ErrorCode;
            }

            ipPacketInformation = default(IPPacketInformation);
            bytesReceived = 0;
            socketAddressLen = 0;
            receivedFlags = SocketFlags.None;
            return SocketError.IOPending;
        }

        public SocketError Send(ReadOnlySpan<byte> buffer, SocketFlags flags, int timeout, out int bytesSent) =>
            SendTo(buffer, flags, Memory<byte>.Empty, timeout, out bytesSent);

        public SocketError Send(byte[] buffer, int offset, int count, SocketFlags flags, int timeout, out int bytesSent)
        {
            return SendTo(buffer, offset, count, flags, Memory<byte>.Empty, timeout, out bytesSent);
        }

        public SocketError SendAsync(Memory<byte> buffer, int offset, int count, SocketFlags flags, out int bytesSent, Action<int, Memory<byte>, SocketFlags, SocketError> callback, CancellationToken cancellationToken)
        {
            return SendToAsync(buffer, offset, count, flags, Memory<byte>.Empty, out bytesSent, callback, cancellationToken);
        }

        public SocketError SendTo(byte[] buffer, int offset, int count, SocketFlags flags, Memory<byte> socketAddress, int timeout, out int bytesSent)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            bytesSent = 0;
            SocketError errorCode;
            int observedSequenceNumber;
            if (_sendQueue.IsReady(this, out observedSequenceNumber) &&
                (SocketPal.TryCompleteSendTo(_socket, buffer, ref offset, ref count, flags, socketAddress.Span, ref bytesSent, out errorCode) ||
                !ShouldRetrySyncOperation(out errorCode)))
            {
                return errorCode;
            }

            var operation = new BufferMemorySendOperation(this)
            {
                Buffer = buffer,
                Offset = offset,
                Count = count,
                Flags = flags,
                SocketAddress = socketAddress,
                BytesTransferred = bytesSent
            };

            PerformSyncOperation(ref _sendQueue, operation, timeout, observedSequenceNumber);

            bytesSent = operation.BytesTransferred;
            return operation.ErrorCode;
        }

        public unsafe SocketError SendTo(ReadOnlySpan<byte> buffer, SocketFlags flags, Memory<byte> socketAddress, int timeout, out int bytesSent)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            bytesSent = 0;
            SocketError errorCode;
            int bufferIndexIgnored = 0, offset = 0, count = buffer.Length;
            int observedSequenceNumber;
            if (_sendQueue.IsReady(this, out observedSequenceNumber) &&
                (SocketPal.TryCompleteSendTo(_socket, buffer, null, ref bufferIndexIgnored, ref offset, ref count, flags, socketAddress.Span, ref bytesSent, out errorCode) ||
                !ShouldRetrySyncOperation(out errorCode)))
            {
                return errorCode;
            }

            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer))
            {
                var operation = new BufferPtrSendOperation(this)
                {
                    BufferPtr = bufferPtr,
                    Offset = offset,
                    Count = count,
                    Flags = flags,
                    SocketAddress = socketAddress,
                    BytesTransferred = bytesSent
                };

                PerformSyncOperation(ref _sendQueue, operation, timeout, observedSequenceNumber);

                bytesSent = operation.BytesTransferred;
                return operation.ErrorCode;
            }
        }

        public SocketError SendToAsync(Memory<byte> buffer, int offset, int count, SocketFlags flags, Memory<byte> socketAddress, out int bytesSent, Action<int, Memory<byte>, SocketFlags, SocketError> callback, CancellationToken cancellationToken = default)
        {
            SetHandleNonBlocking();

            bytesSent = 0;
            SocketError errorCode;
            int observedSequenceNumber;
            if (_sendQueue.IsReady(this, out observedSequenceNumber) &&
                SocketPal.TryCompleteSendTo(_socket, buffer.Span, ref offset, ref count, flags, socketAddress.Span, ref bytesSent, out errorCode))
            {
                return errorCode;
            }

            BufferMemorySendOperation operation = RentBufferMemorySendOperation();
            operation.Callback = callback;
            operation.Buffer = buffer;
            operation.Offset = offset;
            operation.Count = count;
            operation.Flags = flags;
            operation.SocketAddress = socketAddress;
            operation.BytesTransferred = bytesSent;

            if (!_sendQueue.StartAsyncOperation(this, operation, observedSequenceNumber, cancellationToken))
            {
                bytesSent = operation.BytesTransferred;
                errorCode = operation.ErrorCode;

                ReturnOperation(operation);
                return errorCode;
            }

            return SocketError.IOPending;
        }

        public SocketError Send(IList<ArraySegment<byte>> buffers, SocketFlags flags, int timeout, out int bytesSent)
        {
            return SendTo(buffers, flags, Memory<byte>.Empty, timeout, out bytesSent);
        }

        public SocketError SendAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags, out int bytesSent, Action<int, Memory<byte>, SocketFlags, SocketError> callback)
        {
            return SendToAsync(buffers, flags, Memory<byte>.Empty, out bytesSent, callback);
        }

        public SocketError SendTo(IList<ArraySegment<byte>> buffers, SocketFlags flags, Memory<byte> socketAddress, int timeout, out int bytesSent)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            bytesSent = 0;
            int bufferIndex = 0;
            int offset = 0;
            SocketError errorCode;
            int observedSequenceNumber;
            if (_sendQueue.IsReady(this, out observedSequenceNumber) &&
                (SocketPal.TryCompleteSendTo(_socket, buffers, ref bufferIndex, ref offset, flags, socketAddress.Span, ref bytesSent, out errorCode) ||
                !ShouldRetrySyncOperation(out errorCode)))
            {
                return errorCode;
            }

            var operation = new BufferListSendOperation(this)
            {
                Buffers = buffers,
                BufferIndex = bufferIndex,
                Offset = offset,
                Flags = flags,
                SocketAddress = socketAddress,
                BytesTransferred = bytesSent
            };

            PerformSyncOperation(ref _sendQueue, operation, timeout, observedSequenceNumber);

            bytesSent = operation.BytesTransferred;
            return operation.ErrorCode;
        }

        public SocketError SendToAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags, Memory<byte> socketAddress, out int bytesSent, Action<int, Memory<byte>, SocketFlags, SocketError> callback)
        {
            SetHandleNonBlocking();

            bytesSent = 0;
            int bufferIndex = 0;
            int offset = 0;
            SocketError errorCode;
            int observedSequenceNumber;
            if (_sendQueue.IsReady(this, out observedSequenceNumber) &&
                SocketPal.TryCompleteSendTo(_socket, buffers, ref bufferIndex, ref offset, flags, socketAddress.Span, ref bytesSent, out errorCode))
            {
                return errorCode;
            }

            BufferListSendOperation operation = RentBufferListSendOperation();
            operation.Callback = callback;
            operation.Buffers = buffers;
            operation.BufferIndex = bufferIndex;
            operation.Offset = offset;
            operation.Flags = flags;
            operation.SocketAddress = socketAddress;
            operation.BytesTransferred = bytesSent;

            if (!_sendQueue.StartAsyncOperation(this, operation, observedSequenceNumber))
            {
                bytesSent = operation.BytesTransferred;
                errorCode = operation.ErrorCode;

                ReturnOperation(operation);
                return errorCode;
            }

            return SocketError.IOPending;
        }

        public SocketError SendFile(SafeFileHandle fileHandle, long offset, long count, int timeout, out long bytesSent)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            bytesSent = 0;
            SocketError errorCode;
            int observedSequenceNumber;
            if (_sendQueue.IsReady(this, out observedSequenceNumber) &&
                (SocketPal.TryCompleteSendFile(_socket, fileHandle, ref offset, ref count, ref bytesSent, out errorCode) ||
                !ShouldRetrySyncOperation(out errorCode)))
            {
                return errorCode;
            }

            var operation = new SendFileOperation(this)
            {
                FileHandle = fileHandle,
                Offset = offset,
                Count = count,
                BytesTransferred = bytesSent
            };

            PerformSyncOperation(ref _sendQueue, operation, timeout, observedSequenceNumber);

            bytesSent = operation.BytesTransferred;
            return operation.ErrorCode;
        }

        public SocketError SendFileAsync(SafeFileHandle fileHandle, long offset, long count, out long bytesSent, Action<long, SocketError> callback, CancellationToken cancellationToken = default)
        {
            SetHandleNonBlocking();

            bytesSent = 0;
            SocketError errorCode;
            int observedSequenceNumber;
            if (_sendQueue.IsReady(this, out observedSequenceNumber) &&
                SocketPal.TryCompleteSendFile(_socket, fileHandle, ref offset, ref count, ref bytesSent, out errorCode))
            {
                return errorCode;
            }

            var operation = new SendFileOperation(this)
            {
                Callback = callback,
                FileHandle = fileHandle,
                Offset = offset,
                Count = count,
                BytesTransferred = bytesSent
            };

            if (!_sendQueue.StartAsyncOperation(this, operation, observedSequenceNumber, cancellationToken))
            {
                bytesSent = operation.BytesTransferred;
                return operation.ErrorCode;
            }

            return SocketError.IOPending;
        }

        // Called on the epoll thread, speculatively tries to process synchronous events and errors for synchronous events, and
        // returns any remaining events that remain to be processed. Taking a lock for each operation queue to deterministically
        // handle synchronous events on the epoll thread seems to significantly reduce throughput in benchmarks. On the other
        // hand, the speculative checks make it nondeterministic, where it would be possible for the epoll thread to think that
        // the next operation in a queue is not synchronous when it is (due to a race, old caches, etc.) and cause the event to
        // be scheduled instead. It's not functionally incorrect to schedule the release of a synchronous operation, just it may
        // lead to thread pool starvation issues if the synchronous operations are blocking thread pool threads (typically not
        // advised) and more threads are not immediately available to run work items that would release those operations.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Interop.Sys.SocketEvents HandleSyncEventsSpeculatively(Interop.Sys.SocketEvents events)
        {
            if ((events & Interop.Sys.SocketEvents.Error) != 0)
            {
                // Set the Read and Write flags; the processing for these events
                // will pick up the error.
                events ^= Interop.Sys.SocketEvents.Error;
                events |= Interop.Sys.SocketEvents.Read | Interop.Sys.SocketEvents.Write;
            }

            if ((events & Interop.Sys.SocketEvents.Read) != 0 &&
                _receiveQueue.IsNextOperationSynchronous_Speculative &&
                _receiveQueue.ProcessSyncEventOrGetAsyncEvent(this, skipAsyncEvents: true) == null)
            {
                events ^= Interop.Sys.SocketEvents.Read;
            }

            if ((events & Interop.Sys.SocketEvents.Write) != 0 &&
                _sendQueue.IsNextOperationSynchronous_Speculative &&
                _sendQueue.ProcessSyncEventOrGetAsyncEvent(this, skipAsyncEvents: true) == null)
            {
                events ^= Interop.Sys.SocketEvents.Write;
            }

            return events;
        }

        // Called on the epoll thread.
        public void HandleEventsInline(Interop.Sys.SocketEvents events)
        {
            if ((events & Interop.Sys.SocketEvents.Error) != 0)
            {
                // Set the Read and Write flags; the processing for these events
                // will pick up the error.
                events ^= Interop.Sys.SocketEvents.Error;
                events |= Interop.Sys.SocketEvents.Read | Interop.Sys.SocketEvents.Write;
            }

            if ((events & Interop.Sys.SocketEvents.Read) != 0)
            {
                AsyncOperation? receiveOperation = _receiveQueue.ProcessSyncEventOrGetAsyncEvent(this);
                receiveOperation?.Process();
            }

            if ((events & Interop.Sys.SocketEvents.Write) != 0)
            {
                AsyncOperation? sendOperation = _sendQueue.ProcessSyncEventOrGetAsyncEvent(this);
                sendOperation?.Process();
            }
        }

        // Called on ThreadPool thread.
        public unsafe void HandleEvents(Interop.Sys.SocketEvents events)
        {
            Debug.Assert((events & Interop.Sys.SocketEvents.Error) == 0);

            AsyncOperation? receiveOperation =
                (events & Interop.Sys.SocketEvents.Read) != 0 ? _receiveQueue.ProcessSyncEventOrGetAsyncEvent(this) : null;
            AsyncOperation? sendOperation =
                (events & Interop.Sys.SocketEvents.Write) != 0 ? _sendQueue.ProcessSyncEventOrGetAsyncEvent(this) : null;

            // This method is called from a thread pool thread. When we have only one operation to process, process it
            // synchronously to avoid an extra thread pool work item. When we have two operations to process, processing both
            // synchronously may delay the second operation, so schedule one onto the thread pool and process the other
            // synchronously. There might be better ways of doing this.
            if (sendOperation == null)
            {
                receiveOperation?.Process();
            }
            else
            {
                receiveOperation?.Schedule();
                sendOperation.Process();
            }
        }

        //
        // Tracing stuff
        //

        // To enabled tracing:
        // (1) Add reference to System.Console in the csproj
        // (2) #define SOCKETASYNCCONTEXT_TRACE

        [Conditional("SOCKETASYNCCONTEXT_TRACE")]
        public void Trace(string message, [CallerMemberName] string? memberName = null)
        {
            OutputTrace($"{IdOf(this)}.{memberName}: {message}");
        }

        [Conditional("SOCKETASYNCCONTEXT_TRACE")]
        public static void OutputTrace(string s)
        {
            // CONSIDER: Change to NetEventSource
#if SOCKETASYNCCONTEXT_TRACE
            Console.WriteLine(s);
#endif
        }

        public static string IdOf(object o) => o == null ? "(null)" : $"{o.GetType().Name}#{o.GetHashCode():X2}";
    }
}
