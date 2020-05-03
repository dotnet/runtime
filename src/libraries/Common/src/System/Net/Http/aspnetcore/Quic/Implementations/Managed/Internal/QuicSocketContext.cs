#nullable enable

using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Class responsible for serving a socket for QUIC connections.
    /// </summary>
    internal abstract class QuicSocketContext
    {
        private static readonly Task _infiniteTimeoutTask = new TaskCompletionSource<int>().Task;

        private readonly IPEndPoint _listenEndpoint;
        private readonly CancellationTokenSource _socketTaskCts;

        private TaskCompletionSource<int> _signalTcs =
            new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        private Task? _backgroundWorkerTask;

        private readonly QuicReader _reader;
        private readonly QuicWriter _writer;

        private readonly SendContext _sendContext;
        private readonly RecvContext _recvContext;

        private Task _timeoutTask;

        private long _currentTimeout = long.MaxValue;
        private CancellationTokenSource _timeoutCts = new CancellationTokenSource();

        private readonly Task[] _waitingTasks = new Task[4];

        private readonly Socket _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        private readonly byte[] _sendBuffer = new byte[64 * 1024];
        private readonly byte[] _recvBuffer = new byte[64 * 1024];

        protected QuicSocketContext(IPEndPoint listenEndpoint)
        {
            _listenEndpoint = listenEndpoint;

            _socketTaskCts = new CancellationTokenSource();
            _timeoutTask = _infiniteTimeoutTask;

            _reader = new QuicReader(_recvBuffer);
            _writer = new QuicWriter(_sendBuffer);

            var sentPacketPool = new ObjectPool<SentPacket>(128);
            _sendContext = new SendContext(sentPacketPool);
            _recvContext = new RecvContext(sentPacketPool);
        }

        public IPEndPoint LocalEndPoint => (IPEndPoint)_socket.LocalEndPoint;

        internal void Start()
        {
            Debug.Assert(_backgroundWorkerTask == null);
            _socket.Bind(_listenEndpoint);
            _backgroundWorkerTask = Task.Run(BackgroundWorker);
        }

        /// <summary>
        ///     Used to signal the thread that one of the connections has data to send.
        /// </summary>
        internal void Ping()
        {
            _signalTcs.TrySetResult(0);
        }

        private async Task UpdateAsync(ManagedQuicConnection connection, QuicConnectionState previousState)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            // TODO-RZ: I would like to have unbound loop there, but until flow control is implemented, it might loop
            // indefinitely
            // while (true)
            for (int i = 0; i < 2; i++)
            {
                _writer.Reset(_sendBuffer);
                _sendContext.Timestamp = Timestamp.Now;
                _sendContext.SentPacket.Reset();
                connection.SendData(_writer, out var receiver, _sendContext);

                var newState = connection.ConnectionState;
                if (newState != previousState)
                {
                    OnConnectionStateChanged(connection, newState);
                }

                previousState = newState;

                if (_writer.BytesWritten == 0)
                {
                    break;
                }

                if (NetEventSource.IsEnabled) NetEventSource.DatagramSent(connection, _writer.Buffer.Span.Slice(0, _writer.BytesWritten));

                await _socket.SendToAsync(new ArraySegment<byte>(_sendBuffer, 0, _writer.BytesWritten),
                    SocketFlags.None,
                    receiver).ConfigureAwait(false);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        protected Task UpdateAsync(ManagedQuicConnection connection)
        {
            return UpdateAsync(connection, connection.ConnectionState);
        }

        protected void UpdateTimeout(long timestamp)
        {
            if (timestamp < _currentTimeout)
            {
                int milliseconds = (int)Timestamp.GetMilliseconds(Timestamp.Now - timestamp);

                // don't create tasks needlessly
                if (milliseconds > 0)
                {
                    // cancel previous delay task
                    _timeoutCts.Cancel();
                    _timeoutCts = new CancellationTokenSource();

                    _timeoutTask = Task.Delay(milliseconds, _timeoutCts.Token);
                }
                else
                {
                    _timeoutTask = Task.CompletedTask;
                }

                _waitingTasks[2] = _timeoutTask;
                _currentTimeout = timestamp;
            }
        }

        protected abstract ManagedQuicConnection? FindConnection(QuicReader reader, IPEndPoint sender);

        private async Task DoReceive(QuicReader reader, IPEndPoint sender)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            var connection = FindConnection(reader, sender);
            if (connection != null)
            {
                if (NetEventSource.IsEnabled) NetEventSource.DatagramReceived(connection, reader.Buffer.Span);

                var previousState = connection.ConnectionState;
                _recvContext.Timestamp = Timestamp.Now;
                connection.ReceiveData(reader, sender, _recvContext);
                await UpdateAsync(connection, previousState).ConfigureAwait(false);
                UpdateTimeout(connection.GetNextTimerTimestamp());
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        private async Task DoSignal()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            _signalTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waitingTasks[1] = _signalTcs.Task;
            await OnSignal().ConfigureAwait(false);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        private async Task DoTimeout()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            // clear previous timeout
            _currentTimeout = long.MaxValue;
            _waitingTasks[2] = _timeoutTask = _infiniteTimeoutTask;

            await OnTimeout().ConfigureAwait(false);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        protected abstract Task OnSignal();

        protected abstract Task OnTimeout();

        protected abstract void
            OnConnectionStateChanged(ManagedQuicConnection connection, QuicConnectionState newState);

        private async Task BackgroundWorker()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            var token = _socketTaskCts.Token;

            Task<SocketReceiveFromResult> socketReceiveTask =
                _socket.ReceiveFromAsync(_recvBuffer, SocketFlags.None, _listenEndpoint);

            var shutdownTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            _waitingTasks[0] = socketReceiveTask;
            _waitingTasks[1] = _signalTcs.Task;
            _waitingTasks[2] = _timeoutTask;
            _waitingTasks[3] = shutdownTcs.Task;

            await using var registration = token.Register(() => shutdownTcs.TrySetResult(0));

            // TODO-RZ: allow timers for multiple connections on server
            try
            {
                while (ShouldContinue && !token.IsCancellationRequested)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Enter(this, "Wait");
                    await Task.WhenAny(_waitingTasks).ConfigureAwait(false);
                    if (NetEventSource.IsEnabled) NetEventSource.Exit(this, "Wait");

                    if (_timeoutTask.IsCompleted)
                    {
                        await DoTimeout().ConfigureAwait(false);
                    }

                    if (socketReceiveTask.IsCompleted)
                    {
                        // TODO-RZ: Avoid connection forcibly closed exception
                        if (socketReceiveTask.IsCompletedSuccessfully)
                        {
                            var result = await socketReceiveTask.ConfigureAwait(false);

                            // process only datagrams big enough to contain valid QUIC packets
                            if (result.ReceivedBytes >= QuicConstants.MinimumPacketSize)
                            {
                                _reader.Reset(_recvBuffer.AsMemory(0, result.ReceivedBytes));
                                await DoReceive(_reader, (IPEndPoint)result.RemoteEndPoint).ConfigureAwait(false);
                            }
                        }

                        // start new receiving task
                        _waitingTasks[0] = socketReceiveTask =
                            _socket.ReceiveFromAsync(_recvBuffer, SocketFlags.None, _listenEndpoint);
                    }

                    if (_signalTcs.Task.IsCompleted)
                    {
                        await DoSignal().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            // cleanup everything

            _socket.Close();
            _socket.Dispose();

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        protected abstract bool ShouldContinue { get; }

        /// <summary>
        ///     Detaches the given connection from this context, the connection will no longer be updated from the
        ///     thread running at this socket.
        /// </summary>
        /// <param name="connection"></param>
        protected abstract void DetachConnection(ManagedQuicConnection connection);

        internal class ContextBase
        {
            public ContextBase(ObjectPool<SentPacket> sentPacketPool) => SentPacketPool = sentPacketPool;

            /// <summary>
            ///     Timestamp when the next tick of internal processing was requested.
            /// </summary>
            internal long Timestamp { get; set; }

            protected ObjectPool<SentPacket> SentPacketPool { get; }

            internal void ReturnPacket(SentPacket packet)
            {
                SentPacketPool.Return(packet);
            }
        }

        internal sealed class RecvContext : ContextBase
        {
            /// <summary>
            ///     Flag whether TLS handshake should be incremented at the end of packet processing, perhaps due to
            ///     having received crypto data.
            /// </summary>
            internal bool HandshakeWanted { get; set; }

            public RecvContext(ObjectPool<SentPacket> sentPacketPool) : base(sentPacketPool)
            {
            }
        }

        internal sealed class SendContext : ContextBase
        {
            /// <summary>
            ///     Data about next packet that is to be sent.
            /// </summary>
            internal SentPacket SentPacket { get; private set; } = new SentPacket();

            internal void StartNextPacket()
            {
                SentPacket = SentPacketPool.Rent();
            }

            public SendContext(ObjectPool<SentPacket> sentPacketPool) : base(sentPacketPool)
            {
            }
        }
    }
}
