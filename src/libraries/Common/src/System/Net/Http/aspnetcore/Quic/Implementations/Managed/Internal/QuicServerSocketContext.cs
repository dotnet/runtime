using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal sealed class QuicServerSocketContext : QuicSocketContext
    {
        private readonly ChannelWriter<ManagedQuicConnection> _newConnections;
        private readonly QuicListenerOptions _listenerOptions;

        private ImmutableDictionary<IPEndPoint, ManagedQuicConnection> _connectionsByEndpoint;

        private bool _acceptNewConnections;

        internal QuicServerSocketContext(IPEndPoint localEndPoint, QuicListenerOptions listenerOptions,
            ChannelWriter<ManagedQuicConnection> newConnectionsWriter)
            : base(localEndPoint, null, true)
        {
            _newConnections = newConnectionsWriter;
            _listenerOptions = listenerOptions;

            _connectionsByEndpoint = ImmutableDictionary<IPEndPoint, ManagedQuicConnection>.Empty;

            _acceptNewConnections = true;
        }

        protected override ManagedQuicConnection? FindConnection(QuicReader reader, IPEndPoint remoteEp)
        {
            // TODO-RZ: dispatch needs more work, currently only one outbound connection per socket works
            if (!_connectionsByEndpoint.TryGetValue(remoteEp, out ManagedQuicConnection? connection))
            {
                if (!_acceptNewConnections || HeaderHelpers.GetPacketType(reader.Peek()) != PacketType.Initial)
                {
                    // drop packet
                    return null;
                }

                // TODO-RZ: handle connection failures when the initial packet is discarded (e.g. because connection id is
                // too long). This likely will need moving header parsing from Connection to socket context.

                connection = new ManagedQuicConnection(_listenerOptions, this, remoteEp);
                ImmutableInterlocked.TryAdd(ref _connectionsByEndpoint, remoteEp, connection);
            }

            return connection;
        }

        /// <summary>
        ///     Signals that the context should no longer accept new connections.
        /// </summary>
        internal void StopAcceptingConnections()
        {
            _acceptNewConnections = false;
            _newConnections.TryComplete();
            if (_connectionsByEndpoint.IsEmpty)
            {
                // awake the thread so that it exists
                Ping();
                Stop();
            }
        }

        protected override void OnSignal()
        {
            // TODO-RZ: make connections signal which connection wishes to do something
            long nextTimeout = long.MaxValue;

            foreach (var (_, connection) in _connectionsByEndpoint)
            {
                Update(connection);
                nextTimeout = Math.Min(nextTimeout, connection.GetNextTimerTimestamp());
            }

            UpdateTimeout(nextTimeout);
        }

        protected override void OnTimeout(long now)
        {
            long nextTimeout = long.MaxValue;

            foreach (var (_, connection) in _connectionsByEndpoint)
            {
                long oldTimeout = connection.GetNextTimerTimestamp();
                if (now < oldTimeout)
                {
                    // do not fire yet
                    nextTimeout = Math.Min(nextTimeout, oldTimeout);
                    continue;
                }

                var origState = connection.ConnectionState;
                connection.OnTimeout(now);

                // the connection may have data to send
                Update(connection, origState);

                long newTimeout = connection.GetNextTimerTimestamp();
                Debug.Assert(newTimeout != oldTimeout);
                nextTimeout = Math.Min(nextTimeout, newTimeout);
            }

            UpdateTimeout(nextTimeout);
        }

        private void OnConnectionHandshakeCompleted(ManagedQuicConnection connection)
        {
            // Connection established -> pass it to the listener
            _newConnections.TryWrite(connection);
        }

        protected override bool OnConnectionStateChanged(ManagedQuicConnection connection, QuicConnectionState newState)
        {
            switch (newState)
            {
                case QuicConnectionState.None:
                    return false;
                case QuicConnectionState.Connected:
                    OnConnectionHandshakeCompleted(connection);
                    return true;
                case QuicConnectionState.Closing:
                    return false;
                case QuicConnectionState.Draining:
                    // RFC: Servers that retain an open socket for accepting new connections SHOULD NOT exit the closing
                    // or draining period early.

                    // this means that we need to keep the connection in the map until the timer runs out, closing event
                    // will be already signaled to user.
                    if (!_acceptNewConnections)
                    {
                        DetachConnection(connection);
                    }

                    return false;
                case QuicConnectionState.Closed:
                    // draining timer elapsed, discard the state
                    DetachConnection(connection);
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }
        }

        protected override void OnException(Exception e)
        {
            foreach (var connection in _connectionsByEndpoint.Values)
            {
                connection.OnSocketContextException(e);
            }
        }

        protected internal override void DetachConnection(ManagedQuicConnection connection)
        {
            bool removed = ImmutableInterlocked.TryRemove(ref _connectionsByEndpoint, connection.RemoteEndPoint, out _);
            if (_connectionsByEndpoint.IsEmpty && !_acceptNewConnections)
            {
                Ping();
                Stop();
            }
            Debug.Assert(removed);
        }
    }
}
