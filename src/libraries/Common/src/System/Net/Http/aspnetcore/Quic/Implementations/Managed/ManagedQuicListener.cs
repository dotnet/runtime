using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed
{
    internal class ManagedQuicListener : QuicListenerProvider
    {
        private bool _disposed;

        private readonly ChannelReader<ManagedQuicConnection> _acceptQueue;
        private readonly QuicServerSocketContext _socketContext;

        public ManagedQuicListener(QuicListenerOptions options)
        {
            var listenEndPoint = options.ListenEndPoint ?? new IPEndPoint(IPAddress.Any, 0);

            var channel = Channel.CreateBounded<ManagedQuicConnection>(new BoundedChannelOptions(options.ListenBacklog)
            {
                SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.DropWrite
            });

            _acceptQueue = channel.Reader;
            _socketContext = new QuicServerSocketContext(listenEndPoint, options, channel.Writer);
        }

        internal override IPEndPoint ListenEndPoint
        {
            get
            {
                ThrowIfDisposed();
                return _socketContext.LocalEndPoint;
            }
        }

        internal override async ValueTask<QuicConnectionProvider> AcceptConnectionAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            // TODO-RZ: make this non-async when the cast is no longer needed
            return await _acceptQueue.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        internal override void Start()
        {
            ThrowIfDisposed();

            _socketContext.Start();
        }

        internal override void Close()
        {
            Dispose();
        }

        public override void Dispose()
        {
            if (_disposed) return;
            // TODO-RZ: this will leak if no connection is attached
            _socketContext.StopAcceptingConnections();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ManagedQuicListener));
            }
        }
    }
}
