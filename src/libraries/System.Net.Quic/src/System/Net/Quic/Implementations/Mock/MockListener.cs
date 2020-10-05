// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Mock
{
    internal sealed class MockListener : QuicListenerProvider
    {
        private bool _disposed;
        private readonly QuicListenerOptions _options;
        private readonly MockQuicEndPoint _listenEndPoint;
        private Channel<MockConnection.ConnectionState> _listenQueue;

        internal MockListener(QuicListenerOptions options)
        {
            if (options.ListenEndPoint is null || options.ListenEndPoint.Address != IPAddress.Loopback || options.ListenEndPoint.Port != 0)
            {
                throw new ArgumentException("Must pass loopback address and port 0");
            }

            _options = options;
            _listenEndPoint = new MockQuicEndPoint(this);
            _listenQueue = Channel.CreateBounded<MockConnection.ConnectionState>(new BoundedChannelOptions(options.ListenBacklog));
        }

        // IPEndPoint is mutable, so we must create a new instance every time this is retrieved.
        internal override IPEndPoint ListenEndPoint => _listenEndPoint.Clone();

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

        internal override void Start()
        {
            CheckDisposed();

            // TODO: Track start
        }

        internal override void Close()
        {
            Dispose();
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

        internal sealed class MockQuicEndPoint : IPEndPoint
        {
            private readonly MockListener _listener;

            // Set the port to 1 just so that code (e.g. SocketsHttpHandler) won't choke on it. This is meaningless.
            public MockQuicEndPoint(MockListener listener) : base(IPAddress.Loopback, 1)
            {
                _listener = listener;
            }

            public MockListener Listener => _listener;

            public MockQuicEndPoint Clone() => new MockQuicEndPoint(_listener);

            public override bool Equals(object? comparand)
            {
                return (comparand is MockQuicEndPoint mockQuicEndPoint && mockQuicEndPoint.Listener == this.Listener);
            }

            public override int GetHashCode() => _listener.GetHashCode();
        }
    }
}
