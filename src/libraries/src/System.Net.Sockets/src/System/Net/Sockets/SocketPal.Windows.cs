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
    internal static class SocketPal
    {
        public const bool SupportsMultipleConnectAttempts = true;

        private readonly static int s_protocolInformationSize = Marshal.SizeOf<Interop.Winsock.WSAPROTOCOL_INFO>();

        public static int ProtocolInformationSize { get { return s_protocolInformationSize; } }

        private static void MicrosecondsToTimeValue(long microseconds, ref Interop.Winsock.TimeValue socketTime)
        {
            const int microcnv = 1000000;

            socketTime.Seconds = (int)(microseconds / microcnv);
            socketTime.Microseconds = (int)(microseconds % microcnv);
        }

        public static void Initialize()
        {
            // Ensure that WSAStartup has been called once per process.  
            // The System.Net.NameResolution contract is responsible for the initialization.
            Dns.GetHostName();
        }

        public static SocketError GetLastSocketError()
        {
            return (SocketError)Marshal.GetLastWin32Error();
        }

        public static SocketError CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, out SafeCloseSocket socket)
        {
            socket = SafeCloseSocket.CreateWSASocket(addressFamily, socketType, protocolType);
            return socket.IsInvalid ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError SetBlocking(SafeCloseSocket handle, bool shouldBlock, out bool willBlock)
        {
            int intBlocking = shouldBlock ? 0 : -1;

            SocketError errorCode;
            errorCode = Interop.Winsock.ioctlsocket(
                handle,
                Interop.Winsock.IoctlSocketConstants.FIONBIO,
                ref intBlocking);

            if (errorCode == SocketError.SocketError)
            {
                errorCode = (SocketError)Marshal.GetLastWin32Error();
            }

            willBlock = intBlocking == 0;
            return errorCode;
        }

        public static SocketError GetSockName(SafeCloseSocket handle, byte[] buffer, ref int nameLen)
        {
            SocketError errorCode = Interop.Winsock.getsockname(handle, buffer, ref nameLen);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError GetAvailable(SafeCloseSocket handle, out int available)
        {
            int value = 0;
            SocketError errorCode = Interop.Winsock.ioctlsocket(
                handle,
                Interop.Winsock.IoctlSocketConstants.FIONREAD,
                ref value);
            available = value;
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError GetPeerName(SafeCloseSocket handle, byte[] buffer, ref int nameLen)
        {
            SocketError errorCode = Interop.Winsock.getpeername(handle, buffer, ref nameLen);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Bind(SafeCloseSocket handle, byte[] buffer, int nameLen)
        {
            SocketError errorCode = Interop.Winsock.bind(handle, buffer, nameLen);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Listen(SafeCloseSocket handle, int backlog)
        {
            SocketError errorCode = Interop.Winsock.listen(handle, backlog);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Accept(SafeCloseSocket handle, byte[] buffer, ref int nameLen, out SafeCloseSocket socket)
        {
            socket = SafeCloseSocket.Accept(handle, buffer, ref nameLen);
            return socket.IsInvalid ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Connect(SafeCloseSocket handle, byte[] peerAddress, int peerAddressLen)
        {
            SocketError errorCode = Interop.Winsock.WSAConnect(
                handle.DangerousGetHandle(),
                peerAddress,
                peerAddressLen,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Send(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out int bytesTransferred)
        {
            int count = buffers.Count;
            WSABuffer[] WSABuffers = new WSABuffer[count];
            GCHandle[] objectsToPin = null;

            try
            {
                objectsToPin = new GCHandle[count];
                for (int i = 0; i < count; ++i)
                {
                    ArraySegment<byte> buffer = buffers[i];
                    RangeValidationHelpers.ValidateSegment(buffer);
                    objectsToPin[i] = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
                    WSABuffers[i].Length = buffer.Count;
                    WSABuffers[i].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(buffer.Array, buffer.Offset);
                }

                // This may throw ObjectDisposedException.
                SocketError errorCode = Interop.Winsock.WSASend_Blocking(
                    handle.DangerousGetHandle(),
                    WSABuffers,
                    count,
                    out bytesTransferred,
                    socketFlags,
                    SafeNativeOverlapped.Zero,
                    IntPtr.Zero);

                if ((SocketError)errorCode == SocketError.SocketError)
                {
                    errorCode = (SocketError)Marshal.GetLastWin32Error();
                }

                return errorCode;
            }
            finally
            {
                if (objectsToPin != null)
                {
                    for (int i = 0; i < objectsToPin.Length; ++i)
                    {
                        if (objectsToPin[i].IsAllocated)
                        {
                            objectsToPin[i].Free();
                        }
                    }
                }
            }
        }

        public static unsafe SocketError Send(SafeCloseSocket handle, byte[] buffer, int offset, int size, SocketFlags socketFlags, out int bytesTransferred)
        {
            int bytesSent;
            if (buffer.Length == 0)
            {
                bytesSent = Interop.Winsock.send(handle.DangerousGetHandle(), null, 0, socketFlags);
            }
            else
            {
                fixed (byte* pinnedBuffer = buffer)
                {
                    bytesSent = Interop.Winsock.send(
                        handle.DangerousGetHandle(),
                        pinnedBuffer + offset,
                        size,
                        socketFlags);
                }
            }

            if (bytesSent == (int)SocketError.SocketError)
            {
                bytesTransferred = 0;
                return GetLastSocketError();
            }

            bytesTransferred = bytesSent;
            return SocketError.Success;
        }

        public static unsafe SocketError SendTo(SafeCloseSocket handle, byte[] buffer, int offset, int size, SocketFlags socketFlags, byte[] peerAddress, int peerAddressSize, out int bytesTransferred)
        {
            int bytesSent;
            if (buffer.Length == 0)
            {
                bytesSent = Interop.Winsock.sendto(
                    handle.DangerousGetHandle(),
                    null,
                    0,
                    socketFlags,
                    peerAddress,
                    peerAddressSize);
            }
            else
            {
                fixed (byte* pinnedBuffer = buffer)
                {
                    bytesSent = Interop.Winsock.sendto(
                        handle.DangerousGetHandle(),
                        pinnedBuffer + offset,
                        size,
                        socketFlags,
                        peerAddress,
                        peerAddressSize);
                }
            }

            if (bytesSent == (int)SocketError.SocketError)
            {
                bytesTransferred = 0;
                return GetLastSocketError();
            }

            bytesTransferred = bytesSent;
            return SocketError.Success;
        }

        public static SocketError Receive(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, ref SocketFlags socketFlags, out int bytesTransferred)
        {
            int count = buffers.Count;
            WSABuffer[] WSABuffers = new WSABuffer[count];
            GCHandle[] objectsToPin = null;

            try
            {
                objectsToPin = new GCHandle[count];
                for (int i = 0; i < count; ++i)
                {
                    ArraySegment<byte> buffer = buffers[i];
                    RangeValidationHelpers.ValidateSegment(buffer);
                    objectsToPin[i] = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
                    WSABuffers[i].Length = buffer.Count;
                    WSABuffers[i].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(buffer.Array, buffer.Offset);
                }

                // This can throw ObjectDisposedException.
                SocketError errorCode = Interop.Winsock.WSARecv_Blocking(
                    handle.DangerousGetHandle(),
                    WSABuffers,
                    count,
                    out bytesTransferred,
                    ref socketFlags,
                    SafeNativeOverlapped.Zero,
                    IntPtr.Zero);

                if ((SocketError)errorCode == SocketError.SocketError)
                {
                    errorCode = (SocketError)Marshal.GetLastWin32Error();
                }

                return errorCode;
            }
            finally
            {
                if (objectsToPin != null)
                {
                    for (int i = 0; i < objectsToPin.Length; ++i)
                    {
                        if (objectsToPin[i].IsAllocated)
                        {
                            objectsToPin[i].Free();
                        }
                    }
                }
            }
        }

        public static unsafe SocketError Receive(SafeCloseSocket handle, byte[] buffer, int offset, int size, SocketFlags socketFlags, out int bytesTransferred)
        {
            int bytesReceived;
            if (buffer?.Length == 0)
            {
                bytesReceived = Interop.Winsock.recv(handle.DangerousGetHandle(), null, 0, socketFlags);
            }
            else
            {
                fixed (byte* pinnedBuffer = buffer)
                {
                    bytesReceived = Interop.Winsock.recv(handle.DangerousGetHandle(), pinnedBuffer + offset, size, socketFlags);
                }
            }

            if (bytesReceived == (int)SocketError.SocketError)
            {
                bytesTransferred = 0;
                return GetLastSocketError();
            }

            bytesTransferred = bytesReceived;
            return SocketError.Success;
        }

        public static SocketError ReceiveMessageFrom(Socket socket, SafeCloseSocket handle, byte[] buffer, int offset, int size, ref SocketFlags socketFlags, Internals.SocketAddress socketAddress, out Internals.SocketAddress receiveAddress, out IPPacketInformation ipPacketInformation, out int bytesTransferred)
        {
            ReceiveMessageOverlappedAsyncResult asyncResult = new ReceiveMessageOverlappedAsyncResult(socket, null, null);
            asyncResult.SetUnmanagedStructures(buffer, offset, size, socketAddress, socketFlags);

            SocketError errorCode = SocketError.Success;

            bytesTransferred = 0;
            try
            {
                // This can throw ObjectDisposedException (retrieving the delegate AND resolving the handle).
                if (socket.WSARecvMsgBlocking(
                    handle.DangerousGetHandle(),
                    Marshal.UnsafeAddrOfPinnedArrayElement(asyncResult._messageBuffer, 0),
                    out bytesTransferred,
                    IntPtr.Zero,
                    IntPtr.Zero) == SocketError.SocketError)
                {
                    errorCode = (SocketError)Marshal.GetLastWin32Error();
                }
            }
            finally
            {
                asyncResult.SyncReleaseUnmanagedStructures();
            }

            socketFlags = asyncResult.SocketFlags;
            receiveAddress = asyncResult.SocketAddress;
            ipPacketInformation = asyncResult.IPPacketInformation;

            return errorCode;
        }

        public static unsafe SocketError ReceiveFrom(SafeCloseSocket handle, byte[] buffer, int offset, int size, SocketFlags socketFlags, byte[] socketAddress, ref int addressLength, out int bytesTransferred)
        {
            int bytesReceived;
            if (buffer.Length == 0)
            {
                bytesReceived = Interop.Winsock.recvfrom(handle.DangerousGetHandle(), null, 0, socketFlags, socketAddress, ref addressLength);
            }
            else
            {
                fixed (byte* pinnedBuffer = buffer)
                {
                    bytesReceived = Interop.Winsock.recvfrom(handle.DangerousGetHandle(), pinnedBuffer + offset, size, socketFlags, socketAddress, ref addressLength);
                }
            }

            if (bytesReceived == (int)SocketError.SocketError)
            {
                bytesTransferred = 0;
                return GetLastSocketError();
            }

            bytesTransferred = bytesReceived;
            return SocketError.Success;
        }

        public static SocketError WindowsIoctl(SafeCloseSocket handle, int ioControlCode, byte[] optionInValue, byte[] optionOutValue, out int optionLength)
        {
            if (ioControlCode == Interop.Winsock.IoctlSocketConstants.FIONBIO)
            {
                throw new InvalidOperationException(SR.net_sockets_useblocking);
            }

            SocketError errorCode = Interop.Winsock.WSAIoctl_Blocking(
                handle.DangerousGetHandle(),
                ioControlCode,
                optionInValue,
                optionInValue != null ? optionInValue.Length : 0,
                optionOutValue,
                optionOutValue != null ? optionOutValue.Length : 0,
                out optionLength,
                SafeNativeOverlapped.Zero,
                IntPtr.Zero);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError SetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
        {
            SocketError errorCode = Interop.Winsock.setsockopt(
                handle,
                optionLevel,
                optionName,
                ref optionValue,
                sizeof(int));
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError SetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            SocketError errorCode = Interop.Winsock.setsockopt(
                handle,
                optionLevel,
                optionName,
                optionValue,
                optionValue != null ? optionValue.Length : 0);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static void SetReceivingDualModeIPv4PacketInformation(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
        }

        public static SocketError SetMulticastOption(SafeCloseSocket handle, SocketOptionName optionName, MulticastOption optionValue)
        {
            Interop.Winsock.IPMulticastRequest ipmr = new Interop.Winsock.IPMulticastRequest();

            ipmr.MulticastAddress = unchecked((int)optionValue.Group.GetAddress());

            if (optionValue.LocalAddress != null)
            {
                ipmr.InterfaceAddress = unchecked((int)optionValue.LocalAddress.GetAddress());
            }
            else
            {  //this structure works w/ interfaces as well
                int ifIndex = IPAddress.HostToNetworkOrder(optionValue.InterfaceIndex);
                ipmr.InterfaceAddress = unchecked((int)ifIndex);
            }

#if BIGENDIAN
            ipmr.MulticastAddress = (int) (((uint) ipmr.MulticastAddress << 24) |
                                           (((uint) ipmr.MulticastAddress & 0x0000FF00) << 8) |
                                           (((uint) ipmr.MulticastAddress >> 8) & 0x0000FF00) |
                                           ((uint) ipmr.MulticastAddress >> 24));

            if (optionValue.LocalAddress != null)
            {
                ipmr.InterfaceAddress = (int) (((uint) ipmr.InterfaceAddress << 24) |
                                           (((uint) ipmr.InterfaceAddress & 0x0000FF00) << 8) |
                                           (((uint) ipmr.InterfaceAddress >> 8) & 0x0000FF00) |
                                           ((uint) ipmr.InterfaceAddress >> 24));
            }
#endif

            // This can throw ObjectDisposedException.
            SocketError errorCode = Interop.Winsock.setsockopt(
                handle,
                SocketOptionLevel.IP,
                optionName,
                ref ipmr,
                Interop.Winsock.IPMulticastRequest.Size);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError SetIPv6MulticastOption(SafeCloseSocket handle, SocketOptionName optionName, IPv6MulticastOption optionValue)
        {
            Interop.Winsock.IPv6MulticastRequest ipmr = new Interop.Winsock.IPv6MulticastRequest();

            ipmr.MulticastAddress = optionValue.Group.GetAddressBytes();
            ipmr.InterfaceIndex = unchecked((int)optionValue.InterfaceIndex);

            // This can throw ObjectDisposedException.
            SocketError errorCode = Interop.Winsock.setsockopt(
                handle,
                SocketOptionLevel.IPv6,
                optionName,
                ref ipmr,
                Interop.Winsock.IPv6MulticastRequest.Size);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError SetLingerOption(SafeCloseSocket handle, LingerOption optionValue)
        {
            Interop.Winsock.Linger lngopt = new Interop.Winsock.Linger();
            lngopt.OnOff = optionValue.Enabled ? (ushort)1 : (ushort)0;
            lngopt.Time = (ushort)optionValue.LingerTime;

            // This can throw ObjectDisposedException.
            SocketError errorCode = Interop.Winsock.setsockopt(
                handle,
                SocketOptionLevel.Socket,
                SocketOptionName.Linger,
                ref lngopt,
                4);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static void SetIPProtectionLevel(Socket socket, SocketOptionLevel optionLevel, int protectionLevel)
        {
            socket.SetSocketOption(optionLevel, SocketOptionName.IPProtectionLevel, protectionLevel);
        }

        public static SocketError GetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, out int optionValue)
        {
            int optionLength = 4; // sizeof(int)
            SocketError errorCode = Interop.Winsock.getsockopt(
                handle,
                optionLevel,
                optionName,
                out optionValue,
                ref optionLength);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError GetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue, ref int optionLength)
        {
            SocketError errorCode = Interop.Winsock.getsockopt(
               handle,
               optionLevel,
               optionName,
               optionValue,
               ref optionLength);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError GetMulticastOption(SafeCloseSocket handle, SocketOptionName optionName, out MulticastOption optionValue)
        {
            Interop.Winsock.IPMulticastRequest ipmr = new Interop.Winsock.IPMulticastRequest();
            int optlen = Interop.Winsock.IPMulticastRequest.Size;

            // This can throw ObjectDisposedException.
            SocketError errorCode = Interop.Winsock.getsockopt(
                handle,
                SocketOptionLevel.IP,
                optionName,
                out ipmr,
                ref optlen);

            if (errorCode == SocketError.SocketError)
            {
                optionValue = default(MulticastOption);
                return GetLastSocketError();
            }

#if BIGENDIAN
            ipmr.MulticastAddress = (int) (((uint) ipmr.MulticastAddress << 24) |
                                           (((uint) ipmr.MulticastAddress & 0x0000FF00) << 8) |
                                           (((uint) ipmr.MulticastAddress >> 8) & 0x0000FF00) |
                                           ((uint) ipmr.MulticastAddress >> 24));
            ipmr.InterfaceAddress = (int) (((uint) ipmr.InterfaceAddress << 24) |
                                           (((uint) ipmr.InterfaceAddress & 0x0000FF00) << 8) |
                                           (((uint) ipmr.InterfaceAddress >> 8) & 0x0000FF00) |
                                           ((uint) ipmr.InterfaceAddress >> 24));
#endif  // BIGENDIAN

            IPAddress multicastAddr = new IPAddress(ipmr.MulticastAddress);
            IPAddress multicastIntr = new IPAddress(ipmr.InterfaceAddress);
            optionValue = new MulticastOption(multicastAddr, multicastIntr);

            return SocketError.Success;
        }

        public static SocketError GetIPv6MulticastOption(SafeCloseSocket handle, SocketOptionName optionName, out IPv6MulticastOption optionValue)
        {
            Interop.Winsock.IPv6MulticastRequest ipmr = new Interop.Winsock.IPv6MulticastRequest();

            int optlen = Interop.Winsock.IPv6MulticastRequest.Size;

            // This can throw ObjectDisposedException.
            SocketError errorCode = Interop.Winsock.getsockopt(
                handle,
                SocketOptionLevel.IP,
                optionName,
                out ipmr,
                ref optlen);

            if (errorCode == SocketError.SocketError)
            {
                optionValue = default(IPv6MulticastOption);
                return GetLastSocketError();
            }

            optionValue = new IPv6MulticastOption(new IPAddress(ipmr.MulticastAddress), ipmr.InterfaceIndex);
            return SocketError.Success;
        }

        public static SocketError GetLingerOption(SafeCloseSocket handle, out LingerOption optionValue)
        {
            Interop.Winsock.Linger lngopt = new Interop.Winsock.Linger();
            int optlen = 4;

            // This can throw ObjectDisposedException.
            SocketError errorCode = Interop.Winsock.getsockopt(
                handle,
                SocketOptionLevel.Socket,
                SocketOptionName.Linger,
                out lngopt,
                ref optlen);

            if (errorCode == SocketError.SocketError)
            {
                optionValue = default(LingerOption);
                return GetLastSocketError();
            }

            optionValue = new LingerOption(lngopt.OnOff != 0, (int)lngopt.Time);
            return SocketError.Success;
        }

        public static SocketError Poll(SafeCloseSocket handle, int microseconds, SelectMode mode, out bool status)
        {
            IntPtr rawHandle = handle.DangerousGetHandle();
            IntPtr[] fileDescriptorSet = new IntPtr[2] { (IntPtr)1, rawHandle };
            Interop.Winsock.TimeValue IOwait = new Interop.Winsock.TimeValue();

            // A negative timeout value implies an indefinite wait.
            int socketCount;
            if (microseconds != -1)
            {
                MicrosecondsToTimeValue((long)(uint)microseconds, ref IOwait);
                socketCount =
                    Interop.Winsock.select(
                        0,
                        mode == SelectMode.SelectRead ? fileDescriptorSet : null,
                        mode == SelectMode.SelectWrite ? fileDescriptorSet : null,
                        mode == SelectMode.SelectError ? fileDescriptorSet : null,
                        ref IOwait);
            }
            else
            {
                socketCount =
                    Interop.Winsock.select(
                        0,
                        mode == SelectMode.SelectRead ? fileDescriptorSet : null,
                        mode == SelectMode.SelectWrite ? fileDescriptorSet : null,
                        mode == SelectMode.SelectError ? fileDescriptorSet : null,
                        IntPtr.Zero);
            }

            if ((SocketError)socketCount == SocketError.SocketError)
            {
                status = false;
                return GetLastSocketError();
            }

            status = (int)fileDescriptorSet[0] != 0 && fileDescriptorSet[1] == rawHandle;
            return SocketError.Success;
        }

        public static SocketError Select(IList checkRead, IList checkWrite, IList checkError, int microseconds)
        {
            IntPtr[] readfileDescriptorSet = Socket.SocketListToFileDescriptorSet(checkRead);
            IntPtr[] writefileDescriptorSet = Socket.SocketListToFileDescriptorSet(checkWrite);
            IntPtr[] errfileDescriptorSet = Socket.SocketListToFileDescriptorSet(checkError);

            // This code used to erroneously pass a non-null timeval structure containing zeroes 
            // to select() when the caller specified (-1) for the microseconds parameter.  That 
            // caused select to actually have a *zero* timeout instead of an infinite timeout
            // turning the operation into a non-blocking poll.
            //
            // Now we pass a null timeval struct when microseconds is (-1).
            // 
            // Negative microsecond values that weren't exactly (-1) were originally successfully 
            // converted to a timeval struct containing unsigned non-zero integers.  This code 
            // retains that behavior so that any app working around the original bug with, 
            // for example, (-2) specified for microseconds, will continue to get the same behavior.

            int socketCount;
            if (microseconds != -1)
            {
                Interop.Winsock.TimeValue IOwait = new Interop.Winsock.TimeValue();
                MicrosecondsToTimeValue((long)(uint)microseconds, ref IOwait);

                socketCount =
                    Interop.Winsock.select(
                        0, // ignored value
                        readfileDescriptorSet,
                        writefileDescriptorSet,
                        errfileDescriptorSet,
                        ref IOwait);
            }
            else
            {
                socketCount =
                    Interop.Winsock.select(
                        0, // ignored value
                        readfileDescriptorSet,
                        writefileDescriptorSet,
                        errfileDescriptorSet,
                        IntPtr.Zero);
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("Socket::Select() Interop.Winsock.select returns socketCount:" + socketCount);
            }

            if ((SocketError)socketCount == SocketError.SocketError)
            {
                return GetLastSocketError();
            }

            Socket.SelectFileDescriptor(checkRead, readfileDescriptorSet);
            Socket.SelectFileDescriptor(checkWrite, writefileDescriptorSet);
            Socket.SelectFileDescriptor(checkError, errfileDescriptorSet);

            return SocketError.Success;
        }

        public static SocketError Shutdown(SafeCloseSocket handle, bool isConnected, bool isDisconnected, SocketShutdown how)
        {
            SocketError err = Interop.Winsock.shutdown(handle, (int)how);
            if (err != SocketError.SocketError)
            {
                return SocketError.Success;
            }

            err = GetLastSocketError();
            Debug.Assert(err != SocketError.NotConnected || (!isConnected && !isDisconnected));
            return err;
        }

        public static unsafe SocketError ConnectAsync(Socket socket, SafeCloseSocket handle, byte[] socketAddress, int socketAddressLen, ConnectOverlappedAsyncResult asyncResult)
        {
            // This will pin the socketAddress buffer.
            asyncResult.SetUnmanagedStructures(socketAddress);

            int ignoreBytesSent;
            if (!socket.ConnectEx(
                handle,
                Marshal.UnsafeAddrOfPinnedArrayElement(socketAddress, 0),
                socketAddressLen,
                IntPtr.Zero,
                0,
                out ignoreBytesSent,
                asyncResult.OverlappedHandle))
            {
                return GetLastSocketError();
            }

            return SocketError.Success;
        }

        public static unsafe SocketError SendAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            // Set up asyncResult for overlapped WSASend.
            // This call will use completion ports.
            asyncResult.SetUnmanagedStructures(buffer, offset, count, null, false /*don't pin null remoteEP*/);

            // This can throw ObjectDisposedException.
            int bytesTransferred;
            SocketError errorCode = Interop.Winsock.WSASend(
                handle,
                ref asyncResult._singleBuffer,
                1, // There is only ever 1 buffer being sent.
                out bytesTransferred,
                socketFlags,
                asyncResult.OverlappedHandle,
                IntPtr.Zero);

            if (errorCode != SocketError.Success)
            {
                errorCode = GetLastSocketError();
            }

            return errorCode;
        }

        public static unsafe SocketError SendAsync(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            // Set up asyncResult for overlapped WSASend.
            // This call will use completion ports.
            asyncResult.SetUnmanagedStructures(buffers);

            // This can throw ObjectDisposedException.
            int bytesTransferred;
            SocketError errorCode = Interop.Winsock.WSASend(
                handle,
                asyncResult._wsaBuffers,
                asyncResult._wsaBuffers.Length,
                out bytesTransferred,
                socketFlags,
                asyncResult.OverlappedHandle,
                IntPtr.Zero);

            if (errorCode != SocketError.Success)
            {
                errorCode = GetLastSocketError();
            }

            return errorCode;
        }

        public static unsafe SocketError SendToAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, OverlappedAsyncResult asyncResult)
        {
            // Set up asyncResult for overlapped WSASendTo.
            // This call will use completion ports.
            asyncResult.SetUnmanagedStructures(buffer, offset, count, socketAddress, false /* don't pin RemoteEP*/);

            int bytesTransferred;
            SocketError errorCode = Interop.Winsock.WSASendTo(
                handle,
                ref asyncResult._singleBuffer,
                1, // There is only ever 1 buffer being sent.
                out bytesTransferred,
                socketFlags,
                asyncResult.GetSocketAddressPtr(),
                asyncResult.SocketAddress.Size,
                asyncResult.OverlappedHandle,
                IntPtr.Zero);

            if (errorCode != SocketError.Success)
            {
                errorCode = GetLastSocketError();
            }

            return errorCode;
        }

        public static unsafe SocketError ReceiveAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            // Set up asyncResult for overlapped WSARecv.
            // This call will use completion ports.
            asyncResult.SetUnmanagedStructures(buffer, offset, count, null, false /* don't pin null RemoteEP*/);

            // This can throw ObjectDisposedException.
            int bytesTransferred;
            SocketError errorCode = Interop.Winsock.WSARecv(
                handle,
                ref asyncResult._singleBuffer,
                1,
                out bytesTransferred,
                ref socketFlags,
                asyncResult.OverlappedHandle,
                IntPtr.Zero);

            if (errorCode != SocketError.Success)
            {
                errorCode = GetLastSocketError();
            }

            return errorCode;
        }

        public static unsafe SocketError ReceiveAsync(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            // Set up asyncResult for overlapped WSASend.
            // This call will use completion ports.
            asyncResult.SetUnmanagedStructures(buffers);

            // This can throw ObjectDisposedException.
            int bytesTransferred;
            SocketError errorCode = Interop.Winsock.WSARecv(
                handle,
                asyncResult._wsaBuffers,
                asyncResult._wsaBuffers.Length,
                out bytesTransferred,
                ref socketFlags,
                asyncResult.OverlappedHandle,
                IntPtr.Zero);

            if (errorCode != SocketError.Success)
            {
                errorCode = GetLastSocketError();
            }

            return errorCode;
        }

        public static unsafe SocketError ReceiveFromAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, OverlappedAsyncResult asyncResult)
        {
            // Set up asyncResult for overlapped WSARecvFrom.
            // This call will use completion ports on WinNT and Overlapped IO on Win9x.
            asyncResult.SetUnmanagedStructures(buffer, offset, count, socketAddress, true);

            int bytesTransferred;
            SocketError errorCode = Interop.Winsock.WSARecvFrom(
                handle,
                ref asyncResult._singleBuffer,
                1,
                out bytesTransferred,
                ref socketFlags,
                asyncResult.GetSocketAddressPtr(),
                asyncResult.GetSocketAddressSizePtr(),
                asyncResult.OverlappedHandle,
                IntPtr.Zero);

            if (errorCode != SocketError.Success)
            {
                errorCode = GetLastSocketError();
            }

            return errorCode;
        }

        public static unsafe SocketError ReceiveMessageFromAsync(Socket socket, SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, ReceiveMessageOverlappedAsyncResult asyncResult)
        {
            asyncResult.SetUnmanagedStructures(buffer, offset, count, socketAddress, socketFlags);

            int bytesTransfered;
            SocketError errorCode = (SocketError)socket.WSARecvMsg(
                handle,
                Marshal.UnsafeAddrOfPinnedArrayElement(asyncResult._messageBuffer, 0),
                out bytesTransfered,
                asyncResult.OverlappedHandle,
                IntPtr.Zero);

            if (errorCode != SocketError.Success)
            {
                errorCode = GetLastSocketError();
            }

            return errorCode;
        }

        public static unsafe SocketError AcceptAsync(Socket socket, SafeCloseSocket handle, SafeCloseSocket acceptHandle, int receiveSize, int socketAddressSize, AcceptOverlappedAsyncResult asyncResult)
        {
            // The buffer needs to contain the requested data plus room for two sockaddrs and 16 bytes
            // of associated data for each.
            int addressBufferSize = socketAddressSize + 16;
            byte[] buffer = new byte[receiveSize + ((addressBufferSize) * 2)];

            // Set up asyncResult for overlapped AcceptEx.
            // This call will use completion ports on WinNT.
            asyncResult.SetUnmanagedStructures(buffer, addressBufferSize);

            // This can throw ObjectDisposedException.
            int bytesTransferred;
            SocketError errorCode = SocketError.Success;
            if (!socket.AcceptEx(
                handle,
                acceptHandle,
                Marshal.UnsafeAddrOfPinnedArrayElement(asyncResult.Buffer, 0),
                receiveSize,
                addressBufferSize,
                addressBufferSize,
                out bytesTransferred,
                asyncResult.OverlappedHandle))
            {
                errorCode = GetLastSocketError();
            }

            return errorCode;
        }

        public static void CheckDualModeReceiveSupport(Socket socket)
        {
            // Dual-mode sockets support received packet info on Windows.
        }

        internal static SocketError DisconnectAsync(Socket socket, SafeCloseSocket handle, bool reuseSocket, DisconnectOverlappedAsyncResult asyncResult)
        {
            asyncResult.SetUnmanagedStructures(null);

            // This can throw ObjectDisposedException
            SocketError errorCode = SocketError.Success;
            if (!socket.DisconnectEx(handle, asyncResult.OverlappedHandle, (int)(reuseSocket ? TransmitFileOptions.ReuseSocket : 0), 0))
            {
                errorCode = GetLastSocketError();
            }

            return errorCode;
        }

        internal static SocketError Disconnect(Socket socket, SafeCloseSocket handle, bool reuseSocket)
        {
            SocketError errorCode = SocketError.Success;

            // This can throw ObjectDisposedException (handle, and retrieving the delegate).
            if (!socket.DisconnectExBlocking(handle, IntPtr.Zero, (int)(reuseSocket ? TransmitFileOptions.ReuseSocket : 0), 0))
            {
                errorCode = (SocketError)Marshal.GetLastWin32Error();
            }

            return errorCode;
        }
    }
}
