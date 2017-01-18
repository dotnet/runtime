// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    internal static partial class SocketPal
    {
        // The API that uses this information is not supported on *nix, and will throw
        // PlatformNotSupportedException instead.
        public const int ProtocolInformationSize = 0;

        public const bool SupportsMultipleConnectAttempts = false;
        private static readonly bool SupportsDualModeIPv4PacketInfo = GetPlatformSupportsDualModeIPv4PacketInfo();

        private static bool GetPlatformSupportsDualModeIPv4PacketInfo()
        {
            return Interop.Sys.PlatformSupportsDualModeIPv4PacketInfo();
        }

        public static void Initialize()
        {
            // nop.  No initialization required.
        }

        public static SocketError GetSocketErrorForErrorCode(Interop.Error errorCode)
        {
            return SocketErrorPal.GetSocketErrorForNativeError(errorCode);
        }

        public static void CheckDualModeReceiveSupport(Socket socket)
        {
            if (!SupportsDualModeIPv4PacketInfo && socket.AddressFamily == AddressFamily.InterNetworkV6 && socket.DualMode)
            {
                throw new PlatformNotSupportedException(SR.net_sockets_dualmode_receivefrom_notsupported);
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

        private static unsafe int Receive(SafeCloseSocket socket, SocketFlags flags, byte[] buffer, int offset, int count, byte[] socketAddress, ref int socketAddressLen, out SocketFlags receivedFlags, out Interop.Error errno)
        {
            Debug.Assert(socketAddress != null || socketAddressLen == 0, $"Unexpected values: socketAddress={socketAddress}, socketAddressLen={socketAddressLen}");

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
                    Base = (b == null) ? null : &b[offset],
                    Count = (UIntPtr)count
                };

                var messageHeader = new Interop.Sys.MessageHeader {
                    SocketAddress = sockAddr,
                    SocketAddressLen = sockAddrLen,
                    IOVectors = &iov,
                    IOVectorCount = 1
                };

                errno = Interop.Sys.ReceiveMessage(socket, &messageHeader, flags, &received);
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

        private static unsafe int Send(SafeCloseSocket socket, SocketFlags flags, byte[] buffer, ref int offset, ref int count, byte[] socketAddress, int socketAddressLen, out Interop.Error errno)
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
                errno = Interop.Sys.SendMessage(socket, &messageHeader, flags, &bytesSent);

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

        private static unsafe int Send(SafeCloseSocket socket, SocketFlags flags, IList<ArraySegment<byte>> buffers, ref int bufferIndex, ref int offset, byte[] socketAddress, int socketAddressLen, out Interop.Error errno)
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
                    Debug.Assert(buffer.Offset + startOffset < buffer.Array.Length, $"Unexpected values: Offset={buffer.Offset}, startOffset={startOffset}, Length={buffer.Array.Length}");

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
                    errno = Interop.Sys.SendMessage(socket, &messageHeader, flags, &bytesSent);

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

        private static unsafe long SendFile(SafeCloseSocket socket, SafeFileHandle fileHandle, ref long offset, ref long count, out Interop.Error errno)
        {
            long bytesSent; 
            errno = Interop.Sys.SendFile(socket, fileHandle, offset, count, out bytesSent);

            if (errno != Interop.Error.SUCCESS)
            {
                return -1;
            }

            offset += bytesSent;
            count -= bytesSent;
            return bytesSent;
        }

        private static unsafe int Receive(SafeCloseSocket socket, SocketFlags flags, IList<ArraySegment<byte>> buffers, byte[] socketAddress, ref int socketAddressLen, out SocketFlags receivedFlags, out Interop.Error errno)
        {
            int available;
            errno = Interop.Sys.GetBytesAvailable(socket, &available);
            if (errno != Interop.Error.SUCCESS)
            {
                receivedFlags = 0;
                return -1;
            }
            if (available == 0)
            {
                // Always request at least one byte.
                available = 1;
            }

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

                    errno = Interop.Sys.ReceiveMessage(socket, &messageHeader, flags, &received);
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

        private static unsafe int ReceiveMessageFrom(SafeCloseSocket socket, SocketFlags flags, byte[] buffer, int offset, int count, byte[] socketAddress, ref int socketAddressLen, bool isIPv4, bool isIPv6, out SocketFlags receivedFlags, out IPPacketInformation ipPacketInformation, out Interop.Error errno)
        {
            Debug.Assert(socketAddress != null, "Expected non-null socketAddress");

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

                errno = Interop.Sys.ReceiveMessage(socket, &messageHeader, flags, &received);
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

        public static unsafe bool TryCompleteAccept(SafeCloseSocket socket, byte[] socketAddress, ref int socketAddressLen, out IntPtr acceptedFd, out SocketError errorCode)
        {
            IntPtr fd;
            Interop.Error errno;
            int sockAddrLen = socketAddressLen;
            fixed (byte* rawSocketAddress = socketAddress)
            {
                try
                {
                    errno = Interop.Sys.Accept(socket, rawSocketAddress, &sockAddrLen, &fd);
                }
                catch (ObjectDisposedException)
                {
                    // The socket was closed, or is closing.
                    errorCode = SocketError.OperationAborted;
                    acceptedFd = (IntPtr)(-1);
                    return true;
                }
            }

            if (errno == Interop.Error.SUCCESS)
            {
                Debug.Assert(fd != (IntPtr)(-1), "Expected fd != -1");

                socketAddressLen = sockAddrLen;
                errorCode = SocketError.Success;
                acceptedFd = fd;

                return true;
            }

            acceptedFd = (IntPtr)(-1);
            if (errno != Interop.Error.EAGAIN && errno != Interop.Error.EWOULDBLOCK)
            {
                errorCode = GetSocketErrorForErrorCode(errno);
                return true;
            }

            errorCode = SocketError.Success;
            return false;
        }

        public static unsafe bool TryStartConnect(SafeCloseSocket socket, byte[] socketAddress, int socketAddressLen, out SocketError errorCode)
        {
            Debug.Assert(socketAddress != null, "Expected non-null socketAddress");
            Debug.Assert(socketAddressLen > 0, $"Unexpected socketAddressLen: {socketAddressLen}");

            Interop.Error err;
            fixed (byte* rawSocketAddress = socketAddress)
            {
                err = Interop.Sys.Connect(socket, rawSocketAddress, socketAddressLen);
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

        public static unsafe bool TryCompleteConnect(SafeCloseSocket socket, int socketAddressLen, out SocketError errorCode)
        {
            Interop.Error socketError;
            Interop.Error err;
            try
            {
                err = Interop.Sys.GetSocketErrorOption(socket, &socketError);
            }
            catch (ObjectDisposedException)
            {
                // The socket was closed, or is closing.
                errorCode = SocketError.OperationAborted;
                return true;
            }

            if (err != Interop.Error.SUCCESS)
            {
                Debug.Assert(err == Interop.Error.EBADF, $"Unexpected err: {err}");
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

        public static bool TryCompleteReceiveFrom(SafeCloseSocket socket, byte[] buffer, int offset, int count, SocketFlags flags, byte[] socketAddress, ref int socketAddressLen, out int bytesReceived, out SocketFlags receivedFlags, out SocketError errorCode)
        {
            return TryCompleteReceiveFrom(socket, buffer, null, offset, count, flags, socketAddress, ref socketAddressLen, out bytesReceived, out receivedFlags, out errorCode);
        }

        public static bool TryCompleteReceiveFrom(SafeCloseSocket socket, IList<ArraySegment<byte>> buffers, SocketFlags flags, byte[] socketAddress, ref int socketAddressLen, out int bytesReceived, out SocketFlags receivedFlags, out SocketError errorCode)
        {
            return TryCompleteReceiveFrom(socket, null, buffers, 0, 0, flags, socketAddress, ref socketAddressLen, out bytesReceived, out receivedFlags, out errorCode);
        }

        public static unsafe bool TryCompleteReceiveFrom(SafeCloseSocket socket, byte[] buffer, IList<ArraySegment<byte>> buffers, int offset, int count, SocketFlags flags, byte[] socketAddress, ref int socketAddressLen, out int bytesReceived, out SocketFlags receivedFlags, out SocketError errorCode)
        {
            try
            {
                Interop.Error errno;
                int received;
                if (buffers != null)
                {
                    Debug.Assert(buffer == null);
                    received = Receive(socket, flags, buffers, socketAddress, ref socketAddressLen, out receivedFlags, out errno);
                }
                else
                {
                    received = Receive(socket, flags, buffer, offset, count, socketAddress, ref socketAddressLen, out receivedFlags, out errno);
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
            catch (ObjectDisposedException)
            {
                // The socket was closed, or is closing.
                bytesReceived = 0;
                receivedFlags = 0;
                errorCode = SocketError.OperationAborted;
                return true;
            }
        }

        public static unsafe bool TryCompleteReceiveMessageFrom(SafeCloseSocket socket, byte[] buffer, int offset, int count, SocketFlags flags, byte[] socketAddress, ref int socketAddressLen, bool isIPv4, bool isIPv6, out int bytesReceived, out SocketFlags receivedFlags, out IPPacketInformation ipPacketInformation, out SocketError errorCode)
        {
            try
            {
                Interop.Error errno;

                int received = ReceiveMessageFrom(socket, flags, buffer, offset, count, socketAddress, ref socketAddressLen, isIPv4, isIPv6, out receivedFlags, out ipPacketInformation, out errno);

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
            catch (ObjectDisposedException)
            {
                // The socket was closed, or is closing.
                bytesReceived = 0;
                receivedFlags = 0;
                ipPacketInformation = default(IPPacketInformation);
                errorCode = SocketError.OperationAborted;
                return true;
            }
        }

        public static bool TryCompleteSendTo(SafeCloseSocket socket, byte[] buffer, ref int offset, ref int count, SocketFlags flags, byte[] socketAddress, int socketAddressLen, ref int bytesSent, out SocketError errorCode)
        {
            int bufferIndex = 0;
            return TryCompleteSendTo(socket, buffer, null, ref bufferIndex, ref offset, ref count, flags, socketAddress, socketAddressLen, ref bytesSent, out errorCode);
        }

        public static bool TryCompleteSendTo(SafeCloseSocket socket, IList<ArraySegment<byte>> buffers, ref int bufferIndex, ref int offset, SocketFlags flags, byte[] socketAddress, int socketAddressLen, ref int bytesSent, out SocketError errorCode)
        {
            int count = 0;
            return TryCompleteSendTo(socket, null, buffers, ref bufferIndex, ref offset, ref count, flags, socketAddress, socketAddressLen, ref bytesSent, out errorCode);
        }

        public static bool TryCompleteSendTo(SafeCloseSocket socket, byte[] buffer, IList<ArraySegment<byte>> buffers, ref int bufferIndex, ref int offset, ref int count, SocketFlags flags, byte[] socketAddress, int socketAddressLen, ref int bytesSent, out SocketError errorCode)
        {
            for (;;)
            {
                int sent;
                Interop.Error errno;
                try
                {
                    if (buffer != null)
                    {
                        sent = Send(socket, flags, buffer, ref offset, ref count, socketAddress, socketAddressLen, out errno);
                    }
                    else
                    {
                        sent = Send(socket, flags, buffers, ref bufferIndex, ref offset, socketAddress, socketAddressLen, out errno);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The socket was closed, or is closing.
                    errorCode = SocketError.OperationAborted;
                    return true;
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

        public static bool TryCompleteSendFile(SafeCloseSocket socket, SafeFileHandle handle, ref long offset, ref long count, ref long bytesSent, out SocketError errorCode)
        {
            for (;;)
            {
                long sent;
                Interop.Error errno;
                try
                {
                    sent = SendFile(socket, handle, ref offset, ref count, out errno);
                }
                catch (ObjectDisposedException)
                {
                    // The socket was closed, or is closing.
                    errorCode = SocketError.OperationAborted;
                    return true;
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

                if (sent == 0 || count == 0)
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
                err = Interop.Sys.GetSockName(handle, rawBuffer, &addrLen);
            }

            nameLen = addrLen;
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError GetAvailable(SafeCloseSocket handle, out int available)
        {
            int value = 0;
            Interop.Error err = Interop.Sys.GetBytesAvailable(handle, &value);
            available = value;

            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError GetPeerName(SafeCloseSocket handle, byte[] buffer, ref int nameLen)
        {
            Interop.Error err;
            int addrLen = nameLen;
            fixed (byte* rawBuffer = buffer)
            {
                err = Interop.Sys.GetPeerName(handle, rawBuffer, &addrLen);
            }

            nameLen = addrLen;
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError Bind(SafeCloseSocket handle, byte[] buffer, int nameLen)
        {
            Interop.Error err;
            fixed (byte* rawBuffer = buffer)
            {
                err = Interop.Sys.Bind(handle, rawBuffer, nameLen);
            }

            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static SocketError Listen(SafeCloseSocket handle, int backlog)
        {
            Interop.Error err = Interop.Sys.Listen(handle, backlog);
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
            bool completed = TryStartConnect(handle, socketAddress, socketAddressLen, out errorCode);
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
            bool completed = TryCompleteSendTo(handle, bufferList, ref bufferIndex, ref offset, socketFlags, null, 0, ref bytesTransferred, out errorCode);
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
            bool completed = TryCompleteSendTo(handle, buffer, ref offset, ref count, socketFlags, null, 0, ref bytesTransferred, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError SendFile(SafeCloseSocket handle, FileStream fileStream)
        {
            long offset = 0;
            long length = fileStream.Length;

            SafeFileHandle fileHandle = fileStream.SafeFileHandle;

            long bytesTransferred = 0;

            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.SendFile(fileHandle, offset, length, handle.SendTimeout, out bytesTransferred);
            }

            SocketError errorCode;
            bool completed = TryCompleteSendFile(handle, fileHandle, ref offset, ref length, ref bytesTransferred, out errorCode);
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
            bool completed = TryCompleteSendTo(handle, buffer, ref offset, ref count, socketFlags, socketAddress, socketAddressLen, ref bytesTransferred, out errorCode);
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
                if (!TryCompleteReceiveFrom(handle, buffers, socketFlags, null, ref socketAddressLen, out bytesTransferred, out socketFlags, out errorCode))
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
            bool completed = TryCompleteReceiveFrom(handle, buffer, offset, count, socketFlags, null, ref socketAddressLen, out bytesTransferred, out socketFlags, out errorCode);
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
                if (!TryCompleteReceiveMessageFrom(handle, buffer, offset, count, socketFlags, socketAddressBuffer, ref socketAddressLen, isIPv4, isIPv6, out bytesTransferred, out socketFlags, out ipPacketInformation, out errorCode))
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
            bool completed = TryCompleteReceiveFrom(handle, buffer, offset, count, socketFlags, socketAddress, ref socketAddressLen, out bytesTransferred, out socketFlags, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError WindowsIoctl(SafeCloseSocket handle, int ioControlCode, byte[] optionInValue, byte[] optionOutValue, out int optionLength)
        {            
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
                    err = Interop.Sys.SetReceiveTimeout(handle, optionValue);
                    return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
                }
                else if (optionName == SocketOptionName.SendTimeout)
                {
                    handle.SendTimeout = optionValue == 0 ? -1 : optionValue;
                    err = Interop.Sys.SetSendTimeout(handle, optionValue);
                    return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
                }
            }
            else if (optionLevel == SocketOptionLevel.IP)
            {
                if (optionName == SocketOptionName.MulticastInterface)
                {
                    // if the value of the IP_MULTICAST_IF is an address in the 0.x.x.x block
                    // the value is interpreted as an interface index
                    int interfaceIndex = IPAddress.NetworkToHostOrder(optionValue);
                    if ((interfaceIndex & 0xff000000) == 0)
                    {
                        var opt = new Interop.Sys.IPv4MulticastOption {
                            MulticastAddress = 0,
                            LocalAddress = 0,
                            InterfaceIndex = interfaceIndex
                        };

                        err = Interop.Sys.SetIPv4MulticastOption(handle, Interop.Sys.MulticastOption.MULTICAST_IF, &opt);
                        return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
                    }
                }
            }

            err = Interop.Sys.SetSockOpt(handle, optionLevel, optionName, (byte*)&optionValue, sizeof(int));
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError SetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            Interop.Error err;
            if (optionValue == null || optionValue.Length == 0)
            {
                err = Interop.Sys.SetSockOpt(handle, optionLevel, optionName, null, 0);
            }
            else
            {
                fixed (byte* pinnedValue = optionValue)
                {
                    err = Interop.Sys.SetSockOpt(handle, optionLevel, optionName, pinnedValue, optionValue.Length);
                }
            }

            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError SetMulticastOption(SafeCloseSocket handle, SocketOptionName optionName, MulticastOption optionValue)
        {
            Debug.Assert(optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership, $"Unexpected optionName: {optionName}");

            Interop.Sys.MulticastOption optName = optionName == SocketOptionName.AddMembership ?
                Interop.Sys.MulticastOption.MULTICAST_ADD :
                Interop.Sys.MulticastOption.MULTICAST_DROP;

            IPAddress localAddress = optionValue.LocalAddress ?? IPAddress.Any;

            var opt = new Interop.Sys.IPv4MulticastOption {
                MulticastAddress = unchecked((uint)optionValue.Group.GetAddress()),
                LocalAddress = unchecked((uint)localAddress.GetAddress()),
                InterfaceIndex = optionValue.InterfaceIndex
            };

            Interop.Error err = Interop.Sys.SetIPv4MulticastOption(handle, optName, &opt);
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError SetIPv6MulticastOption(SafeCloseSocket handle, SocketOptionName optionName, IPv6MulticastOption optionValue)
        {
            Debug.Assert(optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership, $"Unexpected optionName={optionName}");

            Interop.Sys.MulticastOption optName = optionName == SocketOptionName.AddMembership ?
                Interop.Sys.MulticastOption.MULTICAST_ADD :
                Interop.Sys.MulticastOption.MULTICAST_DROP;

            var opt = new Interop.Sys.IPv6MulticastOption {
                Address = optionValue.Group.GetNativeIPAddress(),
                InterfaceIndex = (int)optionValue.InterfaceIndex
            };

            Interop.Error err = Interop.Sys.SetIPv6MulticastOption(handle, optName, &opt);
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError SetLingerOption(SafeCloseSocket handle, LingerOption optionValue)
        {
            var opt = new Interop.Sys.LingerOption {
                OnOff = optionValue.Enabled ? 1 : 0,
                Seconds = optionValue.LingerTime
            };

            Interop.Error err = Interop.Sys.SetLingerOption(handle, &opt);
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

        public static void SetIPProtectionLevel(Socket socket, SocketOptionLevel optionLevel, int protectionLevel)
        {
            throw new PlatformNotSupportedException();
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

            if (optionName == SocketOptionName.Error)
            {
                Interop.Error socketError = default(Interop.Error);
                Interop.Error getErrorError = Interop.Sys.GetSocketErrorOption(handle, &socketError);
                optionValue = (int)GetSocketErrorForErrorCode(socketError);
                return getErrorError == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(getErrorError);
            }

            int value = 0;
            int optLen = sizeof(int);
            Interop.Error err = Interop.Sys.GetSockOpt(handle, optionLevel, optionName, (byte*)&value, &optLen);

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
                err = Interop.Sys.GetSockOpt(handle, optionLevel, optionName, null, &optLen);
            }
            else if (optionName == SocketOptionName.Error && optionValue.Length >= sizeof(int))
            {
                int outError;
                SocketError returnError = GetSockOpt(handle, optionLevel, optionName, out outError);
                if (returnError == SocketError.Success)
                {
                    fixed (byte* pinnedValue = optionValue)
                    {
                        Debug.Assert(BitConverter.IsLittleEndian, "Expected little endian");
                        *((int*)pinnedValue) = outError;
                    }
                    optionLength = sizeof(int);
                }
                return returnError;
            }
            else
            {
                fixed (byte* pinnedValue = optionValue)
                {
                    err = Interop.Sys.GetSockOpt(handle, optionLevel, optionName, pinnedValue, &optLen);
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
            Debug.Assert(optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership, $"Unexpected optionName={optionName}");

            Interop.Sys.MulticastOption optName = optionName == SocketOptionName.AddMembership ?
                Interop.Sys.MulticastOption.MULTICAST_ADD :
                Interop.Sys.MulticastOption.MULTICAST_DROP;

            Interop.Sys.IPv4MulticastOption opt;
            Interop.Error err = Interop.Sys.GetIPv4MulticastOption(handle, optName, &opt);
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
            Debug.Assert(optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership, $"Unexpected optionName={optionName}");

            Interop.Sys.MulticastOption optName = optionName == SocketOptionName.AddMembership ?
                Interop.Sys.MulticastOption.MULTICAST_ADD :
                Interop.Sys.MulticastOption.MULTICAST_DROP;

            Interop.Sys.IPv6MulticastOption opt;
            Interop.Error err = Interop.Sys.GetIPv6MulticastOption(handle, optName, &opt);
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
            Interop.Error err = Interop.Sys.GetLingerOption(handle, &opt);
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
            Interop.Sys.PollEvents inEvent = Interop.Sys.PollEvents.POLLNONE;
            switch (mode)
            {
                case SelectMode.SelectRead: inEvent = Interop.Sys.PollEvents.POLLIN; break;
                case SelectMode.SelectWrite: inEvent = Interop.Sys.PollEvents.POLLOUT; break;
                case SelectMode.SelectError: inEvent = Interop.Sys.PollEvents.POLLPRI; break;
            }

            int milliseconds = microseconds == -1 ? -1 : microseconds / 1000;

            Interop.Sys.PollEvents outEvents;
            Interop.Error err = Interop.Sys.Poll(handle, inEvent, milliseconds, out outEvents);
            if (err != Interop.Error.SUCCESS)
            {
                status = false;
                return GetSocketErrorForErrorCode(err);
            }

            switch (mode)
            {
                case SelectMode.SelectRead: status = (outEvents & (Interop.Sys.PollEvents.POLLIN | Interop.Sys.PollEvents.POLLHUP)) != 0; break;
                case SelectMode.SelectWrite: status = (outEvents & Interop.Sys.PollEvents.POLLOUT) != 0; break;
                case SelectMode.SelectError: status = (outEvents & (Interop.Sys.PollEvents.POLLERR | Interop.Sys.PollEvents.POLLPRI)) != 0; break;
                default: status = false; break;
            }
            return SocketError.Success;
        }

        public static unsafe SocketError Select(IList checkRead, IList checkWrite, IList checkError, int microseconds)
        {
            int checkReadInitialCount = checkRead != null ? checkRead.Count : 0;
            int checkWriteInitialCount = checkWrite != null ? checkWrite.Count : 0;
            int checkErrorInitialCount = checkError != null ? checkError.Count : 0;
            int count = checked(checkReadInitialCount + checkWriteInitialCount + checkErrorInitialCount);
            Debug.Assert(count > 0, $"Expected at least one entry.");

            // Rather than using the select syscall, we use poll.  While this has a mismatch in API from Select and
            // requires some translation, it avoids the significant limitation of select only working with file descriptors
            // less than FD_SETSIZE, and thus failing arbitrarily depending on the file descriptor value assigned
            // by the system.  Since poll then expects an array of entries, we try to allocate the array on the stack,
            // only falling back to allocating it on the heap if it's deemed too big.

            const int StackThreshold = 80; // arbitrary limit to avoid too much space on stack
            if (count < StackThreshold)
            {
                Interop.Sys.PollEvent* eventsOnStack = stackalloc Interop.Sys.PollEvent[count];
                return SelectViaPoll(
                    checkRead, checkReadInitialCount,
                    checkWrite, checkWriteInitialCount,
                    checkError, checkErrorInitialCount,
                    eventsOnStack, count, microseconds);
            }
            else
            {
                var eventsOnHeap = new Interop.Sys.PollEvent[count];
                fixed (Interop.Sys.PollEvent* eventsOnHeapPtr = eventsOnHeap)
                {
                    return SelectViaPoll(
                        checkRead, checkReadInitialCount,
                        checkWrite, checkWriteInitialCount,
                        checkError, checkErrorInitialCount,
                        eventsOnHeapPtr, count, microseconds);
                }
            }
        }

        private static unsafe SocketError SelectViaPoll(
            IList checkRead, int checkReadInitialCount,
            IList checkWrite, int checkWriteInitialCount,
            IList checkError, int checkErrorInitialCount,
            Interop.Sys.PollEvent* events, int eventsLength,
            int microseconds)
        {
            // Add each of the list's contents to the events array 
            Debug.Assert(eventsLength == checkReadInitialCount + checkWriteInitialCount + checkErrorInitialCount, "Invalid eventsLength");
            int offset = 0;
            AddToPollArray(events, eventsLength, checkRead, ref offset, Interop.Sys.PollEvents.POLLIN | Interop.Sys.PollEvents.POLLHUP);
            AddToPollArray(events, eventsLength, checkWrite, ref offset, Interop.Sys.PollEvents.POLLOUT);
            AddToPollArray(events, eventsLength, checkError, ref offset, Interop.Sys.PollEvents.POLLPRI);
            Debug.Assert(offset == eventsLength, $"Invalid adds. offset={offset}, eventsLength={eventsLength}.");

            // Do the poll
            uint triggered = 0;
            int milliseconds = microseconds == -1 ? -1 : microseconds / 1000;
            Interop.Error err = Interop.Sys.Poll(events, (uint)eventsLength, milliseconds, &triggered);
            if (err != Interop.Error.SUCCESS)
            {
                return GetSocketErrorForErrorCode(err);
            }

            // Remove from the lists any entries which weren't set
            if (triggered == 0)
            {
                checkRead?.Clear();
                checkWrite?.Clear();
                checkError?.Clear();
            }
            else
            {
                FilterPollList(checkRead, events, checkReadInitialCount - 1, Interop.Sys.PollEvents.POLLIN | Interop.Sys.PollEvents.POLLHUP);
                FilterPollList(checkWrite, events, checkWriteInitialCount + checkReadInitialCount - 1, Interop.Sys.PollEvents.POLLOUT);
                FilterPollList(checkError, events, checkErrorInitialCount + checkWriteInitialCount + checkReadInitialCount - 1, Interop.Sys.PollEvents.POLLERR | Interop.Sys.PollEvents.POLLPRI);
            }
            return SocketError.Success;
        }

        private static unsafe void AddToPollArray(Interop.Sys.PollEvent* arr, int arrLength, IList socketList, ref int arrOffset, Interop.Sys.PollEvents events)
        {
            if (socketList == null)
                return;

            int listCount = socketList.Count;
            for (int i = 0; i < listCount; i++)
            {
                if (arrOffset >= arrLength)
                {
                    Debug.Fail("IList.Count must have been faulty, returning a negative value and/or returning a different value across calls.");
                    throw new ArgumentOutOfRangeException(nameof(socketList));
                }

                Socket socket = socketList[i] as Socket;
                if (socket == null)
                {
                    throw new ArgumentException(SR.Format(SR.net_sockets_select, socket?.GetType().FullName ?? "null", typeof(Socket).FullName));
                }

                int fd = (int)socket.SafeHandle.DangerousGetHandle();
                arr[arrOffset++] = new Interop.Sys.PollEvent { Events = events, FileDescriptor = fd };
            }
        }

        private static unsafe void FilterPollList(IList socketList, Interop.Sys.PollEvent* arr, int arrEndOffset, Interop.Sys.PollEvents desiredEvents)
        {
            if (socketList == null)
                return;

            // The Select API requires leaving in the input lists only those sockets that were ready.  As such, we need to loop
            // through each poll event, and for each that wasn't ready, remove the corresponding Socket from its list.  Technically
            // this is O(n^2), due to removing from the list requiring shifting down all elements after it.  However, this doesn't
            // happen with the most common cases.  If very few sockets were ready, then as we iterate from the end of the list, each
            // removal will typically be O(1) rather than O(n).  If most sockets were ready, then we only need to remove a few, in
            // which case we're only doing a small number of O(n) shifts.  It's only for the intermediate case, where a non-trivial
            // number of sockets are ready and a non-trivial number of sockets are not ready that we end up paying the most.  We could
            // avoid these costs by, for example, allocating a side list that we fill with the sockets that should remain, clearing
            // the original list, and then populating the original list with the contents of the side list.  That of course has its
            // own costs, and so for now we do the "simple" thing.  This can be changed in the future as needed.

            for (int i = socketList.Count - 1; i >= 0; --i, --arrEndOffset)
            {
                if (arrEndOffset < 0)
                {
                    Debug.Fail("IList.Count must have been faulty, returning a negative value and/or returning a different value across calls.");
                    throw new ArgumentOutOfRangeException(nameof(arrEndOffset));
                }

                if ((arr[arrEndOffset].TriggeredEvents & desiredEvents) == 0)
                {
                    socketList.RemoveAt(i);
                }
            }
        }

        public static SocketError Shutdown(SafeCloseSocket handle, bool isConnected, bool isDisconnected, SocketShutdown how)
        {
            Interop.Error err = Interop.Sys.Shutdown(handle, how);
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
            SocketError socketError = handle.AsyncContext.ConnectAsync(socketAddress, socketAddressLen, asyncResult.CompletionCallback);
            if (socketError == SocketError.Success)
            {
                asyncResult.CompletionCallback(SocketError.Success);
            }
            return socketError;
        }

        public static SocketError SendAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            int bytesSent;
            SocketError socketError = handle.AsyncContext.SendAsync(buffer, offset, count, socketFlags, out bytesSent, asyncResult.CompletionCallback);
            if (socketError == SocketError.Success)
            {
                asyncResult.CompletionCallback(bytesSent, null, 0, SocketFlags.None, SocketError.Success);
            }
            return socketError;
        }

        public static SocketError SendAsync(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            int bytesSent;
            SocketError socketError = handle.AsyncContext.SendAsync(buffers, socketFlags, out bytesSent, asyncResult.CompletionCallback);
            if (socketError == SocketError.Success)
            {
                asyncResult.CompletionCallback(bytesSent, null, 0, SocketFlags.None, SocketError.Success);
            }
            return socketError;
        }

        public static SocketError SendFileAsync(SafeCloseSocket handle, FileStream fileStream, Action<long, SocketError> callback)
        {
            long bytesSent;
            SocketError socketError = handle.AsyncContext.SendFileAsync(fileStream.SafeFileHandle, 0, (int)fileStream.Length, out bytesSent, callback);
            if (socketError == SocketError.Success)
            {
                callback(bytesSent, SocketError.Success);
            }
            return socketError;
        }

        public static SocketError SendToAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, OverlappedAsyncResult asyncResult)
        {
            asyncResult.SocketAddress = socketAddress;

            int bytesSent;
            int socketAddressLen = socketAddress.Size;
            SocketError socketError = handle.AsyncContext.SendToAsync(buffer, offset, count, socketFlags, socketAddress.Buffer, ref socketAddressLen, out bytesSent, asyncResult.CompletionCallback);
            if (socketError == SocketError.Success)
            {
                asyncResult.CompletionCallback(bytesSent, socketAddress.Buffer, socketAddressLen, SocketFlags.None, SocketError.Success);
            }
            return socketError;
        }

        public static SocketError ReceiveAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            int bytesReceived;
            SocketFlags receivedFlags;
            SocketError socketError = handle.AsyncContext.ReceiveAsync(buffer, offset, count, socketFlags, out bytesReceived, out receivedFlags, asyncResult.CompletionCallback);
            if (socketError == SocketError.Success)
            {
                asyncResult.CompletionCallback(bytesReceived, null, 0, receivedFlags, SocketError.Success);
            }
            return socketError;
        }

        public static SocketError ReceiveAsync(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            int bytesReceived;
            SocketFlags receivedFlags;
            SocketError socketError = handle.AsyncContext.ReceiveAsync(buffers, socketFlags, out bytesReceived, out receivedFlags, asyncResult.CompletionCallback);
            if (socketError == SocketError.Success)
            {
                asyncResult.CompletionCallback(bytesReceived, null, 0, receivedFlags, SocketError.Success);
            }
            return socketError;
        }

        public static SocketError ReceiveFromAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, OverlappedAsyncResult asyncResult)
        {
            asyncResult.SocketAddress = socketAddress;

            int socketAddressSize = socketAddress.InternalSize;
            int bytesReceived;
            SocketFlags receivedFlags;
            SocketError socketError = handle.AsyncContext.ReceiveFromAsync(buffer, offset, count, socketFlags, socketAddress.Buffer, ref socketAddressSize, out bytesReceived, out receivedFlags, asyncResult.CompletionCallback);
            if (socketError == SocketError.Success)
            {
                asyncResult.CompletionCallback(bytesReceived, socketAddress.Buffer, socketAddressSize, receivedFlags, SocketError.Success);
            }
            return socketError;
        }

        public static SocketError ReceiveMessageFromAsync(Socket socket, SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, ReceiveMessageOverlappedAsyncResult asyncResult)
        {
            asyncResult.SocketAddress = socketAddress;

            bool isIPv4, isIPv6;
            Socket.GetIPProtocolInformation(((Socket)asyncResult.AsyncObject).AddressFamily, socketAddress, out isIPv4, out isIPv6);

            int socketAddressSize = socketAddress.InternalSize;
            int bytesReceived;
            SocketFlags receivedFlags;
            IPPacketInformation ipPacketInformation;
            SocketError socketError = handle.AsyncContext.ReceiveMessageFromAsync(buffer, offset, count, socketFlags, socketAddress.Buffer, ref socketAddressSize, isIPv4, isIPv6, out bytesReceived, out receivedFlags, out ipPacketInformation, asyncResult.CompletionCallback);
            if (socketError == SocketError.Success)
            {
                asyncResult.CompletionCallback(bytesReceived, socketAddress.Buffer, socketAddressSize, receivedFlags, ipPacketInformation, SocketError.Success);
            }
            return socketError;
        }

        public static SocketError AcceptAsync(Socket socket, SafeCloseSocket handle, SafeCloseSocket acceptHandle, int receiveSize, int socketAddressSize, AcceptOverlappedAsyncResult asyncResult)
        {
            Debug.Assert(acceptHandle == null, $"Unexpected acceptHandle: {acceptHandle}");
            Debug.Assert(receiveSize == 0, $"Unexpected receiveSize: {receiveSize}");

            byte[] socketAddressBuffer = new byte[socketAddressSize];

            IntPtr acceptedFd;
            SocketError socketError = handle.AsyncContext.AcceptAsync(socketAddressBuffer, ref socketAddressSize, out acceptedFd, asyncResult.CompletionCallback);
            if (socketError == SocketError.Success)
            {
                asyncResult.CompletionCallback(acceptedFd, socketAddressBuffer, socketAddressSize, SocketError.Success);
            }

            return socketError;
        }

        internal static SocketError DisconnectAsync(Socket socket, SafeCloseSocket handle, bool reuseSocket, DisconnectOverlappedAsyncResult asyncResult)
        {
            throw new PlatformNotSupportedException(SR.net_sockets_disconnect_notsupported);
        }

        internal static SocketError Disconnect(Socket socket, SafeCloseSocket handle, bool reuseSocket)
        {
            throw new PlatformNotSupportedException(SR.net_sockets_disconnect_notsupported);
        }
    }
}
