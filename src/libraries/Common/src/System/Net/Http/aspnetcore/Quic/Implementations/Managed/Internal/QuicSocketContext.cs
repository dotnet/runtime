#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Net.Quic.Implementations.MsQuic.Internal;
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

        private TaskCompletionSource<int> _signalTcs = new TaskCompletionSource<int>();

        private Task? _backgroundWorkerTask;

        // constructor for server
        internal QuicSocketContext(IPEndPoint localEndpoint, QuicListenerOptions listenerOptions,
            ChannelWriter<ManagedQuicConnection> newConnectionsWriter)
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
            Debug.Assert(_backgroundWorkerTask == null);
            _backgroundWorkerTask = Task.Run(BackgroundWorker);
        }

        /// <summary>
        ///     Used to signal the thread that one of the connections has data to send.
        /// </summary>
        internal void Ping()
        {
            _signalTcs.TrySetResult(0);
        }

        private ManagedQuicConnection? DispatchPacket(QuicReader reader, IPEndPoint remoteEp)
        {
            if (_client != null)
            {
                _client.ReceiveData(reader, remoteEp, DateTime.Now);
                return _client;
            }

            ManagedQuicConnection? connection;
            // we need to dispatch packets to appropriate connection based on the connection Id, so we need to parse the headers
            byte first = reader.Peek();
            if (HeaderHelpers.IsLongHeader(first))
            {
                if (!LongPacketHeader.Read(reader, out var header))
                {
                    // drop packet
                    return null;
                }

                var connectionId = _connectionIds!.FindConnectionId(header.DestinationConnectionId);
                if (connectionId == null)
                {
                    connectionId = new ConnectionId(header.DestinationConnectionId.ToArray());
                    _connectionIds!.Add(connectionId!);
                }

                if (!_connections!.TryGetValue(connectionId!, out connection))
                {
                    connection = _connections![connectionId!] =
                        new ManagedQuicConnection(_listenerOptions!, this, remoteEp);
                }
            }
            else
            {
                if (!ShortPacketHeader.Read(reader, _connectionIds!, out var header))
                {
                    // drop packet
                    return null;
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

            return connection;
        }


        private async Task BackgroundWorker()
        {
            var token = _socketTaskCts.Token;

            async Task SendData(ManagedQuicConnection c, QuicWriter w, Socket s, byte[] buf)
            {
                w.Reset(buf);
                c.SendData(w, out var addr, DateTime.Now);
                int written = w.BytesWritten;
                if (written > 0)
                {
                    await s.SendToAsync(new ArraySegment<byte>(buf, 0, written), SocketFlags.None, addr)
                        .ConfigureAwait(false);
                }
            }

            byte[] recvBuffer = new byte[64 * 1024]; // max UDP packet size
            var reader = new QuicReader(recvBuffer);
            byte[] sendBuffer = new byte[64 * 1024]; // max UDP packet size
            var writer = new QuicWriter(sendBuffer);

            // TODO-RZ we need a way to notify clientside QuicConnection of newly assigned port number if none was specified beforehand
            var socket = new Socket(_localEndpoint?.AddressFamily ?? AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            if (_localEndpoint != null)
            {
                socket.Bind(_localEndpoint);
            }

            Task<SocketReceiveFromResult> socketReceiveTask =
                socket.ReceiveFromAsync(recvBuffer, SocketFlags.None, _localEndpoint);

            Task[] waitingTasks = {socketReceiveTask, _signalTcs.Task};

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.WhenAny(waitingTasks).ConfigureAwait(false);

                    if (socketReceiveTask.IsCompleted)
                    {
                        var result = await socketReceiveTask.ConfigureAwait(false);

                        // process only datagrams big enough to contain valid QUIC packets
                        if (result.ReceivedBytes >= QuicConstants.MinimumPacketSize)
                        {
                            reader.Reset(recvBuffer.AsMemory(0, result.ReceivedBytes));
                            var connection = DispatchPacket(reader, (IPEndPoint) result.RemoteEndPoint);
                            if (connection != null)
                            {
                                // also query the connection for data to be sent back
                                await SendData(connection!, writer, socket, sendBuffer).ConfigureAwait(false);
                            }
                        }

                        // start new receiving task
                        waitingTasks[0] = socketReceiveTask =
                            socket.ReceiveFromAsync(recvBuffer, SocketFlags.None, _localEndpoint);
                    }

                    if (_signalTcs.Task.IsCompleted)
                    {
                        _signalTcs = new TaskCompletionSource<int>();
                        waitingTasks[1] = _signalTcs.Task;

                        if (_client != null)
                        {
                            await SendData(_client, writer, socket, sendBuffer).ConfigureAwait(false);
                        }
                        else
                        {
                            foreach (ManagedQuicConnection connection in _connections!.Values)
                            {
                                await SendData(connection, writer, socket, sendBuffer).ConfigureAwait(false);
                            }
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        internal void Close()
        {
            throw new NotImplementedException();
        }
    }
}
