// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
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
        private SslClientAuthenticationOptions? _sslClientAuthenticationOptions;
        private IPEndPoint _remoteEndPoint;
        private IPEndPoint _localEndPoint;
        private object _syncObject = new object();
        private long _nextOutboundBidirectionalStream;
        private long _nextOutboundUnidirectionalStream;

        private ConnectionState? _state;

        // Constructor for outbound connections
        internal MockConnection(EndPoint? remoteEndPoint, SslClientAuthenticationOptions? sslClientAuthenticationOptions, IPEndPoint? localEndPoint = null)
        {
            if (remoteEndPoint is null)
            {
                throw new ArgumentNullException(nameof(remoteEndPoint));
            }

            IPEndPoint ipEndPoint = GetIPEndPoint(remoteEndPoint);
            if (ipEndPoint.Address != IPAddress.Loopback)
            {
                throw new ArgumentException("Expected loopback address", nameof(remoteEndPoint));
            }

            _isClient = true;
            _remoteEndPoint = ipEndPoint;
            _localEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            _sslClientAuthenticationOptions = sslClientAuthenticationOptions;
            _nextOutboundBidirectionalStream = 0;
            _nextOutboundUnidirectionalStream = 2;

            // _state is not initialized until ConnectAsync
        }

        // Constructor for accepted inbound connections
        internal MockConnection(IPEndPoint localEndPoint, ConnectionState state)
        {
            _isClient = false;
            _remoteEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            _localEndPoint = localEndPoint;

            _nextOutboundBidirectionalStream = 1;
            _nextOutboundUnidirectionalStream = 3;

            _state = state;
        }

        private static IPEndPoint GetIPEndPoint(EndPoint endPoint)
        {
            if (endPoint is IPEndPoint ipEndPoint)
            {
                return ipEndPoint;
            }

            if (endPoint is DnsEndPoint dnsEndPoint)
            {
                if (dnsEndPoint.Host == "127.0.0.1")
                {
                    return new IPEndPoint(IPAddress.Loopback, dnsEndPoint.Port);
                }

                throw new InvalidOperationException($"invalid DNS name {dnsEndPoint.Host}");
            }

            throw new InvalidOperationException("unknown EndPoint type");
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

        internal override SslApplicationProtocol NegotiatedApplicationProtocol
        {
            get
            {
                if (_state is null)
                {
                    throw new InvalidOperationException("not connected");
                }

                return _state._applicationProtocol;
            }
        }

        internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (Connected)
            {
                throw new InvalidOperationException("Already connected");
            }

            Debug.Assert(_isClient, "not connected but also not _isClient??");

            MockListener? listener = MockListener.TryGetListener(_remoteEndPoint);
            if (listener is null)
            {
                throw new InvalidOperationException("Could not find listener");
            }

            // TODO: deal with protocol negotiation
            _state = new ConnectionState(_sslClientAuthenticationOptions!.ApplicationProtocols![0]);
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

            return new MockStream(streamState, true);
        }

        internal override long GetRemoteAvailableUnidirectionalStreamCount() => long.MaxValue;

        internal override long GetRemoteAvailableBidirectionalStreamCount() => long.MaxValue;

        internal override async ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            ConnectionState? state = _state;
            if (state is null)
            {
                throw new InvalidOperationException("Not connected");
            }

            Channel<MockStream.StreamState> streamChannel = _isClient ? state._serverInitiatedStreamChannel : state._clientInitiatedStreamChannel;

            try
            {
                MockStream.StreamState streamState = await streamChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                return new MockStream(streamState, false);
            }
            catch (ChannelClosedException)
            {
                long errorCode = _isClient ? state._serverErrorCode : state._clientErrorCode;
                throw new QuicConnectionAbortedException(errorCode);
            }
        }

        internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default)
        {
            ConnectionState? state = _state;
            if (state is not null)
            {
                if (_isClient)
                {
                    state._clientErrorCode = errorCode;
                }
                else
                {
                    state._serverErrorCode = errorCode;
                }
            }

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
                    ConnectionState? state = _state;
                    if (state is not null)
                    {
                        Channel<MockStream.StreamState> streamChannel = _isClient ? state._clientInitiatedStreamChannel : state._serverInitiatedStreamChannel;
                        streamChannel.Writer.Complete();
                    }
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
            public readonly SslApplicationProtocol _applicationProtocol;
            public Channel<MockStream.StreamState> _clientInitiatedStreamChannel;
            public Channel<MockStream.StreamState> _serverInitiatedStreamChannel;
            public long _clientErrorCode;
            public long _serverErrorCode;

            public ConnectionState(SslApplicationProtocol applicationProtocol)
            {
                _applicationProtocol = applicationProtocol;
                _clientInitiatedStreamChannel = Channel.CreateUnbounded<MockStream.StreamState>();
                _serverInitiatedStreamChannel = Channel.CreateUnbounded<MockStream.StreamState>();
            }
        }
    }
}
