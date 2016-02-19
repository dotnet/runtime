// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

namespace System.Net.Sockets
{
    public partial class Socket
    {
        private DynamicWinsockMethods _dynamicWinsockMethods;

        private void EnsureDynamicWinsockMethods()
        {
            if (_dynamicWinsockMethods == null)
            {
                _dynamicWinsockMethods = DynamicWinsockMethods.GetMethods(_addressFamily, _socketType, _protocolType);
            }
        }

        internal bool AcceptEx(SafeCloseSocket listenSocketHandle,
            SafeCloseSocket acceptSocketHandle,
            IntPtr buffer,
            int len,
            int localAddressLength,
            int remoteAddressLength,
            out int bytesReceived,
            SafeHandle overlapped)
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

        internal bool ConnectEx(SafeCloseSocket socketHandle,
            IntPtr socketAddress,
            int socketAddressSize,
            IntPtr buffer,
            int dataLength,
            out int bytesSent,
            SafeHandle overlapped)
        {
            EnsureDynamicWinsockMethods();
            ConnectExDelegate connectEx = _dynamicWinsockMethods.GetDelegate<ConnectExDelegate>(socketHandle);

            return connectEx(socketHandle, socketAddress, socketAddressSize, buffer, dataLength, out bytesSent, overlapped);
        }

        internal SocketError WSARecvMsg(SafeCloseSocket socketHandle, IntPtr msg, out int bytesTransferred, SafeHandle overlapped, IntPtr completionRoutine)
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

        internal bool TransmitPackets(SafeCloseSocket socketHandle, IntPtr packetArray, int elementCount, int sendSize, SafeNativeOverlapped overlapped)
        {
            EnsureDynamicWinsockMethods();
            TransmitPacketsDelegate transmitPackets = _dynamicWinsockMethods.GetDelegate<TransmitPacketsDelegate>(socketHandle);

            // UseDefaultWorkerThread = 0.
            return transmitPackets(socketHandle, packetArray, elementCount, sendSize, overlapped, 0);
        }

        internal static IntPtr[] SocketListToFileDescriptorSet(IList socketList)
        {
            if (socketList == null || socketList.Count == 0)
            {
                return null;
            }

            IntPtr[] fileDescriptorSet = new IntPtr[socketList.Count + 1];
            fileDescriptorSet[0] = (IntPtr)socketList.Count;
            for (int current = 0; current < socketList.Count; current++)
            {
                if (!(socketList[current] is Socket))
                {
                    throw new ArgumentException(SR.Format(SR.net_sockets_select, socketList[current].GetType().FullName, typeof(System.Net.Sockets.Socket).FullName), nameof(socketList));
                }

                fileDescriptorSet[current + 1] = ((Socket)socketList[current])._handle.DangerousGetHandle();
            }
            return fileDescriptorSet;
        }

        // Transform the list socketList such that the only sockets left are those
        // with a file descriptor contained in the array "fileDescriptorArray".
        internal static void SelectFileDescriptor(IList socketList, IntPtr[] fileDescriptorSet)
        {
            // Walk the list in order.
            //
            // Note that the counter is not necessarily incremented at each step;
            // when the socket is removed, advancing occurs automatically as the
            // other elements are shifted down.
            if (socketList == null || socketList.Count == 0)
            {
                return;
            }

            if ((int)fileDescriptorSet[0] == 0)
            {
                // No socket present, will never find any socket, remove them all.
                socketList.Clear();
                return;
            }

            lock (socketList)
            {
                for (int currentSocket = 0; currentSocket < socketList.Count; currentSocket++)
                {
                    Socket socket = socketList[currentSocket] as Socket;

                    // Look for the file descriptor in the array.
                    int currentFileDescriptor;
                    for (currentFileDescriptor = 0; currentFileDescriptor < (int)fileDescriptorSet[0]; currentFileDescriptor++)
                    {
                        if (fileDescriptorSet[currentFileDescriptor + 1] == socket._handle.DangerousGetHandle())
                        {
                            break;
                        }
                    }

                    if (currentFileDescriptor == (int)fileDescriptorSet[0])
                    {
                        // Descriptor not found: remove the current socket and start again.
                        socketList.RemoveAt(currentSocket--);
                    }
                }
            }
        }

        private Socket GetOrCreateAcceptSocket(Socket acceptSocket, bool checkDisconnected, string propertyName, out SafeCloseSocket handle)
        {
            // If an acceptSocket isn't specified, then we need to create one.
            if (acceptSocket == null)
            {
                acceptSocket = new Socket(_addressFamily, _socketType, _protocolType);
            }
            else
            {
                if (acceptSocket._rightEndPoint != null && (!checkDisconnected || !acceptSocket._isDisconnected))
                {
                    throw new InvalidOperationException(SR.Format(SR.net_sockets_namedmustnotbebound, propertyName));
                }
            }

            handle = acceptSocket._handle;
            return acceptSocket;
        }
    }
}
