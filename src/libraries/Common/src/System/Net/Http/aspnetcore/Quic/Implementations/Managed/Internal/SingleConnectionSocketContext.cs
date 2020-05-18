using System.Diagnostics;

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

        protected override void OnSignal()
        {
            Update(_connection);
            UpdateTimeout(_connection.GetNextTimerTimestamp());
        }

        protected override void OnTimeout(long now)
        {
            long oldTimeout = _connection.GetNextTimerTimestamp();

            // timout may have changed since have set it
            if (oldTimeout <= now)
            {
                var origState = _connection.ConnectionState;
                _connection.OnTimeout(now);

                // the connection may have data to send
                Update(_connection, origState);

                long newTimeout = _connection.GetNextTimerTimestamp();
                if (newTimeout == oldTimeout)
                {
                    Debug.Assert(newTimeout != oldTimeout);
                }
                UpdateTimeout(newTimeout);
            }
            else
            {
                // set timer to the current value
                UpdateTimeout(oldTimeout);
            }
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

        protected override void DetachConnection(ManagedQuicConnection connection)
        {
            Debug.Assert(connection.IsClosed);
            Debug.Assert(connection == _connection);
            // only one connection, so we can stop the background worker and free resources
            _stop = true;
        }
    }
}
