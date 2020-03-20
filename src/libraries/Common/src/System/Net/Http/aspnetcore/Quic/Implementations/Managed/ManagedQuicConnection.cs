using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed
{
    internal class ManagedQuicConnection : QuicConnectionProvider
    {
        public ManagedQuicConnection(QuicClientConnectionOptions options)
        {
        }

        internal override bool Connected { get; }
        internal override IPEndPoint LocalEndPoint { get; }
        internal override IPEndPoint RemoteEndPoint { get; }
        internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override QuicStreamProvider OpenUnidirectionalStream() => throw new NotImplementedException();

        internal override QuicStreamProvider OpenBidirectionalStream() => throw new NotImplementedException();

        internal override long GetRemoteAvailableUnidirectionalStreamCount() => throw new NotImplementedException();

        internal override long GetRemoteAvailableBidirectionalStreamCount() => throw new NotImplementedException();

        internal override ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override SslApplicationProtocol NegotiatedApplicationProtocol { get; }
        internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public override void Dispose() => throw new NotImplementedException();
    }
}
