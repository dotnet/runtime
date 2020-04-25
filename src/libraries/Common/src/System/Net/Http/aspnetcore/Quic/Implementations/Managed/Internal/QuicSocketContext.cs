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

        private TaskCompletionSource<int> _signalTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        private Task? _backgroundWorkerTask;

        private readonly QuicReader _reader;
        private readonly QuicWriter _writer;

        private Task _timeoutTask;
        private long _currentTimeout = long.MaxValue;

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

            while (true)
            {
                _writer.Reset(_sendBuffer);
                connection.SendData(_writer, out var receiver, Timestamp.Now);

                if (_writer.BytesWritten == 0)
                {
                    break;
                }

                await _socket.SendToAsync(new ArraySegment<byte>(_sendBuffer, 0, _writer.BytesWritten),
                    SocketFlags.None,
                    receiver).ConfigureAwait(false);
            }

            var newState = connection.GetConnectionState();
            if (newState != previousState)
            {
                OnConnectionStateChanged(connection, newState);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        protected Task UpdateAsync(ManagedQuicConnection connection)
        {
            return UpdateAsync(connection, connection.GetConnectionState());
        }

        protected void UpdateTimeout(long timestamp)
        {
            if (timestamp < _currentTimeout)
            {
                int milliseconds = (int)Timestamp.GetMilliseconds(Math.Max(0, Timestamp.Now - timestamp));

                // TODO-RZ: don't create tasks needlessly
                // if (milliseconds > 0)
                {
                    _timeoutTask = Task.Delay(milliseconds);
                    _waitingTasks[2] = _timeoutTask;
                }

                _currentTimeout = timestamp;
            }
        }

        protected void ClearTimeout()
        {
            // TODO-RZ: gracefully stop the current timeout task
            _currentTimeout = long.MaxValue;
            _timeoutTask = _infiniteTimeoutTask;
            _waitingTasks[2] = _timeoutTask;
        }

        protected abstract ManagedQuicConnection? FindConnection(QuicReader reader, IPEndPoint sender);

        private async Task DoReceive(QuicReader reader, IPEndPoint sender)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            var connection = FindConnection(reader, sender);
            if (connection != null)
            {
                var previousState = connection.GetConnectionState();
                reader.Seek(0);
                connection.ReceiveData(reader, sender, Timestamp.Now);
                await UpdateAsync(connection, previousState).ConfigureAwait(false);
                UpdateTimeout(connection.GetNextTimerTimestamp());
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        private async Task DoSignal()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            await OnSignal();

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        private async Task DoTimeout()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            await OnTimeout();

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        protected abstract Task OnSignal();

        protected abstract Task OnTimeout();

        protected abstract void OnConnectionStateChanged(ManagedQuicConnection connection, QuicConnectionState newState);

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
                do
                {
                    bool immediateTimeout = Timestamp.Now >= _currentTimeout;
                    if (immediateTimeout)
                    {
                        ClearTimeout();
                        await DoTimeout().ConfigureAwait(false);
                    }
                    else
                    {
                        if (NetEventSource.IsEnabled) NetEventSource.Enter(this, "Wait");
                        await Task.WhenAny(_waitingTasks).ConfigureAwait(false);
                        if (NetEventSource.IsEnabled) NetEventSource.Exit(this, "Wait");
                    }


                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (socketReceiveTask.IsCompleted)
                    {
                        var result = await socketReceiveTask.ConfigureAwait(false);

                        // process only datagrams big enough to contain valid QUIC packets
                        if (result.ReceivedBytes >= QuicConstants.MinimumPacketSize)
                        {
                            _reader.Reset(_recvBuffer.AsMemory(0, result.ReceivedBytes));
                            await DoReceive(_reader, (IPEndPoint)result.RemoteEndPoint).ConfigureAwait(false);
                        }

                        // start new receiving task
                        _waitingTasks[0] = socketReceiveTask =
                            _socket.ReceiveFromAsync(_recvBuffer, SocketFlags.None, _listenEndpoint);
                    }

                    if (_signalTcs.Task.IsCompleted)
                    {
                        _signalTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                        _waitingTasks[1] = _signalTcs.Task;
                        await DoSignal().ConfigureAwait(false);
                    }
                } while (ShouldContinue);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            // cleanup everything
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
    }
}
