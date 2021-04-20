// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Mock
{
    internal sealed class MockListener : QuicListenerProvider
    {
        private bool _disposed;
        private readonly QuicListenerOptions _options;
        private readonly IPEndPoint _listenEndPoint;
        private Channel<MockConnection.ConnectionState> _listenQueue;

        // We synthesize port numbers for the listener, starting with 1, and track these in a dictionary.
        private static int s_mockPort;
        private static ConcurrentDictionary<int, MockListener> s_listenerMap = new ConcurrentDictionary<int, MockListener>();

        internal MockListener(QuicListenerOptions options)
        {
            if (options.ListenEndPoint is null || options.ListenEndPoint.Address != IPAddress.Loopback || options.ListenEndPoint.Port != 0)
            {
                throw new ArgumentException("Must pass loopback address and port 0");
            }

            _options = options;

            int port = Interlocked.Increment(ref s_mockPort);

            _listenEndPoint = new IPEndPoint(IPAddress.Loopback, port);
            bool success = s_listenerMap.TryAdd(port, this);
            Debug.Assert(success);

            _listenQueue = Channel.CreateBounded<MockConnection.ConnectionState>(new BoundedChannelOptions(options.ListenBacklog));
        }

        // TODO: IPEndPoint is mutable, so we should create a copy here.
        internal override IPEndPoint ListenEndPoint => _listenEndPoint;

        internal static MockListener? TryGetListener(IPEndPoint endpoint)
        {
            if (endpoint.Address != IPAddress.Loopback || endpoint.Port == 0)
            {
                return null;
            }

            MockListener? listener;
            if (!s_listenerMap.TryGetValue(endpoint.Port, out listener))
            {
                return null;
            }

            return listener;
        }

        internal override async ValueTask<QuicConnectionProvider> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            MockConnection.ConnectionState state = await _listenQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            return new MockConnection(_listenEndPoint, state);
        }

        // Returns false if backlog queue is full.
        internal bool TryConnect(MockConnection.ConnectionState state)
        {
            return _listenQueue.Writer.TryWrite(state);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(QuicListener));
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    MockListener? listener;
                    bool success = s_listenerMap.TryRemove(_listenEndPoint.Port, out listener);
                    Debug.Assert(success);
                    Debug.Assert(listener == this);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposed = true;
            }
        }

        ~MockListener()
        {
            Dispose(false);
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
