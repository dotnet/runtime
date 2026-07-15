// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using AsyncResult = System.Threading.UnixHandleAsyncContext.AsyncResult;
using OnCompletedResult = System.Threading.UnixHandleAsyncContext.OnCompletedResult;
using SyncResult = System.Threading.UnixHandleAsyncContext.SyncResult;

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

    internal sealed partial class SocketAsyncContext
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

        internal abstract class AsyncOperation : UnixHandleAsyncContext.Operation
        {
#if DEBUG
            private bool _callbackQueued; // When true, the callback has been queued.
#endif

            public readonly SocketAsyncContext AssociatedContext;
            public SocketError ErrorCode;
            public Memory<byte> SocketAddress;

            public AsyncOperation(SocketAsyncContext context)
            {
                AssociatedContext = context;
                Reset();
            }

#pragma warning disable CA1822
            public void Reset()
            {
#if DEBUG
                _callbackQueued = false;
#endif
            }
#pragma warning restore CA1822

            protected sealed override bool TryCompleteOperation(SafeHandle handle)
                => TryCompleteOperation(AssociatedContext);

            protected override void OnCompleted(OnCompletedResult result)
            {
#if DEBUG
                Debug.Assert(!Interlocked.Exchange(ref _callbackQueued, true), $"Unexpected _callbackQueued: {_callbackQueued}");
#endif
                if (result != OnCompletedResult.Completed)
                {
                    ErrorCode = SocketError.OperationAborted;
                }

                InvokeCallback(allowPooling: result == OnCompletedResult.Completed);
            }

            protected abstract bool TryCompleteOperation(SocketAsyncContext context);

            public abstract void InvokeCallback(bool allowPooling);

        }

        private abstract class SendOperation : AsyncOperation
        {
            public SocketFlags Flags;
            public int BytesTransferred;
            public int Offset;
            public int Count;

            public SendOperation(SocketAsyncContext context) : base(context) { }

            public Action<int, Memory<byte>, SocketFlags, SocketError>? Callback { get; set; }

            public override void InvokeCallback(bool allowPooling) =>
                Callback!(BytesTransferred, SocketAddress, SocketFlags.None, ErrorCode);
        }

        private class BufferMemorySendOperation : SendOperation
        {
            public Memory<byte> Buffer;

            public BufferMemorySendOperation(SocketAsyncContext context) : base(context) { }

            protected override bool TryCompleteOperation(SocketAsyncContext context)
            {
                int bufferIndex = 0;
                return SocketPal.TryCompleteSendTo(context._socket, Buffer.Span, null, ref bufferIndex, ref Offset, ref Count, Flags, SocketAddress.Span, ref BytesTransferred, out ErrorCode);
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

        private sealed class BufferListSendOperation : SendOperation
        {
            public IList<ArraySegment<byte>>? Buffers;
            public int BufferIndex;

            public BufferListSendOperation(SocketAsyncContext context) : base(context) { }

            protected override bool TryCompleteOperation(SocketAsyncContext context)
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

            protected override bool TryCompleteOperation(SocketAsyncContext context)
            {
                int bufferIndex = 0;
                int bufferLength = Offset + Count; // TryCompleteSendTo expects the entire buffer, which it then indexes into with the ref Offset and ref Count arguments
                return SocketPal.TryCompleteSendTo(context._socket, new ReadOnlySpan<byte>(BufferPtr, bufferLength), null, ref bufferIndex, ref Offset, ref Count, Flags, SocketAddress.Span, ref BytesTransferred, out ErrorCode);
            }
        }

        private abstract class ReceiveOperation : AsyncOperation
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

            protected override bool TryCompleteOperation(SocketAsyncContext context)
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

            protected override bool TryCompleteOperation(SocketAsyncContext context)
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

            protected override bool TryCompleteOperation(SocketAsyncContext context)
            {
                bool completed = SocketPal.TryCompleteReceiveFrom(context._socket, new Span<byte>(BufferPtr, Length), null, Flags, SocketAddress.Span, out int socketAddressLen, out BytesTransferred, out ReceivedFlags, out ErrorCode);
                if (completed && ErrorCode == SocketError.Success)
                {
                    SocketAddress = SocketAddress.Slice(0, socketAddressLen);
                }
                return completed;
            }
        }

        private sealed class ReceiveMessageFromOperation : AsyncOperation
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

            protected override bool TryCompleteOperation(SocketAsyncContext context)
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

        private sealed unsafe class BufferPtrReceiveMessageFromOperation : AsyncOperation
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

            protected override bool TryCompleteOperation(SocketAsyncContext context)
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

        private sealed class AcceptOperation : AsyncOperation
        {
            public IntPtr AcceptedFileDescriptor;

            public AcceptOperation(SocketAsyncContext context) : base(context) { }

            public Action<IntPtr, Memory<byte>, SocketError>? Callback { get; set; }

            protected override bool TryCompleteOperation(SocketAsyncContext context)
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

        private sealed class ConnectOperation : BufferMemorySendOperation
        {
            public ConnectOperation(SocketAsyncContext context) : base(context) { }

            protected override bool TryCompleteOperation(SocketAsyncContext context)
            {
                bool result = SocketPal.TryCompleteConnect(context._socket, out ErrorCode);
                context._socket.RegisterConnectResult(ErrorCode);

                if (result && ErrorCode == SocketError.Success &&  Buffer.Length > 0)
                {
                    SocketError error = context.SendToAsync(Buffer, 0, Buffer.Length, SocketFlags.None, Memory<byte>.Empty, ref BytesTransferred, Callback!, default);
                    if (error != SocketError.Success && error != SocketError.IOPending)
                    {
                        context._socket.RegisterConnectResult(ErrorCode);
                    }
                }
                return result;
            }

            public override void InvokeCallback(bool allowPooling)
            {
                var cb = Callback!;
                int bt = BytesTransferred;
                Memory<byte> sa = SocketAddress;
                SocketError ec = ErrorCode;
                Memory<byte> buffer = Buffer;

                if (buffer.Length == 0 || ec != SocketError.Success)
                {
                    AssociatedContext._socket.SetBlocking();

                    // Invoke callback only when we are completely done.
                    // In case data were provided for Connect we may or may not send them all.
                    // If we did not we will need follow-up with Send operation
                    cb(bt, sa, SocketFlags.None, ec);
                }
            }
        }

        private sealed class SendFileOperation : AsyncOperation
        {
            public SafeFileHandle FileHandle = null!; // always set when constructed
            public long Offset;
            public long Count;
            public long BytesTransferred;

            public SendFileOperation(SocketAsyncContext context) : base(context) { }

            public Action<long, SocketError>? Callback { get; set; }

            public override void InvokeCallback(bool allowPooling) =>
                Callback!(BytesTransferred, ErrorCode);

            protected override bool TryCompleteOperation(SocketAsyncContext context) =>
                SocketPal.TryCompleteSendFile(context._socket, FileHandle, ref Offset, ref Count, ref BytesTransferred, out ErrorCode);
        }

        internal static readonly bool InlineSocketCompletionsEnabled = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS") == "1";

        internal readonly SafeSocketHandle _socket;
        private UnixHandleAsyncContext? _asyncContext;
        private bool _isHandleNonBlocking = OperatingSystem.IsWasi(); // WASI sockets are always non-blocking, because we don't have another thread which could be blocked

        // Socket.PreferInlineCompletions is an experimental API with internal access modifier.
        // DynamicDependency ensures the setter is available externally using reflection.
        [DynamicDependency("set_PreferInlineCompletions", typeof(Socket))]
        internal void SetInlineCompletions(bool value)
        {
            if (SocketAsyncContext.InlineSocketCompletionsEnabled)
            {
                // Ignore value. All completions are inline.
                return;
            }

            if (_asyncContext is not null || value)
            {
                AsyncContext.InlineCompletions = value;
            }
        }

        internal UnixHandleAsyncContext AsyncContext
        {
            get
            {
                if (_asyncContext == null)
                {
                    var asyncContext = CreateAsyncContext(_socket);
                    asyncContext.InlineCompletions = InlineSocketCompletionsEnabled;
                    Interlocked.CompareExchange(ref _asyncContext, asyncContext, null);
                }
                return _asyncContext!;
            }
        }

        public SocketAsyncContext(SafeSocketHandle socket)
        {
            _socket = socket;
        }

        public bool StopAndAbort()
        {
            return _asyncContext?.AbortAndDispose() ?? false;
        }

        public void SetHandleNonBlocking()
        {
            if (OperatingSystem.IsWasi())
            {
                // WASI sockets are always non-blocking, because in ST we don't have another thread which could be blocked
                return;
            }
            //
            // Our sockets may start as blocking, and later transition to non-blocking, either because the user
            // explicitly requested non-blocking mode, or because we need non-blocking mode to support async
            // operations. After ConnectAsync completes (success or failure), if there is no pending follow-up
            // async send, we may transition back to blocking mode to optimize subsequent synchronous operations
            // (see SetHandleBlocking). The socket will be set back to non-blocking when another async operation
            // is performed.
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

        public void SetHandleBlocking()
        {
            if (OperatingSystem.IsWasi())
            {
                // WASI sockets are always non-blocking
                return;
            }

            if (_isHandleNonBlocking)
            {
                if (Interop.Sys.Fcntl.SetIsNonBlocking(_socket, 0) == 0)
                {
                    _isHandleNonBlocking = false;
                }
            }
        }

        private void PerformSyncOperation(AsyncOperation operation, bool isRead, int timeout, int observedSequenceNumber)
        {
            if (!Socket.OSSupportsThreads) throw new PlatformNotSupportedException();
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SyncResult result =
                isRead ? AsyncContext.ReadSync(operation, observedSequenceNumber, timeout)
                       : AsyncContext.WriteSync(operation, observedSequenceNumber, timeout);

            if (result == SyncResult.TimedOut)
            {
                operation.ErrorCode = SocketError.TimedOut;
            }
            else if (result == SyncResult.Aborted)
            {
                operation.ErrorCode = SocketError.OperationAborted;
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

        public SocketError Accept(Memory<byte> socketAddress, out int socketAddressLen, out IntPtr acceptedFd)
        {
            Debug.Assert(socketAddress.Length > 0, $"Unexpected socketAddressLen: {socketAddress.Length}");

            SocketError errorCode;
            int observedSequenceNumber;
            if (AsyncContext.IsReadReady(out observedSequenceNumber) &&
                SocketPal.TryCompleteAccept(_socket, socketAddress, out socketAddressLen, out acceptedFd, out errorCode))
            {
                Debug.Assert(errorCode == SocketError.Success || acceptedFd == (IntPtr)(-1), $"Unexpected values: errorCode={errorCode}, acceptedFd={acceptedFd}");
                return errorCode;
            }

            var operation = new AcceptOperation(this)
            {
                SocketAddress = socketAddress,
            };

            PerformSyncOperation(operation, isRead: true, -1, observedSequenceNumber);

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
            if (AsyncContext.IsReadReady(out observedSequenceNumber) &&
                SocketPal.TryCompleteAccept(_socket, socketAddress, out socketAddressLen, out acceptedFd, out errorCode))
            {
                Debug.Assert(errorCode == SocketError.Success || acceptedFd == (IntPtr)(-1), $"Unexpected values: errorCode={errorCode}, acceptedFd={acceptedFd}");

                return errorCode;
            }

            AcceptOperation operation = RentAcceptOperation();
            operation.Callback = callback;
            operation.SocketAddress = socketAddress;

            AsyncResult result = AsyncContext.ReadAsync(operation, observedSequenceNumber, cancellationToken);

            if (result == AsyncResult.Completed)
            {
                errorCode = operation.ErrorCode;
                socketAddressLen = operation.SocketAddress.Length;
                acceptedFd = operation.AcceptedFileDescriptor;

                ReturnOperation(operation);
                return errorCode;
            }

            acceptedFd = (IntPtr)(-1);
            socketAddressLen = 0;
            return GetSocketErrorForNonCompleted(result);
        }

        public SocketError Connect(Memory<byte> socketAddress)
        {
            if (!Socket.OSSupportsThreads) throw new PlatformNotSupportedException();

            Debug.Assert(socketAddress.Length > 0, $"Unexpected socketAddressLen: {socketAddress.Length}");
            // Connect is different than the usual "readiness" pattern of other operations.
            // We need to call TryStartConnect to initiate the connect with the OS,
            // before we try to complete it via epoll notification.
            // Thus, always call TryStartConnect regardless of readiness.
            SocketError errorCode;
            int observedSequenceNumber;
            AsyncContext.IsWriteReady(out observedSequenceNumber);
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

            PerformSyncOperation(operation, isRead: false, -1, observedSequenceNumber);

            return operation.ErrorCode;
        }

        public SocketError ConnectAsync(Memory<byte> socketAddress, Action<int, Memory<byte>, SocketFlags, SocketError> callback, Memory<byte> buffer, out int sentBytes, CancellationToken cancellationToken)
        {
            Debug.Assert(socketAddress.Length > 0, $"Unexpected socketAddressLen: {socketAddress.Length}");
            Debug.Assert(callback != null, "Expected non-null callback");

            SetHandleNonBlocking();

            // Connect is different than the usual "readiness" pattern of other operations.
            // We need to initiate the connect before we try to complete it.
            // Thus, always call TryStartConnect regardless of readiness.
            SocketError errorCode;
            int observedSequenceNumber;
            AsyncContext.IsWriteReady(out observedSequenceNumber);
#if SYSTEM_NET_SOCKETS_APPLE_PLATFORM
            if (SocketPal.TryStartConnect(_socket, socketAddress, out errorCode, buffer.Span, _socket.TfoEnabled, out sentBytes))
#else
            if (SocketPal.TryStartConnect(_socket, socketAddress, out errorCode, buffer.Span, false, out sentBytes)) // In Linux, we can figure it out as needed inside PAL.
#endif
            {
                _socket.RegisterConnectResult(errorCode);

                int remains = buffer.Length - sentBytes;

                if (errorCode == SocketError.Success && remains > 0)
                {
                    errorCode = SendToAsync(buffer.Slice(sentBytes), 0, remains, SocketFlags.None, Memory<byte>.Empty, ref sentBytes, callback!, default);
                }

                if (remains == 0 || errorCode != SocketError.IOPending)
                {
                    _socket.SetBlocking();
                }
                return errorCode;
            }

            var operation = new ConnectOperation(this)
            {
                Callback = callback,
                SocketAddress = socketAddress,
                Buffer = buffer.Slice(sentBytes),
                BytesTransferred = sentBytes,
            };

            AsyncResult result = AsyncContext.WriteAsync(operation, observedSequenceNumber, cancellationToken);

            if (result == AsyncResult.Completed)
            {
                errorCode = operation.ErrorCode;
                if (errorCode == SocketError.Success)
                {
                    sentBytes += operation.BytesTransferred;
                }

                if (buffer.Length == 0 || errorCode != SocketError.Success)
                {
                    _socket.SetBlocking();
                }

                return errorCode;
            }

            return GetSocketErrorForNonCompleted(result);
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

        public SocketError ReceiveFrom(Memory<byte> buffer, ref SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, int timeout, out int bytesReceived)
        {
            if (!Socket.OSSupportsThreads) throw new PlatformNotSupportedException();

            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SocketFlags receivedFlags;
            SocketError errorCode;
            int observedSequenceNumber;
            if (AsyncContext.IsReadReady(out observedSequenceNumber) &&
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

            PerformSyncOperation(operation, isRead: true, timeout, observedSequenceNumber);

            flags = operation.ReceivedFlags;
            bytesReceived = operation.BytesTransferred;
            socketAddressLen = operation.SocketAddress.Length;
            return operation.ErrorCode;
        }

        public unsafe SocketError ReceiveFrom(Span<byte> buffer, ref SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, int timeout, out int bytesReceived)
        {
            if (!Socket.OSSupportsThreads) throw new PlatformNotSupportedException();

            SocketFlags receivedFlags;
            SocketError errorCode;
            int observedSequenceNumber;
            if (AsyncContext.IsReadReady(out observedSequenceNumber) &&
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

                PerformSyncOperation(operation, isRead: true, timeout, observedSequenceNumber);

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
            if (AsyncContext.IsReadReady(out observedSequenceNumber) &&
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

            AsyncResult result = AsyncContext.ReadAsync(operation, observedSequenceNumber, cancellationToken);

            if (result == AsyncResult.Completed)
            {
                errorCode = operation.ErrorCode;
                bytesReceived = operation.BytesTransferred;

                ReturnOperation(operation);
                return errorCode;
            }

            bytesReceived = 0;
            return GetSocketErrorForNonCompleted(result);
        }

        public SocketError ReceiveFromAsync(Memory<byte> buffer, SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, out int bytesReceived, out SocketFlags receivedFlags, Action<int, Memory<byte>, SocketFlags, SocketError> callback, CancellationToken cancellationToken = default)
        {
            SetHandleNonBlocking();

            SocketError errorCode;
            int observedSequenceNumber;
            if (AsyncContext.IsReadReady(out observedSequenceNumber) &&
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

            AsyncResult result = AsyncContext.ReadAsync(operation, observedSequenceNumber, cancellationToken);

            if (result == AsyncResult.Completed)
            {
                errorCode = operation.ErrorCode;
                receivedFlags = operation.ReceivedFlags;
                bytesReceived = operation.BytesTransferred;
                socketAddressLen = operation.SocketAddress.Length;

                ReturnOperation(operation);
                return errorCode;
            }

            bytesReceived = 0;
            socketAddressLen = 0;
            receivedFlags = SocketFlags.None;
            return GetSocketErrorForNonCompleted(result);
        }

        public SocketError Receive(IList<ArraySegment<byte>> buffers, SocketFlags flags, int timeout, out int bytesReceived)
        {
            return ReceiveFrom(buffers, ref flags, Memory<byte>.Empty, out int _, timeout, out bytesReceived);
        }

        public SocketError ReceiveAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags, out int bytesReceived, out SocketFlags receivedFlags, Action<int, Memory<byte>, SocketFlags, SocketError> callback)
        {
            return ReceiveFromAsync(buffers, flags, Memory<byte>.Empty, out int _, out bytesReceived, out receivedFlags, callback);
        }

        public SocketError ReceiveFrom(IList<ArraySegment<byte>> buffers, ref SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, int timeout, out int bytesReceived)
        {
            if (!Socket.OSSupportsThreads) throw new PlatformNotSupportedException();

            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SocketFlags receivedFlags;
            SocketError errorCode;
            int observedSequenceNumber;
            if (AsyncContext.IsReadReady(out observedSequenceNumber) &&
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

            PerformSyncOperation(operation, isRead: true, timeout, observedSequenceNumber);

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
            if (AsyncContext.IsReadReady(out observedSequenceNumber) &&
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

            AsyncResult result = AsyncContext.ReadAsync(operation, observedSequenceNumber, default);

            if (result == AsyncResult.Completed)
            {
                errorCode = operation.ErrorCode;
                socketAddressLen = operation.SocketAddress.Length;
                receivedFlags = operation.ReceivedFlags;
                bytesReceived = operation.BytesTransferred;

                ReturnOperation(operation);
                return errorCode;
            }

            receivedFlags = SocketFlags.None;
            socketAddressLen = 0;
            bytesReceived = 0;
            return GetSocketErrorForNonCompleted(result);
        }

        public SocketError ReceiveMessageFrom(
            Memory<byte> buffer, ref SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, bool isIPv4, bool isIPv6, int timeout, out IPPacketInformation ipPacketInformation, out int bytesReceived)
        {
            if (!Socket.OSSupportsThreads) throw new PlatformNotSupportedException();

            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SocketFlags receivedFlags;
            SocketError errorCode;
            int observedSequenceNumber;
            if (AsyncContext.IsReadReady(out observedSequenceNumber) &&
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

            PerformSyncOperation(operation, isRead: true, timeout, observedSequenceNumber);

            socketAddressLen = operation.SocketAddress.Length;
            flags = operation.ReceivedFlags;
            ipPacketInformation = operation.IPPacketInformation;
            bytesReceived = operation.BytesTransferred;
            return operation.ErrorCode;
        }

        public unsafe SocketError ReceiveMessageFrom(
            Span<byte> buffer, ref SocketFlags flags, Memory<byte> socketAddress, out int socketAddressLen, bool isIPv4, bool isIPv6, int timeout, out IPPacketInformation ipPacketInformation, out int bytesReceived)
        {
            if (!Socket.OSSupportsThreads) throw new PlatformNotSupportedException();

            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            SocketFlags receivedFlags;
            SocketError errorCode;
            int observedSequenceNumber;
            if (AsyncContext.IsReadReady(out observedSequenceNumber) &&
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

                PerformSyncOperation(operation, isRead: true, timeout, observedSequenceNumber);

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
            if (AsyncContext.IsReadReady(out observedSequenceNumber) &&
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

            AsyncResult result = AsyncContext.ReadAsync(operation, observedSequenceNumber, cancellationToken);

            if (result == AsyncResult.Completed)
            {
                errorCode = operation.ErrorCode;
                socketAddressLen = operation.SocketAddress.Length;
                receivedFlags = operation.ReceivedFlags;
                ipPacketInformation = operation.IPPacketInformation;
                bytesReceived = operation.BytesTransferred;
                return errorCode;
            }

            ipPacketInformation = default(IPPacketInformation);
            bytesReceived = 0;
            socketAddressLen = 0;
            receivedFlags = SocketFlags.None;
            return GetSocketErrorForNonCompleted(result);
        }

        public SocketError Send(ReadOnlySpan<byte> buffer, SocketFlags flags, int timeout, out int bytesSent) =>
            SendTo(buffer, flags, Memory<byte>.Empty, timeout, out bytesSent);

        public SocketError Send(byte[] buffer, int offset, int count, SocketFlags flags, int timeout, out int bytesSent)
        {
            return SendTo(buffer, offset, count, flags, Memory<byte>.Empty, timeout, out bytesSent);
        }

        public SocketError SendAsync(Memory<byte> buffer, int offset, int count, SocketFlags flags, out int bytesSent, Action<int, Memory<byte>, SocketFlags, SocketError> callback, CancellationToken cancellationToken)
        {
            bytesSent = 0;
            return SendToAsync(buffer, offset, count, flags, Memory<byte>.Empty, ref bytesSent, callback, cancellationToken);
        }

        public SocketError SendTo(byte[] buffer, int offset, int count, SocketFlags flags, Memory<byte> socketAddress, int timeout, out int bytesSent)
        {
            if (!Socket.OSSupportsThreads) throw new PlatformNotSupportedException();

            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            bytesSent = 0;
            SocketError errorCode;
            int observedSequenceNumber;
            if (AsyncContext.IsWriteReady(out observedSequenceNumber) &&
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

            PerformSyncOperation(operation, isRead: false, timeout, observedSequenceNumber);

            bytesSent = operation.BytesTransferred;
            return operation.ErrorCode;
        }

        public unsafe SocketError SendTo(ReadOnlySpan<byte> buffer, SocketFlags flags, Memory<byte> socketAddress, int timeout, out int bytesSent)
        {
            if (!Socket.OSSupportsThreads) throw new PlatformNotSupportedException();

            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            bytesSent = 0;
            SocketError errorCode;
            int bufferIndexIgnored = 0, offset = 0, count = buffer.Length;
            int observedSequenceNumber;
            if (AsyncContext.IsWriteReady(out observedSequenceNumber) &&
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

                PerformSyncOperation(operation, isRead: false, timeout, observedSequenceNumber);

                bytesSent = operation.BytesTransferred;
                return operation.ErrorCode;
            }
        }

        public SocketError SendToAsync(Memory<byte> buffer, int offset, int count, SocketFlags flags, Memory<byte> socketAddress, ref int bytesSent, Action<int, Memory<byte>, SocketFlags, SocketError> callback, CancellationToken cancellationToken = default)
        {
            SetHandleNonBlocking();

            SocketError errorCode;
            int observedSequenceNumber;
            if (AsyncContext.IsWriteReady(out observedSequenceNumber) &&
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

            AsyncResult result = AsyncContext.WriteAsync(operation, observedSequenceNumber, cancellationToken);

            if (result == AsyncResult.Completed)
            {
                errorCode = operation.ErrorCode;
                bytesSent = operation.BytesTransferred;

                ReturnOperation(operation);
                return errorCode;
            }

            return GetSocketErrorForNonCompleted(result);
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
            if (!Socket.OSSupportsThreads) throw new PlatformNotSupportedException();

            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            bytesSent = 0;
            int bufferIndex = 0;
            int offset = 0;
            SocketError errorCode;
            int observedSequenceNumber;
            if (AsyncContext.IsWriteReady(out observedSequenceNumber) &&
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

            PerformSyncOperation(operation, isRead: false, timeout, observedSequenceNumber);

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
            if (AsyncContext.IsWriteReady(out observedSequenceNumber) &&
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

            AsyncResult result = AsyncContext.WriteAsync(operation, observedSequenceNumber, default);

            if (result == AsyncResult.Completed)
            {
                errorCode = operation.ErrorCode;
                bytesSent = operation.BytesTransferred;

                ReturnOperation(operation);
                return errorCode;
            }

            return GetSocketErrorForNonCompleted(result);
        }

        public SocketError SendFile(SafeFileHandle fileHandle, long offset, long count, int timeout, out long bytesSent)
        {
            if (!Socket.OSSupportsThreads) throw new PlatformNotSupportedException();

            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            bytesSent = 0;
            SocketError errorCode;
            int observedSequenceNumber;
            if (AsyncContext.IsWriteReady(out observedSequenceNumber) &&
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

            PerformSyncOperation(operation, isRead: false, timeout, observedSequenceNumber);

            bytesSent = operation.BytesTransferred;
            return operation.ErrorCode;
        }

        public SocketError SendFileAsync(SafeFileHandle fileHandle, long offset, long count, out long bytesSent, Action<long, SocketError> callback, CancellationToken cancellationToken = default)
        {
            SetHandleNonBlocking();

            bytesSent = 0;
            SocketError errorCode;
            int observedSequenceNumber;
            if (AsyncContext.IsWriteReady(out observedSequenceNumber) &&
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

            AsyncResult result = AsyncContext.WriteAsync(operation, observedSequenceNumber, cancellationToken);

            if (result == AsyncResult.Completed)
            {
                errorCode = operation.ErrorCode;
                bytesSent = operation.BytesTransferred;
                return errorCode;
            }

            return GetSocketErrorForNonCompleted(result);
        }

        private static SocketError GetSocketErrorForNonCompleted(AsyncResult result)
        {
            Debug.Assert(result is AsyncResult.Pending or AsyncResult.Aborted);
            return result == AsyncResult.Pending ? SocketError.IOPending : SocketError.OperationAborted;
        }

        //
        // Tracing stuff
        //

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        private static extern UnixHandleAsyncContext CreateAsyncContext(SafeHandle handle);
    }
}
