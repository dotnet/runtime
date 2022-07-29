// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Sockets
{
    public partial class SocketAsyncEventArgs : EventArgs, IDisposable
    {
        /// <summary>
        /// Value used to indicate whether the thread starting an async operation or invoking the callback owns completion and cleanup,
        /// and potentially a packed result when ownership is transferred from the overlapped callback to the initial thread.
        /// </summary>
        /// <remarks>
        /// An async operation may complete asynchronously so quickly that the overlapped callback may be invoked even while the thread
        /// launching the operation is still in the process of launching it, including setting up state that's only configured after
        /// the Winsock call has been made, e.g. registering with a cancellation token.  In order to ensure that cleanup and announcement
        /// of completion happen only once all work related to launching the operation has quiesced, the launcher and the callback
        /// coordinate via this flag.  It's initially set to 0.  When either the launcher completes its work or the callback is invoked,
        /// they each try to transition the flag to non-0, and if successful, the other entity owns completion and cleanup; if unsuccessful,
        /// they themselves own cleanup and completion.  For cases where the operation frequently completes asynchronously but quickly,
        /// e.g. accepts with an already pending connection, this also helps to turn what would otherwise be treated as asynchronous completion
        /// into a synchronous completion, which can help with performance for the caller, e.g. an async method awaiting the operation simply
        /// continues its execution synchronously rather than needing to hook up a continuation and go through the async completion path.
        /// If the overlapped callback succeeds in transferring ownership, the value is a combination of the error code (bottom 32-bits) and
        /// the number of bytes transferred (bits 33-63); the top bit is also set just in case both the error code and number of bytes
        /// transferred are 0.
        /// </remarks>
        private ulong _asyncCompletionOwnership;

        /// <summary>Pinned handle for a single buffer.</summary>
        /// <remarks>
        /// This should only be set in <see cref="ProcessIOCPResult"/> when <see cref="_asyncCompletionOwnership"/> is also being
        /// set to non-0, and then cleaned up in <see cref="CompleteCore"/>.  If it's set and <see cref="_asyncCompletionOwnership"/>
        /// remains 0, it may not get cleaned up correctly.
        /// </remarks>
        private MemoryHandle _singleBufferHandle;

        // BufferList property variables.
        // Note that these arrays are allocated and then grown as necessary, but never shrunk.
        // Thus the actual in-use length is defined by _bufferListInternal.Count, not the length of these arrays.
        private WSABuffer[]? _wsaBufferArrayPinned;
        private MemoryHandle[]? _multipleBufferMemoryHandles;

        // Internal buffers for WSARecvMsg
        private byte[]? _wsaMessageBufferPinned;
        private byte[]? _controlBufferPinned;
        private WSABuffer[]? _wsaRecvMsgWSABufferArrayPinned;

        // Internal SocketAddress buffer
        private GCHandle _socketAddressGCHandle;
        private Internals.SocketAddress? _pinnedSocketAddress;

        // SendPacketsElements property variables.
        private SafeFileHandle[]? _sendPacketsFileHandles;

        // Overlapped object related variables.
        private PreAllocatedOverlapped _preAllocatedOverlapped;
        private readonly StrongBox<SocketAsyncEventArgs?> _strongThisRef = new StrongBox<SocketAsyncEventArgs?>(); // state for _preAllocatedOverlapped; .Value set to this while operations in flight

        /// <summary>Registration with a cancellation token for an asynchronous operation.</summary>
        /// <remarks>
        /// This should only be set in <see cref="ProcessIOCPResult"/> when <see cref="_asyncCompletionOwnership"/> is also being
        /// set to non-0, and then cleaned up in <see cref="CompleteCore"/>.  If it's set and <see cref="_asyncCompletionOwnership"/>
        /// remains 0, it may not get cleaned up correctly.
        /// </remarks>
        private CancellationTokenRegistration _registrationToCancelPendingIO;
        private unsafe NativeOverlapped* _pendingOverlappedForCancellation;

        private PinState _pinState;
        private enum PinState : byte { None = 0, MultipleBuffer, SendPackets }

        [MemberNotNull(nameof(_preAllocatedOverlapped))]
        private void InitializeInternals()
        {
            Debug.Assert(OperatingSystem.IsWindows());

            _preAllocatedOverlapped = PreAllocatedOverlapped.UnsafeCreate(s_completionPortCallback, _strongThisRef, null);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"new PreAllocatedOverlapped {_preAllocatedOverlapped}");
        }

        private void FreeInternals()
        {
            FreePinHandles();
            FreeOverlapped();
        }

        private unsafe NativeOverlapped* AllocateNativeOverlapped()
        {
            Debug.Assert(OperatingSystem.IsWindows());
            Debug.Assert(_operating == InProgress, $"Expected {nameof(_operating)} == {nameof(InProgress)}, got {_operating}");
            Debug.Assert(_currentSocket != null, "_currentSocket is null");
            Debug.Assert(_currentSocket.SafeHandle != null, "_currentSocket.SafeHandle is null");
            Debug.Assert(_preAllocatedOverlapped != null, "_preAllocatedOverlapped is null");

            ThreadPoolBoundHandle boundHandle = _currentSocket.GetOrAllocateThreadPoolBoundHandle();
            return boundHandle.AllocateNativeOverlapped(_preAllocatedOverlapped);
        }

        private unsafe void FreeNativeOverlapped(ref NativeOverlapped* overlapped)
        {
            Debug.Assert(OperatingSystem.IsWindows());
            Debug.Assert(overlapped != null, "overlapped is null");
            Debug.Assert(_operating == InProgress, $"Expected _operating == InProgress, got {_operating}");
            Debug.Assert(_currentSocket != null, "_currentSocket is null");
            Debug.Assert(_currentSocket.SafeHandle != null, "_currentSocket.SafeHandle is null");
            Debug.Assert(_currentSocket.SafeHandle.IOCPBoundHandle != null, "_currentSocket.SafeHandle.IOCPBoundHandle is null");
            Debug.Assert(_preAllocatedOverlapped != null, "_preAllocatedOverlapped is null");

            _currentSocket.SafeHandle.IOCPBoundHandle.FreeNativeOverlapped(overlapped);
            overlapped = null;
        }

        partial void StartOperationCommonCore()
        {
            // Store the reference to this instance so that it's kept alive by the preallocated
            // overlapped during the asynchronous operation and so that it's available in the
            // I/O completion callback.  Once the operation completes, we null this out so
            // that the SocketAsyncEventArgs instance isn't kept alive unnecessarily.
            _strongThisRef.Value = this;
        }

        /// <summary>Gets the result of an IOCP operation and determines how it should be handled (synchronously or asynchronously).</summary>
        /// <param name="success">true if the IOCP operation indicated synchronous success; otherwise, false.</param>
        /// <param name="overlapped">The overlapped that was used for this operation. Will be freed if the operation result will be handled synchronously.</param>
        /// <returns>The SocketError for the operation. This will be SocketError.IOPending if the operation will be handled asynchronously.</returns>
        private unsafe SocketError GetIOCPResult(bool success, ref NativeOverlapped* overlapped)
        {
            // Note: We need to dispose of the overlapped iff the operation result will be handled synchronously.

            if (success)
            {
                // Synchronous success.
                if (_currentSocket!.SafeHandle.SkipCompletionPortOnSuccess)
                {
                    // The socket handle is configured to skip completion on success, so we can handle the result synchronously.
                    FreeNativeOverlapped(ref overlapped);
                    return SocketError.Success;
                }

                // Completed synchronously, but the handle wasn't marked as skip completion port on success,
                // so we still need to behave as if the IO was pending and wait for the completion to come through on the IOCP.
                return SocketError.IOPending;
            }
            else
            {
                // Get the socket error (which may be IOPending)
                SocketError socketError = SocketPal.GetLastSocketError();
                Debug.Assert(socketError != SocketError.Success);
                if (socketError != SocketError.IOPending)
                {
                    // Completed synchronously with a failure.
                    // No IOCP completion will occur.
                    FreeNativeOverlapped(ref overlapped);
                    return socketError;
                }

                // The completion will arrive on the IOCP when the operation is done.
                return SocketError.IOPending;
            }
        }

        /// <summary>Handles the result of an IOCP operation for which we have deferred async processing logic (buffer pinning or cancellation).</summary>
        /// <param name="success">true if the IOCP operation indicated synchronous success; otherwise, false.</param>
        /// <param name="bytesTransferred">The number of bytes transferred, if the operation completed synchronously and successfully.</param>
        /// <param name="overlapped">The overlapped that was used for this operation. Will be freed if the operation result will be handled synchronously.</param>
        /// <param name="bufferToPin">The buffer to pin. May be Memory.Empty if no buffer should be pinned.
        ///     Note this buffer (if not empty) should already be pinned locally using `fixed` prior to the OS async call and until after this method returns.</param>
        /// <param name="cancellationToken">The cancellation token to use to cancel the operation.</param>
        /// <returns>The result status of the operation.</returns>
        private unsafe SocketError ProcessIOCPResult(bool success, int bytesTransferred, ref NativeOverlapped* overlapped, Memory<byte> bufferToPin, CancellationToken cancellationToken)
        {
            SocketError socketError = GetIOCPResult(success, ref overlapped);
            SocketFlags socketFlags = SocketFlags.None;

            if (socketError == SocketError.IOPending)
            {
                // Perform any required setup of the asynchronous operation.  Everything set up here needs to be undone in CompleteCore.CleanupIOCPResult.
                if (cancellationToken.CanBeCanceled)
                {
                    Debug.Assert(_pendingOverlappedForCancellation == null);
                    _pendingOverlappedForCancellation = overlapped;
                    _registrationToCancelPendingIO = cancellationToken.UnsafeRegister(static s =>
                    {
                        // Try to cancel the I/O.  We ignore the return value (other than for logging), as cancellation
                        // is opportunistic and we don't want to fail the operation because we couldn't cancel it.
                        var thisRef = (SocketAsyncEventArgs)s!;
                        SafeSocketHandle handle = thisRef._currentSocket!.SafeHandle;
                        if (!handle.IsClosed)
                        {
                            try
                            {
                                bool canceled = Interop.Kernel32.CancelIoEx(handle, thisRef._pendingOverlappedForCancellation);
                                if (NetEventSource.Log.IsEnabled())
                                {
                                    NetEventSource.Info(thisRef, canceled ?
                                        "Socket operation canceled." :
                                        $"CancelIoEx failed with error '{Marshal.GetLastWin32Error()}'.");
                                }
                            }
                            catch (ObjectDisposedException)
                            {
                                // Ignore errors resulting from the SafeHandle being closed concurrently.
                            }
                        }
                    }, this);
                }
                if (!bufferToPin.Equals(default))
                {
                    _singleBufferHandle = bufferToPin.Pin();
                }

                // We've finished setting up and launching the operation.  Coordinate with the callback.
                // The expectation is that in the majority of cases either the operation will have completed
                // synchronously (in which case we won't be here) or the operation will complete asynchronously
                // and this function will typically win the race condition with the callback.
                ulong packedResult = Interlocked.Exchange(ref _asyncCompletionOwnership, 1);
                if (packedResult == 0)
                {
                    // We won the race condition with the callback. It now owns completion and clean up.
                    return SocketError.IOPending;
                }

                // The callback was already invoked and transferred ownership to us, so now behave as if the operation completed synchronously.
                // Since the success/bytesTransferred arguments passed into this method are stale, we need to retrieve the actual status info
                // from the overlapped directly.  It's also now our responsibility to clean up as GetIOCPResult would have, so free the overlapped.
                Debug.Assert((packedResult & 0x8000000000000000) != 0, "Top bit should have been set");
                bytesTransferred = (int)((packedResult >> 32) & 0x7FFFFFFF);
                socketError = (SocketError)(packedResult & 0xFFFFFFFF);
                if (socketError != SocketError.Success)
                {
                    GetOverlappedResultOnError(ref socketError, ref Unsafe.As<int, uint>(ref bytesTransferred), ref socketFlags, overlapped);
                }
                FreeNativeOverlapped(ref overlapped);
            }

            // The operation completed, either synchronously and the callback won't be invoked, or asynchronously
            // but so fast the callback has already executed and left clean up to us.
            FinishOperationSync(socketError, bytesTransferred, socketFlags);
            return socketError;
        }

        internal unsafe SocketError DoOperationAccept(Socket socket, SafeSocketHandle handle, SafeSocketHandle acceptHandle, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            bool userBuffer = _count != 0;
            Debug.Assert(!userBuffer || (!_buffer.Equals(default) && _count >= _acceptAddressBufferCount));
            Memory<byte> buffer = userBuffer ? _buffer : _acceptBuffer;

            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer.Span))
            {
                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    bool success = socket.AcceptEx(
                        handle,
                        acceptHandle,
                        (IntPtr)(userBuffer ? (bufferPtr + _offset) : bufferPtr),
                        userBuffer ? _count - _acceptAddressBufferCount : 0,
                        _acceptAddressBufferCount / 2,
                        _acceptAddressBufferCount / 2,
                        out int bytesTransferred,
                        overlapped);

                    return ProcessIOCPResult(success, bytesTransferred, ref overlapped, buffer, cancellationToken);
                }
                catch when (overlapped is not null)
                {
                    FreeNativeOverlapped(ref overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationConnect(Socket socket, SafeSocketHandle handle)
        {
            // Called for connectionless protocols.
            SocketError socketError = SocketPal.Connect(handle, _socketAddress!.Buffer, _socketAddress.Size);
            FinishOperationSync(socketError, 0, SocketFlags.None);
            return socketError;
        }

        internal unsafe SocketError DoOperationConnectEx(Socket socket, SafeSocketHandle handle)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            // ConnectEx uses a sockaddr buffer containing the remote address to which to connect.
            // It can also optionally take a single buffer of data to send after the connection is complete.
            // The sockaddr is pinned with a GCHandle to avoid having to use the object array form of UnsafePack.
            PinSocketAddressBuffer();

            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
            {
                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    bool success = socket.ConnectEx(
                        handle,
                        PtrSocketAddressBuffer,
                        _socketAddress!.Size,
                        (IntPtr)((byte*)_singleBufferHandle.Pointer + _offset),
                        _count,
                        out int bytesTransferred,
                        overlapped);

                    return ProcessIOCPResult(success, bytesTransferred, ref overlapped, _buffer, cancellationToken: default);
                }
                catch when (overlapped is not null)
                {
                    FreeNativeOverlapped(ref overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationDisconnect(Socket socket, SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                bool success = socket.DisconnectEx(
                    handle,
                    overlapped,
                    (int)(DisconnectReuseSocket ? TransmitFileOptions.ReuseSocket : 0),
                    0);

                return ProcessIOCPResult(success, 0, ref overlapped, bufferToPin: default, cancellationToken: cancellationToken);
            }
            catch when (overlapped is not null)
            {
                FreeNativeOverlapped(ref overlapped);
                throw;
            }
        }

        internal SocketError DoOperationReceive(SafeSocketHandle handle, CancellationToken cancellationToken) => _bufferList == null ?
            DoOperationReceiveSingleBuffer(handle, cancellationToken) :
            DoOperationReceiveMultiBuffer(handle);

        internal unsafe SocketError DoOperationReceiveSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
            {
                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    var wsaBuffer = new WSABuffer { Length = _count, Pointer = (IntPtr)(bufferPtr + _offset) };

                    SocketFlags flags = _socketFlags;
                    SocketError socketError = Interop.Winsock.WSARecv(
                        handle,
                        &wsaBuffer,
                        1,
                        out int bytesTransferred,
                        ref flags,
                        overlapped,
                        IntPtr.Zero);

                    return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _buffer, cancellationToken);
                }
                catch when (overlapped is not null)
                {
                    FreeNativeOverlapped(ref overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationReceiveMultiBuffer(SafeSocketHandle handle)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                SocketFlags flags = _socketFlags;
                SocketError socketError = Interop.Winsock.WSARecv(
                    handle,
                    _wsaBufferArrayPinned,
                    _bufferListInternal!.Count,
                    out int bytesTransferred,
                    ref flags,
                    overlapped,
                    IntPtr.Zero);

                return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, bufferToPin: default, cancellationToken: default);
            }
            catch when (overlapped is not null)
            {
                FreeNativeOverlapped(ref overlapped);
                throw;
            }
        }

        internal unsafe SocketError DoOperationReceiveFrom(SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            // WSARecvFrom uses a WSABuffer array describing buffers in which to
            // receive data and from which to send data respectively. Single and multiple buffers
            // are handled differently so as to optimize performance for the more common single buffer case.
            // WSARecvFrom and WSASendTo also uses a sockaddr buffer in which to store the address from which the data was received.
            // The sockaddr is pinned with a GCHandle to avoid having to use the object array form of UnsafePack.
            PinSocketAddressBuffer();

            return _bufferList == null ?
                DoOperationReceiveFromSingleBuffer(handle, cancellationToken) :
                DoOperationReceiveFromMultiBuffer(handle);
        }

        internal unsafe SocketError DoOperationReceiveFromSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
            {
                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    var wsaBuffer = new WSABuffer { Length = _count, Pointer = (IntPtr)(bufferPtr + _offset) };

                    SocketFlags flags = _socketFlags;
                    SocketError socketError = Interop.Winsock.WSARecvFrom(
                        handle,
                        ref wsaBuffer,
                        1,
                        out int bytesTransferred,
                        ref flags,
                        PtrSocketAddressBuffer,
                        PtrSocketAddressBufferSize,
                        overlapped,
                        IntPtr.Zero);

                    return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _buffer, cancellationToken);
                }
                catch when (overlapped is not null)
                {
                    FreeNativeOverlapped(ref overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationReceiveFromMultiBuffer(SafeSocketHandle handle)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                SocketFlags flags = _socketFlags;
                SocketError socketError = Interop.Winsock.WSARecvFrom(
                    handle,
                    _wsaBufferArrayPinned!,
                    _bufferListInternal!.Count,
                    out int bytesTransferred,
                    ref flags,
                    PtrSocketAddressBuffer,
                    PtrSocketAddressBufferSize,
                    overlapped,
                    IntPtr.Zero);

                return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, bufferToPin: default, cancellationToken: default);
            }
            catch when (overlapped is not null)
            {
                FreeNativeOverlapped(ref overlapped);
                throw;
            }
        }

        internal unsafe SocketError DoOperationReceiveMessageFrom(Socket socket, SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            // WSARecvMsg uses a WSAMsg descriptor.
            // The WSAMsg buffer is a pinned array to avoid complicating the use of Overlapped.
            // WSAMsg contains a pointer to a sockaddr.
            // The sockaddr is pinned with a GCHandle to avoid complicating the use of Overlapped.
            // WSAMsg contains a pointer to a WSABuffer array describing data buffers.
            // WSAMsg also contains a single WSABuffer describing a control buffer.
            PinSocketAddressBuffer();

            // Create a WSAMessageBuffer if none exists yet.
            _wsaMessageBufferPinned ??= GC.AllocateUninitializedArray<byte>(sizeof(Interop.Winsock.WSAMsg), pinned: true);

            // Create and pin an appropriately sized control buffer if none already
            IPAddress? ipAddress = (_socketAddress!.Family == AddressFamily.InterNetworkV6 ? _socketAddress.GetIPAddress() : null);
            bool ipv4 = (_currentSocket!.AddressFamily == AddressFamily.InterNetwork || (ipAddress != null && ipAddress.IsIPv4MappedToIPv6)); // DualMode
            bool ipv6 = _currentSocket.AddressFamily == AddressFamily.InterNetworkV6;

            if (ipv6 && (_controlBufferPinned == null || _controlBufferPinned.Length != sizeof(Interop.Winsock.ControlDataIPv6)))
            {
                _controlBufferPinned = GC.AllocateUninitializedArray<byte>(sizeof(Interop.Winsock.ControlDataIPv6), pinned: true);
            }
            else if (ipv4 && (_controlBufferPinned == null || _controlBufferPinned.Length != sizeof(Interop.Winsock.ControlData)))
            {
                _controlBufferPinned = GC.AllocateUninitializedArray<byte>(sizeof(Interop.Winsock.ControlData), pinned: true);
            }

            // If single buffer we need a single element WSABuffer.
            WSABuffer[] wsaRecvMsgWSABufferArray;
            uint wsaRecvMsgWSABufferCount;
            if (_bufferList == null)
            {
                _wsaRecvMsgWSABufferArrayPinned ??= GC.AllocateUninitializedArray<WSABuffer>(1, pinned: true);

                fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
                {
                    _wsaRecvMsgWSABufferArrayPinned[0].Pointer = (IntPtr)bufferPtr + _offset;
                    _wsaRecvMsgWSABufferArrayPinned[0].Length = _count;
                    wsaRecvMsgWSABufferArray = _wsaRecvMsgWSABufferArrayPinned;
                    wsaRecvMsgWSABufferCount = 1;

                    return Core();
                }
            }
            else
            {
                // Use the multi-buffer WSABuffer.
                wsaRecvMsgWSABufferArray = _wsaBufferArrayPinned!;
                wsaRecvMsgWSABufferCount = (uint)_bufferListInternal!.Count;

                return Core();
            }

            // Fill in WSAMessageBuffer, run WSARecvMsg and process the IOCP result.
            // Logic is in a separate method so we can share code between the (pinned) single buffer and the multi-buffer case
            SocketError Core()
            {
                // Fill in WSAMessageBuffer.
                Interop.Winsock.WSAMsg* pMessage = (Interop.Winsock.WSAMsg*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0);
                pMessage->socketAddress = PtrSocketAddressBuffer;
                pMessage->addressLength = (uint)_socketAddress.Size;
                fixed (void* ptrWSARecvMsgWSABufferArray = &wsaRecvMsgWSABufferArray[0])
                {
                    pMessage->buffers = (IntPtr)ptrWSARecvMsgWSABufferArray;
                }
                pMessage->count = wsaRecvMsgWSABufferCount;

                if (_controlBufferPinned != null)
                {
                    Debug.Assert(_controlBufferPinned.Length > 0);
                    fixed (void* ptrControlBuffer = &_controlBufferPinned[0])
                    {
                        pMessage->controlBuffer.Pointer = (IntPtr)ptrControlBuffer;
                    }
                    pMessage->controlBuffer.Length = _controlBufferPinned.Length;
                }
                pMessage->flags = _socketFlags;

                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    SocketError socketError = socket.WSARecvMsg(
                        handle,
                        Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0),
                        out int bytesTransferred,
                        overlapped,
                        IntPtr.Zero);

                    return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _bufferList == null ? _buffer : default, cancellationToken);
                }
                catch when (overlapped is not null)
                {
                    FreeNativeOverlapped(ref overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationSend(SafeSocketHandle handle, CancellationToken cancellationToken) => _bufferList == null ?
            DoOperationSendSingleBuffer(handle, cancellationToken) :
            DoOperationSendMultiBuffer(handle);

        internal unsafe SocketError DoOperationSendSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
            {
                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    var wsaBuffer = new WSABuffer { Length = _count, Pointer = (IntPtr)(bufferPtr + _offset) };

                    SocketError socketError = Interop.Winsock.WSASend(
                        handle,
                        &wsaBuffer,
                        1,
                        out int bytesTransferred,
                        _socketFlags,
                        overlapped,
                        IntPtr.Zero);

                    return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _buffer, cancellationToken);
                }
                catch when (overlapped is not null)
                {
                    FreeNativeOverlapped(ref overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationSendMultiBuffer(SafeSocketHandle handle)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                SocketError socketError = Interop.Winsock.WSASend(
                    handle,
                    _wsaBufferArrayPinned,
                    _bufferListInternal!.Count,
                    out int bytesTransferred,
                    _socketFlags,
                    overlapped,
                    IntPtr.Zero);

                return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, bufferToPin: default, cancellationToken: default);
            }
            catch when (overlapped is not null)
            {
                FreeNativeOverlapped(ref overlapped);
                throw;
            }
        }

        internal unsafe SocketError DoOperationSendPackets(Socket socket, SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            // Cache copy to avoid problems with concurrent manipulation during the async operation.
            Debug.Assert(_sendPacketsElements != null);
            SendPacketsElement[] sendPacketsElementsCopy = (SendPacketsElement[])_sendPacketsElements.Clone();

            // TransmitPackets uses an array of TRANSMIT_PACKET_ELEMENT structs as
            // descriptors for buffers and files to be sent.  It also takes a send size
            // and some flags.  The TRANSMIT_PACKET_ELEMENT for a file contains a native file handle.
            // Opens the files to get the file handles, pin down any buffers specified and builds the
            // native TRANSMIT_PACKET_ELEMENT array that will be passed to TransmitPackets.

            // Scan the elements to count files and buffers.
            int sendPacketsElementsFileCount = 0, sendPacketsElementsFileStreamCount = 0, sendPacketsElementsBufferCount = 0;
            foreach (SendPacketsElement spe in sendPacketsElementsCopy)
            {
                if (spe != null)
                {
                    if (spe.FilePath != null)
                    {
                        sendPacketsElementsFileCount++;
                    }
                    else if (spe.FileStream != null)
                    {
                        sendPacketsElementsFileStreamCount++;
                    }
                    else if (spe.MemoryBuffer != null && spe.Count > 0)
                    {
                        sendPacketsElementsBufferCount++;
                    }
                }
            }

            if (sendPacketsElementsFileCount + sendPacketsElementsFileStreamCount + sendPacketsElementsBufferCount == 0)
            {
                FinishOperationSyncSuccess(0, SocketFlags.None);
                return SocketError.Success;
            }

            // Attempt to open the files if any were given.
            if (sendPacketsElementsFileCount > 0)
            {
                // Loop through the elements attempting to open each files and get its handle.
                int index = 0;
                _sendPacketsFileHandles = new SafeFileHandle[sendPacketsElementsFileCount];
                try
                {
                    foreach (SendPacketsElement spe in sendPacketsElementsCopy)
                    {
                        if (spe?.FilePath != null)
                        {
                            // Create a FileStream to open the file.
                            _sendPacketsFileHandles[index] =
                                File.OpenHandle(spe.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                            // Get the file handle from the stream.
                            index++;
                        }
                    }
                }
                catch
                {
                    // Got an exception opening a file - close any open streams, then throw.
                    for (int i = index - 1; i >= 0; i--)
                        _sendPacketsFileHandles[i].Dispose();
                    _sendPacketsFileHandles = null;
                    throw;
                }
            }

            Interop.Winsock.TransmitPacketsElement[] sendPacketsDescriptorPinned =
                SetupPinHandlesSendPackets(sendPacketsElementsCopy, sendPacketsElementsFileCount,
                    sendPacketsElementsFileStreamCount, sendPacketsElementsBufferCount);
            Debug.Assert(sendPacketsDescriptorPinned != null);
            Debug.Assert(sendPacketsDescriptorPinned.Length > 0);

            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                bool result = socket.TransmitPackets(
                    handle,
                    Marshal.UnsafeAddrOfPinnedArrayElement(sendPacketsDescriptorPinned, 0),
                    sendPacketsDescriptorPinned.Length,
                    _sendPacketsSendSize,
                    overlapped,
                    _sendPacketsFlags);

                return ProcessIOCPResult(result, 0, ref overlapped, bufferToPin: default, cancellationToken: cancellationToken);
            }
            catch when (overlapped is not null)
            {
                FreeNativeOverlapped(ref overlapped);
                throw;
            }
        }

        internal unsafe SocketError DoOperationSendTo(SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            // WSASendTo uses a WSABuffer array describing buffers in which to
            // receive data and from which to send data respectively. Single and multiple buffers
            // are handled differently so as to optimize performance for the more common single buffer case.
            //
            // WSARecvFrom and WSASendTo also uses a sockaddr buffer in which to store the address from which the data was received.
            // The sockaddr is pinned with a GCHandle to avoid having to use the object array form of UnsafePack.
            PinSocketAddressBuffer();

            return _bufferList == null ?
                DoOperationSendToSingleBuffer(handle, cancellationToken) :
                DoOperationSendToMultiBuffer(handle);
        }

        internal unsafe SocketError DoOperationSendToSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
            {
                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    var wsaBuffer = new WSABuffer { Length = _count, Pointer = (IntPtr)(bufferPtr + _offset) };

                    SocketError socketError = Interop.Winsock.WSASendTo(
                        handle,
                        ref wsaBuffer,
                        1,
                        out int bytesTransferred,
                        _socketFlags,
                        PtrSocketAddressBuffer,
                        _socketAddress!.Size,
                        overlapped,
                        IntPtr.Zero);

                    return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _buffer, cancellationToken);
                }
                catch when (overlapped is not null)
                {
                    FreeNativeOverlapped(ref overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationSendToMultiBuffer(SafeSocketHandle handle)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                SocketError socketError = Interop.Winsock.WSASendTo(
                    handle,
                    _wsaBufferArrayPinned!,
                    _bufferListInternal!.Count,
                    out int bytesTransferred,
                    _socketFlags,
                    PtrSocketAddressBuffer,
                    _socketAddress!.Size,
                    overlapped,
                    IntPtr.Zero);

                return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, bufferToPin: default, cancellationToken: default);
            }
            catch when (overlapped is not null)
            {
                FreeNativeOverlapped(ref overlapped);
                throw;
            }
        }

        // Ensures Overlapped object exists with appropriate multiple buffers pinned.
        private void SetupMultipleBuffers()
        {
            if (_bufferListInternal == null || _bufferListInternal.Count == 0)
            {
                // No buffer list is set so unpin any existing multiple buffer pinning.
                if (_pinState == PinState.MultipleBuffer)
                {
                    FreePinHandles();
                }
            }
            else
            {
                // Need to setup a new Overlapped.
                FreePinHandles();
                try
                {
                    int bufferCount = _bufferListInternal.Count;

                    // Number of things to pin is number of buffers.
                    // Ensure we have properly sized object array.
                    if (_multipleBufferMemoryHandles == null || (_multipleBufferMemoryHandles.Length < bufferCount))
                    {
                        _multipleBufferMemoryHandles = new MemoryHandle[bufferCount];
                    }

                    // Pin the buffers.
                    for (int i = 0; i < bufferCount; i++)
                    {
                        _multipleBufferMemoryHandles[i] = _bufferListInternal[i].Array.AsMemory().Pin();
                    }

                    if (_wsaBufferArrayPinned == null || _wsaBufferArrayPinned.Length < bufferCount)
                    {
                        _wsaBufferArrayPinned = GC.AllocateUninitializedArray<WSABuffer>(bufferCount, pinned: true);
                    }

                    for (int i = 0; i < bufferCount; i++)
                    {
                        ArraySegment<byte> localCopy = _bufferListInternal[i];
                        _wsaBufferArrayPinned[i].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(localCopy.Array!, localCopy.Offset);
                        _wsaBufferArrayPinned[i].Length = localCopy.Count;
                    }

                    _pinState = PinState.MultipleBuffer;
                }
                catch (Exception)
                {
                    FreePinHandles();
                    throw;
                }
            }
        }

        // Ensures appropriate SocketAddress buffer is pinned.
        private void PinSocketAddressBuffer()
        {
            // Check if already pinned.
            if (_pinnedSocketAddress == _socketAddress)
            {
                return;
            }

            // Unpin any existing.
            if (_socketAddressGCHandle.IsAllocated)
            {
                _socketAddressGCHandle.Free();
            }

            // Pin down the new one.
            _socketAddressGCHandle = GCHandle.Alloc(_socketAddress!.Buffer, GCHandleType.Pinned);
            _socketAddress.CopyAddressSizeIntoBuffer();
            _pinnedSocketAddress = _socketAddress;
        }

        private unsafe IntPtr PtrSocketAddressBuffer
        {
            get
            {
                Debug.Assert(_pinnedSocketAddress != null);
                Debug.Assert(_pinnedSocketAddress.Buffer != null);
                Debug.Assert(_pinnedSocketAddress.Buffer.Length > 0);
                Debug.Assert(_socketAddressGCHandle.IsAllocated);
                Debug.Assert(_socketAddressGCHandle.Target == _pinnedSocketAddress.Buffer);
                fixed (void* ptrSocketAddressBuffer = &_pinnedSocketAddress.Buffer[0])
                {
                    return (IntPtr)ptrSocketAddressBuffer;
                }
            }
        }

        private IntPtr PtrSocketAddressBufferSize => PtrSocketAddressBuffer + _socketAddress!.GetAddressSizeOffset();

        // Cleans up any existing Overlapped object and related state variables.
        private void FreeOverlapped()
        {
            // Free the preallocated overlapped object. This in turn will unpin
            // any pinned buffers.
            if (_preAllocatedOverlapped != null)
            {
                Debug.Assert(OperatingSystem.IsWindows());
                _preAllocatedOverlapped.Dispose();
                _preAllocatedOverlapped = null!;
            }
        }

        private void FreePinHandles()
        {
            _pinState = PinState.None;

            if (_multipleBufferMemoryHandles != null)
            {
                for (int i = 0; i < _multipleBufferMemoryHandles.Length; i++)
                {
                    _multipleBufferMemoryHandles[i].Dispose();
                    _multipleBufferMemoryHandles[i] = default;
                }
            }

            if (_socketAddressGCHandle.IsAllocated)
            {
                _socketAddressGCHandle.Free();
                _pinnedSocketAddress = null;
            }

            Debug.Assert(_singleBufferHandle.Equals(default(MemoryHandle)));
        }

        // Sets up an Overlapped object for SendPacketsAsync.
        private unsafe Interop.Winsock.TransmitPacketsElement[] SetupPinHandlesSendPackets(
            SendPacketsElement[] sendPacketsElementsCopy, int sendPacketsElementsFileCount, int sendPacketsElementsFileStreamCount, int sendPacketsElementsBufferCount)
        {
            if (_pinState != PinState.None)
            {
                FreePinHandles();
            }

            // Alloc native descriptor.
            var sendPacketsDescriptorPinned = GC.AllocateUninitializedArray<Interop.Winsock.TransmitPacketsElement>(
                sendPacketsElementsFileCount + sendPacketsElementsFileStreamCount + sendPacketsElementsBufferCount,
                pinned: true);

            // Number of things to pin is number of buffers + 1 (native descriptor).
            // Ensure we have properly sized object array.
            if (_multipleBufferMemoryHandles == null || (_multipleBufferMemoryHandles.Length < sendPacketsElementsBufferCount))
            {
                _multipleBufferMemoryHandles = new MemoryHandle[sendPacketsElementsBufferCount];
            }

            // Pin user specified buffers.
            int index = 0;
            foreach (SendPacketsElement spe in sendPacketsElementsCopy)
            {
                if (spe?.MemoryBuffer != null && spe.Count > 0)
                {
                    _multipleBufferMemoryHandles[index] = spe.MemoryBuffer.Value.Pin();
                    index++;
                }
            }

            // Fill in native descriptor.
            int bufferIndex = 0;
            int descriptorIndex = 0;
            int fileIndex = 0;
            foreach (SendPacketsElement spe in sendPacketsElementsCopy)
            {
                if (spe != null)
                {
                    if (spe.MemoryBuffer != null && spe.Count > 0)
                    {
                        // This element is a buffer.
                        sendPacketsDescriptorPinned[descriptorIndex].buffer = (IntPtr)_multipleBufferMemoryHandles[bufferIndex].Pointer;
                        sendPacketsDescriptorPinned[descriptorIndex].length = (uint)spe.Count;
                        sendPacketsDescriptorPinned[descriptorIndex].flags =
                            Interop.Winsock.TransmitPacketsElementFlags.Memory | (spe.EndOfPacket
                                ? Interop.Winsock.TransmitPacketsElementFlags.EndOfPacket
                                : 0);
                        bufferIndex++;
                        descriptorIndex++;
                    }
                    else if (spe.FilePath != null)
                    {
                        // This element is a file.
                        sendPacketsDescriptorPinned[descriptorIndex].fileHandle = _sendPacketsFileHandles![fileIndex].DangerousGetHandle();
                        sendPacketsDescriptorPinned[descriptorIndex].fileOffset = spe.OffsetLong;
                        sendPacketsDescriptorPinned[descriptorIndex].length = (uint)spe.Count;
                        sendPacketsDescriptorPinned[descriptorIndex].flags =
                            Interop.Winsock.TransmitPacketsElementFlags.File | (spe.EndOfPacket
                                ? Interop.Winsock.TransmitPacketsElementFlags.EndOfPacket
                                : 0);
                        fileIndex++;
                        descriptorIndex++;
                    }
                    else if (spe.FileStream != null)
                    {
                        // This element is a file stream. SendPacketsElement throws if the FileStream is not opened asynchronously;
                        // Synchronously opened FileStream can't be used concurrently (e.g. multiple SendPacketsElements with the same
                        // FileStream).
                        sendPacketsDescriptorPinned[descriptorIndex].fileHandle = spe.FileStream.SafeFileHandle.DangerousGetHandle();
                        sendPacketsDescriptorPinned[descriptorIndex].fileOffset = spe.OffsetLong;

                        sendPacketsDescriptorPinned[descriptorIndex].length = (uint)spe.Count;
                        sendPacketsDescriptorPinned[descriptorIndex].flags =
                            Interop.Winsock.TransmitPacketsElementFlags.File | (spe.EndOfPacket
                                ? Interop.Winsock.TransmitPacketsElementFlags.EndOfPacket
                                : 0);
                        descriptorIndex++;
                    }
                }
            }

            _pinState = PinState.SendPackets;
            return sendPacketsDescriptorPinned;
        }

        internal void LogBuffer(int size)
        {
            // This should only be called if tracing is enabled. However, there is the potential for a race
            // condition where tracing is disabled between a calling check and here, in which case the assert
            // may fire erroneously.
            Debug.Assert(NetEventSource.Log.IsEnabled());

            if (_bufferList != null)
            {
                for (int i = 0; i < _bufferListInternal!.Count; i++)
                {
                    WSABuffer wsaBuffer = _wsaBufferArrayPinned![i];
                    NetEventSource.DumpBuffer(this, wsaBuffer.Pointer, Math.Min(wsaBuffer.Length, size));
                    if ((size -= wsaBuffer.Length) <= 0)
                    {
                        break;
                    }
                }
            }
            else if (_buffer.Length != 0)
            {
                NetEventSource.DumpBuffer(this, _buffer, _offset, size);
            }
        }

        private unsafe SocketError FinishOperationAccept(Internals.SocketAddress remoteSocketAddress)
        {
            SocketError socketError;
            IntPtr localAddr;
            int localAddrLength;
            IntPtr remoteAddr;

            bool refAdded = false;
            SafeHandle safeHandle = _currentSocket!.SafeHandle;
            try
            {
                safeHandle.DangerousAddRef(ref refAdded);
                IntPtr handle = safeHandle.DangerousGetHandle();

                // This matches the logic in DoOperationAccept
                bool userBuffer = _count != 0;
                Debug.Assert(!userBuffer || (!_buffer.Equals(default) && _count >= _acceptAddressBufferCount));
                Memory<byte> buffer = userBuffer ? _buffer : _acceptBuffer;

                fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer.Span))
                {
                    _currentSocket.GetAcceptExSockaddrs(
                        (IntPtr)(userBuffer ? (bufferPtr + _offset) : bufferPtr),
                        userBuffer ? _count - _acceptAddressBufferCount : 0,
                        _acceptAddressBufferCount / 2,
                        _acceptAddressBufferCount / 2,
                        out localAddr,
                        out localAddrLength,
                        out remoteAddr,
                        out remoteSocketAddress.InternalSize
                    );

                    Marshal.Copy(remoteAddr, remoteSocketAddress.Buffer, 0, remoteSocketAddress.Size);
                }

                socketError = Interop.Winsock.setsockopt(
                    _acceptSocket!.SafeHandle,
                    SocketOptionLevel.Socket,
                    SocketOptionName.UpdateAcceptContext,
                    ref handle,
                    IntPtr.Size);

                if (socketError == SocketError.SocketError)
                {
                    socketError = SocketPal.GetLastSocketError();
                }
            }
            catch (ObjectDisposedException)
            {
                socketError = SocketError.OperationAborted;
            }
            finally
            {
                if (refAdded)
                {
                    safeHandle.DangerousRelease();
                }
            }

            return socketError;
        }

        private unsafe SocketError FinishOperationConnect()
        {
            try
            {
                if (_currentSocket!.SocketType != SocketType.Stream)
                {
                    // With connectionless sockets, regular connect is used instead of ConnectEx,
                    // attempting to set SO_UPDATE_CONNECT_CONTEXT will result in an error.
                    return SocketError.Success;
                }

                // Update the socket context.
                SocketError socketError = Interop.Winsock.setsockopt(
                    _currentSocket!.SafeHandle,
                    SocketOptionLevel.Socket,
                    SocketOptionName.UpdateConnectContext,
                    null,
                    0);
                return socketError == SocketError.SocketError ?
                    SocketPal.GetLastSocketError() :
                    socketError;
            }
            catch (ObjectDisposedException)
            {
                return SocketError.OperationAborted;
            }
        }

        private unsafe int GetSocketAddressSize() => *(int*)PtrSocketAddressBufferSize;

        private void CompleteCore()
        {
            _strongThisRef.Value = null; // null out this reference from the overlapped so this isn't kept alive artificially

            if (_asyncCompletionOwnership != 0)
            {
                // If the state isn't 0, then the operation didn't complete synchronously, in which case there's state to cleanup.
                CleanupIOCPResult();
            }

            // Separate out to help inline the CompleteCore fast path, as CompleteCore is used with all operations.
            // We want to optimize for the case where the async operation actually completes synchronously, without
            // having registered any state yet, in particular for sends and receives.
            void CleanupIOCPResult()
            {
                // Remove any cancellation state.  First dispose the registration
                // to ensure that cancellation will either never fine or will have completed
                // firing before we continue.  Only then can we safely null out the overlapped.
                _registrationToCancelPendingIO.Dispose();
                _registrationToCancelPendingIO = default;
                unsafe
                {
                    _pendingOverlappedForCancellation = null;
                }

                // Release any GC handles.
                _singleBufferHandle.Dispose();
                _singleBufferHandle = default;

                // Finished cleanup.
                _asyncCompletionOwnership = 0;
            }
        }

        private unsafe void FinishOperationReceiveMessageFrom()
        {
            Interop.Winsock.WSAMsg* PtrMessage = (Interop.Winsock.WSAMsg*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned!, 0);
            _socketFlags = PtrMessage->flags;

            if (_controlBufferPinned!.Length == sizeof(Interop.Winsock.ControlData))
            {
                // IPv4.
                _receiveMessageFromPacketInfo = SocketPal.GetIPPacketInformation((Interop.Winsock.ControlData*)PtrMessage->controlBuffer.Pointer);
            }
            else if (_controlBufferPinned.Length == sizeof(Interop.Winsock.ControlDataIPv6))
            {
                // IPv6.
                _receiveMessageFromPacketInfo = SocketPal.GetIPPacketInformation((Interop.Winsock.ControlDataIPv6*)PtrMessage->controlBuffer.Pointer);
            }
            else
            {
                // Other.
                _receiveMessageFromPacketInfo = default;
            }
        }

        private void FinishOperationSendPackets()
        {
            // Close the files if open.
            if (_sendPacketsFileHandles != null)
            {
                for (int i = 0; i < _sendPacketsFileHandles.Length; i++)
                {
                    _sendPacketsFileHandles[i]?.Dispose();
                }

                _sendPacketsFileHandles = null;
            }
        }

        private static readonly unsafe IOCompletionCallback s_completionPortCallback = delegate (uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            Debug.Assert(OperatingSystem.IsWindows());
            var saeaBox = (StrongBox<SocketAsyncEventArgs>)(ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped)!);

            Debug.Assert(saeaBox.Value != null);
            SocketAsyncEventArgs saea = saeaBox.Value;

            // We need to coordinate with the launching thread, just in case it hasn't yet finished setting up the operation.
            // We typically expect the launching thread to have already completed setup, in which case _asyncCompletionOwnership
            // will be 1, so we do a fast non-synchronized check to see if it's still 0, and only if it is do we proceed to
            // pack the results for use with an interlocked coordination with that thread.
            if (saea._asyncCompletionOwnership == 0)
            {
                // Pack the error code and number of bytes transferred into a single ulong we can store into
                // _asyncCompletionOwnership.  If the field was already set by the launcher, the value won't
                // be needed, but if this callback wins the race condition and transfers ownership to the
                // launcher to handle completion and clean up, transfering these values over prevents needing
                // to make an additional call to WSAGetOverlappedResult.
                Debug.Assert(numBytes <= int.MaxValue, "We rely on being able to set the top bit to ensure the whole packed result isn't 0.");
                ulong packedResult = (1ul << 63) | ((ulong)numBytes << 32) | errorCode;

                if (Interlocked.Exchange(ref saea._asyncCompletionOwnership, packedResult) == 0)
                {
                    // The operation completed asynchronously so quickly that the thread launching the operation still hasn't finished setting
                    // up the state for the operation.  Leave all cleanup and completion logic to that thread.
                    return;
                }
            }

            // This callback owns the completion and cleanup for the operation.
            if ((SocketError)errorCode == SocketError.Success)
            {
                saea.FreeNativeOverlapped(ref nativeOverlapped);
                saea.FinishOperationAsyncSuccess((int)numBytes, SocketFlags.None);
            }
            else
            {
                SocketError socketError = (SocketError)errorCode;
                SocketFlags socketFlags = SocketFlags.None;
                saea.GetOverlappedResultOnError(ref socketError, ref numBytes, ref socketFlags, nativeOverlapped);

                saea.FreeNativeOverlapped(ref nativeOverlapped);
                saea.FinishOperationAsyncFailure(socketError, (int)numBytes, socketFlags);
            }
        };

        private unsafe void GetOverlappedResultOnError(ref SocketError socketError, ref uint numBytes, ref SocketFlags socketFlags, NativeOverlapped* nativeOverlapped)
        {
            if (socketError != SocketError.OperationAborted)
            {
                if (_currentSocket!.Disposed)
                {
                    socketError = SocketError.OperationAborted;
                }
                else
                {
                    try
                    {
                        // Call WSAGetOverlappedResult() so GetLastSocketError() will return the correct error.
                        Interop.Winsock.WSAGetOverlappedResult(_currentSocket.SafeHandle, nativeOverlapped, out numBytes, wait: false, out socketFlags);
                        socketError = SocketPal.GetLastSocketError();
                    }
                    catch
                    {
                        // _currentSocket may have been disposed after the Disposed check above, in which case the P/Invoke may throw.
                        socketError = SocketError.OperationAborted;
                    }
                }
            }
        }
    }
}
