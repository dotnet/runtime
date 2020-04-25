using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal sealed class QuicServerSocketContext : QuicSocketContext
    {
        private readonly ChannelWriter<ManagedQuicConnection> _newConnections;
        private readonly QuicListenerOptions _listenerOptions;
        private ImmutableDictionary<ConnectionId, ManagedQuicConnection> _connections;
        private readonly ConnectionIdCollection _connectionIds;

        private bool _acceptNewConnections;

        internal QuicServerSocketContext(IPEndPoint listenEndpoint, QuicListenerOptions listenerOptions,
            ChannelWriter<ManagedQuicConnection> newConnectionsWriter)
            : base(listenEndpoint)
        {
            _newConnections = newConnectionsWriter;
            _listenerOptions = listenerOptions;

            _connections = ImmutableDictionary<ConnectionId, ManagedQuicConnection>.Empty;
            _connectionIds = new ConnectionIdCollection();
            _acceptNewConnections = true;
        }

        protected override ManagedQuicConnection? FindConnection(QuicReader reader, IPEndPoint remoteEp)
        {
            ManagedQuicConnection? connection;

            // TODO-RZ: dispatch based on remoteEp

            // we need to dispatch packets to appropriate connection based on the connection Id, so we need to parse the headers
            byte first = reader.Peek();
            if (HeaderHelpers.IsLongHeader(first))
            {
                if (!LongPacketHeader.Read(reader, out var header))
                {
                    // drop packet
                    return null;
                }

                var connectionId = _connectionIds!.Find(header.DestinationConnectionId);
                if (connectionId == null)
                {
                    // new connection attempt
                    if (!_acceptNewConnections ||
                        header.PacketType != PacketType.Initial)
                    {
                        return null;
                    }

                    // TODO-RZ: This normally shouldn't race with Detach method (that can only be called from
                    // other thread for connected connections), but there is very improbable scenario when the connection
                    // was detached and we received delayed Initial/Handshake packet.
                    connectionId = new ConnectionId(header.DestinationConnectionId.ToArray());
                    _connectionIds.Add(connectionId!);

                    connection = new ManagedQuicConnection(_listenerOptions!, this, remoteEp);
                    ImmutableInterlocked.TryAdd(ref _connections, connectionId!, connection);
                }
                else if (!_connections.TryGetValue(connectionId!, out connection))
                {
                    // the connection has been just detached
                    return null;
                }
            }
            else
            {
                if (!ShortPacketHeader.Read(reader, _connectionIds!, out var header) ||
                    !_connections.TryGetValue(header.DestinationConnectionId, out connection))
                {
                    // either unknown connection or the connection is connection not associated with this context
                    // anymore
                    return null;
                }
            }

            return connection;
        }

        internal void StopAcceptingConnections()
        {
            _acceptNewConnections = false;
        }

        protected override async Task OnSignal()
        {
            // TODO-RZ: make connections signal which connection wishes to do something
            long nextTimeout = long.MaxValue;

            foreach (var (_, connection) in _connections)
            {
                await UpdateAsync(connection).ConfigureAwait(false);
                nextTimeout = Math.Min(nextTimeout, connection.GetNextTimerTimestamp());
            }

            UpdateTimeout(nextTimeout);
        }

        protected override async Task OnTimeout()
        {
            long now = Timestamp.Now;

            long nextTimeout = long.MaxValue;

            foreach (var (_, connection) in _connections)
            {
                if (connection.GetNextTimerTimestamp() <= now)
                {
                    await UpdateAsync(connection).ConfigureAwait(false);
                }

                nextTimeout = Math.Min(nextTimeout, connection.GetNextTimerTimestamp());
            }

            UpdateTimeout(nextTimeout);
        }

        protected override void OnConnectionStateChanged(ManagedQuicConnection connection, QuicConnectionState newState)
        {
            switch (newState)
            {
                case QuicConnectionState.None:
                    break;
                case QuicConnectionState.Connected:
                    _newConnections.TryWrite(connection);
                    break;
                case QuicConnectionState.Closing:
                    break;
                case QuicConnectionState.Draining:
                    // RFC: Servers that retain an open socket for accepting new connections SHOULD NOT exit the closing
                    // or draining period early.

                    // this means that we need to keep the connection in the map until the timer runs out, closing event
                    // will be already signaled to user.
                    if (!_acceptNewConnections)
                    {
                        DetachConnection(connection);
                    }

                    break;
                case QuicConnectionState.Closed:
                    // draining timer elapsed, discard the state
                    DetachConnection(connection);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }
        }

        protected override bool ShouldContinue => _acceptNewConnections || !_connections.IsEmpty;

        protected override void DetachConnection(ManagedQuicConnection connection)
        {
            Debug.Assert(connection.IsClosed);
            _connectionIds.Remove(connection.SourceConnectionId!);
            ImmutableInterlocked.TryRemove(ref _connections, connection.SourceConnectionId!, out _);
            Console.WriteLine("Server closing");
        }
    }
}
