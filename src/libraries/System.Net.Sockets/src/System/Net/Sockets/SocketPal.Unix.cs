// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    internal static partial class SocketPal
    {
        // The API that uses this information is not supported on *nix, and will throw
        // PlatformNotSupportedException instead.
        public const int ProtocolInformationSize = 0;

        public const bool SupportsMultipleConnectAttempts = false;
        private readonly static bool SupportsDualModeIPv4PacketInfo = GetPlatformSupportsDualModeIPv4PacketInfo();

        private static bool GetPlatformSupportsDualModeIPv4PacketInfo()
        {
            return Interop.Sys.PlatformSupportsDualModeIPv4PacketInfo();
        }

        public static SocketError GetSocketErrorForErrorCode(Interop.Error errorCode)
        {
            // NOTE: SocketError values with no direct mapping have been omitted for now.
            //
            //       These values are:
            //       - SocketError.SocketNotSupported
            //       - SocketError.OperationNotSupported
            //       - SocketError.ProtocolFamilyNotSupported
            //       - SocketError.NoBufferSpaceAvailable
            //       - SocketError.HostDown
            //       - SocketError.ProcessLimit
            //
            //        Although they are not present in this mapping, SocketError.IOPending and
            //        SocketError.OperationAborted are returned directly methods on
            //        SocketAsyncContext (these errors are only relevant for async I/O).

            switch (errorCode)
            {
                case Interop.Error.SUCCESS:
                    return SocketError.Success;

                case Interop.Error.EINTR:
                    return SocketError.Interrupted;

                case Interop.Error.EACCES:
                    return SocketError.AccessDenied;

                case Interop.Error.EFAULT:
                    return SocketError.Fault;

                case Interop.Error.EBADF:
                case Interop.Error.EINVAL:
                    return SocketError.InvalidArgument;

                case Interop.Error.EMFILE:
                case Interop.Error.ENFILE:
                    return SocketError.TooManyOpenSockets;

                case Interop.Error.EAGAIN:
                    return SocketError.WouldBlock;

                case Interop.Error.EINPROGRESS:
                    return SocketError.InProgress;

                case Interop.Error.EALREADY:
                    return SocketError.AlreadyInProgress;

                case Interop.Error.ENOTSOCK:
                    return SocketError.NotSocket;

                case Interop.Error.EDESTADDRREQ:
                    return SocketError.DestinationAddressRequired;

                case Interop.Error.EMSGSIZE:
                    return SocketError.MessageSize;

                case Interop.Error.EPROTOTYPE:
                    return SocketError.ProtocolType;

                case Interop.Error.ENOPROTOOPT:
                case Interop.Error.ENOTSUP:
                    return SocketError.ProtocolOption;

                case Interop.Error.EPROTONOSUPPORT:
                    return SocketError.ProtocolNotSupported;

                case Interop.Error.EAFNOSUPPORT:
                    return SocketError.AddressFamilyNotSupported;

                case Interop.Error.EADDRINUSE:
                    return SocketError.AddressAlreadyInUse;

                case Interop.Error.EADDRNOTAVAIL:
                    return SocketError.AddressNotAvailable;

                case Interop.Error.ENETDOWN:
                    return SocketError.NetworkDown;

                case Interop.Error.ENETUNREACH:
                    return SocketError.NetworkUnreachable;

                case Interop.Error.ENETRESET:
                    return SocketError.NetworkReset;

                case Interop.Error.ECONNABORTED:
                    return SocketError.ConnectionAborted;

                case Interop.Error.ECONNRESET:
                    return SocketError.ConnectionReset;

                case Interop.Error.EISCONN:
                    return SocketError.IsConnected;

                case Interop.Error.ENOTCONN:
                    return SocketError.NotConnected;

                case Interop.Error.ETIMEDOUT:
                    return SocketError.TimedOut;

                case Interop.Error.ECONNREFUSED:
                    return SocketError.ConnectionRefused;

                case Interop.Error.EHOSTUNREACH:
                    return SocketError.HostUnreachable;

                case Interop.Error.EPIPE:
                    return SocketError.Shutdown;

                default:
                    return SocketError.SocketError;
            }
        }

        private static unsafe IPPacketInformation GetIPPacketInformation(Interop.Sys.MessageHeader* messageHeader, bool isIPv4, bool isIPv6)
        {
            if (!isIPv4 && !isIPv6)
            {
                return default(IPPacketInformation);
            }

            Interop.Sys.IPPacketInformation nativePacketInfo;
            if (!Interop.Sys.TryGetIPPacketInformation(messageHeader, isIPv4, &nativePacketInfo))
            {
                return default(IPPacketInformation);
            }

            return new IPPacketInformation(nativePacketInfo.Address.GetIPAddress(), nativePacketInfo.InterfaceIndex);
        }

        public static SocketError CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, out SafeCloseSocket socket)
        {
            return SafeCloseSocket.CreateSocket(addressFamily, socketType, protocolType, out socket);
        }

        private static unsafe int Receive(int fd, SocketFlags flags, int available, byte[] buffer, int offset, int count, byte[] socketAddress, ref int socketAddressLen, out SocketFlags receivedFlags, out Interop.Error errno)
        {
            Debug.Assert(socketAddress != null || socketAddressLen == 0);

            long received;

            int sockAddrLen = 0;
            if (socketAddress != null)
            {
                sockAddrLen = socketAddressLen;
            }

            fixed (byte* sockAddr = socketAddress)
            fixed (byte* b = buffer)
            {
                var iov = new Interop.Sys.IOVector {
                    Base = &b[offset],
                    Count = (UIntPtr)count
                };

                var messageHeader = new Interop.Sys.MessageHeader {
                    SocketAddress = sockAddr,
                    SocketAddressLen = sockAddrLen,
                    IOVectors = &iov,
                    IOVectorCount = 1
                };

                errno = Interop.Sys.ReceiveMessage(fd, &messageHeader, flags, &received);
                receivedFlags = messageHeader.Flags;
                sockAddrLen = messageHeader.SocketAddressLen;
            }

            if (errno != Interop.Error.SUCCESS)
            {
                return -1;
            }

            socketAddressLen = sockAddrLen;
            return checked((int)received);
        }

        private static unsafe int Send(int fd, SocketFlags flags, byte[] buffer, ref int offset, ref int count, byte[] socketAddress, int socketAddressLen, out Interop.Error errno)
        {
            int sent;

            int sockAddrLen = 0;
            if (socketAddress != null)
            {
                sockAddrLen = socketAddressLen;
            }

            fixed (byte* sockAddr = socketAddress)
            fixed (byte* b = buffer)
            {
                var iov = new Interop.Sys.IOVector {
                    Base = &b[offset],
                    Count = (UIntPtr)count
                };

                var messageHeader = new Interop.Sys.MessageHeader {
                    SocketAddress = sockAddr,
                    SocketAddressLen = sockAddrLen,
                    IOVectors = &iov,
                    IOVectorCount = 1
                };

                long bytesSent;
                errno = Interop.Sys.SendMessage(fd, &messageHeader, flags, &bytesSent);

                sent = checked((int)bytesSent);
            }

            if (errno != Interop.Error.SUCCESS)
            {
                return -1;
            }

            
            offset += sent;
            count -= sent;
            return sent;
        }

        private static unsafe int Send(int fd, SocketFlags flags, IList<ArraySegment<byte>> buffers, ref int bufferIndex, ref int offset, byte[] socketAddress, int socketAddressLen, out Interop.Error errno)
        {
            // Pin buffers and set up iovecs.
            int startIndex = bufferIndex, startOffset = offset;

            int sockAddrLen = 0;
            if (socketAddress != null)
            {
                sockAddrLen = socketAddressLen;
            }

            int maxBuffers = buffers.Count - startIndex;
            var handles = new GCHandle[maxBuffers];
            var iovecs = new Interop.Sys.IOVector[maxBuffers];

            int sent;
            int toSend = 0, iovCount = maxBuffers;
            try
            {
                for (int i = 0; i < maxBuffers; i++, startOffset = 0)
                {
                    ArraySegment<byte> buffer = buffers[startIndex + i];
                    Debug.Assert(buffer.Offset + startOffset < buffer.Array.Length);

                    handles[i] = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
                    iovecs[i].Base = &((byte*)handles[i].AddrOfPinnedObject())[buffer.Offset + startOffset];

                    toSend += (buffer.Count - startOffset);
                    iovecs[i].Count = (UIntPtr)(buffer.Count - startOffset);
                }

                // Make the call
                fixed (byte* sockAddr = socketAddress)
                fixed (Interop.Sys.IOVector* iov = iovecs)
                {
                    var messageHeader = new Interop.Sys.MessageHeader {
                        SocketAddress = sockAddr,
                        SocketAddressLen = sockAddrLen,
                        IOVectors = iov,
                        IOVectorCount = iovCount
                    };

                    long bytesSent;
                    errno = Interop.Sys.SendMessage(fd, &messageHeader, flags, &bytesSent);

                    sent = checked((int)bytesSent);
                }
            }
            finally
            {
                // Free GC handles.
                for (int i = 0; i < iovCount; i++)
                {
                    if (handles[i].IsAllocated)
                    {
                        handles[i].Free();
                    }
                }
            }

            if (errno != Interop.Error.SUCCESS)
            {
                return -1;
            }

            // Update position.
            int endIndex = bufferIndex, endOffset = offset, unconsumed = sent;
            for (; endIndex < buffers.Count && unconsumed > 0; endIndex++, endOffset = 0)
            {
                int space = buffers[endIndex].Count - endOffset;
                if (space > unconsumed)
                {
                    endOffset += unconsumed;
                    break;
                }
                unconsumed -= space;
            }

            bufferIndex = endIndex;
            offset = endOffset;

            return sent;
        }

        private static unsafe int Receive(int fd, SocketFlags flags, int available, IList<ArraySegment<byte>> buffers, byte[] socketAddress, ref int socketAddressLen, out SocketFlags receivedFlags, out Interop.Error errno)
        {
            // Pin buffers and set up iovecs.
            int maxBuffers = buffers.Count;
            var handles = new GCHandle[maxBuffers];
            var iovecs = new Interop.Sys.IOVector[maxBuffers];

            int sockAddrLen = 0;
            if (socketAddress != null)
            {
                sockAddrLen = socketAddressLen;
            }

            long received = 0;
            int toReceive = 0, iovCount = maxBuffers;
            try
            {
                for (int i = 0; i < maxBuffers; i++)
                {
                    ArraySegment<byte> buffer = buffers[i];
                    handles[i] = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
                    iovecs[i].Base = &((byte*)handles[i].AddrOfPinnedObject())[buffer.Offset];

                    int space = buffer.Count;
                    toReceive += space;
                    if (toReceive >= available)
                    {
                        iovecs[i].Count = (UIntPtr)(space - (toReceive - available));
                        toReceive = available;
                        iovCount = i + 1;
                        break;
                    }

                    iovecs[i].Count = (UIntPtr)space;
                }

                // Make the call.
                fixed (byte* sockAddr = socketAddress)
                fixed (Interop.Sys.IOVector* iov = iovecs)
                {
                    var messageHeader = new Interop.Sys.MessageHeader {
                        SocketAddress = sockAddr,
                        SocketAddressLen = sockAddrLen,
                        IOVectors = iov,
                        IOVectorCount = iovCount
                    };

                    errno = Interop.Sys.ReceiveMessage(fd, &messageHeader, flags, &received);
                    receivedFlags = messageHeader.Flags;
                    sockAddrLen = messageHeader.SocketAddressLen;
                }
            }
            finally
            {
                // Free GC handles.
                for (int i = 0; i < iovCount; i++)
                {
                    if (handles[i].IsAllocated)
                    {
                        handles[i].Free();
                    }
                }
            }

            if (errno != Interop.Error.SUCCESS)
            {
                return -1;
            }

            socketAddressLen = sockAddrLen;
            return checked((int)received);
        }

        private static unsafe int ReceiveMessageFrom(int fd, SocketFlags flags, int available, byte[] buffer, int offset, int count, byte[] socketAddress, ref int socketAddressLen, bool isIPv4, bool isIPv6, out SocketFlags receivedFlags, out IPPacketInformation ipPacketInformation, out Interop.Error errno)
        {
            Debug.Assert(socketAddress != null);

            int cmsgBufferLen = Interop.Sys.GetControlMessageBufferSize(isIPv4, isIPv6);
            var cmsgBuffer = stackalloc byte[cmsgBufferLen];

            int sockAddrLen = socketAddressLen;

            Interop.Sys.MessageHeader messageHeader;

            long received;
            fixed (byte* rawSocketAddress = socketAddress)
            fixed (byte* b = buffer)
            {
                var sockAddr = (byte*)rawSocketAddress;

                var iov = new Interop.Sys.IOVector {
                    Base = &b[offset],
                    Count = (UIntPtr)count
                };

                messageHeader = new Interop.Sys.MessageHeader {
                    SocketAddress = sockAddr,
                    SocketAddressLen = sockAddrLen,
                    IOVectors = &iov,
                    IOVectorCount = 1,
                    ControlBuffer = cmsgBuffer,
                    ControlBufferLen = cmsgBufferLen
                };

                errno = Interop.Sys.ReceiveMessage(fd, &messageHeader, flags, &received);
                receivedFlags = messageHeader.Flags;
                sockAddrLen = messageHeader.SocketAddressLen;
            }

            ipPacketInformation = GetIPPacketInformation(&messageHeader, isIPv4, isIPv6);

            if (errno != Interop.Error.SUCCESS)
            {
                return -1;
            }

            socketAddressLen = sockAddrLen;
            return checked((int)received);
        }

        public static unsafe bool TryCompleteAccept(int fileDescriptor, byte[] socketAddress, ref int socketAddressLen, out int acceptedFd, out SocketError errorCode)
        {
            int fd;
            Interop.Error errno;
            int sockAddrLen = socketAddressLen;
            fixed (byte* rawSocketAddress = socketAddress)
            {
                errno = Interop.Sys.Accept(fileDescriptor, rawSocketAddress, &sockAddrLen, &fd);
            }

            if (errno == Interop.Error.SUCCESS)
            {
                Debug.Assert(fd != -1);

                socketAddressLen = sockAddrLen;
                errorCode = SocketError.Success;
                acceptedFd = fd;

                return true;
            }

            acceptedFd = -1;
            if (errno != Interop.Error.EAGAIN && errno != Interop.Error.EWOULDBLOCK)
            {
                errorCode = GetSocketErrorForErrorCode(errno);
                return true;
            }

            errorCode = SocketError.Success;
            return false;
        }

        public static unsafe bool TryStartConnect(int fileDescriptor, byte[] socketAddress, int socketAddressLen, out SocketError errorCode)
        {
            Debug.Assert(socketAddress != null);
            Debug.Assert(socketAddressLen > 0);

            Interop.Error err;
            fixed (byte* rawSocketAddress = socketAddress)
            {
                err = Interop.Sys.Connect(fileDescriptor, rawSocketAddress, socketAddressLen);
            }

            if (err == Interop.Error.SUCCESS)
            {
                errorCode = SocketError.Success;
                return true;
            }

            if (err != Interop.Error.EINPROGRESS)
            {
                errorCode = GetSocketErrorForErrorCode(err);
                return true;
            }

            errorCode = SocketError.Success;
            return false;
        }

        public static unsafe bool TryCompleteConnect(int fileDescriptor, int socketAddressLen, out SocketError errorCode)
        {
            Interop.Error socketError;
            Interop.Error err = Interop.Sys.GetSocketErrorOption(fileDescriptor, &socketError);
            if (err != Interop.Error.SUCCESS)
            {
                Debug.Assert(err == Interop.Error.EBADF);
                errorCode = SocketError.SocketError;
                return true;
            }

            if (socketError == Interop.Error.SUCCESS)
            {
                errorCode = SocketError.Success;
                return true;
            }
            else if (socketError == Interop.Error.EINPROGRESS)
            {
                errorCode = SocketError.Success;
                return false;
            }

            errorCode = GetSocketErrorForErrorCode(socketError);
            return true;
        }

        public static bool TryCompleteReceiveFrom(int fileDescriptor, byte[] buffer, int offset, int count, SocketFlags flags, byte[] socketAddress, ref int socketAddressLen, out int bytesReceived, out SocketFlags receivedFlags, out SocketError errorCode)
        {
            return TryCompleteReceiveFrom(fileDescriptor, buffer, null, offset, count, flags, socketAddress, ref socketAddressLen, out bytesReceived, out receivedFlags, out errorCode);
        }

        public static bool TryCompleteReceiveFrom(int fileDescriptor, IList<ArraySegment<byte>> buffers, SocketFlags flags, byte[] socketAddress, ref int socketAddressLen, out int bytesReceived, out SocketFlags receivedFlags, out SocketError errorCode)
        {
            return TryCompleteReceiveFrom(fileDescriptor, null, buffers, 0, 0, flags, socketAddress, ref socketAddressLen, out bytesReceived, out receivedFlags, out errorCode);
        }

        public static unsafe bool TryCompleteReceiveFrom(int fileDescriptor, byte[] buffer, IList<ArraySegment<byte>> buffers, int offset, int count, SocketFlags flags, byte[] socketAddress, ref int socketAddressLen, out int bytesReceived, out SocketFlags receivedFlags, out SocketError errorCode)
        {
            int available;
            Interop.Error errno = Interop.Sys.GetBytesAvailable(fileDescriptor, &available);
            if (errno != Interop.Error.SUCCESS)
            {
                bytesReceived = 0;
                receivedFlags = 0;
                errorCode = GetSocketErrorForErrorCode(errno);
                return true;
            }
            if (available == 0)
            {
                // Always request at least one byte.
                available = 1;
            }

            int received;
            if (buffer != null)
            {
                received = Receive(fileDescriptor, flags, available, buffer, offset, count, socketAddress, ref socketAddressLen, out receivedFlags, out errno);
            }
            else
            {
                received = Receive(fileDescriptor, flags, available, buffers, socketAddress, ref socketAddressLen, out receivedFlags, out errno);
            }

            if (received != -1)
            {
                bytesReceived = received;
                errorCode = SocketError.Success;
                return true;
            }

            bytesReceived = 0;

            if (errno != Interop.Error.EAGAIN && errno != Interop.Error.EWOULDBLOCK)
            {
                errorCode = GetSocketErrorForErrorCode(errno);
                return true;
            }

            errorCode = SocketError.Success;
            return false;
        }

        public static unsafe bool TryCompleteReceiveMessageFrom(int fileDescriptor, byte[] buffer, int offset, int count, SocketFlags flags, byte[] socketAddress, ref int socketAddressLen, bool isIPv4, bool isIPv6, out int bytesReceived, out SocketFlags receivedFlags, out IPPacketInformation ipPacketInformation, out SocketError errorCode)
        {
            int available;
            Interop.Error errno = Interop.Sys.GetBytesAvailable(fileDescriptor, &available);
            if (errno != Interop.Error.SUCCESS)
            {
                bytesReceived = 0;
                receivedFlags = 0;
                ipPacketInformation = default(IPPacketInformation);
                errorCode = GetSocketErrorForErrorCode(errno);
                return true;
            }
            if (available == 0)
            {
                // Always request at least one byte.
                available = 1;
            }

            int received = ReceiveMessageFrom(fileDescriptor, flags, available, buffer, offset, count, socketAddress, ref socketAddressLen, isIPv4, isIPv6, out receivedFlags, out ipPacketInformation, out errno);

            if (received != -1)
            {
                bytesReceived = received;
                errorCode = SocketError.Success;
                return true;
            }

            bytesReceived = 0;

            if (errno != Interop.Error.EAGAIN && errno != Interop.Error.EWOULDBLOCK)
            {
                errorCode = GetSocketErrorForErrorCode(errno);
                return true;
            }

            errorCode = SocketError.Success;
            return false;
        }

        public static bool TryCompleteSendTo(int fileDescriptor, byte[] buffer, ref int offset, ref int count, SocketFlags flags, byte[] socketAddress, int socketAddressLen, ref int bytesSent, out SocketError errorCode)
        {
            int bufferIndex = 0;
            return TryCompleteSendTo(fileDescriptor, buffer, null, ref bufferIndex, ref offset, ref count, flags, socketAddress, socketAddressLen, ref bytesSent, out errorCode);
        }

        public static bool TryCompleteSendTo(int fileDescriptor, IList<ArraySegment<byte>> buffers, ref int bufferIndex, ref int offset, SocketFlags flags, byte[] socketAddress, int socketAddressLen, ref int bytesSent, out SocketError errorCode)
        {
            int count = 0;
            return TryCompleteSendTo(fileDescriptor, null, buffers, ref bufferIndex, ref offset, ref count, flags, socketAddress, socketAddressLen, ref bytesSent, out errorCode);
        }

        public static bool TryCompleteSendTo(int fileDescriptor, byte[] buffer, IList<ArraySegment<byte>> buffers, ref int bufferIndex, ref int offset, ref int count, SocketFlags flags, byte[] socketAddress, int socketAddressLen, ref int bytesSent, out SocketError errorCode)
        {
            for (;;)
            {
                int sent;
                Interop.Error errno;
                if (buffer != null)
                {
                    sent = Send(fileDescriptor, flags, buffer, ref offset, ref count, socketAddress, socketAddressLen, out errno);
                }
                else
                {
                    sent = Send(fileDescriptor, flags, buffers, ref bufferIndex, ref offset, socketAddress, socketAddressLen, out errno);
                }

                if (sent == -1)
                {
                    if (errno != Interop.Error.EAGAIN && errno != Interop.Error.EWOULDBLOCK)
                    {
                        errorCode = GetSocketErrorForErrorCode(errno);
                        return true;
                    }

                    errorCode = SocketError.Success;
                    return false;
                }

                bytesSent += sent;

                bool isComplete = sent == 0 ||
                    (buffer != null && count == 0) ||
                    (buffers != null && bufferIndex == buffers.Count);
                if (isComplete)
                {
                    errorCode = SocketError.Success;
                    return true;
                }
            }
        }

        public static SocketError SetBlocking(SafeCloseSocket handle, bool shouldBlock, out bool willBlock)
        {
            handle.IsNonBlocking = !shouldBlock;
            willBlock = shouldBlock;
            return SocketError.Success;
        }

        public static unsafe SocketError GetSockName(SafeCloseSocket handle, byte[] buffer, ref int nameLen)
        {
            Interop.Error err;
            int addrLen = nameLen;
            fixed (byte* rawBuffer = buffer)
            {
                err = Interop.Sys.GetSockName(handle.FileDescriptor, rawBuffer, &addrLen);
            }

            nameLen = addrLen;
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError GetAvailable(SafeCloseSocket handle, out int available)
        {
            int value = 0;
            Interop.Error err = Interop.Sys.GetBytesAvailable(handle.FileDescriptor, &value);
            available = value;

            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError GetPeerName(SafeCloseSocket handle, byte[] buffer, ref int nameLen)
        {
            Interop.Error err;
            int addrLen = nameLen;
            fixed (byte* rawBuffer = buffer)
            {
                err = Interop.Sys.GetPeerName(handle.FileDescriptor, rawBuffer, &addrLen);
            }

            nameLen = addrLen;
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError Bind(SafeCloseSocket handle, byte[] buffer, int nameLen)
        {
            Interop.Error err;
            fixed (byte* rawBuffer = buffer)
            {
                err = Interop.Sys.Bind(handle.FileDescriptor, rawBuffer, nameLen);
            }

            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static SocketError Listen(SafeCloseSocket handle, int backlog)
        {
            Interop.Error err = Interop.Sys.Listen(handle.FileDescriptor, backlog);
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static SocketError Accept(SafeCloseSocket handle, byte[] buffer, ref int nameLen, out SafeCloseSocket socket)
        {
            return SafeCloseSocket.Accept(handle, buffer, ref nameLen, out socket);
        }

        public static SocketError Connect(SafeCloseSocket handle, byte[] socketAddress, int socketAddressLen)
        {
            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.Connect(socketAddress, socketAddressLen, -1);
            }

            handle.AsyncContext.CheckForPriorConnectFailure();

            SocketError errorCode;
            bool completed = TryStartConnect(handle.FileDescriptor, socketAddress, socketAddressLen, out errorCode);
            if (completed)
            {
                handle.AsyncContext.RegisterConnectResult(errorCode);
                return errorCode;
            }
            else
            {
                return SocketError.WouldBlock;
            }
        }

        public static SocketError Disconnect(Socket socket, SafeCloseSocket handle, bool reuseSocket)
        {
            throw new PlatformNotSupportedException();
        }

        public static SocketError Send(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out int bytesTransferred)
        {
            var bufferList = buffers;
            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.Send(bufferList, socketFlags, handle.SendTimeout, out bytesTransferred);
            }

            bytesTransferred = 0;
            int bufferIndex = 0;
            int offset = 0;
            SocketError errorCode;
            bool completed = TryCompleteSendTo(handle.FileDescriptor, bufferList, ref bufferIndex, ref offset, socketFlags, null, 0, ref bytesTransferred, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError Send(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, out int bytesTransferred)
        {
            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.Send(buffer, offset, count, socketFlags, handle.SendTimeout, out bytesTransferred);
            }

            bytesTransferred = 0;
            SocketError errorCode;
            bool completed = TryCompleteSendTo(handle.FileDescriptor, buffer, ref offset, ref count, socketFlags, null, 0, ref bytesTransferred, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError SendTo(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, byte[] socketAddress, int socketAddressLen, out int bytesTransferred)
        {
            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.SendTo(buffer, offset, count, socketFlags, socketAddress, socketAddressLen, handle.SendTimeout, out bytesTransferred);
            }

            bytesTransferred = 0;
            SocketError errorCode;
            bool completed = TryCompleteSendTo(handle.FileDescriptor, buffer, ref offset, ref count, socketFlags, socketAddress, socketAddressLen, ref bytesTransferred, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError Receive(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, ref SocketFlags socketFlags, out int bytesTransferred)
        {
            SocketError errorCode;
            if (!handle.IsNonBlocking)
            {
                errorCode = handle.AsyncContext.Receive(buffers, ref socketFlags, handle.ReceiveTimeout, out bytesTransferred);
            }
            else
            {
                int socketAddressLen = 0;
                if (!TryCompleteReceiveFrom(handle.FileDescriptor, buffers, socketFlags, null, ref socketAddressLen, out bytesTransferred, out socketFlags, out errorCode))
                {
                    errorCode = SocketError.WouldBlock;
                }
            }

            return errorCode;
        }

        public static SocketError Receive(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, out int bytesTransferred)
        {
            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.Receive(buffer, offset, count, ref socketFlags, handle.ReceiveTimeout, out bytesTransferred);
            }

            int socketAddressLen = 0;
            SocketError errorCode;
            bool completed = TryCompleteReceiveFrom(handle.FileDescriptor, buffer, offset, count, socketFlags, null, ref socketAddressLen, out bytesTransferred, out socketFlags, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError ReceiveMessageFrom(Socket socket, SafeCloseSocket handle, byte[] buffer, int offset, int count, ref SocketFlags socketFlags, Internals.SocketAddress socketAddress, out Internals.SocketAddress receiveAddress, out IPPacketInformation ipPacketInformation, out int bytesTransferred)
        {
            byte[] socketAddressBuffer = socketAddress.Buffer;
            int socketAddressLen = socketAddress.Size;

            bool isIPv4, isIPv6;
            Socket.GetIPProtocolInformation(socket.AddressFamily, socketAddress, out isIPv4, out isIPv6);

            SocketError errorCode;
            if (!handle.IsNonBlocking)
            {
                errorCode = handle.AsyncContext.ReceiveMessageFrom(buffer, offset, count, ref socketFlags, socketAddressBuffer, ref socketAddressLen, isIPv4, isIPv6, handle.ReceiveTimeout, out ipPacketInformation, out bytesTransferred);
            }
            else
            {
                if (!TryCompleteReceiveMessageFrom(handle.FileDescriptor, buffer, offset, count, socketFlags, socketAddressBuffer, ref socketAddressLen, isIPv4, isIPv6, out bytesTransferred, out socketFlags, out ipPacketInformation, out errorCode))
                {
                    errorCode = SocketError.WouldBlock;
                }
            }

            socketAddress.InternalSize = socketAddressLen;
            receiveAddress = socketAddress;
            return errorCode;
        }

        public static SocketError ReceiveFrom(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, byte[] socketAddress, ref int socketAddressLen, out int bytesTransferred)
        {
            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.ReceiveFrom(buffer, offset, count, ref socketFlags, socketAddress, ref socketAddressLen, handle.ReceiveTimeout, out bytesTransferred);
            }

            SocketError errorCode;
            bool completed = TryCompleteReceiveFrom(handle.FileDescriptor, buffer, offset, count, socketFlags, socketAddress, ref socketAddressLen, out bytesTransferred, out socketFlags, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError Ioctl(SafeCloseSocket handle, int ioControlCode, byte[] optionInValue, byte[] optionOutValue, out int optionLength)
        {
            // TODO: can this be supported in some reasonable fashion?
            throw new PlatformNotSupportedException();
        }

        public static SocketError IoctlInternal(SafeCloseSocket handle, IOControlCode ioControlCode, IntPtr optionInValue, int inValueLength, IntPtr optionOutValue, int outValueLength, out int optionLength)
        {
            // TODO: can this be supported in some reasonable fashion?
            throw new PlatformNotSupportedException();
        }

        public static unsafe SocketError SetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
        {
            Interop.Error err;

            if (optionLevel == SocketOptionLevel.Socket)
            {
                if (optionName == SocketOptionName.ReceiveTimeout)
                {
                    handle.ReceiveTimeout = optionValue == 0 ? -1 : optionValue;
                    err = Interop.Sys.SetReceiveTimeout(handle.FileDescriptor, optionValue);
                    return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
                }
                else if (optionName == SocketOptionName.SendTimeout)
                {
                    handle.SendTimeout = optionValue == 0 ? -1 : optionValue;
                    err = Interop.Sys.SetSendTimeout(handle.FileDescriptor, optionValue);
                    return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
                }
            }

            err = Interop.Sys.SetSockOpt(handle.FileDescriptor, optionLevel, optionName, (byte*)&optionValue, sizeof(int));
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError SetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            Interop.Error err;
            if (optionValue == null || optionValue.Length == 0)
            {
                err = Interop.Sys.SetSockOpt(handle.FileDescriptor, optionLevel, optionName, null, 0);
            }
            else
            {
                fixed (byte* pinnedValue = optionValue)
                {
                    err = Interop.Sys.SetSockOpt(handle.FileDescriptor, optionLevel, optionName, pinnedValue, optionValue.Length);
                }
            }

            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError SetMulticastOption(SafeCloseSocket handle, SocketOptionName optionName, MulticastOption optionValue)
        {
            Debug.Assert(optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership);

            Interop.Sys.MulticastOption optName = optionName == SocketOptionName.AddMembership ?
                Interop.Sys.MulticastOption.MULTICAST_ADD :
                Interop.Sys.MulticastOption.MULTICAST_DROP;

            var opt = new Interop.Sys.IPv4MulticastOption {
                MulticastAddress = unchecked((uint)optionValue.Group.GetAddress()),
                LocalAddress = unchecked((uint)optionValue.LocalAddress.GetAddress()),
                InterfaceIndex = optionValue.InterfaceIndex
            };

            Interop.Error err = Interop.Sys.SetIPv4MulticastOption(handle.FileDescriptor, optName, &opt);
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError SetIPv6MulticastOption(SafeCloseSocket handle, SocketOptionName optionName, IPv6MulticastOption optionValue)
        {
            Debug.Assert(optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership);

            Interop.Sys.MulticastOption optName = optionName == SocketOptionName.AddMembership ?
                Interop.Sys.MulticastOption.MULTICAST_ADD :
                Interop.Sys.MulticastOption.MULTICAST_DROP;

            var opt = new Interop.Sys.IPv6MulticastOption {
                Address = optionValue.Group.GetNativeIPAddress(),
                InterfaceIndex = (int)optionValue.InterfaceIndex
            };

            Interop.Error err = Interop.Sys.SetIPv6MulticastOption(handle.FileDescriptor, optName, &opt);
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError SetLingerOption(SafeCloseSocket handle, LingerOption optionValue)
        {
            var opt = new Interop.Sys.LingerOption {
                OnOff = optionValue.Enabled ? 1 : 0,
                Seconds = optionValue.LingerTime
            };

            Interop.Error err = Interop.Sys.SetLingerOption(handle.FileDescriptor, &opt);
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static void SetReceivingDualModeIPv4PacketInformation(Socket socket)
        {
            // NOTE: some platforms (e.g. OS X) do not support receiving IPv4 packet information for packets received
            //       on dual-mode sockets. On these platforms, this call is a no-op.
            if (SupportsDualModeIPv4PacketInfo)
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            }
        }

        public static unsafe SocketError GetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, out int optionValue)
        {
            if (optionLevel == SocketOptionLevel.Socket)
            {
                if (optionName == SocketOptionName.ReceiveTimeout)
                {
                    optionValue = handle.ReceiveTimeout == -1 ? 0 : handle.ReceiveTimeout;
                    return SocketError.Success;
                }
                else if (optionName == SocketOptionName.SendTimeout)
                {
                    optionValue = handle.SendTimeout == -1 ? 0 : handle.SendTimeout;
                    return SocketError.Success;
                }
            }

            int value = 0;
            int optLen = sizeof(int);
            Interop.Error err = Interop.Sys.GetSockOpt(handle.FileDescriptor, optionLevel, optionName, (byte*)&value, &optLen);

            optionValue = value;
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError GetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue, ref int optionLength)
        {
            int optLen = optionLength;

            Interop.Error err;
            if (optionValue == null || optionValue.Length == 0)
            {
                optLen = 0;
                err = Interop.Sys.GetSockOpt(handle.FileDescriptor, optionLevel, optionName, null, &optLen);
            }
            else
            {
                fixed (byte* pinnedValue = optionValue)
                {
                    err = Interop.Sys.GetSockOpt(handle.FileDescriptor, optionLevel, optionName, pinnedValue, &optLen);
                }
            }

            if (err == Interop.Error.SUCCESS)
            {
                optionLength = optLen;
                return SocketError.Success;
            }

            return GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError GetMulticastOption(SafeCloseSocket handle, SocketOptionName optionName, out MulticastOption optionValue)
        {
            Debug.Assert(optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership);

            Interop.Sys.MulticastOption optName = optionName == SocketOptionName.AddMembership ?
                Interop.Sys.MulticastOption.MULTICAST_ADD :
                Interop.Sys.MulticastOption.MULTICAST_DROP;

            Interop.Sys.IPv4MulticastOption opt;
            Interop.Error err = Interop.Sys.GetIPv4MulticastOption(handle.FileDescriptor, optName, &opt);
            if (err != Interop.Error.SUCCESS)
            {
                optionValue = default(MulticastOption);
                return GetSocketErrorForErrorCode(err);
            }

            var multicastAddress = new IPAddress((long)opt.MulticastAddress);
            var localAddress = new IPAddress((long)opt.LocalAddress);
            optionValue = new MulticastOption(multicastAddress, localAddress) {
                InterfaceIndex = opt.InterfaceIndex
            };

            return SocketError.Success;
        }

        public static unsafe SocketError GetIPv6MulticastOption(SafeCloseSocket handle, SocketOptionName optionName, out IPv6MulticastOption optionValue)
        {
            Debug.Assert(optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership);

            Interop.Sys.MulticastOption optName = optionName == SocketOptionName.AddMembership ?
                Interop.Sys.MulticastOption.MULTICAST_ADD :
                Interop.Sys.MulticastOption.MULTICAST_DROP;

            Interop.Sys.IPv6MulticastOption opt;
            Interop.Error err = Interop.Sys.GetIPv6MulticastOption(handle.FileDescriptor, optName, &opt);
            if (err != Interop.Error.SUCCESS)
            {
                optionValue = default(IPv6MulticastOption);
                return GetSocketErrorForErrorCode(err);
            }

            optionValue = new IPv6MulticastOption(opt.Address.GetIPAddress(), opt.InterfaceIndex);
            return SocketError.Success;
        }

        public static unsafe SocketError GetLingerOption(SafeCloseSocket handle, out LingerOption optionValue)
        {
            var opt = new Interop.Sys.LingerOption();
            Interop.Error err = Interop.Sys.GetLingerOption(handle.FileDescriptor, &opt);
            if (err != Interop.Error.SUCCESS)
            {
                optionValue = default(LingerOption);
                return GetSocketErrorForErrorCode(err);
            }

            optionValue = new LingerOption(opt.OnOff != 0, opt.Seconds);
            return SocketError.Success;
        }

        public static unsafe SocketError Poll(SafeCloseSocket handle, int microseconds, SelectMode mode, out bool status)
        {
            uint* fdSet = stackalloc uint[Interop.Sys.FD_SETSIZE_UINTS];
            Interop.Sys.FD_ZERO(fdSet);

            Interop.Sys.FD_SET(handle.FileDescriptor, fdSet);

            int fdCount = 0;
            uint* readFds = null;
            uint* writeFds = null;
            uint* errorFds = null;
            switch (mode)
            {
                case SelectMode.SelectRead:
                    readFds = fdSet;
                    fdCount = handle.FileDescriptor + 1;
                    break;

                case SelectMode.SelectWrite:
                    writeFds = fdSet;
                    fdCount = handle.FileDescriptor + 1;
                    break;

                case SelectMode.SelectError:
                    errorFds = fdSet;
                    fdCount = handle.FileDescriptor + 1;
                    break;
            }

            int socketCount = 0;
            Interop.Error err = Interop.Sys.Select(fdCount, readFds, writeFds, errorFds, microseconds, &socketCount);
            if (err != Interop.Error.SUCCESS)
            {
                status = false;
                return GetSocketErrorForErrorCode(err);
            }

            status = Interop.Sys.FD_ISSET(handle.FileDescriptor, fdSet);
            return SocketError.Success;
        }

        public static unsafe SocketError Select(IList checkRead, IList checkWrite, IList checkError, int microseconds)
        {
            uint* readSet = stackalloc uint[Interop.Sys.FD_SETSIZE_UINTS];
            int maxReadFd = Socket.FillFdSetFromSocketList(readSet, checkRead);

            uint* writeSet = stackalloc uint[Interop.Sys.FD_SETSIZE_UINTS];
            int maxWriteFd = Socket.FillFdSetFromSocketList(writeSet, checkWrite);

            uint* errorSet = stackalloc uint[Interop.Sys.FD_SETSIZE_UINTS];
            int maxErrorFd = Socket.FillFdSetFromSocketList(errorSet, checkError);

            int fdCount = 0;
            uint* readFds = null;
            uint* writeFds = null;
            uint* errorFds = null;

            if (maxReadFd != 0)
            {
                readFds = readSet;
                fdCount = maxReadFd;
            }

            if (maxWriteFd != 0)
            {
                writeFds = writeSet;
                if (maxWriteFd > fdCount)
                {
                    fdCount = maxWriteFd;
                }
            }

            if (maxErrorFd != 0)
            {
                errorFds = errorSet;
                if (maxErrorFd > fdCount)
                {
                    fdCount = maxErrorFd;
                }
            }

            int socketCount;
            Interop.Error err = Interop.Sys.Select(fdCount, readFds, writeFds, errorFds, microseconds, &socketCount);

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("Socket::Select() Interop.Sys.Select returns socketCount:" + socketCount);
            }

            if (err != Interop.Error.SUCCESS)
            {
                return GetSocketErrorForErrorCode(err);
            }

            Socket.FilterSocketListUsingFdSet(readSet, checkRead);
            Socket.FilterSocketListUsingFdSet(writeSet, checkWrite);
            Socket.FilterSocketListUsingFdSet(errorSet, checkError);

            return SocketError.Success;
        }

        public static SocketError Shutdown(SafeCloseSocket handle, bool isConnected, bool isDisconnected, SocketShutdown how)
        {
            Interop.Error err = Interop.Sys.Shutdown(handle.FileDescriptor, how);
            if (err == Interop.Error.SUCCESS)
            {
                return SocketError.Success;
            }

            // If shutdown returns ENOTCONN and we think that this socket has ever been connected,
            // ignore the error. This can happen for TCP connections if the underlying connection
            // has reached the CLOSE state. Ignoring the error matches Winsock behavior.
            if (err == Interop.Error.ENOTCONN && (isConnected || isDisconnected))
            {
                return SocketError.Success;
            }

            return GetSocketErrorForErrorCode(err);
        }

        public static SocketError ConnectAsync(Socket socket, SafeCloseSocket handle, byte[] socketAddress, int socketAddressLen, ConnectOverlappedAsyncResult asyncResult)
        {
            return handle.AsyncContext.ConnectAsync(socketAddress, socketAddressLen, asyncResult.CompletionCallback);
        }

        public static SocketError SendAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            return handle.AsyncContext.SendAsync(buffer, offset, count, socketFlags, asyncResult.CompletionCallback);
        }

        public static SocketError SendAsync(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            return handle.AsyncContext.SendAsync(buffers, socketFlags, asyncResult.CompletionCallback);
        }

        public static SocketError SendToAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, OverlappedAsyncResult asyncResult)
        {
            asyncResult.SocketAddress = socketAddress;

            return handle.AsyncContext.SendToAsync(buffer, offset, count, socketFlags, socketAddress.Buffer, socketAddress.Size, asyncResult.CompletionCallback);
        }

        public static SocketError ReceiveAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            return handle.AsyncContext.ReceiveAsync(buffer, offset, count, socketFlags, asyncResult.CompletionCallback);
        }

        public static SocketError ReceiveAsync(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            return handle.AsyncContext.ReceiveAsync(buffers, socketFlags, asyncResult.CompletionCallback);
        }

        public static SocketError ReceiveFromAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, OverlappedAsyncResult asyncResult)
        {
            asyncResult.SocketAddress = socketAddress;

            return handle.AsyncContext.ReceiveFromAsync(buffer, offset, count, socketFlags, socketAddress.Buffer, socketAddress.InternalSize, asyncResult.CompletionCallback);
        }

        public static SocketError ReceiveMessageFromAsync(Socket socket, SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, ReceiveMessageOverlappedAsyncResult asyncResult)
        {
            asyncResult.SocketAddress = socketAddress;

            bool isIPv4, isIPv6;
            Socket.GetIPProtocolInformation(((Socket)asyncResult.AsyncObject).AddressFamily, socketAddress, out isIPv4, out isIPv6);

            return handle.AsyncContext.ReceiveMessageFromAsync(buffer, offset, count, socketFlags, socketAddress.Buffer, socketAddress.InternalSize, isIPv4, isIPv6, asyncResult.CompletionCallback);
        }

        public static SocketError AcceptAsync(Socket socket, SafeCloseSocket handle, SafeCloseSocket acceptHandle, int receiveSize, int socketAddressSize, AcceptOverlappedAsyncResult asyncResult)
        {
            Debug.Assert(acceptHandle == null);

            byte[] socketAddressBuffer = new byte[socketAddressSize];

            return handle.AsyncContext.AcceptAsync(socketAddressBuffer, socketAddressSize, asyncResult.CompletionCallback);
        }
    }
}
