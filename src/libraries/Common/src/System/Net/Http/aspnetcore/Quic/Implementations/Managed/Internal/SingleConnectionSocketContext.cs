using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal sealed class SingleConnectionSocketContext : QuicSocketContext
    {
        private readonly ManagedQuicConnection _connection;

        internal SingleConnectionSocketContext(IPEndPoint listenEndpoint, ManagedQuicConnection connection)
            : base(listenEndpoint)
        {
            _connection = connection;
        }

        protected override ManagedQuicConnection? FindConnection(QuicReader reader, IPEndPoint sender)
        {
            return _connection;
        }

        protected override ValueTask OnSignal()
        {
            return UpdateConnectionAndTimout();
        }

        protected override ValueTask OnTimeout()
        {
            return UpdateConnectionAndTimout();
        }

        protected override void OnConnectionStateChanged(ManagedQuicConnection connection, QuicConnectionState newState)
        {
            switch (newState)
            {
                case QuicConnectionState.None:
                    break;
                case QuicConnectionState.Connected:
                    break;
                case QuicConnectionState.Closing:
                    break;
                case QuicConnectionState.Draining:
                case QuicConnectionState.Closed:
                    // we can stop immediately and close the socket.
                    DetachConnection(connection);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }
        }

        private bool _stop;

        protected override bool ShouldContinue => !_stop;

        private async ValueTask UpdateConnectionAndTimout()
        {
            await UpdateAsync(_connection).ConfigureAwait(false);
            UpdateTimeout(_connection.GetNextTimerTimestamp());
        }

        protected override void DetachConnection(ManagedQuicConnection connection)
        {
            Debug.Assert(connection.IsClosed);
            Debug.Assert(connection == _connection);
            // only one connection, so we can stop the background worker and free resources
            _stop = true;
        }
    }
}
