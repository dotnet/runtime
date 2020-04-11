#nullable enable

using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Class responsible for serving a socket for QUIC connections.
    /// </summary>
    internal class QuicSocketContext
    {
        // server only
        private readonly ChannelWriter<ManagedQuicConnection>? _newConnections;
        private readonly QuicListenerOptions? _listenerOptions;
        private readonly Dictionary<ConnectionId, ManagedQuicConnection>? _connections;
        private readonly ConnectionIdCollection? _connectionIds;

        // client only
        private readonly ManagedQuicConnection? _client;

        private readonly IPEndPoint _localEndpoint;
        private readonly CancellationTokenSource _socketTaskCts;

        // constructor for server
        internal QuicSocketContext(IPEndPoint localEndpoint, QuicListenerOptions listenerOptions, ChannelWriter<ManagedQuicConnection> newConnectionsWriter)
            : this(localEndpoint)
        {
            _newConnections = newConnectionsWriter;
            _listenerOptions = listenerOptions;
        }

        // constructor for client
        internal QuicSocketContext(IPEndPoint localEndpoint, ManagedQuicConnection clientConnection)
            : this(localEndpoint)
        {
            _client = clientConnection;
        }

        private QuicSocketContext(IPEndPoint localEndpoint)
        {
            _localEndpoint = localEndpoint;

            _socketTaskCts = new CancellationTokenSource();

            _connections = new Dictionary<ConnectionId, ManagedQuicConnection>();
            _connectionIds = new ConnectionIdCollection();
        }

        internal void Start()
        {
            // TODO-RZ: check if already running
            _ = BackgroundWorker(_socketTaskCts.Token);
        }

        private void DispatchDatagram(QuicReader reader, IPEndPoint remoteEp)
        {
            if (_client != null)
            {
                _client!.ReceiveData(reader, remoteEp, DateTime.Now);
                return;
            }

            ManagedQuicConnection? connection;
            // we need to dispatch packets to appropriate connection based on the connection Id, so we need to parse the headers
            byte first = reader.Peek();
            if (HeaderHelpers.IsLongHeader(first))
            {
                if (!LongPacketHeader.Read(reader, out var header))
                {
                    // drop packet
                    return;
                }

                var connectionId = _connectionIds!.FindConnectionId(header.DestinationConnectionId);
                if (connectionId == null)
                {
                    connectionId = new ConnectionId(header.DestinationConnectionId.ToArray());
                    _connectionIds!.Add(connectionId!);
                }

                if (!_connections!.TryGetValue(connectionId!, out connection))
                {
                    connection = _connections![connectionId!] = new ManagedQuicConnection(_listenerOptions!, this, remoteEp);
                }
            }
            else
            {
                if (!ShortPacketHeader.Read(reader, _connectionIds!, out var header))
                {
                    // drop packet
                    return;
                }

                connection = _connections![header.DestinationConnectionId];
            }

            reader.Seek(0);
            bool connected = connection!.Connected;
            connection.ReceiveData(reader, remoteEp, DateTime.Now);
            // TODO-RZ: handle failed connection attempts
            if (!connected && connection!.Connected)
            {
                // new connection established
                _newConnections!.TryWrite(connection!);
            }
        }


        private async Task BackgroundWorker(CancellationToken token)
        {
            async Task SendData(ManagedQuicConnection c, QuicWriter w, Socket s, byte[] buf)
            {
                w.Reset(buf);
                c.SendData(w, out var addr, DateTime.Now);
                int written = w.BytesWritten + w.Buffer.Offset;
                if (written > 0)
                {
                    await s.SendToAsync(new ArraySegment<byte>(buf, 0, written), SocketFlags.None, addr);
                }
            }

            await Task.Yield();

            byte[] buffer = new byte[64 * 1024]; // max UDP packet size
            var reader = new QuicReader(buffer);
            var writer = new QuicWriter(buffer);

            var socket = new Socket(_localEndpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            if (_localEndpoint != null)
            {
                socket.Bind(_localEndpoint);
            }

            while (!token.IsCancellationRequested)
            {
                EndPoint endpoint = _localEndpoint;

                var received = socket.Available > 0 ? socket.ReceiveFrom(buffer, SocketFlags.None, ref endpoint) : 0;
                IPEndPoint remoteEp = (IPEndPoint)endpoint;

                if (received >= QuicConstants.MinimumPacketSize)
                {
                    reader.Reset(buffer, 0, received);
                    DispatchDatagram(reader, (IPEndPoint)endpoint);
                }

                if (_client != null)
                {
                    await SendData(_client, writer, socket, buffer);
                }
                else
                {
                    foreach (ManagedQuicConnection connection in _connections!.Values)
                    {
                        await SendData(connection, writer, socket, buffer);
                    }
                }
            }
        }

        internal void Close()
        {
            throw new NotImplementedException();
        }
    }
}
