// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal static class SocketPal
    {
        public const bool SupportsMultipleConnectAttempts = true;

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
            int win32Error = Marshal.GetLastWin32Error();
            Debug.Assert(win32Error != 0, "Expected non-0 error");
            return (SocketError)win32Error;
        }

        public static SocketError CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, out SafeSocketHandle socket)
        {
            socket = SafeSocketHandle.CreateWSASocket(addressFamily, socketType, protocolType);
            return socket.IsInvalid ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError SetBlocking(SafeSocketHandle handle, bool shouldBlock, out bool willBlock)
        {
            int intBlocking = shouldBlock ? 0 : -1;

            SocketError errorCode;
            errorCode = Interop.Winsock.ioctlsocket(
                handle,
                Interop.Winsock.IoctlSocketConstants.FIONBIO,
                ref intBlocking);

            if (errorCode == SocketError.SocketError)
            {
                errorCode = GetLastSocketError();
            }

            willBlock = intBlocking == 0;
            return errorCode;
        }

        public static SocketError GetSockName(SafeSocketHandle handle, byte[] buffer, ref int nameLen)
        {
            SocketError errorCode = Interop.Winsock.getsockname(handle, buffer, ref nameLen);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError GetAvailable(SafeSocketHandle handle, out int available)
        {
            int value = 0;
            SocketError errorCode = Interop.Winsock.ioctlsocket(
                handle,
                Interop.Winsock.IoctlSocketConstants.FIONREAD,
                ref value);
            available = value;
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError GetPeerName(SafeSocketHandle handle, byte[] buffer, ref int nameLen)
        {
            SocketError errorCode = Interop.Winsock.getpeername(handle, buffer, ref nameLen);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Bind(SafeSocketHandle handle, ProtocolType socketProtocolType, byte[] buffer, int nameLen)
        {
            SocketError errorCode = Interop.Winsock.bind(handle, buffer, nameLen);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Listen(SafeSocketHandle handle, int backlog)
        {
            SocketError errorCode = Interop.Winsock.listen(handle, backlog);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Accept(SafeSocketHandle handle, byte[] buffer, ref int nameLen, out SafeSocketHandle socket)
        {
            socket = SafeSocketHandle.Accept(handle, buffer, ref nameLen);
            return socket.IsInvalid ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Connect(SafeSocketHandle handle, byte[] peerAddress, int peerAddressLen)
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

        public static SocketError Send(SafeSocketHandle handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out int bytesTransferred)
        {
            const int StackThreshold = 16; // arbitrary limit to avoid too much space on stack (note: may be over-sized, that's OK - length passed separately)
            int count = buffers.Count;
            bool useStack = count <= StackThreshold;

            WSABuffer[] leasedWSA = null;
            GCHandle[] leasedGC = null;
            Span<WSABuffer> WSABuffers = stackalloc WSABuffer[0];
            Span<GCHandle> objectsToPin = stackalloc GCHandle[0];
            if (useStack)
            {
                WSABuffers = stackalloc WSABuffer[StackThreshold];
                objectsToPin = stackalloc GCHandle[StackThreshold];
            }
            else
            {
                WSABuffers = leasedWSA = ArrayPool<WSABuffer>.Shared.Rent(count);
                objectsToPin = leasedGC = ArrayPool<GCHandle>.Shared.Rent(count);
            }
            objectsToPin = objectsToPin.Slice(0, count);
            objectsToPin.Clear(); // note: touched in finally

            try
            {
                for (int i = 0; i < count; ++i)
                {
                    ArraySegment<byte> buffer = buffers[i];
                    RangeValidationHelpers.ValidateSegment(buffer);
                    objectsToPin[i] = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
                    WSABuffers[i].Length = buffer.Count;
                    WSABuffers[i].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(buffer.Array, buffer.Offset);
                }

                unsafe
                {
                    SocketError errorCode = Interop.Winsock.WSASend(
                        handle.DangerousGetHandle(),
                        WSABuffers,
                        count,
                        out bytesTransferred,
                        socketFlags,
                        null,
                        IntPtr.Zero);

                    if (errorCode == SocketError.SocketError)
                    {
                        errorCode = GetLastSocketError();
                    }

                    return errorCode;
                }
            }
            finally
            {
                for (int i = 0; i < count; ++i)
                {
                    if (objectsToPin[i].IsAllocated)
                    {
                        objectsToPin[i].Free();
                    }
                }
                if (!useStack)
                {
                    ArrayPool<WSABuffer>.Shared.Return(leasedWSA);
                    ArrayPool<GCHandle>.Shared.Return(leasedGC);
                }
            }
        }

        public static unsafe SocketError Send(SafeSocketHandle handle, byte[] buffer, int offset, int size, SocketFlags socketFlags, out int bytesTransferred) =>
            Send(handle, new ReadOnlySpan<byte>(buffer, offset, size), socketFlags, out bytesTransferred);

        public static unsafe SocketError Send(SafeSocketHandle handle, ReadOnlySpan<byte> buffer, SocketFlags socketFlags, out int bytesTransferred)
        {
            int bytesSent;
            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer))
            {
                bytesSent = Interop.Winsock.send(handle.DangerousGetHandle(), bufferPtr, buffer.Length, socketFlags);
            }

            if (bytesSent == (int)SocketError.SocketError)
            {
                bytesTransferred = 0;
                return GetLastSocketError();
            }

            bytesTransferred = bytesSent;
            return SocketError.Success;
        }

        public static unsafe SocketError SendFile(SafeSocketHandle handle, SafeFileHandle fileHandle, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags)
        {
            fixed (byte* prePinnedBuffer = preBuffer)
            fixed (byte* postPinnedBuffer = postBuffer)
            {
                bool success = TransmitFileHelper(handle, fileHandle, null, preBuffer, postBuffer, flags);
                return (success ? SocketError.Success : SocketPal.GetLastSocketError());
            }
        }

        public static unsafe SocketError SendTo(SafeSocketHandle handle, byte[] buffer, int offset, int size, SocketFlags socketFlags, byte[] peerAddress, int peerAddressSize, out int bytesTransferred)
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
                fixed (byte* pinnedBuffer = &buffer[0])
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

        public static SocketError Receive(SafeSocketHandle handle, IList<ArraySegment<byte>> buffers, ref SocketFlags socketFlags, out int bytesTransferred)
        {
            const int StackThreshold = 16; // arbitrary limit to avoid too much space on stack (note: may be over-sized, that's OK - length passed separately)
            int count = buffers.Count;
            bool useStack = count <= StackThreshold;

            WSABuffer[] leasedWSA = null;
            GCHandle[] leasedGC = null;
            Span<WSABuffer> WSABuffers = stackalloc WSABuffer[0];
            Span<GCHandle> objectsToPin = stackalloc GCHandle[0];
            if (useStack)
            {
                WSABuffers = stackalloc WSABuffer[StackThreshold];
                objectsToPin = stackalloc GCHandle[StackThreshold];
            }
            else
            {
                WSABuffers = leasedWSA = ArrayPool<WSABuffer>.Shared.Rent(count);
                objectsToPin = leasedGC = ArrayPool<GCHandle>.Shared.Rent(count);
            }
            objectsToPin = objectsToPin.Slice(0, count);
            objectsToPin.Clear(); // note: touched in finally

            try
            {
                for (int i = 0; i < count; ++i)
                {
                    ArraySegment<byte> buffer = buffers[i];
                    RangeValidationHelpers.ValidateSegment(buffer);
                    objectsToPin[i] = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
                    WSABuffers[i].Length = buffer.Count;
                    WSABuffers[i].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(buffer.Array, buffer.Offset);
                }

                unsafe
                {
                    SocketError errorCode = Interop.Winsock.WSARecv(
                        handle.DangerousGetHandle(),
                        WSABuffers,
                        count,
                        out bytesTransferred,
                        ref socketFlags,
                        null,
                        IntPtr.Zero);

                    if (errorCode == SocketError.SocketError)
                    {
                        errorCode = GetLastSocketError();
                    }

                    return errorCode;
                }
            }
            finally
            {
                for (int i = 0; i < count; ++i)
                {
                    if (objectsToPin[i].IsAllocated)
                    {
                        objectsToPin[i].Free();
                    }
                }
                if (!useStack)
                {
                    ArrayPool<WSABuffer>.Shared.Return(leasedWSA);
                    ArrayPool<GCHandle>.Shared.Return(leasedGC);
                }
            }
        }

        public static unsafe SocketError Receive(SafeSocketHandle handle, byte[] buffer, int offset, int size, SocketFlags socketFlags, out int bytesTransferred) =>
            Receive(handle, new Span<byte>(buffer, offset, size), socketFlags, out bytesTransferred);

        public static unsafe SocketError Receive(SafeSocketHandle handle, Span<byte> buffer, SocketFlags socketFlags, out int bytesTransferred)
        {
            int bytesReceived;
            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer))
            {
                bytesReceived = Interop.Winsock.recv(handle.DangerousGetHandle(), bufferPtr, buffer.Length, socketFlags);
            }

            if (bytesReceived == (int)SocketError.SocketError)
            {
                bytesTransferred = 0;
                return GetLastSocketError();
            }

            bytesTransferred = bytesReceived;
            return SocketError.Success;
        }

        public static unsafe IPPacketInformation GetIPPacketInformation(Interop.Winsock.ControlData* controlBuffer)
        {
            IPAddress address = controlBuffer->length == UIntPtr.Zero ? IPAddress.None : new IPAddress((long)controlBuffer->address);
            return new IPPacketInformation(address, (int)controlBuffer->index);
        }

        public static unsafe IPPacketInformation GetIPPacketInformation(Interop.Winsock.ControlDataIPv6* controlBuffer)
        {
            IPAddress address = controlBuffer->length != UIntPtr.Zero ?
                new IPAddress(new ReadOnlySpan<byte>(controlBuffer->address, Interop.Winsock.IPv6AddressLength)) :
                IPAddress.IPv6None;

            return new IPPacketInformation(address, (int)controlBuffer->index);
        }

        public static unsafe SocketError ReceiveMessageFrom(Socket socket, SafeSocketHandle handle, byte[] buffer, int offset, int size, ref SocketFlags socketFlags, Internals.SocketAddress socketAddress, out Internals.SocketAddress receiveAddress, out IPPacketInformation ipPacketInformation, out int bytesTransferred)
        {
            bool ipv4, ipv6;
            Socket.GetIPProtocolInformation(socket.AddressFamily, socketAddress, out ipv4, out ipv6);

            bytesTransferred = 0;
            receiveAddress = socketAddress;
            ipPacketInformation = default(IPPacketInformation);

            fixed (byte* ptrBuffer = buffer)
            fixed (byte* ptrSocketAddress = socketAddress.Buffer)
            {
                Interop.Winsock.WSAMsg wsaMsg;
                wsaMsg.socketAddress = (IntPtr)ptrSocketAddress;
                wsaMsg.addressLength = (uint)socketAddress.Size;
                wsaMsg.flags = socketFlags;

                WSABuffer wsaBuffer;
                wsaBuffer.Length = size;
                wsaBuffer.Pointer = (IntPtr)(ptrBuffer + offset);
                wsaMsg.buffers = (IntPtr)(&wsaBuffer);
                wsaMsg.count = 1;

                if (ipv4)
                {
                    Interop.Winsock.ControlData controlBuffer;
                    wsaMsg.controlBuffer.Pointer = (IntPtr)(&controlBuffer);
                    wsaMsg.controlBuffer.Length = sizeof(Interop.Winsock.ControlData);

                    if (socket.WSARecvMsgBlocking(
                        handle.DangerousGetHandle(),
                        (IntPtr)(&wsaMsg),
                        out bytesTransferred,
                        IntPtr.Zero,
                        IntPtr.Zero) == SocketError.SocketError)
                    {
                        return GetLastSocketError();
                    }

                    ipPacketInformation = GetIPPacketInformation(&controlBuffer);
                }
                else if (ipv6)
                {
                    Interop.Winsock.ControlDataIPv6 controlBuffer;
                    wsaMsg.controlBuffer.Pointer = (IntPtr)(&controlBuffer);
                    wsaMsg.controlBuffer.Length = sizeof(Interop.Winsock.ControlDataIPv6);

                    if (socket.WSARecvMsgBlocking(
                        handle.DangerousGetHandle(),
                        (IntPtr)(&wsaMsg),
                        out bytesTransferred,
                        IntPtr.Zero,
                        IntPtr.Zero) == SocketError.SocketError)
                    {
                        return GetLastSocketError();
                    }

                    ipPacketInformation = GetIPPacketInformation(&controlBuffer);
                }
                else
                {
                    wsaMsg.controlBuffer.Pointer = IntPtr.Zero;
                    wsaMsg.controlBuffer.Length = 0;

                    if (socket.WSARecvMsgBlocking(
                        handle.DangerousGetHandle(),
                        (IntPtr)(&wsaMsg),
                        out bytesTransferred,
                        IntPtr.Zero,
                        IntPtr.Zero) == SocketError.SocketError)
                    {
                        return GetLastSocketError();
                    }
                }

                socketFlags = wsaMsg.flags;
            }

            return SocketError.Success;
        }

        public static unsafe SocketError ReceiveFrom(SafeSocketHandle handle, byte[] buffer, int offset, int size, SocketFlags socketFlags, byte[] socketAddress, ref int addressLength, out int bytesTransferred)
        {
            int bytesReceived;
            if (buffer.Length == 0)
            {
                bytesReceived = Interop.Winsock.recvfrom(handle.DangerousGetHandle(), null, 0, socketFlags, socketAddress, ref addressLength);
            }
            else
            {
                fixed (byte* pinnedBuffer = &buffer[0])
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

        public static SocketError WindowsIoctl(SafeSocketHandle handle, int ioControlCode, byte[] optionInValue, byte[] optionOutValue, out int optionLength)
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
                IntPtr.Zero,
                IntPtr.Zero);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError SetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
        {
            SocketError errorCode;
            if (optionLevel == SocketOptionLevel.Tcp &&
                (optionName == SocketOptionName.TcpKeepAliveTime || optionName == SocketOptionName.TcpKeepAliveInterval) &&
                IOControlKeepAlive.IsNeeded)
            {
                errorCode = IOControlKeepAlive.Set(handle, optionName, optionValue);
            }
            else
            {
                errorCode = Interop.Winsock.setsockopt(
                    handle,
                    optionLevel,
                    optionName,
                    ref optionValue,
                    sizeof(int));
            }
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError SetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            SocketError errorCode;
            if (optionLevel == SocketOptionLevel.Tcp &&
                (optionName == SocketOptionName.TcpKeepAliveTime || optionName == SocketOptionName.TcpKeepAliveInterval) &&
                IOControlKeepAlive.IsNeeded)
            {
                return IOControlKeepAlive.Set(handle, optionName, optionValue);
            }
            else
            {
                errorCode = Interop.Winsock.setsockopt(
                    handle,
                    optionLevel,
                    optionName,
                    optionValue,
                    optionValue != null ? optionValue.Length : 0);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
        }

        public static void SetReceivingDualModeIPv4PacketInformation(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
        }

        public static SocketError SetMulticastOption(SafeSocketHandle handle, SocketOptionName optionName, MulticastOption optionValue)
        {
            Interop.Winsock.IPMulticastRequest ipmr = new Interop.Winsock.IPMulticastRequest();

#pragma warning disable CS0618 // Address is marked obsolete
            ipmr.MulticastAddress = unchecked((int)optionValue.Group.Address);
#pragma warning restore CS0618

            if (optionValue.LocalAddress != null)
            {
#pragma warning disable CS0618 // Address is marked obsolete
                ipmr.InterfaceAddress = unchecked((int)optionValue.LocalAddress.Address);
#pragma warning restore CS0618
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

        public static SocketError SetIPv6MulticastOption(SafeSocketHandle handle, SocketOptionName optionName, IPv6MulticastOption optionValue)
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

        public static SocketError SetLingerOption(SafeSocketHandle handle, LingerOption optionValue)
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

        public static SocketError GetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, out int optionValue)
        {
            if (optionLevel == SocketOptionLevel.Tcp &&
                (optionName == SocketOptionName.TcpKeepAliveTime || optionName == SocketOptionName.TcpKeepAliveInterval) &&
                IOControlKeepAlive.IsNeeded)
            {
                optionValue = IOControlKeepAlive.Get(handle, optionName);
                return SocketError.Success;
            }

            int optionLength = 4; // sizeof(int)
            SocketError errorCode = Interop.Winsock.getsockopt(
                handle,
                optionLevel,
                optionName,
                out optionValue,
                ref optionLength);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError GetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue, ref int optionLength)
        {
            if (optionLevel == SocketOptionLevel.Tcp &&
                (optionName == SocketOptionName.TcpKeepAliveTime || optionName == SocketOptionName.TcpKeepAliveInterval) &&
                IOControlKeepAlive.IsNeeded)
            {
                return IOControlKeepAlive.Get(handle, optionName, optionValue, ref optionLength);
            }

            SocketError errorCode = Interop.Winsock.getsockopt(
               handle,
               optionLevel,
               optionName,
               optionValue,
               ref optionLength);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError GetMulticastOption(SafeSocketHandle handle, SocketOptionName optionName, out MulticastOption optionValue)
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

        public static SocketError GetIPv6MulticastOption(SafeSocketHandle handle, SocketOptionName optionName, out IPv6MulticastOption optionValue)
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

        public static SocketError GetLingerOption(SafeSocketHandle handle, out LingerOption optionValue)
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

        public static unsafe SocketError Poll(SafeSocketHandle handle, int microseconds, SelectMode mode, out bool status)
        {
            IntPtr rawHandle = handle.DangerousGetHandle();
            IntPtr* fileDescriptorSet = stackalloc IntPtr[2] { (IntPtr)1, rawHandle };
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

        public static unsafe SocketError Select(IList checkRead, IList checkWrite, IList checkError, int microseconds)
        {
            const int StackThreshold = 64; // arbitrary limit to avoid too much space on stack
            bool ShouldStackAlloc(IList list, ref IntPtr[] lease, out Span<IntPtr> span)
            {
                int count;
                if (list == null || (count = list.Count) == 0)
                {
                    span = default;
                    return false;
                }
                if (count >= StackThreshold) // note on >= : the first element is reserved for internal length
                {
                    span = lease = ArrayPool<IntPtr>.Shared.Rent(count + 1);
                    return false;
                }
                span = default;
                return true;
            }

            IntPtr[] leaseRead = null, leaseWrite = null, leaseError = null;
            try
            {
                Span<IntPtr> readfileDescriptorSet = ShouldStackAlloc(checkRead, ref leaseRead, out var tmp) ? stackalloc IntPtr[StackThreshold] : tmp;
                Socket.SocketListToFileDescriptorSet(checkRead, readfileDescriptorSet);
                Span<IntPtr> writefileDescriptorSet = ShouldStackAlloc(checkWrite, ref leaseWrite, out tmp) ? stackalloc IntPtr[StackThreshold] : tmp;
                Socket.SocketListToFileDescriptorSet(checkWrite, writefileDescriptorSet);
                Span<IntPtr> errfileDescriptorSet = ShouldStackAlloc(checkError, ref leaseError, out tmp) ? stackalloc IntPtr[StackThreshold] : tmp;
                Socket.SocketListToFileDescriptorSet(checkError, errfileDescriptorSet);
                
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
                fixed (IntPtr* readPtr = &MemoryMarshal.GetReference(readfileDescriptorSet))
                fixed (IntPtr* writePtr = &MemoryMarshal.GetReference(writefileDescriptorSet))
                fixed (IntPtr* errPtr = &MemoryMarshal.GetReference(errfileDescriptorSet))
                {
                    if (microseconds != -1)
                    {
                        Interop.Winsock.TimeValue IOwait = new Interop.Winsock.TimeValue();
                        MicrosecondsToTimeValue((long)(uint)microseconds, ref IOwait);

                        socketCount =
                            Interop.Winsock.select(
                                0, // ignored value
                                readPtr,
                                writePtr,
                                errPtr,
                                ref IOwait);
                    }
                    else
                    {
                        socketCount =
                            Interop.Winsock.select(
                                0, // ignored value
                                readPtr,
                                writePtr,
                                errPtr,
                                IntPtr.Zero);
                    }
                }
                if (NetEventSource.IsEnabled)
                    NetEventSource.Info(null, $"Interop.Winsock.select returns socketCount:{socketCount}");

                if ((SocketError)socketCount == SocketError.SocketError)
                {
                    return GetLastSocketError();
                }

                Socket.SelectFileDescriptor(checkRead, readfileDescriptorSet);
                Socket.SelectFileDescriptor(checkWrite, writefileDescriptorSet);
                Socket.SelectFileDescriptor(checkError, errfileDescriptorSet);

                return SocketError.Success;
            }
            finally
            {
                if (leaseRead != null) ArrayPool<IntPtr>.Shared.Return(leaseRead);
                if (leaseWrite != null) ArrayPool<IntPtr>.Shared.Return(leaseWrite);
                if (leaseError != null) ArrayPool<IntPtr>.Shared.Return(leaseError);
            }
        }

        public static SocketError Shutdown(SafeSocketHandle handle, bool isConnected, bool isDisconnected, SocketShutdown how)
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

        public static unsafe SocketError ConnectAsync(Socket socket, SafeSocketHandle handle, byte[] socketAddress, int socketAddressLen, ConnectOverlappedAsyncResult asyncResult)
        {
            // This will pin the socketAddress buffer.
            asyncResult.SetUnmanagedStructures(socketAddress);
            try
            {
                int ignoreBytesSent;
                bool success = socket.ConnectEx(
                    handle,
                    Marshal.UnsafeAddrOfPinnedArrayElement(socketAddress, 0),
                    socketAddressLen,
                    IntPtr.Zero,
                    0,
                    out ignoreBytesSent,
                    asyncResult.DangerousOverlappedPointer); // SafeHandle was just created in SetUnmanagedStructures

                return asyncResult.ProcessOverlappedResult(success, 0);
            }
            catch
            {
                asyncResult.ReleaseUnmanagedStructures();
                throw;
            }
        }

        public static unsafe SocketError SendAsync(SafeSocketHandle handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            // Set up unmanaged structures for overlapped WSASend.
            asyncResult.SetUnmanagedStructures(buffer, offset, count, null);
            try
            {
                int bytesTransferred;
                SocketError errorCode = Interop.Winsock.WSASend(
                    handle,
                    ref asyncResult._singleBuffer,
                    1, // There is only ever 1 buffer being sent.
                    out bytesTransferred,
                    socketFlags,
                    asyncResult.DangerousOverlappedPointer, // SafeHandle was just created in SetUnmanagedStructures
                    IntPtr.Zero);

                return asyncResult.ProcessOverlappedResult(errorCode == SocketError.Success, bytesTransferred);
            }
            catch
            {
                asyncResult.ReleaseUnmanagedStructures();
                throw;
            }
        }

        public static unsafe SocketError SendAsync(SafeSocketHandle handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            // Set up asyncResult for overlapped WSASend.
            asyncResult.SetUnmanagedStructures(buffers);
            try
            {
                int bytesTransferred;
                SocketError errorCode = Interop.Winsock.WSASend(
                    handle,
                    asyncResult._wsaBuffers,
                    asyncResult._wsaBuffers.Length,
                    out bytesTransferred,
                    socketFlags,
                    asyncResult.DangerousOverlappedPointer, // SafeHandle was just created in SetUnmanagedStructures
                    IntPtr.Zero);

                return asyncResult.ProcessOverlappedResult(errorCode == SocketError.Success, bytesTransferred);
            }
            catch
            {
                asyncResult.ReleaseUnmanagedStructures();
                throw;
            }
        }

        // This assumes preBuffer/postBuffer are pinned already 

        private static unsafe bool TransmitFileHelper(
            SafeHandle socket, 
            SafeHandle fileHandle,
            NativeOverlapped* overlapped,
            byte[] preBuffer,
            byte[] postBuffer,
            TransmitFileOptions flags)
        {
            bool needTransmitFileBuffers = false;
            Interop.Mswsock.TransmitFileBuffers transmitFileBuffers = default(Interop.Mswsock.TransmitFileBuffers);

            if (preBuffer != null && preBuffer.Length > 0)
            {
                needTransmitFileBuffers = true;
                transmitFileBuffers.Head = Marshal.UnsafeAddrOfPinnedArrayElement(preBuffer, 0);
                transmitFileBuffers.HeadLength = preBuffer.Length;
            }

            if (postBuffer != null && postBuffer.Length > 0)
            {
                needTransmitFileBuffers = true;
                transmitFileBuffers.Tail = Marshal.UnsafeAddrOfPinnedArrayElement(postBuffer, 0);
                transmitFileBuffers.TailLength = postBuffer.Length;
            }

            bool success = Interop.Mswsock.TransmitFile(socket, fileHandle, 0, 0, overlapped,
                needTransmitFileBuffers ? &transmitFileBuffers : null, flags);

            return success;
        }

        public static unsafe SocketError SendFileAsync(SafeSocketHandle handle, FileStream fileStream, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags, TransmitFileAsyncResult asyncResult)
        {
            asyncResult.SetUnmanagedStructures(fileStream, preBuffer, postBuffer, (flags & (TransmitFileOptions.Disconnect | TransmitFileOptions.ReuseSocket)) != 0);
            try
            {
                bool success = TransmitFileHelper(
                    handle, 
                    fileStream?.SafeFileHandle, 
                    asyncResult.DangerousOverlappedPointer, // SafeHandle was just created in SetUnmanagedStructures
                    preBuffer, 
                    postBuffer, 
                    flags);

                return asyncResult.ProcessOverlappedResult(success, 0);
            }
            catch
            {
                asyncResult.ReleaseUnmanagedStructures();
                throw;
            }
        }

        public static unsafe SocketError SendToAsync(SafeSocketHandle handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, OverlappedAsyncResult asyncResult)
        {
            // Set up asyncResult for overlapped WSASendTo.
            asyncResult.SetUnmanagedStructures(buffer, offset, count, socketAddress);
            try
            {
                int bytesTransferred;
                SocketError errorCode = Interop.Winsock.WSASendTo(
                    handle,
                    ref asyncResult._singleBuffer,
                    1, // There is only ever 1 buffer being sent.
                    out bytesTransferred,
                    socketFlags,
                    asyncResult.GetSocketAddressPtr(),
                    asyncResult.SocketAddress.Size,
                    asyncResult.DangerousOverlappedPointer, // SafeHandle was just created in SetUnmanagedStructures
                    IntPtr.Zero);

                return asyncResult.ProcessOverlappedResult(errorCode == SocketError.Success, bytesTransferred);
            }
            catch
            {
                asyncResult.ReleaseUnmanagedStructures();
                throw;
            }
        }

        public static unsafe SocketError ReceiveAsync(SafeSocketHandle handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            // Set up asyncResult for overlapped WSARecv.
            asyncResult.SetUnmanagedStructures(buffer, offset, count, null);
            try
            {
                int bytesTransferred;
                SocketError errorCode = Interop.Winsock.WSARecv(
                    handle,
                    ref asyncResult._singleBuffer,
                    1,
                    out bytesTransferred,
                    ref socketFlags,
                    asyncResult.DangerousOverlappedPointer, // SafeHandle was just created in SetUnmanagedStructures
                    IntPtr.Zero);

                return asyncResult.ProcessOverlappedResult(errorCode == SocketError.Success, bytesTransferred);
            }
            catch
            {
                asyncResult.ReleaseUnmanagedStructures();
                throw;
            }
        }

        public static unsafe SocketError ReceiveAsync(SafeSocketHandle handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            // Set up asyncResult for overlapped WSASend.
            asyncResult.SetUnmanagedStructures(buffers);
            try
            {
                int bytesTransferred;
                SocketError errorCode = Interop.Winsock.WSARecv(
                    handle,
                    asyncResult._wsaBuffers,
                    asyncResult._wsaBuffers.Length,
                    out bytesTransferred,
                    ref socketFlags,
                    asyncResult.DangerousOverlappedPointer, // SafeHandle was just created in SetUnmanagedStructures
                    IntPtr.Zero);

                return asyncResult.ProcessOverlappedResult(errorCode == SocketError.Success, bytesTransferred);
            }
            catch
            {
                asyncResult.ReleaseUnmanagedStructures();
                throw;
            }
        }

        public static unsafe SocketError ReceiveFromAsync(SafeSocketHandle handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, OverlappedAsyncResult asyncResult)
        {
            // Set up asyncResult for overlapped WSARecvFrom.
            asyncResult.SetUnmanagedStructures(buffer, offset, count, socketAddress);
            try
            {
                int bytesTransferred;
                SocketError errorCode = Interop.Winsock.WSARecvFrom(
                    handle,
                    ref asyncResult._singleBuffer,
                    1,
                    out bytesTransferred,
                    ref socketFlags,
                    asyncResult.GetSocketAddressPtr(),
                    asyncResult.GetSocketAddressSizePtr(),
                    asyncResult.DangerousOverlappedPointer, // SafeHandle was just created in SetUnmanagedStructures
                    IntPtr.Zero);

                return asyncResult.ProcessOverlappedResult(errorCode == SocketError.Success, bytesTransferred);
            }
            catch
            {
                asyncResult.ReleaseUnmanagedStructures();
                throw;
            }
        }

        public static unsafe SocketError ReceiveMessageFromAsync(Socket socket, SafeSocketHandle handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, ReceiveMessageOverlappedAsyncResult asyncResult)
        {
            asyncResult.SetUnmanagedStructures(buffer, offset, count, socketAddress, socketFlags);
            try
            {
                int bytesTransfered;
                SocketError errorCode = (SocketError)socket.WSARecvMsg(
                    handle,
                    Marshal.UnsafeAddrOfPinnedArrayElement(asyncResult._messageBuffer, 0),
                    out bytesTransfered,
                    asyncResult.DangerousOverlappedPointer, // SafeHandle was just created in SetUnmanagedStructures
                    IntPtr.Zero);

                return asyncResult.ProcessOverlappedResult(errorCode == SocketError.Success, bytesTransfered);
            }
            catch
            {
                asyncResult.ReleaseUnmanagedStructures();
                throw;
            }
        }

        public static unsafe SocketError AcceptAsync(Socket socket, SafeSocketHandle handle, SafeSocketHandle acceptHandle, int receiveSize, int socketAddressSize, AcceptOverlappedAsyncResult asyncResult)
        {
            // The buffer needs to contain the requested data plus room for two sockaddrs and 16 bytes
            // of associated data for each.
            int addressBufferSize = socketAddressSize + 16;
            byte[] buffer = new byte[receiveSize + ((addressBufferSize) * 2)];

            // Set up asyncResult for overlapped AcceptEx.
            // This call will use completion ports on WinNT.
            asyncResult.SetUnmanagedStructures(buffer, addressBufferSize);
            try
            {
                // This can throw ObjectDisposedException.
                int bytesTransferred;
                bool success = socket.AcceptEx(
                    handle,
                    acceptHandle,
                    Marshal.UnsafeAddrOfPinnedArrayElement(asyncResult.Buffer, 0),
                    receiveSize,
                    addressBufferSize,
                    addressBufferSize,
                    out bytesTransferred,
                    asyncResult.DangerousOverlappedPointer); // SafeHandle was just created in SetUnmanagedStructures

                return asyncResult.ProcessOverlappedResult(success, 0);
            }
            catch
            {
                asyncResult.ReleaseUnmanagedStructures();
                throw;
            }
        }

        public static void CheckDualModeReceiveSupport(Socket socket)
        {
            // Dual-mode sockets support received packet info on Windows.
        }

        internal static unsafe SocketError DisconnectAsync(Socket socket, SafeSocketHandle handle, bool reuseSocket, DisconnectOverlappedAsyncResult asyncResult)
        {
            asyncResult.SetUnmanagedStructures(null);
            try
            {
                // This can throw ObjectDisposedException
                bool success = socket.DisconnectEx(
                    handle,
                    asyncResult.DangerousOverlappedPointer, // SafeHandle was just created in SetUnmanagedStructures
                    (int)(reuseSocket ? TransmitFileOptions.ReuseSocket : 0), 
                    0);

                return asyncResult.ProcessOverlappedResult(success, 0);
            }
            catch
            {
                asyncResult.ReleaseUnmanagedStructures();
                throw;
            }
        }

        internal static SocketError Disconnect(Socket socket, SafeSocketHandle handle, bool reuseSocket)
        {
            SocketError errorCode = SocketError.Success;

            // This can throw ObjectDisposedException (handle, and retrieving the delegate).
            if (!socket.DisconnectExBlocking(handle, IntPtr.Zero, (int)(reuseSocket ? TransmitFileOptions.ReuseSocket : 0), 0))
            {
                errorCode = GetLastSocketError();
            }

            return errorCode;
        }
    }
}
