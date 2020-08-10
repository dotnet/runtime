// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    public partial class SocketAsyncEventArgs : EventArgs, IDisposable
    {
        // Single buffer
        private MemoryHandle _singleBufferHandle;
        private volatile SingleBufferHandleState _singleBufferHandleState;
        private enum SingleBufferHandleState : byte { None, InProcess, Set }

        // BufferList property variables.
        // Note that these arrays are allocated and then grown as necessary, but never shrunk.
        // Thus the actual in-use length is defined by _bufferListInternal.Count, not the length of these arrays.
        private WSABuffer[]? _wsaBufferArrayPinned;
        private GCHandle[]? _multipleBufferGCHandles;

        // Internal buffers for WSARecvMsg
        private byte[]? _wsaMessageBufferPinned;
        private byte[]? _controlBufferPinned;
        private WSABuffer[]? _wsaRecvMsgWSABufferArrayPinned;

        // Internal SocketAddress buffer
        private GCHandle _socketAddressGCHandle;
        private Internals.SocketAddress? _pinnedSocketAddress;

        // SendPacketsElements property variables.
        private FileStream[]? _sendPacketsFileStreams;

        // Overlapped object related variables.
        private PreAllocatedOverlapped _preAllocatedOverlapped;
        private readonly StrongBox<SocketAsyncEventArgs?> _strongThisRef = new StrongBox<SocketAsyncEventArgs?>(); // state for _preAllocatedOverlapped; .Value set to this while operations in flight

        // Cancellation support
        private CancellationTokenRegistration _registrationToCancelPendingIO;
        private unsafe NativeOverlapped* _pendingOverlappedForCancellation;

        private PinState _pinState;
        private enum PinState : byte { None = 0, MultipleBuffer, SendPackets }

        [MemberNotNull(nameof(_preAllocatedOverlapped))]
        private void InitializeInternals()
        {
            // PreAllocatedOverlapped captures ExecutionContext, but SocketAsyncEventArgs ensures
            // that context is properly flowed if necessary, and thus we don't need the overlapped
            // infrastructure capturing and flowing as well.
            bool suppressFlow = !ExecutionContext.IsFlowSuppressed();
            try
            {
                if (suppressFlow) ExecutionContext.SuppressFlow();
                _preAllocatedOverlapped = new PreAllocatedOverlapped(s_completionPortCallback, _strongThisRef, null);
            }
            finally
            {
                if (suppressFlow) ExecutionContext.RestoreFlow();
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"new PreAllocatedOverlapped {_preAllocatedOverlapped}");
        }

        private void FreeInternals()
        {
            FreePinHandles();
            FreeOverlapped();
        }

        private unsafe NativeOverlapped* AllocateNativeOverlapped()
        {
            Debug.Assert(_operating == InProgress, $"Expected {nameof(_operating)} == {nameof(InProgress)}, got {_operating}");
            Debug.Assert(_currentSocket != null, "_currentSocket is null");
            Debug.Assert(_currentSocket.SafeHandle != null, "_currentSocket.SafeHandle is null");
            Debug.Assert(_preAllocatedOverlapped != null, "_preAllocatedOverlapped is null");

            ThreadPoolBoundHandle boundHandle = _currentSocket.GetOrAllocateThreadPoolBoundHandle();
            return boundHandle.AllocateNativeOverlapped(_preAllocatedOverlapped);
        }

        private unsafe void FreeNativeOverlapped(NativeOverlapped* overlapped)
        {
            Debug.Assert(overlapped != null, "overlapped is null");
            Debug.Assert(_operating == InProgress, $"Expected _operating == InProgress, got {_operating}");
            Debug.Assert(_currentSocket != null, "_currentSocket is null");
            Debug.Assert(_currentSocket.SafeHandle != null, "_currentSocket.SafeHandle is null");
            Debug.Assert(_currentSocket.SafeHandle.IOCPBoundHandle != null, "_currentSocket.SafeHandle.IOCPBoundHandle is null");
            Debug.Assert(_preAllocatedOverlapped != null, "_preAllocatedOverlapped is null");

            _currentSocket.SafeHandle.IOCPBoundHandle.FreeNativeOverlapped(overlapped);
        }

        private unsafe void RegisterToCancelPendingIO(NativeOverlapped* overlapped, CancellationToken cancellationToken)
        {
            Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.InProcess);
            Debug.Assert(_pendingOverlappedForCancellation == null);
            _pendingOverlappedForCancellation = overlapped;
            _registrationToCancelPendingIO = cancellationToken.UnsafeRegister(s =>
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

        partial void StartOperationCommonCore()
        {
            // Store the reference to this instance so that it's kept alive by the preallocated
            // overlapped during the asynchronous operation and so that it's available in the
            // I/O completion callback.  Once the operation completes, we null this out so
            // that the SocketAsyncEventArgs instance isn't kept alive unnecessarily.
            _strongThisRef.Value = this;
        }

        /// <summary>Handles the result of an IOCP operation.</summary>
        /// <param name="success">true if the operation completed synchronously and successfully; otherwise, false.</param>
        /// <param name="bytesTransferred">The number of bytes transferred, if the operation completed synchronously and successfully.</param>
        /// <param name="overlapped">The overlapped to be freed if the operation completed synchronously.</param>
        /// <returns>The result status of the operation.</returns>
        private unsafe SocketError ProcessIOCPResult(bool success, int bytesTransferred, NativeOverlapped* overlapped)
        {
            // Note: We need to dispose of the overlapped iff the operation completed synchronously,
            // and if we do, we must do so before we mark the operation as completed.

            if (success)
            {
                // Synchronous success.
                if (_currentSocket!.SafeHandle.SkipCompletionPortOnSuccess)
                {
                    // The socket handle is configured to skip completion on success,
                    // so we can set the results right now.
                    FreeNativeOverlapped(overlapped);
                    FinishOperationSyncSuccess(bytesTransferred, SocketFlags.None);
                    return SocketError.Success;
                }

                // Completed synchronously, but the handle wasn't marked as skip completion port on success,
                // so we still need to fall through and behave as if the IO was pending.
            }
            else
            {
                // Get the socket error (which may be IOPending)
                SocketError socketError = SocketPal.GetLastSocketError();
                if (socketError != SocketError.IOPending)
                {
                    // Completed synchronously with a failure.
                    FreeNativeOverlapped(overlapped);
                    FinishOperationSyncFailure(socketError, bytesTransferred, SocketFlags.None);
                    return socketError;
                }

                // Fall through to IOPending handling for asynchronous completion.
            }

            // Socket handle is going to post a completion to the completion port (may have done so already).
            // Return pending and we will continue in the completion port callback.
            return SocketError.IOPending;
        }

        /// <summary>Handles the result of an IOCP operation.</summary>
        /// <param name="socketError">The result status of the operation, as returned from the API call.</param>
        /// <param name="bytesTransferred">The number of bytes transferred, if the operation completed synchronously and successfully.</param>
        /// <param name="overlapped">The overlapped to be freed if the operation completed synchronously.</param>
        /// <param name="cancellationToken">The cancellation token to use to cancel the operation.</param>
        /// <returns>The result status of the operation.</returns>
        private unsafe SocketError ProcessIOCPResultWithSingleBufferHandle(SocketError socketError, int bytesTransferred, NativeOverlapped* overlapped, CancellationToken cancellationToken = default)
        {
            // Note: We need to dispose of the overlapped iff the operation completed synchronously,
            // and if we do, we must do so before we mark the operation as completed.

            if (socketError == SocketError.Success)
            {
                // Synchronous success.
                if (_currentSocket!.SafeHandle.SkipCompletionPortOnSuccess)
                {
                    // The socket handle is configured to skip completion on success,
                    // so we can set the results right now.
                    _singleBufferHandleState = SingleBufferHandleState.None;
                    FreeNativeOverlapped(overlapped);
                    FinishOperationSyncSuccess(bytesTransferred, SocketFlags.None);
                    return SocketError.Success;
                }

                // Completed synchronously, but the handle wasn't marked as skip completion port on success,
                // so we still need to fall through and behave as if the IO was pending.
            }
            else
            {
                // Get the socket error (which may be IOPending)
                socketError = SocketPal.GetLastSocketError();
                if (socketError != SocketError.IOPending)
                {
                    // Completed synchronously with a failure.
                    _singleBufferHandleState = SingleBufferHandleState.None;
                    FreeNativeOverlapped(overlapped);
                    FinishOperationSyncFailure(socketError, bytesTransferred, SocketFlags.None);
                    return socketError;
                }

                // Fall through to IOPending handling for asynchronous completion.
            }

            // Socket handle is going to post a completion to the completion port (may have done so already).
            // Return pending and we will continue in the completion port callback.
            if (_singleBufferHandleState == SingleBufferHandleState.InProcess)
            {
                RegisterToCancelPendingIO(overlapped, cancellationToken); // must happen before we change state to Set to avoid race conditions
                _singleBufferHandle = _buffer.Pin();
                _singleBufferHandleState = SingleBufferHandleState.Set;
            }
            return SocketError.IOPending;
        }

        internal unsafe SocketError DoOperationAccept(Socket socket, SafeSocketHandle handle, SafeSocketHandle acceptHandle)
        {
            bool userBuffer = _count != 0;
            Debug.Assert(!userBuffer || (!_buffer.Equals(default) && _count >= _acceptAddressBufferCount));
            Memory<byte> buffer = userBuffer ? _buffer : _acceptBuffer;
            Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);

            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                _singleBufferHandle = buffer.Pin();
                _singleBufferHandleState = SingleBufferHandleState.Set;

                bool success = socket.AcceptEx(
                    handle,
                    acceptHandle,
                    userBuffer ? (IntPtr)((byte*)_singleBufferHandle.Pointer + _offset) : (IntPtr)_singleBufferHandle.Pointer,
                    userBuffer ? _count - _acceptAddressBufferCount : 0,
                    _acceptAddressBufferCount / 2,
                    _acceptAddressBufferCount / 2,
                    out int bytesTransferred,
                    overlapped);

                return ProcessIOCPResult(success, bytesTransferred, overlapped);
            }
            catch
            {
                _singleBufferHandleState = SingleBufferHandleState.None;
                FreeNativeOverlapped(overlapped);
                _singleBufferHandle.Dispose();
                throw;
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
            // ConnectEx uses a sockaddr buffer containing the remote address to which to connect.
            // It can also optionally take a single buffer of data to send after the connection is complete.
            // The sockaddr is pinned with a GCHandle to avoid having to use the object array form of UnsafePack.
            PinSocketAddressBuffer();

            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);
                _singleBufferHandle = _buffer.Pin();
                _singleBufferHandleState = SingleBufferHandleState.Set;

                bool success = socket.ConnectEx(
                    handle,
                    PtrSocketAddressBuffer,
                    _socketAddress!.Size,
                    (IntPtr)((byte*)_singleBufferHandle.Pointer + _offset),
                    _count,
                    out int bytesTransferred,
                    overlapped);

                return ProcessIOCPResult(success, bytesTransferred, overlapped);
            }
            catch
            {
                _singleBufferHandleState = SingleBufferHandleState.None;
                FreeNativeOverlapped(overlapped);
                _singleBufferHandle.Dispose();
                throw;
            }
        }

        internal unsafe SocketError DoOperationDisconnect(Socket socket, SafeSocketHandle handle)
        {
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                bool success = socket.DisconnectEx(
                    handle,
                    overlapped,
                    (int)(DisconnectReuseSocket ? TransmitFileOptions.ReuseSocket : 0),
                    0);

                return ProcessIOCPResult(success, 0, overlapped);
            }
            catch
            {
                FreeNativeOverlapped(overlapped);
                throw;
            }
        }

        internal SocketError DoOperationReceive(SafeSocketHandle handle, CancellationToken cancellationToken) => _bufferList == null ?
            DoOperationReceiveSingleBuffer(handle, cancellationToken) :
            DoOperationReceiveMultiBuffer(handle);

        internal unsafe SocketError DoOperationReceiveSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
            {
                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None, $"Expected None, got {_singleBufferHandleState}");
                    _singleBufferHandleState = SingleBufferHandleState.InProcess;
                    var wsaBuffer = new WSABuffer { Length = _count, Pointer = (IntPtr)(bufferPtr + _offset) };

                    SocketFlags flags = _socketFlags;
                    SocketError socketError = Interop.Winsock.WSARecv(
                        handle,
                        ref wsaBuffer,
                        1,
                        out int bytesTransferred,
                        ref flags,
                        overlapped,
                        IntPtr.Zero);

                    return ProcessIOCPResultWithSingleBufferHandle(socketError, bytesTransferred, overlapped, cancellationToken);
                }
                catch
                {
                    _singleBufferHandleState = SingleBufferHandleState.None;
                    FreeNativeOverlapped(overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationReceiveMultiBuffer(SafeSocketHandle handle)
        {
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

                return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, overlapped);
            }
            catch
            {
                FreeNativeOverlapped(overlapped);
                throw;
            }
        }

        internal unsafe SocketError DoOperationReceiveFrom(SafeSocketHandle handle)
        {
            // WSARecvFrom uses a WSABuffer array describing buffers in which to
            // receive data and from which to send data respectively. Single and multiple buffers
            // are handled differently so as to optimize performance for the more common single buffer case.
            // WSARecvFrom and WSASendTo also uses a sockaddr buffer in which to store the address from which the data was received.
            // The sockaddr is pinned with a GCHandle to avoid having to use the object array form of UnsafePack.
            PinSocketAddressBuffer();

            return _bufferList == null ?
                DoOperationReceiveFromSingleBuffer(handle) :
                DoOperationReceiveFromMultiBuffer(handle);
        }

        internal unsafe SocketError DoOperationReceiveFromSingleBuffer(SafeSocketHandle handle)
        {
            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
            {
                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);
                    _singleBufferHandleState = SingleBufferHandleState.InProcess;
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

                    return ProcessIOCPResultWithSingleBufferHandle(socketError, bytesTransferred, overlapped);
                }
                catch
                {
                    _singleBufferHandleState = SingleBufferHandleState.None;
                    FreeNativeOverlapped(overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationReceiveFromMultiBuffer(SafeSocketHandle handle)
        {
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

                return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, overlapped);
            }
            catch
            {
                FreeNativeOverlapped(overlapped);
                throw;
            }
        }

        internal unsafe SocketError DoOperationReceiveMessageFrom(Socket socket, SafeSocketHandle handle)
        {
            // WSARecvMsg uses a WSAMsg descriptor.
            // The WSAMsg buffer is a pinned array to avoid complicating the use of Overlapped.
            // WSAMsg contains a pointer to a sockaddr.
            // The sockaddr is pinned with a GCHandle to avoid complicating the use of Overlapped.
            // WSAMsg contains a pointer to a WSABuffer array describing data buffers.
            // WSAMsg also contains a single WSABuffer describing a control buffer.
            PinSocketAddressBuffer();

            // Create a WSAMessageBuffer if none exists yet.
            if (_wsaMessageBufferPinned == null)
            {
                _wsaMessageBufferPinned = GC.AllocateUninitializedArray<byte>(sizeof(Interop.Winsock.WSAMsg), pinned: true);
            }

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
                if (_wsaRecvMsgWSABufferArrayPinned == null)
                {
                    _wsaRecvMsgWSABufferArrayPinned = GC.AllocateUninitializedArray<WSABuffer>(1, pinned: true);
                }

                Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);
                _singleBufferHandle = _buffer.Pin();
                _singleBufferHandleState = SingleBufferHandleState.Set;

                _wsaRecvMsgWSABufferArrayPinned[0].Pointer = (IntPtr)_singleBufferHandle.Pointer;
                _wsaRecvMsgWSABufferArrayPinned[0].Length = _count;
                wsaRecvMsgWSABufferArray = _wsaRecvMsgWSABufferArrayPinned;
                wsaRecvMsgWSABufferCount = 1;
            }
            else
            {
                // Use the multi-buffer WSABuffer.
                wsaRecvMsgWSABufferArray = _wsaBufferArrayPinned!;
                wsaRecvMsgWSABufferCount = (uint)_bufferListInternal!.Count;
            }

            // Fill in WSAMessageBuffer.
            unsafe
            {
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
            }

            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                SocketError socketError = socket.WSARecvMsg(
                    handle,
                    Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0),
                    out int bytesTransferred,
                    overlapped,
                    IntPtr.Zero);

                return ProcessIOCPResultWithSingleBufferHandle(socketError, bytesTransferred, overlapped);
            }
            catch
            {
                _singleBufferHandleState = SingleBufferHandleState.None;
                FreeNativeOverlapped(overlapped);
                _singleBufferHandle.Dispose();
                throw;
            }
        }

        internal unsafe SocketError DoOperationSend(SafeSocketHandle handle, CancellationToken cancellationToken) => _bufferList == null ?
            DoOperationSendSingleBuffer(handle, cancellationToken) :
            DoOperationSendMultiBuffer(handle);

        internal unsafe SocketError DoOperationSendSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
            {
                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);
                    _singleBufferHandleState = SingleBufferHandleState.InProcess;
                    var wsaBuffer = new WSABuffer { Length = _count, Pointer = (IntPtr)(bufferPtr + _offset) };

                    SocketError socketError = Interop.Winsock.WSASend(
                        handle,
                        ref wsaBuffer,
                        1,
                        out int bytesTransferred,
                        _socketFlags,
                        overlapped,
                        IntPtr.Zero);

                    return ProcessIOCPResultWithSingleBufferHandle(socketError, bytesTransferred, overlapped, cancellationToken);
                }
                catch
                {
                    _singleBufferHandleState = SingleBufferHandleState.None;
                    FreeNativeOverlapped(overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationSendMultiBuffer(SafeSocketHandle handle)
        {
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

                return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, overlapped);
            }
            catch
            {
                FreeNativeOverlapped(overlapped);
                throw;
            }
        }

        internal unsafe SocketError DoOperationSendPackets(Socket socket, SafeSocketHandle handle)
        {
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
                    else if (spe.Buffer != null && spe.Count > 0)
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
                _sendPacketsFileStreams = new FileStream[sendPacketsElementsFileCount];
                try
                {
                    foreach (SendPacketsElement spe in sendPacketsElementsCopy)
                    {
                        if (spe?.FilePath != null)
                        {
                            // Create a FileStream to open the file.
                            _sendPacketsFileStreams[index] =
                                new FileStream(spe.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                            // Get the file handle from the stream.
                            index++;
                        }
                    }
                }
                catch
                {
                    // Got an exception opening a file - close any open streams, then throw.
                    for (int i = index - 1; i >= 0; i--)
                        _sendPacketsFileStreams[i].Dispose();
                    _sendPacketsFileStreams = null;
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

                return ProcessIOCPResult(result, 0, overlapped);
            }
            catch
            {
                FreeNativeOverlapped(overlapped);
                throw;
            }
        }

        internal unsafe SocketError DoOperationSendTo(SafeSocketHandle handle)
        {
            // WSASendTo uses a WSABuffer array describing buffers in which to
            // receive data and from which to send data respectively. Single and multiple buffers
            // are handled differently so as to optimize performance for the more common single buffer case.
            //
            // WSARecvFrom and WSASendTo also uses a sockaddr buffer in which to store the address from which the data was received.
            // The sockaddr is pinned with a GCHandle to avoid having to use the object array form of UnsafePack.
            PinSocketAddressBuffer();

            return _bufferList == null ?
                DoOperationSendToSingleBuffer(handle) :
                DoOperationSendToMultiBuffer(handle);
        }

        internal unsafe SocketError DoOperationSendToSingleBuffer(SafeSocketHandle handle)
        {
            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
            {
                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);
                    _singleBufferHandleState = SingleBufferHandleState.InProcess;
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

                    return ProcessIOCPResultWithSingleBufferHandle(socketError, bytesTransferred, overlapped);
                }
                catch
                {
                    _singleBufferHandleState = SingleBufferHandleState.None;
                    FreeNativeOverlapped(overlapped);
                    throw;
                }
            }
        }

        internal unsafe SocketError DoOperationSendToMultiBuffer(SafeSocketHandle handle)
        {
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

                return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, overlapped);
            }
            catch
            {
                FreeNativeOverlapped(overlapped);
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

#if DEBUG
                    if (_multipleBufferGCHandles != null)
                    {
                        foreach (GCHandle gcHandle in _multipleBufferGCHandles)
                        {
                            Debug.Assert(!gcHandle.IsAllocated);
                        }
                    }
#endif

                    // Number of things to pin is number of buffers.
                    // Ensure we have properly sized object array.
                    if (_multipleBufferGCHandles == null || (_multipleBufferGCHandles.Length < bufferCount))
                    {
                        _multipleBufferGCHandles = new GCHandle[bufferCount];
                    }

                    // Pin the buffers.
                    for (int i = 0; i < bufferCount; i++)
                    {
                        Debug.Assert(!_multipleBufferGCHandles[i].IsAllocated);
                        _multipleBufferGCHandles[i] = GCHandle.Alloc(_bufferListInternal[i].Array, GCHandleType.Pinned);
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
                _preAllocatedOverlapped.Dispose();
                _preAllocatedOverlapped = null!;
            }
        }

        private void FreePinHandles()
        {
            _pinState = PinState.None;

            if (_singleBufferHandleState != SingleBufferHandleState.None)
            {
                _singleBufferHandleState = SingleBufferHandleState.None;
                _singleBufferHandle.Dispose();
            }

            if (_multipleBufferGCHandles != null)
            {
                for (int i = 0; i < _multipleBufferGCHandles.Length; i++)
                {
                    if (_multipleBufferGCHandles[i].IsAllocated)
                    {
                        _multipleBufferGCHandles[i].Free();
                    }
                }
            }

            if (_socketAddressGCHandle.IsAllocated)
            {
                _socketAddressGCHandle.Free();
                _pinnedSocketAddress = null;
            }
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
#if DEBUG
            if (_multipleBufferGCHandles != null)
            {
                foreach (GCHandle gcHandle in _multipleBufferGCHandles)
                {
                    Debug.Assert(!gcHandle.IsAllocated);
                }
            }
#endif

            if (_multipleBufferGCHandles == null || (_multipleBufferGCHandles.Length < sendPacketsElementsBufferCount))
            {
                _multipleBufferGCHandles = new GCHandle[sendPacketsElementsBufferCount];
            }

            // Pin user specified buffers.
            int index = 0;
            foreach (SendPacketsElement spe in sendPacketsElementsCopy)
            {
                if (spe?.Buffer != null && spe.Count > 0)
                {
                    Debug.Assert(!_multipleBufferGCHandles[index].IsAllocated);
                    _multipleBufferGCHandles[index] = GCHandle.Alloc(spe.Buffer, GCHandleType.Pinned);

                    index++;
                }
            }

            // Fill in native descriptor.
            int descriptorIndex = 0;
            int fileIndex = 0;
            foreach (SendPacketsElement spe in sendPacketsElementsCopy)
            {
                if (spe != null)
                {
                    if (spe.Buffer != null && spe.Count > 0)
                    {
                        // This element is a buffer.
                        sendPacketsDescriptorPinned[descriptorIndex].buffer = Marshal.UnsafeAddrOfPinnedArrayElement(spe.Buffer, spe.Offset);
                        sendPacketsDescriptorPinned[descriptorIndex].length = (uint)spe.Count;
                        sendPacketsDescriptorPinned[descriptorIndex].flags =
                            Interop.Winsock.TransmitPacketsElementFlags.Memory | (spe.EndOfPacket
                                ? Interop.Winsock.TransmitPacketsElementFlags.EndOfPacket
                                : 0);
                        descriptorIndex++;
                    }
                    else if (spe.FilePath != null)
                    {
                        // This element is a file.
                        sendPacketsDescriptorPinned[descriptorIndex].fileHandle = _sendPacketsFileStreams![fileIndex].SafeFileHandle.DangerousGetHandle();
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

                Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.Set);
                bool userBuffer = _count >= _acceptAddressBufferCount;

                _currentSocket.GetAcceptExSockaddrs(
                    userBuffer ? (IntPtr)((byte*)_singleBufferHandle.Pointer + _offset) : (IntPtr)_singleBufferHandle.Pointer,
                    _count != 0 ? _count - _acceptAddressBufferCount : 0,
                    _acceptAddressBufferCount / 2,
                    _acceptAddressBufferCount / 2,
                    out localAddr,
                    out localAddrLength,
                    out remoteAddr,
                    out remoteSocketAddress.InternalSize
                    );
                Marshal.Copy(remoteAddr, remoteSocketAddress.Buffer, 0, remoteSocketAddress.Size);

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
            if (_singleBufferHandleState != SingleBufferHandleState.None)
            {
                CompleteCoreSpin();
            }

            void CompleteCoreSpin() // separate out to help inline the fast path
            {
                // The operation could complete so quickly that it races with the code
                // initiating it.  Wait until that initiation code has completed before
                // we try to undo the state it configures.
                SpinWait sw = default;
                while (_singleBufferHandleState == SingleBufferHandleState.InProcess)
                {
                    sw.SpinOnce();
                }

                // Remove any cancellation registration.  First dispose the registration
                // to ensure that cancellation will either never fine or will have completed
                // firing before we continue.  Only then can we safely null out the overlapped.
                _registrationToCancelPendingIO.Dispose();
                unsafe
                {
                    _pendingOverlappedForCancellation = null;
                }

                // Release any GC handles.
                if (_singleBufferHandleState == SingleBufferHandleState.Set)
                {
                    _singleBufferHandleState = SingleBufferHandleState.None;
                    _singleBufferHandle.Dispose();
                }
            }
        }

        private unsafe void FinishOperationReceiveMessageFrom()
        {
            Interop.Winsock.WSAMsg* PtrMessage = (Interop.Winsock.WSAMsg*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned!, 0);

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
            if (_sendPacketsFileStreams != null)
            {
                for (int i = 0; i < _sendPacketsFileStreams.Length; i++)
                {
                    _sendPacketsFileStreams[i]?.Dispose();
                }

                _sendPacketsFileStreams = null;
            }
        }

        private static readonly unsafe IOCompletionCallback s_completionPortCallback = delegate (uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            var saeaBox = (StrongBox<SocketAsyncEventArgs>)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped)!;
            Debug.Assert(saeaBox.Value != null);
            SocketAsyncEventArgs saea = saeaBox.Value;

            if ((SocketError)errorCode == SocketError.Success)
            {
                saea.FreeNativeOverlapped(nativeOverlapped);
                saea.FinishOperationAsyncSuccess((int)numBytes, SocketFlags.None);
            }
            else
            {
                saea.HandleCompletionPortCallbackError(errorCode, numBytes, nativeOverlapped);
            }
        };

        private unsafe void HandleCompletionPortCallbackError(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            SocketError socketError = (SocketError)errorCode;
            SocketFlags socketFlags = SocketFlags.None;

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
                        // The Async IO completed with a failure.
                        // here we need to call WSAGetOverlappedResult() just so GetLastSocketError() will return the correct error.
                        Interop.Winsock.WSAGetOverlappedResult(
                            _currentSocket.SafeHandle,
                            nativeOverlapped,
                            out numBytes,
                            false,
                            out socketFlags);
                        socketError = SocketPal.GetLastSocketError();
                    }
                    catch
                    {
                        // _currentSocket.Disposed check above does not always work since this code is subject to race conditions.
                        socketError = SocketError.OperationAborted;
                    }
                }
            }

            FreeNativeOverlapped(nativeOverlapped);
            FinishOperationAsyncFailure(socketError, (int)numBytes, socketFlags);
        }
    }
}
