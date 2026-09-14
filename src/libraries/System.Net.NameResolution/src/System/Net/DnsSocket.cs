// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    // Thin wrapper over System.Net.Sockets.Socket accessed via reflection.
    //
    // System.Net.Sockets depends on System.Net.NameResolution (Socket.Connect(host, port)
    // resolves names through Dns), so NameResolution cannot statically reference the Sockets
    // assembly without introducing a cycle in the shared-framework closure. The managed DNS
    // stub resolver still needs raw UDP/TCP sockets, so it reaches Socket through reflection;
    // the assembly is resolved from the shared framework at runtime. SocketException,
    // SocketError and AddressFamily live in System.Net.Primitives and are used directly.
    //
    // Instance operations are exposed through delegates bound to the underlying Socket so that
    // exceptions (e.g. SocketException) propagate to callers directly instead of being wrapped
    // in a TargetInvocationException.
    internal sealed class DnsSocket : IDisposable
    {
        private sealed class SocketReflection
        {
            public ConstructorInfo Constructor = null!;
            public MethodInfo ConnectAsyncMethod = null!;
            public MethodInfo SendAsyncMethod = null!;
            public MethodInfo ReceiveAsyncMethod = null!;
            public MethodInfo ConnectMethod = null!;
            public MethodInfo SendMethod = null!;
            public MethodInfo ReceiveMethod = null!;
            public MethodInfo BeginConnectMethod = null!;
            public MethodInfo EndConnectMethod = null!;
            public MethodInfo DisposeMethod = null!;
            public MethodInfo SetSendTimeoutMethod = null!;
            public MethodInfo SetReceiveTimeoutMethod = null!;
            public object SocketTypeDgram = null!;
            public object SocketTypeStream = null!;
            public object ProtocolTypeUdp = null!;
            public object ProtocolTypeTcp = null!;
        }

        private static readonly SocketReflection s_reflection = CreateReflection();

        private delegate int SendSpanDelegate(ReadOnlySpan<byte> buffer);
        private delegate int ReceiveSpanDelegate(Span<byte> buffer);

        private readonly Func<EndPoint, CancellationToken, ValueTask> _connectAsync;
        private readonly Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask<int>> _sendAsync;
        private readonly Func<Memory<byte>, CancellationToken, ValueTask<int>> _receiveAsync;
        private readonly Action<EndPoint> _connect;
        private readonly SendSpanDelegate _send;
        private readonly ReceiveSpanDelegate _receive;
        private readonly Func<EndPoint, AsyncCallback?, object?, IAsyncResult> _beginConnect;
        private readonly Action<IAsyncResult> _endConnect;
        private readonly Action<int> _setSendTimeout;
        private readonly Action<int> _setReceiveTimeout;
        private readonly Action _dispose;

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties,
            "System.Net.Sockets.Socket", "System.Net.Sockets")]
        private static SocketReflection CreateReflection()
        {
            Type socketType = Type.GetType("System.Net.Sockets.Socket, System.Net.Sockets", throwOnError: true)!;
            Type socketTypeEnum = Type.GetType("System.Net.Sockets.SocketType, System.Net.Sockets", throwOnError: true)!;
            Type protocolTypeEnum = Type.GetType("System.Net.Sockets.ProtocolType, System.Net.Sockets", throwOnError: true)!;

            return new SocketReflection
            {
                SocketTypeDgram = Enum.Parse(socketTypeEnum, "Dgram"),
                SocketTypeStream = Enum.Parse(socketTypeEnum, "Stream"),
                ProtocolTypeUdp = Enum.Parse(protocolTypeEnum, "Udp"),
                ProtocolTypeTcp = Enum.Parse(protocolTypeEnum, "Tcp"),
                Constructor = socketType.GetConstructor(new[] { typeof(AddressFamily), socketTypeEnum, protocolTypeEnum })!,
                ConnectAsyncMethod = socketType.GetMethod("ConnectAsync", new[] { typeof(EndPoint), typeof(CancellationToken) })!,
                SendAsyncMethod = socketType.GetMethod("SendAsync", new[] { typeof(ReadOnlyMemory<byte>), typeof(CancellationToken) })!,
                ReceiveAsyncMethod = socketType.GetMethod("ReceiveAsync", new[] { typeof(Memory<byte>), typeof(CancellationToken) })!,
                ConnectMethod = socketType.GetMethod("Connect", new[] { typeof(EndPoint) })!,
                SendMethod = socketType.GetMethod("Send", new[] { typeof(ReadOnlySpan<byte>) })!,
                ReceiveMethod = socketType.GetMethod("Receive", new[] { typeof(Span<byte>) })!,
                BeginConnectMethod = socketType.GetMethod("BeginConnect", new[] { typeof(EndPoint), typeof(AsyncCallback), typeof(object) })!,
                EndConnectMethod = socketType.GetMethod("EndConnect", new[] { typeof(IAsyncResult) })!,
                DisposeMethod = socketType.GetMethod("Dispose", Type.EmptyTypes)!,
                SetSendTimeoutMethod = socketType.GetProperty("SendTimeout")!.GetSetMethod()!,
                SetReceiveTimeoutMethod = socketType.GetProperty("ReceiveTimeout")!.GetSetMethod()!,
            };
        }

        public DnsSocket(AddressFamily addressFamily, bool stream)
        {
            SocketReflection reflection = s_reflection;
            object socket;
            try
            {
                socket = reflection.Constructor.Invoke(new object[]
                {
                    addressFamily,
                    stream ? reflection.SocketTypeStream : reflection.SocketTypeDgram,
                    stream ? reflection.ProtocolTypeTcp : reflection.ProtocolTypeUdp,
                })!;
            }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            {
                ExceptionDispatchInfo.Throw(e.InnerException);
                throw; // Unreachable, satisfies definite-assignment.
            }

            _connectAsync = reflection.ConnectAsyncMethod.CreateDelegate<Func<EndPoint, CancellationToken, ValueTask>>(socket);
            _sendAsync = reflection.SendAsyncMethod.CreateDelegate<Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask<int>>>(socket);
            _receiveAsync = reflection.ReceiveAsyncMethod.CreateDelegate<Func<Memory<byte>, CancellationToken, ValueTask<int>>>(socket);
            _connect = reflection.ConnectMethod.CreateDelegate<Action<EndPoint>>(socket);
            _send = reflection.SendMethod.CreateDelegate<SendSpanDelegate>(socket);
            _receive = reflection.ReceiveMethod.CreateDelegate<ReceiveSpanDelegate>(socket);
            _beginConnect = reflection.BeginConnectMethod.CreateDelegate<Func<EndPoint, AsyncCallback?, object?, IAsyncResult>>(socket);
            _endConnect = reflection.EndConnectMethod.CreateDelegate<Action<IAsyncResult>>(socket);
            _setSendTimeout = reflection.SetSendTimeoutMethod.CreateDelegate<Action<int>>(socket);
            _setReceiveTimeout = reflection.SetReceiveTimeoutMethod.CreateDelegate<Action<int>>(socket);
            _dispose = reflection.DisposeMethod.CreateDelegate<Action>(socket);
        }

        public int SendTimeout { set => _setSendTimeout(value); }

        public int ReceiveTimeout { set => _setReceiveTimeout(value); }

        public ValueTask ConnectAsync(EndPoint remoteEndPoint, CancellationToken cancellationToken) =>
            _connectAsync(remoteEndPoint, cancellationToken);

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
            _sendAsync(buffer, cancellationToken);

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
            _receiveAsync(buffer, cancellationToken);

        public void Connect(EndPoint remoteEndPoint) => _connect(remoteEndPoint);

        public int Send(ReadOnlySpan<byte> buffer) => _send(buffer);

        public int Receive(Span<byte> buffer) => _receive(buffer);

        // Connects synchronously with an explicit timeout so an unreachable TCP endpoint cannot
        // block indefinitely. Throws a timed-out SocketException when the timeout elapses.
        public void ConnectWithTimeout(EndPoint remoteEndPoint, TimeSpan timeout)
        {
            IAsyncResult asyncResult = _beginConnect(remoteEndPoint, null, null);
            try
            {
                if (!asyncResult.AsyncWaitHandle.WaitOne(timeout))
                {
                    Dispose();
                    throw new SocketException((int)SocketError.TimedOut);
                }
                _endConnect(asyncResult);
            }
            finally
            {
                asyncResult.AsyncWaitHandle.Close();
            }
        }

        public void Dispose() => _dispose();
    }
}
