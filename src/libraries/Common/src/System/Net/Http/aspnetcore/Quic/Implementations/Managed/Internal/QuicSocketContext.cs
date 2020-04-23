#nullable enable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
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

        private ManagedQuicConnection? DispatchPacket(QuicReader reader, IPEndPoint remoteEp)
        {
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

                var connectionId = _connectionIds!.Find(header.DestinationConnectionId);
                if (connectionId == null)
                {
                    // new connection attempt
                    if (!_acceptNewConnections)
                    {
                        return null;
                    }

                    connectionId = new ConnectionId(header.DestinationConnectionId.ToArray());
                    _connectionIds.Add(connectionId!);

                    // TODO-RZ: there is a data race with Detach() method
                    connection = new ManagedQuicConnection(_listenerOptions!, this, remoteEp);
                    _connections = _connections.Add(connectionId!, connection);
                }
                else
                {
                    connection = _connections[connectionId!];
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

            reader.Seek(0);
            bool connected = connection!.Connected;
            connection.ReceiveData(reader, remoteEp, Timestamp.Now);
            // TODO-RZ: handle failed connection attempts
            if (!connected && connection!.Connected)
            {
                // new connection established
                _newConnections!.TryWrite(connection!);
            }

            return connection;
        }

        internal void StopAcceptingConnections()
        {
            _acceptNewConnections = false;
        }

        protected override Task OnReceived(QuicReader reader, IPEndPoint sender)
        {
            var connection = DispatchPacket(reader, sender);
            return connection != null ? SendAsync(connection) : Task.CompletedTask;
        }

        protected override async Task OnSignal()
        {
            foreach (var (_, connection) in _connections)
            {
                await SendAsync(connection);
            }
        }

        protected override async Task OnTimeout()
        {
            // TODO-RZ: do timeout only on the one connection which needs it
            long now = Timestamp.Now;
            foreach (var (_, connection) in _connections)
            {
                connection.OnTimeout(now);
                await SendAsync(connection);
            }
        }

        internal override bool DetachConnection(ManagedQuicConnection connection)
        {
            _connectionIds.Remove(connection.DestinationConnectionId!);

            // TODO-RZ: Data race with socket thread
            _connections = _connections.Remove(connection.DestinationConnectionId!);
            return _connections.IsEmpty;
        }
    }

    internal sealed class SingleConnectionSocketContext : QuicSocketContext
    {
        private readonly ManagedQuicConnection _connection;

        internal SingleConnectionSocketContext(IPEndPoint listenEndpoint, ManagedQuicConnection connection)
            : base(listenEndpoint)
        {
            _connection = connection;
        }

        protected override Task OnReceived(QuicReader reader, IPEndPoint sender)
        {
            _connection.ReceiveData(reader, sender, Timestamp.Now);
            return SendAsync(_connection);
        }

        protected override async Task OnSignal()
        {
             await SendAsync(_connection);
        }

        protected override Task OnTimeout()
        {
            _connection.OnTimeout(Timestamp.Now);
            return SendAsync(_connection);
        }

        internal override bool DetachConnection(ManagedQuicConnection connection)
        {
            Debug.Assert(connection == _connection);
            // only one connection, so we can stop the background worker
            Stop();
            return true;
        }
    }

    /// <summary>
    ///     Class responsible for serving a socket for QUIC connections.
    /// </summary>
    internal abstract class QuicSocketContext : IDisposable
    {
        private static readonly Task _infiniteTimeoutTask = new TaskCompletionSource<int>().Task;

        protected readonly IPEndPoint _listenEndpoint;
        protected readonly CancellationTokenSource _socketTaskCts;

        protected TaskCompletionSource<int> _signalTcs = new TaskCompletionSource<int>();

        protected Task? _backgroundWorkerTask;

        protected QuicReader _reader;
        protected QuicWriter _writer;

        private Task _timeoutTask;
        private long _currentTimeout = long.MaxValue;

        private Task[] _waitingTasks = new Task[3];

        private Socket _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        private readonly byte[] _sendBuffer = new byte[64 * 1024];
        private readonly byte[] _recvBuffer = new byte[64 * 1024];

        protected QuicSocketContext(IPEndPoint listenEndpoint)
        {
            _listenEndpoint = listenEndpoint;

            _socketTaskCts = new CancellationTokenSource();
            _timeoutTask = _infiniteTimeoutTask;

            _reader = new QuicReader(_recvBuffer);
            _writer = new QuicWriter(_sendBuffer);
        }

        public IPEndPoint LocalEndPoint => (IPEndPoint) _socket.LocalEndPoint;

        internal void Start()
        {
            Debug.Assert(_backgroundWorkerTask == null);
            _socket.Bind(_listenEndpoint);
            _backgroundWorkerTask = Task.Run(BackgroundWorker);
        }

        internal void Stop()
        {
            _socketTaskCts.Cancel();
            _backgroundWorkerTask!.GetAwaiter().GetResult();
        }

        /// <summary>
        ///     Used to signal the thread that one of the connections has data to send.
        /// </summary>
        internal void Ping()
        {
            _signalTcs.TrySetResult(0);
        }

        protected Task SendAsync(ManagedQuicConnection sender)
        {
            _writer.Reset(_sendBuffer);
            sender.SendData(_writer, out var receiver, Timestamp.Now);
            SetTimeout(sender.GetNextTimerTimestamp());
            return _socket.SendToAsync(new ArraySegment<byte>(_sendBuffer, 0, _writer.BytesWritten), SocketFlags.None,
                receiver);
        }

        protected void SetTimeout(long timestamp)
        {
            if (timestamp < _currentTimeout)
            {
                ClearTimeout();
                _timeoutTask = Task.Delay((int) Timestamp.GetMilliseconds(Math.Max(0, Timestamp.Now - timestamp)));
                _waitingTasks[2] = _timeoutTask;
            }
        }

        protected void ClearTimeout()
        {
            // TODO-RZ: gracefully stop the current timeout task
            _timeoutTask = _infiniteTimeoutTask;
        }

        protected abstract Task OnReceived(QuicReader reader, IPEndPoint sender);

        protected abstract Task OnSignal();

        protected abstract Task OnTimeout();

        private async Task BackgroundWorker()
        {
            var token = _socketTaskCts.Token;

            Task<SocketReceiveFromResult> socketReceiveTask =
                _socket.ReceiveFromAsync(_recvBuffer, SocketFlags.None, _listenEndpoint);

            _waitingTasks[0] = socketReceiveTask;
            _waitingTasks[1] = _signalTcs.Task;
            _waitingTasks[2] = _timeoutTask;

            // TODO-RZ: allow timers for multiple connections on server
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.WhenAny(_waitingTasks).ConfigureAwait(false);

                    if (socketReceiveTask.IsCompleted)
                    {
                        var result = await socketReceiveTask.ConfigureAwait(false);

                        // process only datagrams big enough to contain valid QUIC packets
                        if (result.ReceivedBytes >= QuicConstants.MinimumPacketSize)
                        {
                            _reader.Reset(_recvBuffer.AsMemory(0, result.ReceivedBytes));
                            await OnReceived(_reader, (IPEndPoint)result.RemoteEndPoint).ConfigureAwait(false);
                        }

                        // start new receiving task
                        _waitingTasks[0] = socketReceiveTask =
                            _socket.ReceiveFromAsync(_recvBuffer, SocketFlags.None, _listenEndpoint);
                    }

                    if (_signalTcs.Task.IsCompleted)
                    {
                        _signalTcs = new TaskCompletionSource<int>();
                        _waitingTasks[1] = _signalTcs.Task;
                        await OnSignal().ConfigureAwait(false);
                    }

                    if (_timeoutTask.IsCompleted)
                    {
                        _waitingTasks[2] = _infiniteTimeoutTask;
                        await OnTimeout().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        ///     Detaches the given connection from this context, the connection will no longer be updated from the
        ///     thread running at this socket.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>
        ///     Returns true if there are no more connections attached to this context, and the
        ///     context can be disposed.
        /// </returns>
        internal abstract bool DetachConnection(ManagedQuicConnection connection);

        public void Dispose()
        {
            Stop();
            // _socketTaskCts.Dispose();
            _backgroundWorkerTask?.Dispose();
            // _timeoutTask.Dispose();
            _socket.Dispose();
        }
    }
}
