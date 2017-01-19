// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Sockets
{
    public partial class SocketAsyncEventArgs : EventArgs, IDisposable
    {
        private IntPtr _acceptedFileDescriptor;
        private int _socketAddressSize;
        private SocketFlags _receivedFlags;
        private Action<int, byte[], int, SocketFlags, SocketError> _transferCompletionCallback;

        internal int? SendPacketsDescriptorCount { get { return null; } }

        private void InitializeInternals()
        {
            // No-op for *nix.
        }

        private void FreeInternals(bool calledFromFinalizer)
        {
            // No-op for *nix.
        }

        private void SetupSingleBuffer()
        {
            // No-op for *nix.
        }

        private void SetupMultipleBuffers()
        {
            // No-op for *nix.
        }

        private void SetupSendPacketsElements()
        {
            // No-op for *nix.
        }

        private void InnerComplete()
        {
            // No-op for *nix.
        }

        private void InnerStartOperationAccept(bool userSuppliedBuffer)
        {
            _acceptedFileDescriptor = (IntPtr)(-1);
        }

        private void AcceptCompletionCallback(IntPtr acceptedFileDescriptor, byte[] socketAddress, int socketAddressSize, SocketError socketError)
        {
            CompleteAcceptOperation(acceptedFileDescriptor, socketAddress, socketAddressSize, socketError);

            CompletionCallback(0, SocketFlags.None, socketError);
        }

        private void CompleteAcceptOperation(IntPtr acceptedFileDescriptor, byte[] socketAddress, int socketAddressSize, SocketError socketError)
        {
            _acceptedFileDescriptor = acceptedFileDescriptor;
            Debug.Assert(socketAddress == null || socketAddress == _acceptBuffer, $"Unexpected socketAddress: {socketAddress}");
            _acceptAddressBufferCount = socketAddressSize;
        }

        internal unsafe SocketError DoOperationAccept(Socket socket, SafeCloseSocket handle, SafeCloseSocket acceptHandle)
        {
            if (_buffer != null)
            {
                throw new PlatformNotSupportedException(SR.net_sockets_accept_receive_notsupported);
            }

            Debug.Assert(acceptHandle == null, $"Unexpected acceptHandle: {acceptHandle}");

            IntPtr acceptedFd;
            int socketAddressLen = _acceptAddressBufferCount / 2;
            SocketError socketError = handle.AsyncContext.AcceptAsync(_acceptBuffer, ref socketAddressLen, out acceptedFd, AcceptCompletionCallback);

            if (socketError != SocketError.IOPending)
            {
                CompleteAcceptOperation(acceptedFd, _acceptBuffer, socketAddressLen, socketError);
                FinishOperationSync(socketError, 0, SocketFlags.None);
            }

            return socketError;
        }

        private void InnerStartOperationConnect()
        {
            // No-op for *nix.
        }

        private void ConnectCompletionCallback(SocketError socketError)
        {
            CompletionCallback(0, SocketFlags.None, socketError);
        }

        internal unsafe SocketError DoOperationConnect(Socket socket, SafeCloseSocket handle)
        {
            SocketError socketError = handle.AsyncContext.ConnectAsync(_socketAddress.Buffer, _socketAddress.Size, ConnectCompletionCallback);
            if (socketError != SocketError.IOPending)
            {
                FinishOperationSync(socketError, 0, SocketFlags.None);
            }
            return socketError;
        }

        internal SocketError DoOperationDisconnect(Socket socket, SafeCloseSocket handle)
        {
            throw new PlatformNotSupportedException(SR.net_sockets_disconnect_notsupported);
        }

        private void InnerStartOperationDisconnect()
        {
            throw new PlatformNotSupportedException(SR.net_sockets_disconnect_notsupported);
        }

        private Action<int, byte[], int, SocketFlags, SocketError> TransferCompletionCallback =>
            _transferCompletionCallback ?? (_transferCompletionCallback = TransferCompletionCallbackCore);

        private void TransferCompletionCallbackCore(int bytesTransferred, byte[] socketAddress, int socketAddressSize, SocketFlags receivedFlags, SocketError socketError)
        {
            CompleteTransferOperation(bytesTransferred, socketAddress, socketAddressSize, receivedFlags, socketError);

            CompletionCallback(bytesTransferred, receivedFlags, socketError);
        }

        private void CompleteTransferOperation(int bytesTransferred, byte[] socketAddress, int socketAddressSize, SocketFlags receivedFlags, SocketError socketError)
        {
            Debug.Assert(socketAddress == null || socketAddress == _socketAddress.Buffer, $"Unexpected socketAddress: {socketAddress}");
            _socketAddressSize = socketAddressSize;
            _receivedFlags = receivedFlags;
        }

        private void InnerStartOperationReceive()
        {
            _receivedFlags = System.Net.Sockets.SocketFlags.None;
            _socketAddressSize = 0;
        }

        internal unsafe SocketError DoOperationReceive(SafeCloseSocket handle, out SocketFlags flags)
        {
            int bytesReceived;
            SocketError errorCode;
            if (_buffer != null)
            {
                errorCode = handle.AsyncContext.ReceiveAsync(_buffer, _offset, _count, _socketFlags, out bytesReceived, out flags, TransferCompletionCallback);
            }
            else
            {
                errorCode = handle.AsyncContext.ReceiveAsync(_bufferList, _socketFlags, out bytesReceived, out flags, TransferCompletionCallback);
            }

            if (errorCode != SocketError.IOPending)
            {
                CompleteTransferOperation(bytesReceived, null, 0, flags, errorCode);
                FinishOperationSync(errorCode, bytesReceived, flags);
            }

            return errorCode;
        }

        private void InnerStartOperationReceiveFrom()
        {
            _receivedFlags = System.Net.Sockets.SocketFlags.None;
            _socketAddressSize = 0;
        }

        internal unsafe SocketError DoOperationReceiveFrom(SafeCloseSocket handle, out SocketFlags flags)
        {
            SocketError errorCode;
            int bytesReceived = 0;
            int socketAddressLen = _socketAddress.Size;
            if (_buffer != null)
            {
                errorCode = handle.AsyncContext.ReceiveFromAsync(_buffer, _offset, _count, _socketFlags, _socketAddress.Buffer, ref socketAddressLen, out bytesReceived, out flags, TransferCompletionCallback);
            }
            else
            {
                errorCode = handle.AsyncContext.ReceiveFromAsync(_bufferList, _socketFlags, _socketAddress.Buffer, ref socketAddressLen, out bytesReceived, out flags, TransferCompletionCallback);
            }

            if (errorCode != SocketError.IOPending)
            {
                CompleteTransferOperation(bytesReceived, _socketAddress.Buffer, socketAddressLen, flags, errorCode);
                FinishOperationSync(errorCode, bytesReceived, flags);
            }

            return errorCode;
        }

        private void InnerStartOperationReceiveMessageFrom()
        {
            _receiveMessageFromPacketInfo = default(IPPacketInformation);
            _receivedFlags = System.Net.Sockets.SocketFlags.None;
            _socketAddressSize = 0;
        }

        private void ReceiveMessageFromCompletionCallback(int bytesTransferred, byte[] socketAddress, int socketAddressSize, SocketFlags receivedFlags, IPPacketInformation ipPacketInformation, SocketError errorCode)
        {
            CompleteReceiveMessageFromOperation(bytesTransferred, socketAddress, socketAddressSize, receivedFlags, ipPacketInformation, errorCode);

            CompletionCallback(bytesTransferred, receivedFlags, errorCode);
        }

        private void CompleteReceiveMessageFromOperation(int bytesTransferred, byte[] socketAddress, int socketAddressSize, SocketFlags receivedFlags, IPPacketInformation ipPacketInformation, SocketError errorCode)
        {
            Debug.Assert(_socketAddress != null, "Expected non-null _socketAddress");
            Debug.Assert(socketAddress == null || _socketAddress.Buffer == socketAddress, $"Unexpected socketAddress: {socketAddress}");

            _socketAddressSize = socketAddressSize;
            _receivedFlags = receivedFlags;
            _receiveMessageFromPacketInfo = ipPacketInformation;
        }

        internal unsafe SocketError DoOperationReceiveMessageFrom(Socket socket, SafeCloseSocket handle)
        {
            bool isIPv4, isIPv6;
            Socket.GetIPProtocolInformation(socket.AddressFamily, _socketAddress, out isIPv4, out isIPv6);

            int socketAddressSize = _socketAddress.Size;
            int bytesReceived;
            SocketFlags receivedFlags;
            IPPacketInformation ipPacketInformation;
            SocketError socketError = handle.AsyncContext.ReceiveMessageFromAsync(_buffer, _offset, _count, _socketFlags, _socketAddress.Buffer, ref socketAddressSize, isIPv4, isIPv6, out bytesReceived, out receivedFlags, out ipPacketInformation, ReceiveMessageFromCompletionCallback);
            if (socketError != SocketError.IOPending)
            {
                CompleteReceiveMessageFromOperation(bytesReceived, _socketAddress.Buffer, socketAddressSize, receivedFlags, ipPacketInformation, socketError);
                FinishOperationSync(socketError, bytesReceived, receivedFlags);
            }
            return socketError;
        }

        private void InnerStartOperationSend()
        {
            _receivedFlags = System.Net.Sockets.SocketFlags.None;
            _socketAddressSize = 0;
        }

        internal unsafe SocketError DoOperationSend(SafeCloseSocket handle)
        {
            int bytesSent;
            SocketError errorCode;
            if (_buffer != null)
            {
                errorCode = handle.AsyncContext.SendAsync(_buffer, _offset, _count, _socketFlags, out bytesSent, TransferCompletionCallback);
            }
            else
            {
                errorCode = handle.AsyncContext.SendAsync(_bufferList, _socketFlags, out bytesSent, TransferCompletionCallback);
            }

            if (errorCode != SocketError.IOPending)
            {
                CompleteTransferOperation(bytesSent, null, 0, SocketFlags.None, errorCode);
                FinishOperationSync(errorCode, bytesSent, SocketFlags.None);
            }

            return errorCode;
        }

        private void InnerStartOperationSendPackets()
        {
            throw new PlatformNotSupportedException();
        }

        internal SocketError DoOperationSendPackets(Socket socket, SafeCloseSocket handle)
        {
            throw new PlatformNotSupportedException();
        }

        private void InnerStartOperationSendTo()
        {
            _receivedFlags = System.Net.Sockets.SocketFlags.None;
            _socketAddressSize = 0;
        }

        internal SocketError DoOperationSendTo(SafeCloseSocket handle)
        {
            int bytesSent;
            int socketAddressLen = _socketAddress.Size;
            SocketError errorCode;
            if (_buffer != null)
            {
                errorCode = handle.AsyncContext.SendToAsync(_buffer, _offset, _count, _socketFlags, _socketAddress.Buffer, ref socketAddressLen, out bytesSent, TransferCompletionCallback);
            }
            else
            {
                errorCode = handle.AsyncContext.SendToAsync(_bufferList, _socketFlags, _socketAddress.Buffer, ref socketAddressLen, out bytesSent, TransferCompletionCallback);
            }

            if (errorCode != SocketError.IOPending)
            {
                CompleteTransferOperation(bytesSent, _socketAddress.Buffer, socketAddressLen, SocketFlags.None, errorCode);
                FinishOperationSync(errorCode, bytesSent, SocketFlags.None);
            }

            return errorCode;
        }

        internal void LogBuffer(int size)
        {
            if (!NetEventSource.IsEnabled) return;

            if (_buffer != null)
            {
                NetEventSource.DumpBuffer(this, _buffer, _offset, size);
            }
            else if (_acceptBuffer != null)
            {
                NetEventSource.DumpBuffer(this, _acceptBuffer, 0, size);
            }
        }

        internal void LogSendPacketsBuffers(int size)
        {
            throw new PlatformNotSupportedException();
        }

        private SocketError FinishOperationAccept(Internals.SocketAddress remoteSocketAddress)
        {
            System.Buffer.BlockCopy(_acceptBuffer, 0, remoteSocketAddress.Buffer, 0, _acceptAddressBufferCount);
            _acceptSocket = _currentSocket.CreateAcceptSocket(
                SafeCloseSocket.CreateSocket(_acceptedFileDescriptor),
                _currentSocket._rightEndPoint.Create(remoteSocketAddress));
            return SocketError.Success;
        }

        private SocketError FinishOperationConnect()
        {
            // No-op for *nix.
            return SocketError.Success;
        }

        private unsafe int GetSocketAddressSize()
        {
            return _socketAddressSize;
        }

        private unsafe void FinishOperationReceiveMessageFrom()
        {
            // No-op for *nix.
        }

        private void FinishOperationSendPackets()
        {
            throw new PlatformNotSupportedException();
        }

        private void CompletionCallback(int bytesTransferred, SocketFlags flags, SocketError socketError)
        {
            if (socketError == SocketError.Success)
            {
                FinishOperationAsyncSuccess(bytesTransferred, flags);
            }
            else
            {
                if (_currentSocket.CleanedUp)
                {
                    socketError = SocketError.OperationAborted;
                }

                FinishOperationAsyncFailure(socketError, bytesTransferred, flags);
            }
        }
    }
}
