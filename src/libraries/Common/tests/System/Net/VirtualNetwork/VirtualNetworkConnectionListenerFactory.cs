// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Connections;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{

    internal sealed class VirtualNetworkConnectionListenerFactory : ConnectionListenerFactory
    {
        public static ConnectionFactory GetConnectionFactory(ConnectionListener listener)
        {
            bool hasFactory = listener.ListenerProperties.TryGet(out VirtualConnectionFactory factory);
            Debug.Assert(hasFactory);
            return factory;
        }

        public override ValueTask<ConnectionListener> ListenAsync(EndPoint endPoint, IConnectionProperties options = null, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return ValueTask.FromCanceled<ConnectionListener>(cancellationToken);
            return new ValueTask<ConnectionListener>(new VirtualConnectionListener(endPoint));
        }

        protected override void Dispose(bool disposing)
        {
        }

        protected override ValueTask DisposeAsyncCore()
        {
            return default;
        }

        private sealed class VirtualConnectionListener : ConnectionListener, IConnectionProperties
        {
            private readonly Channel<TaskCompletionSource<Connection>> _pendingConnects;
            private readonly VirtualConnectionFactory _connectionFactory;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();

            public override IConnectionProperties ListenerProperties => this;

            public override EndPoint LocalEndPoint { get; }

            public VirtualConnectionListener(EndPoint localEndPoint)
            {
                LocalEndPoint = localEndPoint;

                _pendingConnects = Channel.CreateUnbounded<TaskCompletionSource<Connection>>();
                _connectionFactory = new VirtualConnectionFactory(this);
            }

            public override async ValueTask<Connection> AcceptAsync(IConnectionProperties options = null, CancellationToken cancellationToken = default)
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);

                var network = new VirtualNetwork();
                var serverConnection = new VirtualConnection(network, isServer: true);
                var clientConnection = new VirtualConnection(network, isServer: false);

                while (true)
                {
                    TaskCompletionSource<Connection> tcs = await _pendingConnects.Reader.ReadAsync(cancellationToken);
                    if (tcs.TrySetResult(clientConnection))
                    {
                        return serverConnection;
                    }
                }
            }

            internal async ValueTask<Connection> ConnectAsync(EndPoint endPoint, IConnectionProperties options = null, CancellationToken cancellationToken = default)
            {
                var tcs = new TaskCompletionSource<Connection>();
                await _pendingConnects.Writer.WriteAsync(tcs, cancellationToken).ConfigureAwait(false);

                using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken))
                using (cts.Token.UnsafeRegister(o => ((TaskCompletionSource<Connection>)o).TrySetCanceled(), tcs))
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _cts.Cancel();
                }
            }

            protected override ValueTask DisposeAsyncCore()
            {
                Dispose(true);
                return default;
            }

            bool IConnectionProperties.TryGet(Type propertyKey, out object property)
            {
                if (propertyKey == typeof(VirtualConnectionFactory))
                {
                    property = _connectionFactory;
                    return true;
                }

                property = null;
                return false;
            }
        }

        private sealed class VirtualConnectionFactory : ConnectionFactory
        {
            private readonly VirtualConnectionListener _listener;

            public VirtualConnectionFactory(VirtualConnectionListener listener)
            {
                _listener = listener;
            }

            public override ValueTask<Connection> ConnectAsync(EndPoint endPoint, IConnectionProperties options = null, CancellationToken cancellationToken = default)
            {
                return _listener.ConnectAsync(endPoint, options, cancellationToken);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _listener.Dispose();
                }
            }

            protected override ValueTask DisposeAsyncCore()
            {
                return _listener.DisposeAsync();
            }
        }

        private sealed class VirtualConnection : Connection, IConnectionProperties
        {
            private readonly VirtualNetwork _network;
            private bool _isServer;

            public override IConnectionProperties ConnectionProperties => this;

            public override EndPoint LocalEndPoint => null;

            public override EndPoint RemoteEndPoint => null;

            public VirtualConnection(VirtualNetwork network, bool isServer)
            {
                _network = network;
                _isServer = isServer;
            }

            protected override Stream CreateStream()
            {
                return new VirtualNetworkStream(_network, _isServer, gracefulShutdown: true);
            }

            protected override ValueTask CloseAsyncCore(ConnectionCloseMethod method, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested) return ValueTask.FromCanceled(cancellationToken);

                if (method == ConnectionCloseMethod.GracefulShutdown)
                {
                    _network.GracefulShutdown(_isServer);
                }
                else
                {
                    _network.BreakConnection();
                }

                return default;
            }

            bool IConnectionProperties.TryGet(Type propertyKey, out object property)
            {
                property = null;
                return false;
            }
        }
    }

}
