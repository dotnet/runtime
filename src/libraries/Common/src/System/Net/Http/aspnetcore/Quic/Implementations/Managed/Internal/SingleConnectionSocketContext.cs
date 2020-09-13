using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal sealed class SingleConnectionSocketContext : QuicSocketContext
    {
        private readonly IPEndPoint _remoteEndPoint;
        private readonly ManagedQuicConnection _connection;

        internal SingleConnectionSocketContext(IPEndPoint? localEndpoint, IPEndPoint remoteEndPoint, ManagedQuicConnection connection)
            : base(localEndpoint)
        {
            _remoteEndPoint = remoteEndPoint;
            _connection = connection;
            Socket.Connect(remoteEndPoint);
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

        protected override int ReceiveFrom(byte[] buffer, ref EndPoint sender)
        {
            // use method without explicit address because we use connected socket
            return Socket.Receive(buffer);
        }

        protected override async Task<SocketReceiveFromResult> ReceiveFromAsync(byte[] buffer, EndPoint sender,
            CancellationToken token)
        {
            // use method without explicit address because we use connected socket
            int i = await Socket.ReceiveAsync(buffer, SocketFlags.None, token);
            return new SocketReceiveFromResult {ReceivedBytes = i, RemoteEndPoint = _remoteEndPoint};
        }

        protected override void DetachConnection(ManagedQuicConnection connection)
        {
            Debug.Assert(connection.IsClosed);
            Debug.Assert(connection == _connection);
            // only one connection, so we can stop the background worker and free resources
            Stop();
        }
    }
}
