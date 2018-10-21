// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Net.Sockets
{
    public partial class Socket
    {
        private DynamicWinsockMethods _dynamicWinsockMethods;

        internal void ReplaceHandleIfNecessaryAfterFailedConnect() { /* nop on Windows */ }

        private void EnsureDynamicWinsockMethods()
        {
            if (_dynamicWinsockMethods == null)
            {
                _dynamicWinsockMethods = DynamicWinsockMethods.GetMethods(_addressFamily, _socketType, _protocolType);
            }
        }

        internal unsafe bool AcceptEx(SafeSocketHandle listenSocketHandle,
            SafeSocketHandle acceptSocketHandle,
            IntPtr buffer,
            int len,
            int localAddressLength,
            int remoteAddressLength,
            out int bytesReceived,
            NativeOverlapped* overlapped)
        {
            EnsureDynamicWinsockMethods();
            AcceptExDelegate acceptEx = _dynamicWinsockMethods.GetDelegate<AcceptExDelegate>(listenSocketHandle);

            return acceptEx(listenSocketHandle,
                acceptSocketHandle,
                buffer,
                len,
                localAddressLength,
                remoteAddressLength,
                out bytesReceived,
                overlapped);
        }

        internal void GetAcceptExSockaddrs(IntPtr buffer,
            int receiveDataLength,
            int localAddressLength,
            int remoteAddressLength,
            out IntPtr localSocketAddress,
            out int localSocketAddressLength,
            out IntPtr remoteSocketAddress,
            out int remoteSocketAddressLength)
        {
            EnsureDynamicWinsockMethods();
            GetAcceptExSockaddrsDelegate getAcceptExSockaddrs = _dynamicWinsockMethods.GetDelegate<GetAcceptExSockaddrsDelegate>(_handle);

            getAcceptExSockaddrs(buffer,
                receiveDataLength,
                localAddressLength,
                remoteAddressLength,
                out localSocketAddress,
                out localSocketAddressLength,
                out remoteSocketAddress,
                out remoteSocketAddressLength);
        }

        internal unsafe bool DisconnectEx(SafeSocketHandle socketHandle, NativeOverlapped* overlapped, int flags, int reserved)
        {
            EnsureDynamicWinsockMethods();
            DisconnectExDelegate disconnectEx = _dynamicWinsockMethods.GetDelegate<DisconnectExDelegate>(socketHandle);

            return disconnectEx(socketHandle, overlapped, flags, reserved);
        }

        internal bool DisconnectExBlocking(SafeSocketHandle socketHandle, IntPtr overlapped, int flags, int reserved)
        {
            EnsureDynamicWinsockMethods();
            DisconnectExDelegateBlocking disconnectEx_Blocking = _dynamicWinsockMethods.GetDelegate<DisconnectExDelegateBlocking>(socketHandle);

            return disconnectEx_Blocking(socketHandle, overlapped, flags, reserved);
        }

        internal unsafe bool ConnectEx(SafeSocketHandle socketHandle,
            IntPtr socketAddress,
            int socketAddressSize,
            IntPtr buffer,
            int dataLength,
            out int bytesSent,
            NativeOverlapped* overlapped)
        {
            EnsureDynamicWinsockMethods();
            ConnectExDelegate connectEx = _dynamicWinsockMethods.GetDelegate<ConnectExDelegate>(socketHandle);

            return connectEx(socketHandle, socketAddress, socketAddressSize, buffer, dataLength, out bytesSent, overlapped);
        }

        internal unsafe SocketError WSARecvMsg(SafeSocketHandle socketHandle, IntPtr msg, out int bytesTransferred, NativeOverlapped* overlapped, IntPtr completionRoutine)
        {
            EnsureDynamicWinsockMethods();
            WSARecvMsgDelegate recvMsg = _dynamicWinsockMethods.GetDelegate<WSARecvMsgDelegate>(socketHandle);

            return recvMsg(socketHandle, msg, out bytesTransferred, overlapped, completionRoutine);
        }

        internal SocketError WSARecvMsgBlocking(IntPtr socketHandle, IntPtr msg, out int bytesTransferred, IntPtr overlapped, IntPtr completionRoutine)
        {
            EnsureDynamicWinsockMethods();
            WSARecvMsgDelegateBlocking recvMsg_Blocking = _dynamicWinsockMethods.GetDelegate<WSARecvMsgDelegateBlocking>(_handle);

            return recvMsg_Blocking(socketHandle, msg, out bytesTransferred, overlapped, completionRoutine);
        }

        internal unsafe bool TransmitPackets(SafeSocketHandle socketHandle, IntPtr packetArray, int elementCount, int sendSize, NativeOverlapped* overlapped, TransmitFileOptions flags)
        {
            EnsureDynamicWinsockMethods();
            TransmitPacketsDelegate transmitPackets = _dynamicWinsockMethods.GetDelegate<TransmitPacketsDelegate>(socketHandle);

            return transmitPackets(socketHandle, packetArray, elementCount, sendSize, overlapped, flags);
        }

        internal static void SocketListToFileDescriptorSet(IList socketList, Span<IntPtr> fileDescriptorSet)
        {
            int count;
            if (socketList == null || (count = socketList.Count) == 0)
            {
                return;
            }

            Debug.Assert(fileDescriptorSet.Length >= count + 1);

            fileDescriptorSet[0] = (IntPtr)count;
            for (int current = 0; current < count; current++)
            {
                if (!(socketList[current] is Socket))
                {
                    throw new ArgumentException(SR.Format(SR.net_sockets_select, socketList[current].GetType().FullName, typeof(System.Net.Sockets.Socket).FullName), nameof(socketList));
                }

                fileDescriptorSet[current + 1] = ((Socket)socketList[current])._handle.DangerousGetHandle();
            }
        }

        // Transform the list socketList such that the only sockets left are those
        // with a file descriptor contained in the array "fileDescriptorArray".
        internal static void SelectFileDescriptor(IList socketList, Span<IntPtr> fileDescriptorSet)
        {
            // Walk the list in order.
            //
            // Note that the counter is not necessarily incremented at each step;
            // when the socket is removed, advancing occurs automatically as the
            // other elements are shifted down.
            int count;
            if (socketList == null || (count = socketList.Count) == 0)
            {
                return;
            }

            Debug.Assert(fileDescriptorSet.Length >= count + 1);

            int returnedCount = (int)fileDescriptorSet[0];
            if (returnedCount == 0)
            {
                // No socket present, will never find any socket, remove them all.
                socketList.Clear();
                return;
            }

            lock (socketList)
            {
                for (int currentSocket = 0; currentSocket < count; currentSocket++)
                {
                    Socket socket = socketList[currentSocket] as Socket;

                    // Look for the file descriptor in the array.
                    int currentFileDescriptor;
                    for (currentFileDescriptor = 0; currentFileDescriptor < returnedCount; currentFileDescriptor++)
                    {
                        if (fileDescriptorSet[currentFileDescriptor + 1] == socket._handle.DangerousGetHandle())
                        {
                            break;
                        }
                    }

                    if (currentFileDescriptor == returnedCount)
                    {
                        // Descriptor not found: remove the current socket and start again.
                        socketList.RemoveAt(currentSocket--);
                        count--;
                    }
                }
            }
        }

        private Socket GetOrCreateAcceptSocket(Socket acceptSocket, bool checkDisconnected, string propertyName, out SafeSocketHandle handle)
        {
            // If an acceptSocket isn't specified, then we need to create one.
            if (acceptSocket == null)
            {
                acceptSocket = new Socket(_addressFamily, _socketType, _protocolType);
            }
            else if (acceptSocket._rightEndPoint != null && (!checkDisconnected || !acceptSocket._isDisconnected))
            {
                throw new InvalidOperationException(SR.Format(SR.net_sockets_namedmustnotbebound, propertyName));
            }

            handle = acceptSocket._handle;
            return acceptSocket;
        }

        private void SendFileInternal(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags)
        {
            // Open the file, if any
            FileStream fileStream = OpenFile(fileName);

            SocketError errorCode;
            using (fileStream)
            {
                SafeFileHandle fileHandle = fileStream?.SafeFileHandle;

                // This can throw ObjectDisposedException.
                errorCode = SocketPal.SendFile(_handle, fileHandle, preBuffer, postBuffer, flags);
            }

            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }

            // If the user passed the Disconnect and/or ReuseSocket flags, then TransmitFile disconnected the socket.
            // Update our state to reflect this.
            if ((flags & (TransmitFileOptions.Disconnect | TransmitFileOptions.ReuseSocket)) != 0)
            {
                SetToDisconnected();
                _remoteEndPoint = null;
            }
        }

        private IAsyncResult BeginSendFileInternal(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags, AsyncCallback callback, object state)
        {
            FileStream fileStream = OpenFile(fileName);

            TransmitFileAsyncResult asyncResult = new TransmitFileAsyncResult(this, state, callback);
            asyncResult.StartPostingAsyncOp(false);

            SocketError errorCode = SocketPal.SendFileAsync(_handle, fileStream, preBuffer, postBuffer, flags, asyncResult);

            // Check for synchronous exception
            if (!CheckErrorAndUpdateStatus(errorCode))
            {
                throw new SocketException((int)errorCode);
            }

            asyncResult.FinishPostingAsyncOp(ref Caches.SendClosureCache);

            return asyncResult;
        }

        private void EndSendFileInternal(IAsyncResult asyncResult)
        {
            TransmitFileAsyncResult castedAsyncResult = asyncResult as TransmitFileAsyncResult;
            if (castedAsyncResult == null || castedAsyncResult.AsyncObject != this)
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
            }

            if (castedAsyncResult.EndCalled)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndSendFile"));
            }

            castedAsyncResult.InternalWaitForCompletion();
            castedAsyncResult.EndCalled = true;

            // If the user passed the Disconnect and/or ReuseSocket flags, then TransmitFile disconnected the socket.
            // Update our state to reflect this.
            if (castedAsyncResult.DoDisconnect)
            {
                SetToDisconnected();
                _remoteEndPoint = null;
            }

            if ((SocketError)castedAsyncResult.ErrorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketErrorAndThrowException((SocketError)castedAsyncResult.ErrorCode);
            }

        }

        internal ThreadPoolBoundHandle GetOrAllocateThreadPoolBoundHandle() =>
            _handle.GetThreadPoolBoundHandle() ??
            GetOrAllocateThreadPoolBoundHandleSlow();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal ThreadPoolBoundHandle GetOrAllocateThreadPoolBoundHandleSlow()
        {
            // There is a known bug that exists through Windows 7 with UDP and SetFileCompletionNotificationModes.
            // So, don't try to enable skipping the completion port on success in this case.
            bool trySkipCompletionPortOnSuccess = !(CompletionPortHelper.PlatformHasUdpIssue && _protocolType == ProtocolType.Udp);
            return _handle.GetOrAllocateThreadPoolBoundHandle(trySkipCompletionPortOnSuccess);
        }
    }
}
