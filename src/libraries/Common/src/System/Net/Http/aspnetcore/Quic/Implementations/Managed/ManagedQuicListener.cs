using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed
{
    internal class ManagedQuicListener : QuicListenerProvider
    {
        private readonly IPEndPoint _listenEndPoint;

        private readonly ChannelReader<ManagedQuicConnection> _acceptQueue;

        private readonly QuicListenerOptions _options;
        private readonly QuicSocketContext _socketContext;

        public ManagedQuicListener(QuicListenerOptions options)
        {
            _options = options;
            _listenEndPoint = options.ListenEndPoint!;

            var channel = Channel.CreateBounded<ManagedQuicConnection>(new BoundedChannelOptions(options.ListenBacklog)
            {
                SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.DropWrite
            });

            _acceptQueue = channel.Reader;
            _socketContext = new QuicSocketContext(_listenEndPoint, options, channel.Writer);
        }

        internal override IPEndPoint ListenEndPoint => new IPEndPoint(_listenEndPoint.Address, _listenEndPoint.Port);


        internal override async ValueTask<QuicConnectionProvider> AcceptConnectionAsync(
            CancellationToken cancellationToken = default)
        {
            // TODO-RZ: make this non-async when the cast is no longer needed
            return await _acceptQueue.ReadAsync(cancellationToken);
        }

        internal override void Start()
        {
            _socketContext.Start();
        }

        internal override void Close()
        {
            _socketContext.Close();
        }

        public override void Dispose() => throw new NotImplementedException();
    }
}
