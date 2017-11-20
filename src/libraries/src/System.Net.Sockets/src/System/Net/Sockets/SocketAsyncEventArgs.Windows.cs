// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    public partial class SocketAsyncEventArgs : EventArgs, IDisposable
    {
        // Single buffer
        private MemoryHandle _singleBufferHandle;
        private volatile SingleBufferHandleState _singleBufferHandleState;
        private enum SingleBufferHandleState : byte
        {
            None,
            InProcess,
            Set
        }

        // BufferList property variables.
        // Note that these arrays are allocated and then grown as necessary, but never shrunk.
        // Thus the actual in-use length is defined by _bufferListInternal.Count, not the length of these arrays.
        private WSABuffer[] _wsaBufferArray;
        private GCHandle[] _multipleBufferGCHandles;

        // Internal buffers for WSARecvMsg
        private byte[] _wsaMessageBuffer;
        private GCHandle _wsaMessageBufferGCHandle;
        private byte[] _controlBuffer;
        private GCHandle _controlBufferGCHandle;
        private WSABuffer[] _wsaRecvMsgWSABufferArray;
        private GCHandle _wsaRecvMsgWSABufferArrayGCHandle;

        // Internal SocketAddress buffer
        private GCHandle _socketAddressGCHandle;
        private Internals.SocketAddress _pinnedSocketAddress;

        // SendPacketsElements property variables.
        private SendPacketsElement[] _sendPacketsElementsInternal;
        private Interop.Winsock.TransmitPacketsElement[] _sendPacketsDescriptor;
        private int _sendPacketsElementsFileCount;
        private int _sendPacketsElementsBufferCount;

        // Internal variables for SendPackets
        private FileStream[] _sendPacketsFileStreams;
        private SafeHandle[] _sendPacketsFileHandles;

        // Overlapped object related variables.
        private PreAllocatedOverlapped _preAllocatedOverlapped;

        private enum PinState : byte
        {
            None = 0,
            MultipleBuffer,
            SendPackets
        }
        private PinState _pinState;

        internal int? SendPacketsDescriptorCount
        {
            get
            {
                return _sendPacketsDescriptor == null ? null : (int?)_sendPacketsDescriptor.Length;
            }
        }

        private void InitializeInternals()
        {
            _preAllocatedOverlapped = new PreAllocatedOverlapped(s_completionPortCallback, this, null);
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"new PreAllocatedOverlapped {_preAllocatedOverlapped}");

            // Zero tells TransmitPackets to select a default send size.
            _sendPacketsSendSize = 0;
        }

        private void FreeInternals()
        {
            FreePinHandles();

            // Free native overlapped data.
            FreeOverlapped();
        }

        private void SetupMultipleBuffers()
        {
            CheckPinMultipleBuffers();
        }

        private void SetupSendPacketsElements()
        {
            _sendPacketsElementsInternal = null;
        }

        private unsafe NativeOverlapped* AllocateNativeOverlapped()
        {
            Debug.Assert(_currentSocket != null, "_currentSocket is null");
            Debug.Assert(_currentSocket.SafeHandle != null, "_currentSocket.SafeHandle is null");
            Debug.Assert(!_currentSocket.SafeHandle.IsInvalid, "_currentSocket.SafeHandle is invalid");
            Debug.Assert(_preAllocatedOverlapped != null, "_preAllocatedOverlapped is null");

            ThreadPoolBoundHandle boundHandle = _currentSocket.GetOrAllocateThreadPoolBoundHandle();
            return boundHandle.AllocateNativeOverlapped(_preAllocatedOverlapped);
        }

        private unsafe void FreeNativeOverlapped(NativeOverlapped* overlapped)
        {
            Debug.Assert(overlapped != null, "overlapped is null");
            Debug.Assert(_currentSocket != null, "_currentSocket is null");
            Debug.Assert(_currentSocket.SafeHandle != null, "_currentSocket.SafeHandle is null");
            Debug.Assert(_currentSocket.SafeHandle.IOCPBoundHandle != null, "_currentSocket.SafeHandle.IOCPBoundHandle is null");
            Debug.Assert(_preAllocatedOverlapped != null, "_preAllocatedOverlapped is null");

            _currentSocket.SafeHandle.IOCPBoundHandle.FreeNativeOverlapped(overlapped);
        }

        private unsafe void FreeNativeOverlappedIfNotPending(NativeOverlapped* overlapped, SocketError error)
        {
            if (error != SocketError.IOPending)
            {
                FreeNativeOverlapped(overlapped);
            }
        }

        private SocketError ProcessIOCPResult(bool success, int bytesTransferred)
        {
            if (success)
            {
                // Synchronous success.
                if (_currentSocket.SafeHandle.SkipCompletionPortOnSuccess)
                {
                    // The socket handle is configured to skip completion on success, 
                    // so we can set the results right now.
                    FinishOperationSyncSuccess(bytesTransferred, SocketFlags.None);
                    return SocketError.Success;
                }
            }
            else
            {
                // Get the socket error (which may be IOPending)
                SocketError socketError = SocketPal.GetLastSocketError();
                if (socketError != SocketError.IOPending)
                {
                    FinishOperationSyncFailure(socketError, bytesTransferred, SocketFlags.None);
                    return socketError;
                }
            }

            // Socket handle is going to post a completion to the completion port (may have done so already).
            // Return pending and we will continue in the completion port callback.
            return SocketError.IOPending;
        }

        private SocketError ProcessIOCPResultWithSingleBufferHandle(SocketError socketError, int bytesTransferred)
        {
            if (socketError == SocketError.Success)
            {
                // Synchronous success.
                if (_currentSocket.SafeHandle.SkipCompletionPortOnSuccess)
                {
                    // The socket handle is configured to skip completion on success, 
                    // so we can set the results right now.
                    _singleBufferHandleState = SingleBufferHandleState.None;
                    FinishOperationSyncSuccess(bytesTransferred, SocketFlags.None);
                    return SocketError.Success;
                }
            }
            else
            {
                // Get the socket error (which may be IOPending)
                socketError = SocketPal.GetLastSocketError();
                if (socketError != SocketError.IOPending)
                {
                    _singleBufferHandleState = SingleBufferHandleState.None;
                    FinishOperationSyncFailure(socketError, bytesTransferred, SocketFlags.None);
                    return socketError;
                }
            }

            // Socket handle is going to post a completion to the completion port (may have done so already).
            // Return pending and we will continue in the completion port callback.
            if (_singleBufferHandleState == SingleBufferHandleState.InProcess)
            {
                _singleBufferHandle = _buffer.Retain(pin: true);
                _singleBufferHandleState = SingleBufferHandleState.Set;
            }
            return SocketError.IOPending;
        }

        private void InnerStartOperationAccept() { }

        internal unsafe SocketError DoOperationAccept(Socket socket, SafeCloseSocket handle, SafeCloseSocket acceptHandle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                bool userBuffer = _count != 0;
                Debug.Assert(!userBuffer || (!_buffer.Equals(default) && _count >= _acceptAddressBufferCount));
                Memory<byte> buffer = userBuffer ? _buffer : _acceptBuffer;
                Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);
                _singleBufferHandle = buffer.Retain(pin: true);
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

                socketError = ProcessIOCPResult(success, bytesTransferred);
                return socketError;
            }
            catch
            {
                _singleBufferHandle.Dispose();
                _singleBufferHandleState = SingleBufferHandleState.None;
                throw;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        private void InnerStartOperationConnect()
        {
            // ConnectEx uses a sockaddr buffer containing the remote address to which to connect.
            // It can also optionally take a single buffer of data to send after the connection is complete.
            // The sockaddr is pinned with a GCHandle to avoid having to use the object array form of UnsafePack.
            PinSocketAddressBuffer();
        }

        internal unsafe SocketError DoOperationConnect(Socket socket, SafeCloseSocket handle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);
                _singleBufferHandle = _buffer.Retain(pin: true);
                _singleBufferHandleState = SingleBufferHandleState.Set;

                bool success = socket.ConnectEx(
                    handle,
                    PtrSocketAddressBuffer,
                    _socketAddress.Size,
                    (IntPtr)((byte*)_singleBufferHandle.Pointer + _offset),
                    _count,
                    out int bytesTransferred,
                    overlapped);

                socketError = ProcessIOCPResult(success, bytesTransferred);
                return socketError;
            }
            catch
            {
                _singleBufferHandle.Dispose();
                _singleBufferHandleState = SingleBufferHandleState.None;
                throw;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        internal unsafe SocketError DoOperationDisconnect(Socket socket, SafeCloseSocket handle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                bool success = socket.DisconnectEx(
                    handle,
                    overlapped,
                    (int)(DisconnectReuseSocket ? TransmitFileOptions.ReuseSocket : 0),
                    0);

                socketError = ProcessIOCPResult(success, 0);
                return socketError;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        private void InnerStartOperationReceive()
        {
        }

        internal SocketError DoOperationReceive(SafeCloseSocket handle, out SocketFlags flags) => _bufferList == null ? 
            DoOperationReceiveSingleBuffer(handle, out flags) :
            DoOperationReceiveMultiBuffer(handle, out flags);

        internal unsafe SocketError DoOperationReceiveSingleBuffer(SafeCloseSocket handle, out SocketFlags flags)
        {
            flags = _socketFlags;
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                fixed (byte* bufferPtr = &_buffer.Span.DangerousGetPinnableReference())
                {
                    Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None, $"Expected None, got {_singleBufferHandleState}");
                    _singleBufferHandleState = SingleBufferHandleState.InProcess;
                    var wsaBuffer = new WSABuffer { Length = _count, Pointer = (IntPtr)(bufferPtr + _offset) };

                    socketError = Interop.Winsock.WSARecv(
                        handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                        ref wsaBuffer,
                        1,
                        out int bytesTransferred,
                        ref flags,
                        overlapped,
                        IntPtr.Zero);
                    GC.KeepAlive(handle); // small extra safe guard against handle getting collected/finalized while P/Invoke in progress

                    socketError = ProcessIOCPResultWithSingleBufferHandle(socketError, bytesTransferred);
                }
                return socketError;
            }
            catch
            {
                _singleBufferHandleState = SingleBufferHandleState.None;
                throw;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        internal unsafe SocketError DoOperationReceiveMultiBuffer(SafeCloseSocket handle, out SocketFlags flags)
        {
            flags = _socketFlags;
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                socketError = Interop.Winsock.WSARecv(
                    handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                    _wsaBufferArray,
                    _bufferListInternal.Count,
                    out int bytesTransferred,
                    ref flags,
                    overlapped,
                    IntPtr.Zero);
                GC.KeepAlive(handle); // small extra safe guard against handle getting collected/finalized while P/Invoke in progress

                socketError = ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred);
                return socketError;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        private void InnerStartOperationReceiveFrom()
        {
            // WSARecvFrom uses a WSABuffer array describing buffers in which to 
            // receive data and from which to send data respectively. Single and multiple buffers
            // are handled differently so as to optimize performance for the more common single buffer case.
            //
            // For a single buffer:
            //   The Overlapped.UnsafePack method is used that takes a single object to pin.
            //   A single WSABuffer that pre-exists in SocketAsyncEventArgs is used.
            //
            // For multiple buffers:
            //   The Overlapped.UnsafePack method is used that takes an array of objects to pin.
            //   An array to reference the multiple buffer is allocated.
            //   An array of WSABuffer descriptors is allocated.
            //
            // WSARecvFrom and WSASendTo also uses a sockaddr buffer in which to store the address from which the data was received.
            // The sockaddr is pinned with a GCHandle to avoid having to use the object array form of UnsafePack.
            PinSocketAddressBuffer();
        }

        internal unsafe SocketError DoOperationReceiveFrom(SafeCloseSocket handle, out SocketFlags flags) => _bufferList == null ?
            DoOperationReceiveFromSingleBuffer(handle, out flags) :
            DoOperationReceiveFromMultiBuffer(handle, out flags);

        internal unsafe SocketError DoOperationReceiveFromSingleBuffer(SafeCloseSocket handle, out SocketFlags flags)
        {
            flags = _socketFlags;

            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                fixed (byte* bufferPtr = &_buffer.Span.DangerousGetPinnableReference())
                {
                    Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);
                    _singleBufferHandleState = SingleBufferHandleState.InProcess;
                    var wsaBuffer = new WSABuffer { Length = _count, Pointer = (IntPtr)(bufferPtr + _offset) };

                    socketError = Interop.Winsock.WSARecvFrom(
                        handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                        ref wsaBuffer,
                        1,
                        out int bytesTransferred,
                        ref flags,
                        PtrSocketAddressBuffer,
                        PtrSocketAddressBufferSize,
                        overlapped,
                        IntPtr.Zero);
                    GC.KeepAlive(handle); // small extra safe guard against handle getting collected/finalized while P/Invoke in progress

                    socketError = ProcessIOCPResultWithSingleBufferHandle(socketError, bytesTransferred);
                    return socketError;
                }
            }
            catch
            {
                _singleBufferHandleState = SingleBufferHandleState.None;
                throw;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        internal unsafe SocketError DoOperationReceiveFromMultiBuffer(SafeCloseSocket handle, out SocketFlags flags)
        {
            flags = _socketFlags;

            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                socketError = Interop.Winsock.WSARecvFrom(
                    handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                    _wsaBufferArray,
                    _bufferListInternal.Count,
                    out int bytesTransferred,
                    ref flags,
                    PtrSocketAddressBuffer,
                    PtrSocketAddressBufferSize,
                    overlapped,
                    IntPtr.Zero);
                GC.KeepAlive(handle); // small extra safe guard against handle getting collected/finalized while P/Invoke in progress

                socketError = ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred);
                return socketError;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        private unsafe void InnerStartOperationReceiveMessageFrom()
        {
            // WSARecvMsg uses a WSAMsg descriptor.
            // The WSAMsg buffer is pinned with a GCHandle to avoid complicating the use of Overlapped.
            // WSAMsg contains a pointer to a sockaddr.  
            // The sockaddr is pinned with a GCHandle to avoid complicating the use of Overlapped.
            // WSAMsg contains a pointer to a WSABuffer array describing data buffers.
            // WSAMsg also contains a single WSABuffer describing a control buffer.
            PinSocketAddressBuffer();

            // Create a WSAMessageBuffer if none exists yet.
            if (_wsaMessageBuffer == null)
            {
                Debug.Assert(!_wsaMessageBufferGCHandle.IsAllocated);
                _wsaMessageBuffer = new byte[sizeof(Interop.Winsock.WSAMsg)];
            }

            // And ensure the WSAMessageBuffer is appropriately pinned.
            Debug.Assert(!_wsaMessageBufferGCHandle.IsAllocated || _wsaMessageBufferGCHandle.Target == _wsaMessageBuffer);
            if (!_wsaMessageBufferGCHandle.IsAllocated)
            {
                _wsaMessageBufferGCHandle = GCHandle.Alloc(_wsaMessageBuffer, GCHandleType.Pinned);
            }

            // Create and pin an appropriately sized control buffer if none already
            IPAddress ipAddress = (_socketAddress.Family == AddressFamily.InterNetworkV6 ? _socketAddress.GetIPAddress() : null);
            bool ipv4 = (_currentSocket.AddressFamily == AddressFamily.InterNetwork || (ipAddress != null && ipAddress.IsIPv4MappedToIPv6)); // DualMode
            bool ipv6 = _currentSocket.AddressFamily == AddressFamily.InterNetworkV6;

            if (ipv4 && (_controlBuffer == null || _controlBuffer.Length != sizeof(Interop.Winsock.ControlData)))
            {
                if (_controlBufferGCHandle.IsAllocated)
                {
                    _controlBufferGCHandle.Free();
                }
                _controlBuffer = new byte[sizeof(Interop.Winsock.ControlData)];
            }
            else if (ipv6 && (_controlBuffer == null || _controlBuffer.Length != sizeof(Interop.Winsock.ControlDataIPv6)))
            {
                if (_controlBufferGCHandle.IsAllocated)
                {
                    _controlBufferGCHandle.Free();
                }
                _controlBuffer = new byte[sizeof(Interop.Winsock.ControlDataIPv6)];
            }

            // If single buffer we need a single element WSABuffer.
            WSABuffer[] wsaRecvMsgWSABufferArray;
            uint wsaRecvMsgWSABufferCount;
            if (_bufferList == null)
            {
                if (_wsaRecvMsgWSABufferArray == null)
                {
                    _wsaRecvMsgWSABufferArray = new WSABuffer[1];
                }

                Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);
                _singleBufferHandle = _buffer.Retain(pin: true);
                _singleBufferHandleState = SingleBufferHandleState.Set;

                _wsaRecvMsgWSABufferArray[0].Pointer = (IntPtr)_singleBufferHandle.Pointer;
                _wsaRecvMsgWSABufferArray[0].Length = _count;
                wsaRecvMsgWSABufferArray = _wsaRecvMsgWSABufferArray;
                wsaRecvMsgWSABufferCount = 1;
            }
            else
            {
                // Use the multi-buffer WSABuffer.
                wsaRecvMsgWSABufferArray = _wsaBufferArray;
                wsaRecvMsgWSABufferCount = (uint)_bufferListInternal.Count;
            }

            // Ensure the array is pinned.
            Debug.Assert(!_wsaRecvMsgWSABufferArrayGCHandle.IsAllocated || _wsaRecvMsgWSABufferArrayGCHandle.Target == wsaRecvMsgWSABufferArray);
            if (!_wsaRecvMsgWSABufferArrayGCHandle.IsAllocated)
            {
                _wsaRecvMsgWSABufferArrayGCHandle = GCHandle.Alloc(wsaRecvMsgWSABufferArray, GCHandleType.Pinned);
            }

            // Fill in WSAMessageBuffer.
            unsafe
            {
                Interop.Winsock.WSAMsg* pMessage = (Interop.Winsock.WSAMsg*)PtrWSAMessageBuffer;
                pMessage->socketAddress = PtrSocketAddressBuffer;
                pMessage->addressLength = (uint)_socketAddress.Size;
                fixed (void* ptrWSARecvMsgWSABufferArray = &wsaRecvMsgWSABufferArray[0])
                {
                    pMessage->buffers = (IntPtr)ptrWSARecvMsgWSABufferArray;
                }
                pMessage->count = wsaRecvMsgWSABufferCount;

                if (_controlBuffer != null)
                {
                    Debug.Assert(_controlBuffer.Length > 0);
                    Debug.Assert(!_controlBufferGCHandle.IsAllocated || _controlBufferGCHandle.Target == _controlBuffer);
                    if (!_controlBufferGCHandle.IsAllocated)
                    {
                        _controlBufferGCHandle = GCHandle.Alloc(_controlBuffer, GCHandleType.Pinned);
                    }

                    fixed (void* ptrControlBuffer = &_controlBuffer[0])
                    {
                        pMessage->controlBuffer.Pointer = (IntPtr)ptrControlBuffer;
                    }
                    pMessage->controlBuffer.Length = _controlBuffer.Length;
                }
                pMessage->flags = _socketFlags;
            }
        }

        private unsafe IntPtr PtrWSAMessageBuffer
        {
            get
            {
                Debug.Assert(_wsaMessageBuffer != null);
                Debug.Assert(_wsaMessageBuffer.Length == sizeof(Interop.Winsock.WSAMsg));
                Debug.Assert(_wsaMessageBufferGCHandle.IsAllocated);
                Debug.Assert(_wsaMessageBufferGCHandle.Target == _wsaMessageBuffer);
                fixed (void* ptrWSAMessageBuffer = &_wsaMessageBuffer[0])
                {
                    return (IntPtr)ptrWSAMessageBuffer;
                }
            }
        }

        internal unsafe SocketError DoOperationReceiveMessageFrom(Socket socket, SafeCloseSocket handle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                socketError = socket.WSARecvMsg(
                    handle,
                    PtrWSAMessageBuffer,
                    out int bytesTransferred,
                    overlapped,
                    IntPtr.Zero);

                socketError = ProcessIOCPResultWithSingleBufferHandle(socketError, bytesTransferred);
                return socketError;
            }
            catch
            {
                _singleBufferHandle.Dispose();
                _singleBufferHandleState = SingleBufferHandleState.None;
                throw;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        private void InnerStartOperationSend()
        {
            // WSASend uses a WSABuffer array describing buffers of data to send.
            //
            // Single and multiple buffers are handled differently so as to optimize
            // performance for the more common single buffer case.  
            //
            // For a single buffer:
            //   The Overlapped.UnsafePack method is used that takes a single object to pin.
            //   A single WSABuffer that pre-exists in SocketAsyncEventArgs is used.
            //
            // For multiple buffers:
            //   The Overlapped.UnsafePack method is used that takes an array of objects to pin.
            //   An array to reference the multiple buffer is allocated.
            //   An array of WSABuffer descriptors is allocated.
        }

        internal unsafe SocketError DoOperationSend(SafeCloseSocket handle) => _bufferList == null ?
            DoOperationSendSingleBuffer(handle) :
            DoOperationSendMultiBuffer(handle);

        internal unsafe SocketError DoOperationSendSingleBuffer(SafeCloseSocket handle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                fixed (byte* bufferPtr = &_buffer.Span.DangerousGetPinnableReference())
                {
                    Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);
                    _singleBufferHandleState = SingleBufferHandleState.InProcess;
                    var wsaBuffer = new WSABuffer { Length = _count, Pointer = (IntPtr)(bufferPtr + _offset) };

                    socketError = Interop.Winsock.WSASend(
                        handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                        ref wsaBuffer,
                        1,
                        out int bytesTransferred,
                        _socketFlags,
                        overlapped,
                        IntPtr.Zero);
                    GC.KeepAlive(handle); // small extra safe guard against handle getting collected/finalized while P/Invoke in progress

                    socketError = ProcessIOCPResultWithSingleBufferHandle(socketError, bytesTransferred);
                    return socketError;
                }
            }
            catch
            {
                _singleBufferHandleState = SingleBufferHandleState.None;
                throw;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        internal unsafe SocketError DoOperationSendMultiBuffer(SafeCloseSocket handle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                socketError = Interop.Winsock.WSASend(
                    handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                    _wsaBufferArray,
                    _bufferListInternal.Count,
                    out int bytesTransferred,
                    _socketFlags,
                    overlapped,
                    IntPtr.Zero);
                GC.KeepAlive(handle); // small extra safe guard against handle getting collected/finalized while P/Invoke in progress

                socketError = ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred);
                return socketError;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        private void InnerStartOperationSendPackets()
        {
            // Prevent mutithreaded manipulation of the list.
            if (_sendPacketsElements != null)
            {
                _sendPacketsElementsInternal = (SendPacketsElement[])_sendPacketsElements.Clone();
            }

            // TransmitPackets uses an array of TRANSMIT_PACKET_ELEMENT structs as
            // descriptors for buffers and files to be sent.  It also takes a send size
            // and some flags.  The TRANSMIT_PACKET_ELEMENT for a file contains a native file handle.
            // This function basically opens the files to get the file handles, pins down any buffers
            // specified and builds the native TRANSMIT_PACKET_ELEMENT array that will be passed
            // to TransmitPackets.

            // Scan the elements to count files and buffers.
            _sendPacketsElementsFileCount = 0;
            _sendPacketsElementsBufferCount = 0;

            Debug.Assert(_sendPacketsElementsInternal != null);

            foreach (SendPacketsElement spe in _sendPacketsElementsInternal)
            {
                if (spe != null)
                {
                    if (spe._filePath != null)
                    {
                        _sendPacketsElementsFileCount++;
                    }
                    if (spe._buffer != null && spe._count > 0)
                    {
                        _sendPacketsElementsBufferCount++;
                    }
                }
            }

            // Attempt to open the files if any were given.
            if (_sendPacketsElementsFileCount > 0)
            {
                // Create arrays for streams and handles.
                _sendPacketsFileStreams = new FileStream[_sendPacketsElementsFileCount];
                _sendPacketsFileHandles = new SafeHandle[_sendPacketsElementsFileCount];

                // Loop through the elements attempting to open each files and get its handle.
                int index = 0;
                foreach (SendPacketsElement spe in _sendPacketsElementsInternal)
                {
                    if (spe != null && spe._filePath != null)
                    {
                        Exception fileStreamException = null;
                        try
                        {
                            // Create a FileStream to open the file.
                            _sendPacketsFileStreams[index] =
                                new FileStream(spe._filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        }
                        catch (Exception ex)
                        {
                            // Save the exception to throw after closing any previous successful file opens.
                            fileStreamException = ex;
                        }
                        if (fileStreamException != null)
                        {
                            // Got an exception opening a file - do some cleanup then throw.
                            for (int i = 0; i < _sendPacketsElementsFileCount; i++)
                            {
                                // Drop handles.
                                _sendPacketsFileHandles[i] = null;

                                // Close any open streams.
                                if (_sendPacketsFileStreams[i] != null)
                                {
                                    _sendPacketsFileStreams[i].Dispose();
                                    _sendPacketsFileStreams[i] = null;
                                }
                            }
                            throw fileStreamException;
                        }

                        // Get the file handle from the stream.
                        _sendPacketsFileHandles[index] = _sendPacketsFileStreams[index].SafeFileHandle;
                        index++;
                    }
                }
            }

            CheckPinSendPackets();
        }

        internal unsafe SocketError DoOperationSendPackets(Socket socket, SafeCloseSocket handle)
        {
            Debug.Assert(_sendPacketsDescriptor != null);
            Debug.Assert(_sendPacketsDescriptor.Length > 0);
            Debug.Assert(_multipleBufferGCHandles != null);
            Debug.Assert(_multipleBufferGCHandles[0].IsAllocated);
            Debug.Assert(_multipleBufferGCHandles[0].Target == _sendPacketsDescriptor);
            IntPtr ptrSendPacketsDescriptor;
            fixed (void* p = &_sendPacketsDescriptor[0])
            {
                ptrSendPacketsDescriptor = (IntPtr)p;
            }

            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                bool result = socket.TransmitPackets(
                    handle,
                    ptrSendPacketsDescriptor,
                    _sendPacketsDescriptor.Length,
                    _sendPacketsSendSize,
                    overlapped,
                    _sendPacketsFlags);

                socketError = ProcessIOCPResult(result, 0);
                return socketError;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        private void InnerStartOperationSendTo()
        {
            // WSASendTo uses a WSABuffer array describing buffers in which to 
            // receive data and from which to send data respectively. Single and multiple buffers
            // are handled differently so as to optimize performance for the more common single buffer case.
            //
            // For a single buffer:
            //   The Overlapped.UnsafePack method is used that takes a single object to pin.
            //   A single WSABuffer that pre-exists in SocketAsyncEventArgs is used.
            //
            // For multiple buffers:
            //   The Overlapped.UnsafePack method is used that takes an array of objects to pin.
            //   An array to reference the multiple buffer is allocated.
            //   An array of WSABuffer descriptors is allocated.
            //
            // WSARecvFrom and WSASendTo also uses a sockaddr buffer in which to store the address from which the data was received.
            // The sockaddr is pinned with a GCHandle to avoid having to use the object array form of UnsafePack.
            PinSocketAddressBuffer();
        }

        internal unsafe SocketError DoOperationSendTo(SafeCloseSocket handle) => _bufferList == null ?
            DoOperationSendToSingleBuffer(handle) :
            DoOperationSendToMultiBuffer(handle);

        internal unsafe SocketError DoOperationSendToSingleBuffer(SafeCloseSocket handle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                fixed (byte* bufferPtr = &_buffer.Span.DangerousGetPinnableReference())
                {
                    Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);
                    _singleBufferHandleState = SingleBufferHandleState.InProcess;
                    var wsaBuffer = new WSABuffer { Length = _count, Pointer = (IntPtr)(bufferPtr + _offset) };

                    socketError = Interop.Winsock.WSASendTo(
                        handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                        ref wsaBuffer,
                        1,
                        out int bytesTransferred,
                        _socketFlags,
                        PtrSocketAddressBuffer,
                        _socketAddress.Size,
                        overlapped,
                        IntPtr.Zero);
                    GC.KeepAlive(handle); // small extra safe guard against handle getting collected/finalized while P/Invoke in progress

                    socketError = ProcessIOCPResultWithSingleBufferHandle(socketError, bytesTransferred);
                    return socketError;
                }
            }
            catch
            {
                _singleBufferHandleState = SingleBufferHandleState.None;
                throw;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        internal unsafe SocketError DoOperationSendToMultiBuffer(SafeCloseSocket handle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                socketError = Interop.Winsock.WSASendTo(
                    handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                    _wsaBufferArray,
                    _bufferListInternal.Count,
                    out int bytesTransferred,
                    _socketFlags,
                    PtrSocketAddressBuffer,
                    _socketAddress.Size,
                    overlapped,
                    IntPtr.Zero);
                GC.KeepAlive(handle); // small extra safe guard against handle getting collected/finalized while P/Invoke in progress

                socketError = ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred);
                return socketError;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        // Ensures Overlapped object exists with appropriate multiple buffers pinned.
        private void CheckPinMultipleBuffers()
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
                    SetupPinHandlesMultiple();
                }
                catch (Exception)
                {
                    FreePinHandles();
                    throw;
                }
            }
        }

        // Ensures Overlapped object exists with appropriate buffers pinned.
        private void CheckPinSendPackets()
        {
            if (_pinState != PinState.None)
            {
                FreePinHandles();
            }
            SetupPinHandlesSendPackets();
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
            _socketAddressGCHandle = GCHandle.Alloc(_socketAddress.Buffer, GCHandleType.Pinned);
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

        private IntPtr PtrSocketAddressBufferSize => PtrSocketAddressBuffer + _socketAddress.GetAddressSizeOffset();

        // Cleans up any existing Overlapped object and related state variables.
        private void FreeOverlapped()
        {
            // Free the preallocated overlapped object. This in turn will unpin
            // any pinned buffers.
            if (_preAllocatedOverlapped != null)
            {
                _preAllocatedOverlapped.Dispose();
                _preAllocatedOverlapped = null;
            }
        }

        private void FreePinHandles()
        {
            _pinState = PinState.None;

            if (_singleBufferHandleState != SingleBufferHandleState.None)
            {
                _singleBufferHandle.Dispose();
                _singleBufferHandleState = SingleBufferHandleState.None;
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

            if (_wsaMessageBufferGCHandle.IsAllocated)
            {
                _wsaMessageBufferGCHandle.Free();
            }

            if (_wsaRecvMsgWSABufferArrayGCHandle.IsAllocated)
            {
                _wsaRecvMsgWSABufferArrayGCHandle.Free();
            }

            if (_controlBufferGCHandle.IsAllocated)
            {
                _controlBufferGCHandle.Free();
            }
        }

        // Sets up an Overlapped object with multiple buffers pinned.
        private unsafe void SetupPinHandlesMultiple()
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

            if (_wsaBufferArray == null || _wsaBufferArray.Length < bufferCount)
            {
                _wsaBufferArray = new WSABuffer[bufferCount];
            }

            for (int i = 0; i < bufferCount; i++)
            {
                ArraySegment<byte> localCopy = _bufferListInternal[i];
                _wsaBufferArray[i].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(localCopy.Array, localCopy.Offset);
                _wsaBufferArray[i].Length = localCopy.Count;
            }
            _pinState = PinState.MultipleBuffer;
        }

        // Sets up an Overlapped object for SendPacketsAsync.
        private unsafe void SetupPinHandlesSendPackets()
        {
            int index;

            // Alloc native descriptor.
            _sendPacketsDescriptor =
                new Interop.Winsock.TransmitPacketsElement[_sendPacketsElementsFileCount + _sendPacketsElementsBufferCount];

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

            if (_multipleBufferGCHandles == null || (_multipleBufferGCHandles.Length < _sendPacketsElementsBufferCount + 1))
            {
                _multipleBufferGCHandles = new GCHandle[_sendPacketsElementsBufferCount + 1];
            }

            // Pin objects.  Native descriptor buffer first and then user specified buffers.
            Debug.Assert(!_multipleBufferGCHandles[0].IsAllocated);
            _multipleBufferGCHandles[0] = GCHandle.Alloc(_sendPacketsDescriptor, GCHandleType.Pinned);
            index = 1;
            foreach (SendPacketsElement spe in _sendPacketsElementsInternal)
            {
                if (spe != null && spe._buffer != null && spe._count > 0)
                {
                    Debug.Assert(!_multipleBufferGCHandles[index].IsAllocated);
                    _multipleBufferGCHandles[index] = GCHandle.Alloc(spe._buffer, GCHandleType.Pinned);

                    index++;
                }
            }

            // Fill in native descriptor.
            int descriptorIndex = 0;
            int fileIndex = 0;
            foreach (SendPacketsElement spe in _sendPacketsElementsInternal)
            {
                if (spe != null)
                {
                    if (spe._buffer != null && spe._count > 0)
                    {
                        // This element is a buffer.
                        _sendPacketsDescriptor[descriptorIndex].buffer = Marshal.UnsafeAddrOfPinnedArrayElement(spe._buffer, spe._offset);
                        _sendPacketsDescriptor[descriptorIndex].length = (uint)spe._count;
                        _sendPacketsDescriptor[descriptorIndex].flags = (Interop.Winsock.TransmitPacketsElementFlags)spe._flags;
                        descriptorIndex++;
                    }
                    else if (spe._filePath != null)
                    {
                        // This element is a file.
                        _sendPacketsDescriptor[descriptorIndex].fileHandle = _sendPacketsFileHandles[fileIndex].DangerousGetHandle();
                        _sendPacketsDescriptor[descriptorIndex].fileOffset = spe._offset;
                        _sendPacketsDescriptor[descriptorIndex].length = (uint)spe._count;
                        _sendPacketsDescriptor[descriptorIndex].flags = (Interop.Winsock.TransmitPacketsElementFlags)spe._flags;
                        fileIndex++;
                        descriptorIndex++;
                    }
                }
            }

            _pinState = PinState.SendPackets;
        }

        internal void LogBuffer(int size)
        {
            // This should only be called if tracing is enabled. However, there is the potential for a race
            // condition where tracing is disabled between a calling check and here, in which case the assert
            // may fire erroneously.
            Debug.Assert(NetEventSource.IsEnabled);

            if (_bufferList != null)
            {
                for (int i = 0; i < _bufferListInternal.Count; i++)
                {
                    WSABuffer wsaBuffer = _wsaBufferArray[i];
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

        internal void LogSendPacketsBuffers(int size)
        {
            if (!NetEventSource.IsEnabled) return;

            foreach (SendPacketsElement spe in _sendPacketsElementsInternal)
            {
                if (spe != null)
                {
                    if (spe._buffer != null && spe._count > 0)
                    {
                        // This element is a buffer.
                        NetEventSource.DumpBuffer(this, spe._buffer, spe._offset, Math.Min(spe._count, size));
                    }
                    else if (spe._filePath != null)
                    {
                        // This element is a file.
                        NetEventSource.NotLoggedFile(spe._filePath, _currentSocket, _completedOperation);
                    }
                }
            }
        }

        private unsafe SocketError FinishOperationAccept(Internals.SocketAddress remoteSocketAddress)
        {
            SocketError socketError;
            IntPtr localAddr;
            int localAddrLength;
            IntPtr remoteAddr;

            try
            {
                Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.Set);
                Debug.Assert(_singleBufferHandle.HasPointer);
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

                // Set the socket context.
                IntPtr handle = _currentSocket.SafeHandle.DangerousGetHandle();

                socketError = Interop.Winsock.setsockopt(
                    _acceptSocket.SafeHandle,
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

            return socketError;
        }

        private SocketError FinishOperationConnect()
        {
            SocketError socketError;

            // Update the socket context.
            try
            {
                socketError = Interop.Winsock.setsockopt(
                    _currentSocket.SafeHandle,
                    SocketOptionLevel.Socket,
                    SocketOptionName.UpdateConnectContext,
                    null,
                    0);
                if (socketError == SocketError.SocketError)
                {
                    socketError = SocketPal.GetLastSocketError();
                }
            }
            catch (ObjectDisposedException)
            {
                socketError = SocketError.OperationAborted;
            }

            return socketError;
        }

        private unsafe int GetSocketAddressSize()
        {
            return *(int*)PtrSocketAddressBufferSize;
        }

        private void CompleteCore()
        {
            if (_singleBufferHandleState != SingleBufferHandleState.None)
            {
                CompleteCoreSpin();
            }

            void CompleteCoreSpin() // separate out to help inline the fast path
            {
                var sw = new SpinWait();
                while (_singleBufferHandleState == SingleBufferHandleState.InProcess)
                {
                    sw.SpinOnce();
                }

                if (_singleBufferHandleState == SingleBufferHandleState.Set)
                {
                    _singleBufferHandle.Dispose();
                    _singleBufferHandleState = SingleBufferHandleState.None;
                }
            }
        }

        private unsafe void FinishOperationReceiveMessageFrom()
        {
            Interop.Winsock.WSAMsg* PtrMessage = (Interop.Winsock.WSAMsg*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBuffer, 0);

            if (_controlBuffer.Length == sizeof(Interop.Winsock.ControlData))
            {
                // IPv4.
                _receiveMessageFromPacketInfo = SocketPal.GetIPPacketInformation((Interop.Winsock.ControlData*)PtrMessage->controlBuffer.Pointer);
            }
            else if (_controlBuffer.Length == sizeof(Interop.Winsock.ControlDataIPv6))
            {
                // IPv6.
                _receiveMessageFromPacketInfo = SocketPal.GetIPPacketInformation((Interop.Winsock.ControlDataIPv6*)PtrMessage->controlBuffer.Pointer);
            }
            else
            {
                // Other.
                _receiveMessageFromPacketInfo = new IPPacketInformation();
            }
        }

        private void FinishOperationSendPackets()
        {
            // Close the files if open.
            if (_sendPacketsFileStreams != null)
            {
                for (int i = 0; i < _sendPacketsElementsFileCount; i++)
                {
                    // Drop handles.
                    _sendPacketsFileHandles[i] = null;

                    // Close any open streams.
                    if (_sendPacketsFileStreams[i] != null)
                    {
                        _sendPacketsFileStreams[i].Dispose();
                        _sendPacketsFileStreams[i] = null;
                    }
                }
            }
            _sendPacketsFileStreams = null;
            _sendPacketsFileHandles = null;
        }

        private static readonly unsafe IOCompletionCallback s_completionPortCallback = delegate (uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            var saea = (SocketAsyncEventArgs)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
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
                if (_currentSocket.CleanedUp)
                {
                    socketError = SocketError.OperationAborted;
                }
                else
                {
                    try
                    {
                        // The Async IO completed with a failure.
                        // here we need to call WSAGetOverlappedResult() just so GetLastSocketError() will return the correct error.
                        bool success = Interop.Winsock.WSAGetOverlappedResult(
                            _currentSocket.SafeHandle,
                            nativeOverlapped,
                            out numBytes,
                            false,
                            out socketFlags);
                        socketError = SocketPal.GetLastSocketError();
                    }
                    catch
                    {
                        // _currentSocket.CleanedUp check above does not always work since this code is subject to race conditions.
                        socketError = SocketError.OperationAborted;
                    }
                }
            }

            FreeNativeOverlapped(nativeOverlapped);
            FinishOperationAsyncFailure(socketError, (int)numBytes, socketFlags);
        }
    }
}
