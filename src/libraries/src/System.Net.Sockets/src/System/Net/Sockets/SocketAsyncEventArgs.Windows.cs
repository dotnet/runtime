// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    public partial class SocketAsyncEventArgs : EventArgs, IDisposable
    {
        // Buffer,Offset,Count property variables.
        private WSABuffer _wsaBuffer;
        private IntPtr _ptrSingleBuffer;
        private GCHandle _singleBufferGCHandle;

        // BufferList property variables.
        // Note that these arrays are allocated and then grown as necessary, but never shrunk.
        // Thus the actual in-use length is defined by _bufferListInternal.Count, not the length of these arrays.
        private WSABuffer[] _wsaBufferArray;
        private GCHandle[] _multipleBufferGCHandles;

        // Internal buffers for WSARecvMsg
        private byte[] _wsaMessageBuffer;
        private GCHandle _wsaMessageBufferGCHandle;
        private IntPtr _ptrWSAMessageBuffer;
        private byte[] _controlBuffer;
        private GCHandle _controlBufferGCHandle;
        private IntPtr _ptrControlBuffer;
        private WSABuffer[] _wsaRecvMsgWSABufferArray;
        private GCHandle _wsaRecvMsgWSABufferArrayGCHandle;
        private IntPtr _ptrWSARecvMsgWSABufferArray;

        // Internal buffer for AcceptEx when Buffer not supplied.
        private IntPtr _ptrAcceptBuffer;

        // Internal SocketAddress buffer
        private GCHandle _socketAddressGCHandle;
        private Internals.SocketAddress _pinnedSocketAddress;
        private IntPtr _ptrSocketAddressBuffer;
        private IntPtr _ptrSocketAddressBufferSize;

        // SendPacketsElements property variables.
        private SendPacketsElement[] _sendPacketsElementsInternal;
        private Interop.Winsock.TransmitPacketsElement[] _sendPacketsDescriptor;
        private int _sendPacketsElementsFileCount;
        private int _sendPacketsElementsBufferCount;

        // Internal variables for SendPackets
        private FileStream[] _sendPacketsFileStreams;
        private SafeHandle[] _sendPacketsFileHandles;
        private IntPtr _ptrSendPacketsDescriptor;

        // Overlapped object related variables.
        private PreAllocatedOverlapped _preAllocatedOverlapped;

        private enum PinState
        {
            None = 0,
            NoBuffer,
            SingleAcceptBuffer,
            SingleBuffer,
            MultipleBuffer,
            SendPackets
        }
        private PinState _pinState;
        private byte[] _pinnedAcceptBuffer;
        private byte[] _pinnedSingleBuffer;
        private int _pinnedSingleBufferOffset;
        private int _pinnedSingleBufferCount;

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

        private void SetupSingleBuffer()
        {
            CheckPinSingleBuffer(true);
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

                // Socket handle is going to post a completion to the completion port (may have done so already).
                // Return pending and we will continue in the completion port callback.
                return SocketError.IOPending;
            }

            // Get the socket error (which may be IOPending)
            SocketError errorCode = SocketPal.GetLastSocketError();

            if (errorCode == SocketError.IOPending)
            {
                return errorCode;
            }

            FinishOperationSyncFailure(errorCode, bytesTransferred, SocketFlags.None);

            return errorCode;
        }

        private void InnerStartOperationAccept(bool userSuppliedBuffer)
        {
            if (!userSuppliedBuffer)
            {
                CheckPinSingleBuffer(false);
            }
        }

        internal unsafe SocketError DoOperationAccept(Socket socket, SafeCloseSocket handle, SafeCloseSocket acceptHandle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                int bytesTransferred;

                bool success = socket.AcceptEx(
                    handle,
                    acceptHandle,
                    (_ptrSingleBuffer != IntPtr.Zero) ? _ptrSingleBuffer : _ptrAcceptBuffer,
                    (_ptrSingleBuffer != IntPtr.Zero) ? Count - _acceptAddressBufferCount : 0,
                    _acceptAddressBufferCount / 2,
                    _acceptAddressBufferCount / 2,
                    out bytesTransferred,
                    overlapped);

                socketError = ProcessIOCPResult(success, bytesTransferred);
                return socketError;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        private void InnerStartOperationConnect()
        {
            // ConnectEx uses a sockaddr buffer containing he remote address to which to connect.
            // It can also optionally take a single buffer of data to send after the connection is complete.
            //
            // The sockaddr is pinned with a GCHandle to avoid having to use the object array form of UnsafePack.
            // The optional buffer is pinned using the Overlapped.UnsafePack method that takes a single object to pin.
            PinSocketAddressBuffer();
            CheckPinNoBuffer();
        }

        internal unsafe SocketError DoOperationConnect(Socket socket, SafeCloseSocket handle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                int bytesTransferred;

                bool success = socket.ConnectEx(
                    handle,
                    _ptrSocketAddressBuffer,
                    _socketAddress.Size,
                    _ptrSingleBuffer,
                    Count,
                    out bytesTransferred,
                    overlapped);
                
                socketError = ProcessIOCPResult(success, bytesTransferred);
                return socketError;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        private void InnerStartOperationDisconnect()
        {
            CheckPinNoBuffer();
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
            // WWSARecv uses a WSABuffer array describing buffers of data to send.
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

        internal unsafe SocketError DoOperationReceive(SafeCloseSocket handle, out SocketFlags flags)
        {
            flags = _socketFlags;

            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                int bytesTransferred;

                if (_buffer != null)
                {
                    // Single buffer case.
                    socketError = Interop.Winsock.WSARecv(
                        handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                        ref _wsaBuffer,
                        1,
                        out bytesTransferred,
                        ref flags,
                        overlapped,
                        IntPtr.Zero);
                }
                else
                {
                    // Multi buffer case.
                    socketError = Interop.Winsock.WSARecv(
                        handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                        _wsaBufferArray,
                        _bufferListInternal.Count,
                        out bytesTransferred,
                        ref flags,
                        overlapped,
                        IntPtr.Zero);
                }
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
            // WSARecvFrom uses e a WSABuffer array describing buffers in which to 
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

        internal unsafe SocketError DoOperationReceiveFrom(SafeCloseSocket handle, out SocketFlags flags)
        {
            flags = _socketFlags;

            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                int bytesTransferred;

                if (_buffer != null)
                {
                    socketError = Interop.Winsock.WSARecvFrom(
                        handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                        ref _wsaBuffer,
                        1,
                        out bytesTransferred,
                        ref flags,
                        _ptrSocketAddressBuffer,
                        _ptrSocketAddressBufferSize,
                        overlapped,
                        IntPtr.Zero);
                }
                else
                {
                    socketError = Interop.Winsock.WSARecvFrom(
                        handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                        _wsaBufferArray,
                        _bufferListInternal.Count,
                        out bytesTransferred,
                        ref flags,
                        _ptrSocketAddressBuffer,
                        _ptrSocketAddressBufferSize,
                        overlapped,
                        IntPtr.Zero);
                }
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

            // Create and pin a WSAMessageBuffer if none already.
            if (_wsaMessageBuffer == null)
            {
                _wsaMessageBuffer = new byte[sizeof(Interop.Winsock.WSAMsg)];
                _wsaMessageBufferGCHandle = GCHandle.Alloc(_wsaMessageBuffer, GCHandleType.Pinned);
                _ptrWSAMessageBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBuffer, 0);
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
            if (!_controlBufferGCHandle.IsAllocated)
            {
                _controlBufferGCHandle = GCHandle.Alloc(_controlBuffer, GCHandleType.Pinned);
                _ptrControlBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(_controlBuffer, 0);
            }

            // If single buffer we need a pinned 1 element WSABuffer.
            if (_buffer != null)
            {
                if (_wsaRecvMsgWSABufferArray == null)
                {
                    _wsaRecvMsgWSABufferArray = new WSABuffer[1];
                }
                _wsaRecvMsgWSABufferArray[0].Pointer = _ptrSingleBuffer;
                _wsaRecvMsgWSABufferArray[0].Length = _count;
                _wsaRecvMsgWSABufferArrayGCHandle = GCHandle.Alloc(_wsaRecvMsgWSABufferArray, GCHandleType.Pinned);
                _ptrWSARecvMsgWSABufferArray = Marshal.UnsafeAddrOfPinnedArrayElement(_wsaRecvMsgWSABufferArray, 0);
            }
            else
            {
                // Just pin the multi-buffer WSABuffer.
                _wsaRecvMsgWSABufferArrayGCHandle = GCHandle.Alloc(_wsaBufferArray, GCHandleType.Pinned);
                _ptrWSARecvMsgWSABufferArray = Marshal.UnsafeAddrOfPinnedArrayElement(_wsaBufferArray, 0);
            }

            // Fill in WSAMessageBuffer.
            unsafe
            {
                Interop.Winsock.WSAMsg* pMessage = (Interop.Winsock.WSAMsg*)_ptrWSAMessageBuffer; ;
                pMessage->socketAddress = _ptrSocketAddressBuffer;
                pMessage->addressLength = (uint)_socketAddress.Size;
                pMessage->buffers = _ptrWSARecvMsgWSABufferArray;
                if (_buffer != null)
                {
                    pMessage->count = (uint)1;
                }
                else
                {
                    pMessage->count = (uint)_bufferListInternal.Count;
                }

                if (_controlBuffer != null)
                {
                    pMessage->controlBuffer.Pointer = _ptrControlBuffer;
                    pMessage->controlBuffer.Length = _controlBuffer.Length;
                }
                pMessage->flags = _socketFlags;
            }
        }

        internal unsafe SocketError DoOperationReceiveMessageFrom(Socket socket, SafeCloseSocket handle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                int bytesTransferred;

                socketError = socket.WSARecvMsg(
                    handle,
                    _ptrWSAMessageBuffer,
                    out bytesTransferred,
                    overlapped,
                    IntPtr.Zero);

                socketError = ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred);
                return socketError;
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

        internal unsafe SocketError DoOperationSend(SafeCloseSocket handle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                int bytesTransferred;

                if (_buffer != null)
                {
                    // Single buffer case.
                    socketError = Interop.Winsock.WSASend(
                        handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                        ref _wsaBuffer,
                        1,
                        out bytesTransferred,
                        _socketFlags,
                        overlapped,
                        IntPtr.Zero);
                }
                else
                {
                    // Multi buffer case.
                    socketError = Interop.Winsock.WSASend(
                        handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                        _wsaBufferArray,
                        _bufferListInternal.Count,
                        out bytesTransferred,
                        _socketFlags,
                        overlapped,
                        IntPtr.Zero);
                }
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
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                bool result = socket.TransmitPackets(
                    handle,
                    _ptrSendPacketsDescriptor,
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

        internal unsafe SocketError DoOperationSendTo(SafeCloseSocket handle)
        {
            SocketError socketError = SocketError.Success;
            NativeOverlapped* overlapped = AllocateNativeOverlapped();
            try
            {
                int bytesTransferred;

                if (_buffer != null)
                {
                    // Single buffer case.
                    socketError = Interop.Winsock.WSASendTo(
                        handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                        ref _wsaBuffer,
                        1,
                        out bytesTransferred,
                        _socketFlags,
                        _ptrSocketAddressBuffer,
                        _socketAddress.Size,
                        overlapped,
                        IntPtr.Zero);
                }
                else
                {
                    socketError = Interop.Winsock.WSASendTo(
                        handle.DangerousGetHandle(), // to minimize chances of handle recycling from misuse, this should use DangerousAddRef/Release, but it adds too much overhead
                        _wsaBufferArray,
                        _bufferListInternal.Count,
                        out bytesTransferred,
                        _socketFlags,
                        _ptrSocketAddressBuffer,
                        _socketAddress.Size,
                        overlapped,
                        IntPtr.Zero);
                }
                GC.KeepAlive(handle); // small extra safe guard against handle getting collected/finalized while P/Invoke in progress

                socketError = ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred);
                return socketError;
            }
            finally
            {
                FreeNativeOverlappedIfNotPending(overlapped, socketError);
            }
        }

        // Ensures Overlapped object exists for operations that need no data buffer.
        private void CheckPinNoBuffer()
        {
            // PreAllocatedOverlapped will be reused.
            if (_pinState == PinState.None)
            {
                SetupPinHandlesSingle(true);
            }
        }

        // Maintains pinned state of single buffer.
        private void CheckPinSingleBuffer(bool pinUsersBuffer)
        {
            if (pinUsersBuffer)
            {
                // Using app supplied buffer.
                if (_buffer == null)
                {
                    // No user buffer is set so unpin any existing single buffer pinning.
                    if (_pinState == PinState.SingleBuffer)
                    {
                        FreePinHandles();
                    }
                }
                else
                {
                    if (_pinState == PinState.SingleBuffer && _pinnedSingleBuffer == _buffer)
                    {
                        // This buffer is already pinned - update if offset or count has changed.
                        if (_offset != _pinnedSingleBufferOffset)
                        {
                            _pinnedSingleBufferOffset = _offset;
                            _ptrSingleBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, _offset);
                            _wsaBuffer.Pointer = _ptrSingleBuffer;
                        }
                        if (_count != _pinnedSingleBufferCount)
                        {
                            _pinnedSingleBufferCount = _count;
                            _wsaBuffer.Length = _count;
                        }
                    }
                    else
                    {
                        FreePinHandles();
                        SetupPinHandlesSingle(true);
                    }
                }
            }
            else
            {
                // Using internal accept buffer.
                if (!(_pinState == PinState.SingleAcceptBuffer) || !(_pinnedSingleBuffer == _acceptBuffer))
                {
                    // Not already pinned - so pin it.
                    FreePinHandles();
                    SetupPinHandlesSingle(false);
                }
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
            _ptrSocketAddressBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(_socketAddress.Buffer, 0);
            _ptrSocketAddressBufferSize = Marshal.UnsafeAddrOfPinnedArrayElement(_socketAddress.Buffer, _socketAddress.GetAddressSizeOffset());
            _pinnedSocketAddress = _socketAddress;
        }

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

            // Free any allocated GCHandles.
            if (_singleBufferGCHandle.IsAllocated)
            {
                _singleBufferGCHandle.Free();

                _pinnedAcceptBuffer = null;
                _pinnedSingleBuffer = null;
                _pinnedSingleBufferOffset = 0;
                _pinnedSingleBufferCount = 0;
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
                _ptrWSAMessageBuffer = IntPtr.Zero;
            }

            if (_wsaRecvMsgWSABufferArrayGCHandle.IsAllocated)
            {
                _wsaRecvMsgWSABufferArrayGCHandle.Free();
                _ptrWSARecvMsgWSABufferArray = IntPtr.Zero;
            }

            if (_controlBufferGCHandle.IsAllocated)
            {
                _controlBufferGCHandle.Free();
                _ptrControlBuffer = IntPtr.Zero;
            }
        }

        // Sets up an Overlapped object with either _buffer or _acceptBuffer pinned.
        private unsafe void SetupPinHandlesSingle(bool pinSingleBuffer)
        {
            Debug.Assert(!_singleBufferGCHandle.IsAllocated);

            // Pin buffer, get native pointers, and fill in WSABuffer descriptor.
            if (pinSingleBuffer)
            {
                if (_buffer != null)
                {
                    _singleBufferGCHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);

                    _pinnedSingleBuffer = _buffer;
                    _pinnedSingleBufferOffset = _offset;
                    _pinnedSingleBufferCount = _count;
                    _ptrSingleBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, _offset);
                    _ptrAcceptBuffer = IntPtr.Zero;
                    _wsaBuffer.Pointer = _ptrSingleBuffer;
                    _wsaBuffer.Length = _count;
                    _pinState = PinState.SingleBuffer;
                }
                else
                {
                    _pinnedSingleBuffer = null;
                    _pinnedSingleBufferOffset = 0;
                    _pinnedSingleBufferCount = 0;
                    _ptrSingleBuffer = IntPtr.Zero;
                    _ptrAcceptBuffer = IntPtr.Zero;
                    _wsaBuffer.Pointer = _ptrSingleBuffer;
                    _wsaBuffer.Length = _count;
                    _pinState = PinState.NoBuffer;
                }
            }
            else
            {
                _singleBufferGCHandle = GCHandle.Alloc(_acceptBuffer, GCHandleType.Pinned);

                _pinnedAcceptBuffer = _acceptBuffer;
                _ptrAcceptBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(_acceptBuffer, 0);
                _ptrSingleBuffer = IntPtr.Zero;
                _pinState = PinState.SingleAcceptBuffer;
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

            // Get pointer to native descriptor.
            _ptrSendPacketsDescriptor = Marshal.UnsafeAddrOfPinnedArrayElement(_sendPacketsDescriptor, 0);

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

            switch (_pinState)
            {
                case PinState.SingleAcceptBuffer:
                    NetEventSource.DumpBuffer(this, _acceptBuffer, 0, size);
                    break;

                case PinState.SingleBuffer:
                    NetEventSource.DumpBuffer(this, _buffer, _offset, size);
                    break;

                case PinState.MultipleBuffer:
                    for (int i = 0; i < _bufferListInternal.Count; i++)
                    {
                        WSABuffer wsaBuffer = _wsaBufferArray[i];
                        NetEventSource.DumpBuffer(this, wsaBuffer.Pointer, Math.Min(wsaBuffer.Length, size));
                        if ((size -= wsaBuffer.Length) <= 0)
                        {
                            break;
                        }
                    }
                    break;

                default:
                    break;
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

        private SocketError FinishOperationAccept(Internals.SocketAddress remoteSocketAddress)
        {
            SocketError socketError;
            IntPtr localAddr;
            int localAddrLength;
            IntPtr remoteAddr;

            try
            {
                _currentSocket.GetAcceptExSockaddrs(
                    _ptrSingleBuffer != IntPtr.Zero ? _ptrSingleBuffer : _ptrAcceptBuffer,
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
            return *(int*)_ptrSocketAddressBufferSize;
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
            object state = ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
            var saea = (SocketAsyncEventArgs)state;
            Debug.Assert(saea != null, $"Expected native overlapped state to contain SAEA, got {state?.GetType().ToString() ?? "(null)"}");
            saea.CompletionPortCallback(errorCode, numBytes, nativeOverlapped);
        };

        private unsafe void CompletionPortCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
#if DEBUG
            DebugThreadTracking.SetThreadSource(ThreadKinds.CompletionPort);
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.System))
            {
                if (NetEventSource.IsEnabled) NetEventSource.Enter(this, $"errorCode:{errorCode}, numBytes:{numBytes}, overlapped:{(IntPtr)nativeOverlapped}");
#endif
                SocketFlags socketFlags = SocketFlags.None;
                SocketError socketError = (SocketError)errorCode;

                if (socketError == SocketError.Success)
                {
                    FreeNativeOverlapped(nativeOverlapped);                        
                    FinishOperationAsyncSuccess((int)numBytes, SocketFlags.None);
                }
                else
                {
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

#if DEBUG
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
#endif
        }
    }
}
