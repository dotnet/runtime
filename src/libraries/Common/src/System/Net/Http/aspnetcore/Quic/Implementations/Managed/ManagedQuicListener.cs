using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed
{
    internal class ManagedQuicListener : QuicListenerProvider
    {
        private readonly QuicListenerOptions _options;

        public ManagedQuicListener(QuicListenerOptions options)
        {
            _options = options;
        }

        internal override IPEndPoint ListenEndPoint => _options.ListenEndPoint!;
        internal override ValueTask<QuicConnectionProvider> AcceptConnectionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override void Start() => throw new NotImplementedException();

        internal override void Close() => throw new NotImplementedException();

        public override void Dispose() => throw new NotImplementedException();
    }
}
