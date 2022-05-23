// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
        private readonly int _maxUnidirectionalStreams;
        private readonly int _maxBidirectionalStreams;

        private ConnectionState? _state;

        internal PeerStreamLimit? LocalStreamLimit => _isClient ? _state?._clientStreamLimit : _state?._serverStreamLimit;
        internal PeerStreamLimit? RemoteStreamLimit => _isClient ? _state?._serverStreamLimit : _state?._clientStreamLimit;

        internal long? ConnectionError
        {
            get
            {
                long? errorCode = _isClient ? _state?._serverErrorCode : _state?._clientErrorCode;
                if (errorCode == -1)
                {
                    errorCode = null;
                }

                return errorCode;
            }
        }

        internal override X509Certificate? RemoteCertificate => null;

        // Constructor for outbound connections
        internal MockConnection(EndPoint remoteEndPoint, SslClientAuthenticationOptions? sslClientAuthenticationOptions, IPEndPoint? localEndPoint = null, int maxUnidirectionalStreams = 100, int maxBidirectionalStreams = 100)
        {
            ArgumentNullException.ThrowIfNull(remoteEndPoint);

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
            _maxUnidirectionalStreams = maxUnidirectionalStreams;
            _maxBidirectionalStreams = maxBidirectionalStreams;

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
        // TODO: could this be made back to non-nullable?
        //       For inbound we have it immediately, for outbound after connect.
        internal override IPEndPoint? LocalEndPoint => _localEndPoint;

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
            _state = new ConnectionState(_sslClientAuthenticationOptions!.ApplicationProtocols![0])
            {
                _clientStreamLimit = new PeerStreamLimit(_maxUnidirectionalStreams, _maxBidirectionalStreams)
            };
            if (!listener.TryConnect(_state))
            {
                throw new QuicException("Connection refused");
            }

            return ValueTask.CompletedTask;
        }

        internal async override ValueTask<QuicStreamProvider> OpenUnidirectionalStreamAsync(CancellationToken cancellationToken)
        {
            PeerStreamLimit? streamLimit = RemoteStreamLimit;
            if (streamLimit is null)
            {
                throw new InvalidOperationException("Not connected");
            }

            while (!streamLimit.Unidirectional.TryIncrement())
            {
                await streamLimit.Unidirectional.WaitForAvailableStreams(cancellationToken).ConfigureAwait(false);
            }

            long streamId;
            lock (_syncObject)
            {
                streamId = _nextOutboundUnidirectionalStream;
                _nextOutboundUnidirectionalStream += 4;
            }

            return OpenStream(streamId, false);
        }

        internal async override ValueTask<QuicStreamProvider> OpenBidirectionalStreamAsync(CancellationToken cancellationToken)
        {
            PeerStreamLimit? streamLimit = RemoteStreamLimit;
            if (streamLimit is null)
            {
                throw new InvalidOperationException("Not connected");
            }

            while (!streamLimit.Bidirectional.TryIncrement())
            {
                await streamLimit.Bidirectional.WaitForAvailableStreams(cancellationToken).ConfigureAwait(false);
            }

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
            CheckDisposed();

            ConnectionState? state = _state;
            if (state is null)
            {
                throw new InvalidOperationException("Not connected");
            }

            MockStream.StreamState streamState = new MockStream.StreamState(streamId, bidirectional);
            // TODO Streams are never removed from a connection. Consider cleaning up in the future.
            state._streams[streamState._streamId] = streamState;

            Channel<MockStream.StreamState> streamChannel = _isClient ? state._clientInitiatedStreamChannel : state._serverInitiatedStreamChannel;
            streamChannel.Writer.TryWrite(streamState);

            return new MockStream(this, streamState, true);
        }

        internal override int GetRemoteAvailableUnidirectionalStreamCount()
        {
            PeerStreamLimit? streamLimit = RemoteStreamLimit;
            if (streamLimit is null)
            {
                throw new InvalidOperationException("Not connected");
            }

            return streamLimit.Unidirectional.AvailableCount;
        }

        internal override int GetRemoteAvailableBidirectionalStreamCount()
        {
            PeerStreamLimit? streamLimit = RemoteStreamLimit;
            if (streamLimit is null)
            {
                throw new InvalidOperationException("Not connected");
            }

            return streamLimit.Bidirectional.AvailableCount;
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

            try
            {
                MockStream.StreamState streamState = await streamChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                return new MockStream(this, streamState, false);
            }
            catch (ChannelClosedException)
            {
                long errorCode = _isClient ? state._serverErrorCode : state._clientErrorCode;
                throw (errorCode == -1) ? new QuicOperationAbortedException() : new QuicConnectionAbortedException(errorCode);
            }
        }

        internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default)
        {
            // TODO: We should abort local streams (and signal the peer to do likewise)
            // Currently, we are not tracking the streams associated with this connection.

            ConnectionState? state = _state;
            if (state is not null)
            {
                if (state._closed)
                {
                    return default;
                }
                state._closed = true;

                if (_isClient)
                {
                    state._clientErrorCode = errorCode;
                    DrainAcceptQueue(-1, errorCode);
                }
                else
                {
                    state._serverErrorCode = errorCode;
                    DrainAcceptQueue(errorCode, -1);
                }

                foreach (KeyValuePair<long, MockStream.StreamState> kvp in state._streams)
                {
                    kvp.Value._outboundWritesCompletedTcs.TrySetException(new QuicConnectionAbortedException(errorCode));
                    kvp.Value._inboundWritesCompletedTcs.TrySetException(new QuicConnectionAbortedException(errorCode));
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

        private void DrainAcceptQueue(long outboundErrorCode, long inboundErrorCode)
        {
            ConnectionState? state = _state;
            if (state is not null)
            {
                // TODO: We really only need to do the complete and drain once, but it doesn't really hurt to do it twice.
                state._clientInitiatedStreamChannel.Writer.TryComplete();
                while (state._clientInitiatedStreamChannel.Reader.TryRead(out MockStream.StreamState? streamState))
                {
                    streamState._outboundReadErrorCode = streamState._outboundWriteErrorCode = outboundErrorCode;
                    streamState._inboundStreamBuffer?.AbortRead();
                    streamState._outboundStreamBuffer?.EndWrite();
                }

                state._serverInitiatedStreamChannel.Writer.TryComplete();
                while (state._serverInitiatedStreamChannel.Reader.TryRead(out MockStream.StreamState? streamState))
                {
                    streamState._inboundReadErrorCode = streamState._inboundWriteErrorCode = inboundErrorCode;
                    streamState._outboundStreamBuffer?.AbortRead();
                    streamState._inboundStreamBuffer?.EndWrite();
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    DrainAcceptQueue(-1, -1);

                    PeerStreamLimit? streamLimit = LocalStreamLimit;
                    if (streamLimit is not null)
                    {
                        streamLimit.Unidirectional.CloseWaiters();
                        streamLimit.Bidirectional.CloseWaiters();
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

        internal sealed class StreamLimit
        {
            public readonly int MaxCount;

            private int _actualCount;
            // Since this is mock, we don't need to be conservative with the allocations.
            // We keep the TCSes allocated all the time for the simplicity of the code.
            private TaskCompletionSource _availableTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly object _syncRoot = new object();

            public StreamLimit(int maxCount)
            {
                MaxCount = maxCount;
            }

            public int AvailableCount => MaxCount - _actualCount;

            public void Decrement()
            {
                TaskCompletionSource? availableTcs = null;
                lock (_syncRoot)
                {
                    --_actualCount;
                    if (!_availableTcs.Task.IsCompleted)
                    {
                        availableTcs = _availableTcs;
                        _availableTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    }
                }
                if (availableTcs is not null)
                {
                    availableTcs.SetResult();
                }
            }

            public bool TryIncrement()
            {
                lock (_syncRoot)
                {
                    if (_actualCount < MaxCount)
                    {
                        ++_actualCount;
                        return true;
                    }
                    return false;
                }
            }

            public ValueTask WaitForAvailableStreams(CancellationToken cancellationToken)
            {
                TaskCompletionSource availableTcs;
                lock (_syncRoot)
                {
                    if (_actualCount > 0)
                    {
                        return default;
                    }
                    availableTcs = _availableTcs;
                }
                return new ValueTask(availableTcs.Task.WaitAsync(cancellationToken));
            }

            public void CloseWaiters()
                => _availableTcs.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException()));
        }

        internal sealed class PeerStreamLimit
        {
            public readonly StreamLimit Unidirectional;
            public readonly StreamLimit Bidirectional;

            public PeerStreamLimit(int maxUnidirectional, int maxBidirectional)
            {
                Unidirectional = new StreamLimit(maxUnidirectional);
                Bidirectional = new StreamLimit(maxBidirectional);
            }
        }

        internal sealed class ConnectionState
        {
            public readonly SslApplicationProtocol _applicationProtocol;
            public readonly Channel<MockStream.StreamState> _clientInitiatedStreamChannel;
            public readonly Channel<MockStream.StreamState> _serverInitiatedStreamChannel;
            public readonly ConcurrentDictionary<long, MockStream.StreamState> _streams;

            public PeerStreamLimit? _clientStreamLimit;
            public PeerStreamLimit? _serverStreamLimit;

            public long _clientErrorCode;
            public long _serverErrorCode;
            public bool _closed;

            public ConnectionState(SslApplicationProtocol applicationProtocol)
            {
                _applicationProtocol = applicationProtocol;
                _clientInitiatedStreamChannel = Channel.CreateUnbounded<MockStream.StreamState>();
                _serverInitiatedStreamChannel = Channel.CreateUnbounded<MockStream.StreamState>();
                _clientErrorCode = _serverErrorCode = -1;
                _streams = new ConcurrentDictionary<long, MockStream.StreamState>();
            }
        }
    }
}
