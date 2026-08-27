// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    // Thin wrapper over System.Net.Sockets.Socket without a static assembly reference.
    //
    // System.Net.Sockets depends on System.Net.NameResolution (Socket.Connect(host, port)
    // resolves names through Dns), so NameResolution cannot statically reference the Sockets
    // assembly without introducing a cycle in the shared-framework closure. The managed DNS
    // stub resolver still needs raw UDP/TCP sockets, so it reaches Socket through reflection;
    // the assembly is resolved from the shared framework at runtime. SocketException,
    // SocketError and AddressFamily live in System.Net.Primitives and are used directly.
    //
    internal sealed class DnsSocket : IDisposable
    {
        private const string SocketTypeName = "System.Net.Sockets.Socket, System.Net.Sockets";
        private const string SocketTypeEnumName = "System.Net.Sockets.SocketType, System.Net.Sockets";
        private const string ProtocolTypeEnumName = "System.Net.Sockets.ProtocolType, System.Net.Sockets";
        private const string SafeSocketHandleTypeName = "System.Net.Sockets.SafeSocketHandle, System.Net.Sockets";
        private const string SocketFlagsTypeName = "System.Net.Sockets.SocketFlags, System.Net.Sockets";

        private static readonly object s_socketTypeDgram = Enum.Parse(Type.GetType(SocketTypeEnumName, throwOnError: true)!, "Dgram");
        private static readonly object s_socketTypeStream = Enum.Parse(Type.GetType(SocketTypeEnumName, throwOnError: true)!, "Stream");
        private static readonly object s_protocolTypeUdp = Enum.Parse(Type.GetType(ProtocolTypeEnumName, throwOnError: true)!, "Udp");
        private static readonly object s_protocolTypeTcp = Enum.Parse(Type.GetType(ProtocolTypeEnumName, throwOnError: true)!, "Tcp");
        private static readonly object s_socketFlagsPeek = Enum.Parse(Type.GetType(SocketFlagsTypeName, throwOnError: true)!, "Peek");

        public DnsSocket(AddressFamily addressFamily, bool stream)
        {
            _socket = CreateSocket(addressFamily,
                stream ? s_socketTypeStream : s_socketTypeDgram,
                stream ? s_protocolTypeTcp : s_protocolTypeUdp);
        }

        private readonly object _socket;

        public int SendTimeout { set => SetSendTimeout(_socket, value); }

        public int ReceiveTimeout { set => SetReceiveTimeout(_socket, value); }

        public ValueTask ConnectAsync(EndPoint remoteEndPoint, CancellationToken cancellationToken) =>
            ConnectAsync(_socket, remoteEndPoint, cancellationToken);

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
            SendAsync(_socket, buffer, cancellationToken);

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
            ReceiveAsync(_socket, buffer, cancellationToken);

        public void Connect(EndPoint remoteEndPoint) => Connect(_socket, remoteEndPoint);

        public int Send(ReadOnlySpan<byte> buffer) => Send(_socket, buffer);

        public int Receive(Span<byte> buffer) => Receive(_socket, buffer);

        // Connects synchronously with an explicit timeout so an unreachable TCP endpoint cannot
        // block indefinitely. Throws a timed-out SocketException when the timeout elapses.
        public void ConnectWithTimeout(EndPoint remoteEndPoint, TimeSpan timeout)
        {
            IAsyncResult asyncResult = BeginConnect(_socket, remoteEndPoint, null, null);
            try
            {
                if (!asyncResult.AsyncWaitHandle.WaitOne(timeout))
                {
                    Dispose();
                    throw new SocketException((int)SocketError.TimedOut);
                }
                EndConnect(_socket, asyncResult);
            }
            finally
            {
                asyncResult.AsyncWaitHandle.Close();
            }
        }

        public void Dispose() => Dispose(_socket);

        // Awaits real readability on a caller-owned fd. On Unix, an empty-buffer
        // Socket.ReceiveAsync completes synchronously with 0 bytes without ever waiting
        // for POLLIN, so we peek 1 byte instead; SocketFlags.Peek leaves the byte in the
        // socket for the subsequent DNSServiceProcessResult to consume. The wrapped
        // SafeSocketHandle is created with ownsHandle: false so disposing the scratch
        // Socket does not close the fd — the caller (mDNSResponder client library)
        // continues to own it.
        public static async ValueTask WaitReadableAsync(IntPtr fileDescriptor, CancellationToken cancellationToken)
        {
            object safeHandle = CreateSafeSocketHandle(fileDescriptor, ownsHandle: false);
            object socket = CreateSocket(safeHandle);

            try
            {
                int bytesReceived = await ReceiveAsync(socket, new byte[1], s_socketFlagsPeek, cancellationToken).ConfigureAwait(false);
                if (bytesReceived == 0)
                {
                    throw new SocketException((int)SocketError.ConnectionReset);
                }
            }
            finally
            {
                ((IDisposable)socket).Dispose();
            }
        }

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType(SocketTypeName)]
        private static extern object CreateSocket(AddressFamily addressFamily,
            [UnsafeAccessorType(SocketTypeEnumName)] object socketType,
            [UnsafeAccessorType(ProtocolTypeEnumName)] object protocolType);

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType(SocketTypeName)]
        private static extern object CreateSocket([UnsafeAccessorType(SafeSocketHandleTypeName)] object safeHandle);

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType(SafeSocketHandleTypeName)]
        private static extern object CreateSafeSocketHandle(IntPtr handle, bool ownsHandle);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ConnectAsync")]
        private static extern ValueTask ConnectAsync([UnsafeAccessorType(SocketTypeName)] object socket, EndPoint remoteEndPoint, CancellationToken cancellationToken);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SendAsync")]
        private static extern ValueTask<int> SendAsync([UnsafeAccessorType(SocketTypeName)] object socket, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ReceiveAsync")]
        private static extern ValueTask<int> ReceiveAsync([UnsafeAccessorType(SocketTypeName)] object socket, Memory<byte> buffer, CancellationToken cancellationToken);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ReceiveAsync")]
        private static extern ValueTask<int> ReceiveAsync([UnsafeAccessorType(SocketTypeName)] object socket, Memory<byte> buffer,
            [UnsafeAccessorType(SocketFlagsTypeName)] object flags, CancellationToken cancellationToken);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Connect")]
        private static extern void Connect([UnsafeAccessorType(SocketTypeName)] object socket, EndPoint remoteEndPoint);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Send")]
        private static extern int Send([UnsafeAccessorType(SocketTypeName)] object socket, ReadOnlySpan<byte> buffer);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Receive")]
        private static extern int Receive([UnsafeAccessorType(SocketTypeName)] object socket, Span<byte> buffer);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "BeginConnect")]
        private static extern IAsyncResult BeginConnect([UnsafeAccessorType(SocketTypeName)] object socket, EndPoint remoteEndPoint,
            AsyncCallback? callback, object? state);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "EndConnect")]
        private static extern void EndConnect([UnsafeAccessorType(SocketTypeName)] object socket, IAsyncResult asyncResult);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Dispose")]
        private static extern void Dispose([UnsafeAccessorType(SocketTypeName)] object socket);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_SendTimeout")]
        private static extern void SetSendTimeout([UnsafeAccessorType(SocketTypeName)] object socket, int value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ReceiveTimeout")]
        private static extern void SetReceiveTimeout([UnsafeAccessorType(SocketTypeName)] object socket, int value);
    }
}
