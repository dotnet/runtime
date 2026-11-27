// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    // Thin wrapper over System.Net.Sockets.Socket without a static assembly reference.
    //
    // System.Net.Sockets depends on System.Net.NameResolution (Socket.Connect(host, port)
    // resolves names through Dns), so NameResolution cannot statically reference the Sockets
    // assembly without introducing a cycle in the shared-framework closure. The managed DNS
    // stub resolver still needs raw UDP/TCP sockets, so it reaches Socket through type-name
    // accessors; the assembly is resolved from the shared framework at runtime. SocketException,
    // SocketError and AddressFamily live in System.Net.Primitives and are used directly.
    //
    internal sealed class DnsSocket : IDisposable
    {
        private const string SocketTypeName = "System.Net.Sockets.Socket, System.Net.Sockets";
        private const string SocketTypeEnumName = "System.Net.Sockets.SocketType, System.Net.Sockets";
        private const string ProtocolTypeEnumName = "System.Net.Sockets.ProtocolType, System.Net.Sockets";
        private const string SafeSocketHandleTypeName = "System.Net.Sockets.SafeSocketHandle, System.Net.Sockets";
        private const string SocketFlagsTypeName = "System.Net.Sockets.SocketFlags, System.Net.Sockets";

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        private static readonly Type s_socketType = Type.GetType(SocketTypeName, throwOnError: true)!;
        private static readonly Type s_socketTypeEnum = Type.GetType(SocketTypeEnumName, throwOnError: true)!;
        private static readonly Type s_protocolTypeEnum = Type.GetType(ProtocolTypeEnumName, throwOnError: true)!;
        private static readonly Type s_socketFlagsEnum = Type.GetType(SocketFlagsTypeName, throwOnError: true)!;
        // UnsafeAccessorType cannot represent the SocketType, ProtocolType and SocketFlags
        // value-type parameters without referencing System.Net.Sockets, which would create a
        // cycle, so the constructor and the SocketFlags-taking ReceiveAsync overload are
        // invoked through reflection instead.
        private static readonly ConstructorInfo s_socketConstructor =
            s_socketType.GetConstructor(new[] { typeof(AddressFamily), s_socketTypeEnum, s_protocolTypeEnum })!;
        private static readonly MethodInfo s_receiveAsyncWithFlags =
            s_socketType.GetMethod("ReceiveAsync", new[] { typeof(Memory<byte>), s_socketFlagsEnum, typeof(CancellationToken) })!;
        private static readonly object s_socketTypeDgram = Enum.Parse(s_socketTypeEnum, "Dgram");
        private static readonly object s_socketTypeStream = Enum.Parse(s_socketTypeEnum, "Stream");
        private static readonly object s_protocolTypeUdp = Enum.Parse(s_protocolTypeEnum, "Udp");
        private static readonly object s_protocolTypeTcp = Enum.Parse(s_protocolTypeEnum, "Tcp");
        private static readonly object s_socketFlagsPeek = Enum.Parse(s_socketFlagsEnum, "Peek");

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties,
            "System.Net.Sockets.Socket", "System.Net.Sockets")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors,
            "System.Net.Sockets.SafeSocketHandle", "System.Net.Sockets")]
        public DnsSocket(AddressFamily addressFamily, bool stream)
        {
            try
            {
                _socket = s_socketConstructor.Invoke(new object[]
                {
                    addressFamily,
                    stream ? s_socketTypeStream : s_socketTypeDgram,
                    stream ? s_protocolTypeTcp : s_protocolTypeUdp,
                })!;
            }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            {
                ExceptionDispatchInfo.Throw(e.InnerException);
                throw;
            }
        }

        public DnsSocket(IntPtr fileDescriptor)
        {
            object safeHandle = CreateSafeSocketHandle(fileDescriptor, ownsHandle: false);
            _socket = CreateSocket(safeHandle);
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

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, bool peek, CancellationToken cancellationToken) =>
            peek
                ? (ValueTask<int>)s_receiveAsyncWithFlags.Invoke(_socket, new object[] { buffer, s_socketFlagsPeek, cancellationToken })!
                : ReceiveAsync(_socket, buffer, cancellationToken);

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

        public void Dispose() => ((IDisposable)_socket).Dispose();

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

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_SendTimeout")]
        private static extern void SetSendTimeout([UnsafeAccessorType(SocketTypeName)] object socket, int value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ReceiveTimeout")]
        private static extern void SetReceiveTimeout([UnsafeAccessorType(SocketTypeName)] object socket, int value);
    }
}
