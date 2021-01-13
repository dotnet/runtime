// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

#if BIGENDIAN
using System.Buffers.Binary;
#endif

namespace System.Net.Sockets
{
    internal static class SocketPal
    {
        public const bool SupportsMultipleConnectAttempts = true;
        public static readonly int MaximumAddressSize = UnixDomainSocketEndPoint.MaxAddressSize;

        private static void MicrosecondsToTimeValue(long microseconds, ref Interop.Winsock.TimeValue socketTime)
        {
            const int microcnv = 1000000;

            socketTime.Seconds = (int)(microseconds / microcnv);
            socketTime.Microseconds = (int)(microseconds % microcnv);
        }

        public static SocketError GetLastSocketError()
        {
            int win32Error = Marshal.GetLastWin32Error();
            Debug.Assert(win32Error != 0, "Expected non-0 error");
            return (SocketError)win32Error;
        }

        public static SocketError CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, out SafeSocketHandle socket)
        {
            Interop.Winsock.EnsureInitialized();

            IntPtr handle = Interop.Winsock.WSASocketW(addressFamily, socketType, protocolType, IntPtr.Zero, 0, Interop.Winsock.SocketConstructorFlags.WSA_FLAG_OVERLAPPED |
                                                                                                                Interop.Winsock.SocketConstructorFlags.WSA_FLAG_NO_HANDLE_INHERIT);

            socket = new SafeSocketHandle(handle, ownsHandle: true);
            if (socket.IsInvalid)
            {
                SocketError error = GetLastSocketError();
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"WSASocketW failed with error {error}");
                socket.Dispose();
                return error;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, socket);
            return SocketError.Success;
        }

        public static unsafe SocketError CreateSocket(
            SocketInformation socketInformation,
            out SafeSocketHandle socket,
            ref AddressFamily addressFamily,
            ref SocketType socketType,
            ref ProtocolType protocolType)
        {
            if (socketInformation.ProtocolInformation == null || socketInformation.ProtocolInformation.Length < sizeof(Interop.Winsock.WSAPROTOCOL_INFOW))
            {
                throw new ArgumentException(SR.net_sockets_invalid_socketinformation, nameof(socketInformation));
            }

            Interop.Winsock.EnsureInitialized();

            fixed (byte* protocolInfoBytes = socketInformation.ProtocolInformation)
            {
                // Sockets are non-inheritable in .NET Core.
                // Handle properties like HANDLE_FLAG_INHERIT are not cloned with socket duplication, therefore
                // we need to disable handle inheritance when constructing the new socket handle from Protocol Info.
                // Additionally, it looks like WSA_FLAG_NO_HANDLE_INHERIT has no effect when being used with the Protocol Info
                // variant of WSASocketW, so it is being passed to that call only for consistency.
                // Inheritance is being disabled with SetHandleInformation(...) after the WSASocketW call.
                IntPtr handle = Interop.Winsock.WSASocketW(
                    (AddressFamily)(-1),
                    (SocketType)(-1),
                    (ProtocolType)(-1),
                    (IntPtr)protocolInfoBytes,
                    0,
                    Interop.Winsock.SocketConstructorFlags.WSA_FLAG_OVERLAPPED |
                    Interop.Winsock.SocketConstructorFlags.WSA_FLAG_NO_HANDLE_INHERIT);

                socket = new SafeSocketHandle(handle, ownsHandle: true);

                if (socket.IsInvalid)
                {
                    SocketError error = GetLastSocketError();
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"WSASocketW failed with error {error}");
                    socket.Dispose();
                    return error;
                }

                if (!Interop.Kernel32.SetHandleInformation(socket, Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT, 0))
                {
                    // Returning SocketError for consistency, since the call site can deal with conversion, and
                    // the most common SetHandleInformation error (AccessDenied) is included in SocketError anyways:
                    SocketError error = GetLastSocketError();
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"SetHandleInformation failed with error {error}");
                    socket.Dispose();

                    return error;
                }

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, socket);

                Interop.Winsock.WSAPROTOCOL_INFOW* protocolInfo = (Interop.Winsock.WSAPROTOCOL_INFOW*)protocolInfoBytes;
                addressFamily = protocolInfo->iAddressFamily;
                socketType = protocolInfo->iSocketType;
                protocolType = protocolInfo->iProtocol;

                return SocketError.Success;
            }
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

        public static unsafe SocketError GetSockName(SafeSocketHandle handle, byte* buffer, int* nameLen)
        {
            SocketError errorCode = Interop.Winsock.getsockname(handle, buffer, nameLen);
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

        public static unsafe SocketError GetPeerName(SafeSocketHandle handle, Span<byte> buffer, ref int nameLen)
        {
            fixed (byte* rawBuffer = buffer)
            {
                SocketError errorCode = Interop.Winsock.getpeername(handle, rawBuffer, ref nameLen);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
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

        public static SocketError Accept(SafeSocketHandle listenSocket, byte[] socketAddress, ref int socketAddressSize, out SafeSocketHandle socket)
        {
            IntPtr handle = Interop.Winsock.accept(listenSocket, socketAddress, ref socketAddressSize);

            socket = new SafeSocketHandle(handle, ownsHandle: true);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, socket);

            return socket.IsInvalid ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Connect(SafeSocketHandle handle, byte[] peerAddress, int peerAddressLen)
        {
            SocketError errorCode = Interop.Winsock.WSAConnect(
                handle,
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

            WSABuffer[]? leasedWSA = null;
            GCHandle[]? leasedGC = null;
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
                    WSABuffers[i].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(buffer.Array!, buffer.Offset);
                }

                unsafe
                {
                    SocketError errorCode = Interop.Winsock.WSASend(
                        handle,
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
                    ArrayPool<WSABuffer>.Shared.Return(leasedWSA!);
                    ArrayPool<GCHandle>.Shared.Return(leasedGC!);
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
                bytesSent = Interop.Winsock.send(handle, bufferPtr, buffer.Length, socketFlags);
            }

            if (bytesSent == (int)SocketError.SocketError)
            {
                bytesTransferred = 0;
                return GetLastSocketError();
            }

            bytesTransferred = bytesSent;
            return SocketError.Success;
        }

        public static unsafe SocketError SendFile(SafeSocketHandle handle, SafeFileHandle? fileHandle, ReadOnlySpan<byte> preBuffer, ReadOnlySpan<byte> postBuffer, TransmitFileOptions flags)
        {
            fixed (byte* prePinnedBuffer = preBuffer)
            fixed (byte* postPinnedBuffer = postBuffer)
            {
                bool success = TransmitFileHelper(handle, fileHandle, null, (IntPtr)prePinnedBuffer, preBuffer.Length, (IntPtr)postPinnedBuffer, postBuffer.Length, flags);
                return success ? SocketError.Success : GetLastSocketError();
            }
        }

        public static unsafe SocketError SendTo(SafeSocketHandle handle, byte[] buffer, int offset, int size, SocketFlags socketFlags, byte[] peerAddress, int peerAddressSize, out int bytesTransferred)
        {
            int bytesSent;
            if (buffer.Length == 0)
            {
                bytesSent = Interop.Winsock.sendto(
                    handle,
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
                        handle,
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

        public static SocketError Receive(SafeSocketHandle handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out int bytesTransferred)
        {
            const int StackThreshold = 16; // arbitrary limit to avoid too much space on stack (note: may be over-sized, that's OK - length passed separately)
            int count = buffers.Count;
            bool useStack = count <= StackThreshold;

            WSABuffer[]? leasedWSA = null;
            GCHandle[]? leasedGC = null;
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
                    WSABuffers[i].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(buffer.Array!, buffer.Offset);
                }

                unsafe
                {
                    SocketError errorCode = Interop.Winsock.WSARecv(
                        handle,
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
                    ArrayPool<WSABuffer>.Shared.Return(leasedWSA!);
                    ArrayPool<GCHandle>.Shared.Return(leasedGC!);
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
                bytesReceived = Interop.Winsock.recv(handle, bufferPtr, buffer.Length, socketFlags);
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
            if (controlBuffer->length == (UIntPtr)sizeof(Interop.Winsock.ControlData))
            {
                // IPv4 client connectiong to dual mode socket.
                return GetIPPacketInformation((Interop.Winsock.ControlData*)controlBuffer);
            }

            IPAddress address = controlBuffer->length != UIntPtr.Zero ?
                new IPAddress(new ReadOnlySpan<byte>(controlBuffer->address, Interop.Winsock.IPv6AddressLength)) :
                IPAddress.IPv6None;

            return new IPPacketInformation(address, (int)controlBuffer->index);
        }

        public static unsafe SocketError ReceiveMessageFrom(Socket socket, SafeSocketHandle handle, byte[] buffer, int offset, int size, ref SocketFlags socketFlags, Internals.SocketAddress socketAddress, out Internals.SocketAddress receiveAddress, out IPPacketInformation ipPacketInformation, out int bytesTransferred)
        {
            return ReceiveMessageFrom(socket, handle, new Span<byte>(buffer, offset, size), ref socketFlags, socketAddress, out receiveAddress, out ipPacketInformation, out bytesTransferred);
        }

        public static unsafe SocketError ReceiveMessageFrom(Socket socket, SafeSocketHandle handle, Span<byte> buffer, ref SocketFlags socketFlags, Internals.SocketAddress socketAddress, out Internals.SocketAddress receiveAddress, out IPPacketInformation ipPacketInformation, out int bytesTransferred)
        {
            bool ipv4, ipv6;
            Socket.GetIPProtocolInformation(socket.AddressFamily, socketAddress, out ipv4, out ipv6);

            bytesTransferred = 0;
            receiveAddress = socketAddress;
            ipPacketInformation = default(IPPacketInformation);
            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer))
            fixed (byte* ptrSocketAddress = socketAddress.Buffer)
            {
                Interop.Winsock.WSAMsg wsaMsg;
                wsaMsg.socketAddress = (IntPtr)ptrSocketAddress;
                wsaMsg.addressLength = (uint)socketAddress.Size;
                wsaMsg.flags = socketFlags;

                WSABuffer wsaBuffer;
                wsaBuffer.Length = buffer.Length;
                wsaBuffer.Pointer = (IntPtr)bufferPtr;
                wsaMsg.buffers = (IntPtr)(&wsaBuffer);
                wsaMsg.count = 1;

                if (ipv4)
                {
                    Interop.Winsock.ControlData controlBuffer;
                    wsaMsg.controlBuffer.Pointer = (IntPtr)(&controlBuffer);
                    wsaMsg.controlBuffer.Length = sizeof(Interop.Winsock.ControlData);

                    if (socket.WSARecvMsgBlocking(
                        handle,
                        (IntPtr)(&wsaMsg),
                        out bytesTransferred) == SocketError.SocketError)
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
                        handle,
                        (IntPtr)(&wsaMsg),
                        out bytesTransferred) == SocketError.SocketError)
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
                        handle,
                        (IntPtr)(&wsaMsg),
                        out bytesTransferred) == SocketError.SocketError)
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
                bytesReceived = Interop.Winsock.recvfrom(handle, null, 0, socketFlags, socketAddress, ref addressLength);
            }
            else
            {
                fixed (byte* pinnedBuffer = &buffer[0])
                {
                    bytesReceived = Interop.Winsock.recvfrom(handle, pinnedBuffer + offset, size, socketFlags, socketAddress, ref addressLength);
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

        public static SocketError WindowsIoctl(SafeSocketHandle handle, int ioControlCode, byte[]? optionInValue, byte[]? optionOutValue, out int optionLength)
        {
            if (ioControlCode == Interop.Winsock.IoctlSocketConstants.FIONBIO)
            {
                throw new InvalidOperationException(SR.net_sockets_useblocking);
            }

            SocketError errorCode = Interop.Winsock.WSAIoctl_Blocking(
                handle,
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

        public static unsafe SocketError SetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            SocketError errorCode;
            if (optionLevel == SocketOptionLevel.Tcp &&
                (optionName == SocketOptionName.TcpKeepAliveTime || optionName == SocketOptionName.TcpKeepAliveInterval) &&
                IOControlKeepAlive.IsNeeded)
            {
                return IOControlKeepAlive.Set(handle, optionName, optionValue);
            }

            fixed (byte* optionValuePtr = optionValue)
            {
                errorCode = Interop.Winsock.setsockopt(
                    handle,
                    optionLevel,
                    optionName,
                    optionValuePtr,
                    optionValue != null ? optionValue.Length : 0);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
        }

        public static unsafe SocketError SetRawSockOpt(SafeSocketHandle handle, int optionLevel, int optionName, ReadOnlySpan<byte> optionValue)
        {
            fixed (byte* optionValuePtr = optionValue)
            {
                SocketError errorCode = Interop.Winsock.setsockopt(
                    handle,
                    (SocketOptionLevel)optionLevel,
                    (SocketOptionName)optionName,
                    optionValuePtr,
                    optionValue.Length);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
        }

        public static void SetReceivingDualModeIPv4PacketInformation(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
        }

        public static SocketError SetMulticastOption(SafeSocketHandle handle, SocketOptionName optionName, MulticastOption optionValue)
        {
            Interop.Winsock.IPMulticastRequest ipmr = default;

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
            ipmr.MulticastAddress = BinaryPrimitives.ReverseEndianness(ipmr.MulticastAddress);

            if (optionValue.LocalAddress != null)
            {
                ipmr.InterfaceAddress = BinaryPrimitives.ReverseEndianness(ipmr.InterfaceAddress);
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
            Interop.Winsock.IPv6MulticastRequest ipmr = default;

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
            Interop.Winsock.Linger lngopt = default;
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

        public static unsafe SocketError GetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, out int optionValue)
        {
            if (optionLevel == SocketOptionLevel.Tcp &&
                (optionName == SocketOptionName.TcpKeepAliveTime || optionName == SocketOptionName.TcpKeepAliveInterval) &&
                IOControlKeepAlive.IsNeeded)
            {
                optionValue = IOControlKeepAlive.Get(handle, optionName);
                return SocketError.Success;
            }

            int optionLength = sizeof(int);
            int tmpOptionValue = 0;
            SocketError errorCode = Interop.Winsock.getsockopt(
                handle,
                optionLevel,
                optionName,
                (byte*)&tmpOptionValue,
                ref optionLength);

            optionValue = tmpOptionValue;
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError GetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue, ref int optionLength)
        {
            if (optionLevel == SocketOptionLevel.Tcp &&
                (optionName == SocketOptionName.TcpKeepAliveTime || optionName == SocketOptionName.TcpKeepAliveInterval) &&
                IOControlKeepAlive.IsNeeded)
            {
                return IOControlKeepAlive.Get(handle, optionName, optionValue, ref optionLength);
            }

            fixed (byte* optionValuePtr = optionValue)
            {
                SocketError errorCode = Interop.Winsock.getsockopt(
                   handle,
                   optionLevel,
                   optionName,
                   optionValuePtr,
                   ref optionLength);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
        }

        public static unsafe SocketError GetRawSockOpt(SafeSocketHandle handle, int optionLevel, int optionName, Span<byte> optionValue, ref int optionLength)
        {
            Debug.Assert((uint)optionLength <= optionValue.Length);

            SocketError errorCode;
            fixed (byte* optionValuePtr = optionValue)
            {
                errorCode = Interop.Winsock.getsockopt(
                    handle,
                    (SocketOptionLevel)optionLevel,
                    (SocketOptionName)optionName,
                    optionValuePtr,
                    ref optionLength);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
        }

        public static SocketError GetMulticastOption(SafeSocketHandle handle, SocketOptionName optionName, out MulticastOption? optionValue)
        {
            Interop.Winsock.IPMulticastRequest ipmr = default;
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
            ipmr.MulticastAddress = BinaryPrimitives.ReverseEndianness(ipmr.MulticastAddress);
            ipmr.InterfaceAddress = BinaryPrimitives.ReverseEndianness(ipmr.InterfaceAddress);
#endif  // BIGENDIAN

            IPAddress multicastAddr = new IPAddress(ipmr.MulticastAddress);
            IPAddress multicastIntr = new IPAddress(ipmr.InterfaceAddress);
            optionValue = new MulticastOption(multicastAddr, multicastIntr);

            return SocketError.Success;
        }

        public static SocketError GetIPv6MulticastOption(SafeSocketHandle handle, SocketOptionName optionName, out IPv6MulticastOption? optionValue)
        {
            Interop.Winsock.IPv6MulticastRequest ipmr = default;

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

        public static SocketError GetLingerOption(SafeSocketHandle handle, out LingerOption? optionValue)
        {
            Interop.Winsock.Linger lngopt = default;
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
            bool refAdded = false;
            try
            {
                handle.DangerousAddRef(ref refAdded);

                IntPtr rawHandle = handle.DangerousGetHandle();
                IntPtr* fileDescriptorSet = stackalloc IntPtr[2] { (IntPtr)1, rawHandle };
                Interop.Winsock.TimeValue IOwait = default;

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
            finally
            {
                if (refAdded)
                {
                    handle.DangerousRelease();
                }
            }
        }

        public static unsafe SocketError Select(IList? checkRead, IList? checkWrite, IList? checkError, int microseconds)
        {
            const int StackThreshold = 64; // arbitrary limit to avoid too much space on stack
            static bool ShouldStackAlloc(IList? list, ref IntPtr[]? lease, out Span<IntPtr> span)
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

            IntPtr[]? leaseRead = null, leaseWrite = null, leaseError = null;
            int refsAdded = 0;
            try
            {
                // In case we can't increase the reference count for each Socket,
                // we'll unref refAdded Sockets in the finally block ordered: [checkRead, checkWrite, checkError].
                Span<IntPtr> readfileDescriptorSet = ShouldStackAlloc(checkRead, ref leaseRead, out var tmp) ? stackalloc IntPtr[StackThreshold] : tmp;
                Socket.SocketListToFileDescriptorSet(checkRead, readfileDescriptorSet, ref refsAdded);
                Span<IntPtr> writefileDescriptorSet = ShouldStackAlloc(checkWrite, ref leaseWrite, out tmp) ? stackalloc IntPtr[StackThreshold] : tmp;
                Socket.SocketListToFileDescriptorSet(checkWrite, writefileDescriptorSet, ref refsAdded);
                Span<IntPtr> errfileDescriptorSet = ShouldStackAlloc(checkError, ref leaseError, out tmp) ? stackalloc IntPtr[StackThreshold] : tmp;
                Socket.SocketListToFileDescriptorSet(checkError, errfileDescriptorSet, ref refsAdded);

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
                        Interop.Winsock.TimeValue IOwait = default;
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
                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(null, $"Interop.Winsock.select returns socketCount:{socketCount}");

                if ((SocketError)socketCount == SocketError.SocketError)
                {
                    return GetLastSocketError();
                }

                // Remove from the lists any entries which weren't set
                Socket.SelectFileDescriptor(checkRead, readfileDescriptorSet, ref refsAdded);
                Socket.SelectFileDescriptor(checkWrite, writefileDescriptorSet, ref refsAdded);
                Socket.SelectFileDescriptor(checkError, errfileDescriptorSet, ref refsAdded);

                return SocketError.Success;
            }
            finally
            {
                if (leaseRead != null) ArrayPool<IntPtr>.Shared.Return(leaseRead);
                if (leaseWrite != null) ArrayPool<IntPtr>.Shared.Return(leaseWrite);
                if (leaseError != null) ArrayPool<IntPtr>.Shared.Return(leaseError);

                // This order matches with the AddToPollArray calls
                // to release only the handles that were ref'd.
                Socket.SocketListDangerousReleaseRefs(checkRead, ref refsAdded);
                Socket.SocketListDangerousReleaseRefs(checkWrite, ref refsAdded);
                Socket.SocketListDangerousReleaseRefs(checkError, ref refsAdded);
                Debug.Assert(refsAdded == 0);
            }
        }

        public static SocketError Shutdown(SafeSocketHandle handle, bool isConnected, bool isDisconnected, SocketShutdown how)
        {
            SocketError err = Interop.Winsock.shutdown(handle, (int)how);
            if (err != SocketError.SocketError)
            {
                handle.TrackShutdown(how);
                return SocketError.Success;
            }

            err = GetLastSocketError();
            Debug.Assert(err != SocketError.NotConnected || (!isConnected && !isDisconnected));
            return err;
        }

        // This assumes preBuffer/postBuffer are pinned already

        private static unsafe bool TransmitFileHelper(
            SafeHandle socket,
            SafeHandle? fileHandle,
            NativeOverlapped* overlapped,
            IntPtr pinnedPreBuffer,
            int preBufferLength,
            IntPtr pinnedPostBuffer,
            int postBufferLength,
            TransmitFileOptions flags)
        {
            bool needTransmitFileBuffers = false;
            Interop.Mswsock.TransmitFileBuffers transmitFileBuffers = default;

            if (preBufferLength > 0)
            {
                needTransmitFileBuffers = true;
                transmitFileBuffers.Head = pinnedPreBuffer;
                transmitFileBuffers.HeadLength = preBufferLength;
            }

            if (postBufferLength > 0)
            {
                needTransmitFileBuffers = true;
                transmitFileBuffers.Tail = pinnedPostBuffer;
                transmitFileBuffers.TailLength = postBufferLength;
            }

            bool releaseRef = false;
            IntPtr fileHandlePtr = IntPtr.Zero;
            try
            {
                if (fileHandle != null)
                {
                    fileHandle.DangerousAddRef(ref releaseRef);
                    fileHandlePtr = fileHandle.DangerousGetHandle();
                }

                return Interop.Mswsock.TransmitFile(
                    socket, fileHandlePtr, 0, 0, overlapped,
                    needTransmitFileBuffers ? &transmitFileBuffers : null, flags);
            }
            finally
            {
                if (releaseRef)
                {
                    fileHandle!.DangerousRelease();
                }
            }
        }

        public static unsafe SocketError SendFileAsync(SafeSocketHandle handle, FileStream? fileStream, byte[]? preBuffer, byte[]? postBuffer, TransmitFileOptions flags, TransmitFileAsyncResult asyncResult)
        {
            asyncResult.SetUnmanagedStructures(fileStream, preBuffer, postBuffer, (flags & (TransmitFileOptions.Disconnect | TransmitFileOptions.ReuseSocket)) != 0);
            try
            {
                bool success = TransmitFileHelper(
                    handle,
                    fileStream?.SafeFileHandle,
                    asyncResult.DangerousOverlappedPointer, // SafeHandle was just created in SetUnmanagedStructures
                    preBuffer is not null ? Marshal.UnsafeAddrOfPinnedArrayElement(preBuffer, 0) : IntPtr.Zero,
                    preBuffer?.Length ?? 0,
                    postBuffer is not null ? Marshal.UnsafeAddrOfPinnedArrayElement(postBuffer, 0) : IntPtr.Zero,
                    postBuffer?.Length ?? 0,
                    flags);

                return asyncResult.ProcessOverlappedResult(success, 0);
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
                    Marshal.UnsafeAddrOfPinnedArrayElement(asyncResult.Buffer!, 0),
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

        internal static SocketError Disconnect(Socket socket, SafeSocketHandle handle, bool reuseSocket)
        {
            SocketError errorCode = SocketError.Success;

            // This can throw ObjectDisposedException (handle, and retrieving the delegate).
            if (!socket.DisconnectExBlocking(handle, (int)(reuseSocket ? TransmitFileOptions.ReuseSocket : 0), 0))
            {
                errorCode = GetLastSocketError();
            }

            return errorCode;
        }

        internal static unsafe SocketError DuplicateSocket(SafeSocketHandle handle, int targetProcessId, out SocketInformation socketInformation)
        {
            socketInformation = new SocketInformation
            {
                ProtocolInformation = new byte[sizeof(Interop.Winsock.WSAPROTOCOL_INFOW)]
            };

            fixed (byte* protocolInfoBytes = socketInformation.ProtocolInformation)
            {
                Interop.Winsock.WSAPROTOCOL_INFOW* lpProtocolInfo = (Interop.Winsock.WSAPROTOCOL_INFOW*)protocolInfoBytes;
                int result = Interop.Winsock.WSADuplicateSocket(handle, (uint)targetProcessId, lpProtocolInfo);
                return result == 0 ? SocketError.Success : GetLastSocketError();
            }
        }
    }
}
