// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Net.Security;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Mock
{
    internal sealed class MockConnection : QuicConnectionProvider
    {
        private readonly bool _isClient;
        private bool _disposed;
        private readonly MockListener _listener;        // null if server
        private IPEndPoint _remoteEndPoint;
        private IPEndPoint _localEndPoint;
        private object _syncObject = new object();
        private long _nextOutboundBidirectionalStream;
        private long _nextOutboundUnidirectionalStream;

        private ConnectionState? _state;

        // Constructor for outbound connections
        internal MockConnection(EndPoint? remoteEndPoint, SslClientAuthenticationOptions? sslClientAuthenticationOptions, IPEndPoint? localEndPoint = null)
        {
            if (!(remoteEndPoint is MockListener.MockQuicEndPoint mockQuicEndPoint))
            {
                throw new ArgumentException("Expected endpoint from MockListener", nameof(remoteEndPoint));
            }

            _isClient = true;
            _remoteEndPoint = mockQuicEndPoint;
            _localEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            _listener = mockQuicEndPoint.Listener;

            _nextOutboundBidirectionalStream = 0;
            _nextOutboundUnidirectionalStream = 2;

            // _state is not initialized until ConnectAsync
        }

        // Constructor for accepted inbound connections
        internal MockConnection(MockListener.MockQuicEndPoint localEndPoint, ConnectionState state)
        {
            _isClient = false;
            _remoteEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            _localEndPoint = localEndPoint;
            _listener = localEndPoint.Listener;

            _nextOutboundBidirectionalStream = 1;
            _nextOutboundUnidirectionalStream = 3;

            _state = state;
        }

        internal override bool Connected
        {
            get
            {
                CheckDisposed();

                return _state != null;
            }
        }

        // TODO: Should clone the endpoint since it is mutable
        internal override IPEndPoint LocalEndPoint => _localEndPoint;

        // TODO: Should clone the endpoint since it is mutable
        internal override EndPoint RemoteEndPoint => _remoteEndPoint!;

        internal override SslApplicationProtocol NegotiatedApplicationProtocol => throw new NotImplementedException();

        internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (Connected)
            {
                throw new InvalidOperationException("Already connected");
            }

            Debug.Assert(_isClient, "not connected but also not _isClient??");

            MockListener listener = ((MockListener.MockQuicEndPoint)_remoteEndPoint).Listener;
            _state = new ConnectionState();
            if (!listener.TryConnect(_state))
            {
                throw new QuicException("Connection refused");
            }

            return ValueTask.CompletedTask;
        }

        internal override QuicStreamProvider OpenUnidirectionalStream()
        {
            long streamId;
            lock (_syncObject)
            {
                streamId = _nextOutboundUnidirectionalStream;
                _nextOutboundUnidirectionalStream += 4;
            }

            return OpenStream(streamId, false);
        }

        internal override QuicStreamProvider OpenBidirectionalStream()
        {
            long streamId;
            lock (_syncObject)
            {
                streamId = _nextOutboundBidirectionalStream;
                _nextOutboundBidirectionalStream += 4;
            }

            return OpenStream(streamId, true);
        }

        internal MockStream OpenStream(long streamId, bool bidirectional)
        {
            ConnectionState? state = _state;
            if (state is null)
            {
                throw new InvalidOperationException("Not connected");
            }

            MockStream.StreamState streamState = new MockStream.StreamState(streamId, bidirectional);
            Channel<MockStream.StreamState> streamChannel = _isClient ? state._clientInitiatedStreamChannel : state._serverInitiatedStreamChannel;
            streamChannel.Writer.TryWrite(streamState);

            return new MockStream(new MockStream.StreamState(streamId, bidirectional), true);
        }

        internal override long GetRemoteAvailableUnidirectionalStreamCount()
        {
            throw new NotImplementedException();
        }

        internal override long GetRemoteAvailableBidirectionalStreamCount()
        {
            throw new NotImplementedException();
        }

        internal override async ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            ConnectionState? state = _state;
            if (state is null)
            {
                throw new InvalidOperationException("Not connected");
            }

            Channel<MockStream.StreamState> streamChannel = _isClient ? state._serverInitiatedStreamChannel : state._clientInitiatedStreamChannel;
            MockStream.StreamState streamState = await streamChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            return new MockStream(streamState, false);
        }

        internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default)
        {
            Dispose();
            return default;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(QuicConnection));
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposed = true;
            }
        }

        ~MockConnection()
        {
            Dispose(false);
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal sealed class ConnectionState
        {
            public Channel<MockStream.StreamState> _clientInitiatedStreamChannel;
            public Channel<MockStream.StreamState> _serverInitiatedStreamChannel;

            public ConnectionState()
            {
                _clientInitiatedStreamChannel = Channel.CreateUnbounded<MockStream.StreamState>();
                _serverInitiatedStreamChannel = Channel.CreateUnbounded<MockStream.StreamState>();
            }
        }
    }
}
