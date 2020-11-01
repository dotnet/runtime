// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed.Internal.Sockets
{
    internal class ConnectionContext : IQuicSocketContext
    {
        private ArrayPool<byte> ArrayPool => _parent.ArrayPool;
        internal ManagedQuicConnection Connection { get; }

        private readonly QuicSocketContext.RecvContext _recvContext;
        private readonly QuicSocketContext.SendContext _sendContext;

        private QuicWriter _writer = new QuicWriter(Memory<byte>.Empty);
        private QuicReader _reader = new QuicReader(Memory<byte>.Empty);

        // TODO-RZ: maybe bounded channel with drop behavior would be better?
        private readonly Channel<DatagramInfo> _recvQueue = Channel.CreateUnbounded<DatagramInfo>(
            new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false
            });

        internal ChannelWriter<DatagramInfo> IncomingDatagramWriter => _recvQueue.Writer;

        private TaskCompletionSource _waitCompletionSource = new TaskCompletionSource();
        private bool _needsUpdate;

        private Task _backgroundWorkerTask = Task.CompletedTask;

        private readonly QuicSocketContext _parent;

        private long _timer = long.MaxValue;

        public ConnectionContext(QuicServerSocketContext parent, EndPoint remoteEndpoint, ReadOnlySpan<byte> odcid)
        {
            _parent = parent;
            Connection = new ManagedQuicConnection(parent.ListenerOptions, this, remoteEndpoint, odcid);
            Connection.SetSocketContext(this);

            var sentPacketPool = new ObjectPool<SentPacket>(256);
            _sendContext = new QuicSocketContext.SendContext(sentPacketPool);
            _recvContext = new QuicSocketContext.RecvContext(sentPacketPool);
        }

        public ConnectionContext(SingleConnectionSocketContext parent, ManagedQuicConnection connection)
        {
            _parent = parent;
            Connection = connection;

            var sentPacketPool = new ObjectPool<SentPacket>(256);
            _sendContext = new QuicSocketContext.SendContext(sentPacketPool);
            _recvContext = new QuicSocketContext.RecvContext(sentPacketPool);
        }

        internal async Task Run()
        {
            while (Connection.ConnectionState != QuicConnectionState.Closed)
            {
                if (Timestamp.Now >= _timer)
                {
                    Connection.OnTimeout(Timestamp.Now);
                }

                while (_recvQueue.Reader.TryRead(out var datagram))
                {
                    DoReceiveDatagram(datagram);
                }

                if (Connection.GetWriteLevel(Timestamp.Now) != EncryptionLevel.None)
                {
                    _needsUpdate = false;
                    // TODO: discover path MTU
                    var buffer = ArrayPool.Rent(QuicConstants.MaximumAllowedDatagramSize);
                    _writer.Reset(buffer);
                    _sendContext.Timestamp = Timestamp.Now;
                    Connection.SendData(_writer, out var receiver, _sendContext);

                    _parent.SendDatagram(new DatagramInfo(buffer, _writer.BytesWritten, receiver));

                    ArrayPool.Return(buffer);
                }

                var now = Timestamp.Now;
                _timer = Connection.GetNextTimerTimestamp();
                if (now < _timer)
                {
                    // asynchronously wait until either the timer expires or we receive a new datagram
                    if (_waitCompletionSource.Task.IsCompleted)
                    {
                        _waitCompletionSource =
                            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    }

                    // protection against race conditions (flag set just after last update finished)
                    if (!_needsUpdate)
                    {
                        CancellationTokenSource cts = new CancellationTokenSource();
                        var read = _recvQueue.Reader.WaitToReadAsync(cts.Token).AsTask();
                        await using var registration = cts.Token.Register(static s =>
                        {
                            ((TaskCompletionSource?)s)?.TrySetResult();
                        }, _waitCompletionSource);

                        if (_timer < long.MaxValue)
                            cts.CancelAfter((int)Timestamp.GetMilliseconds(_timer - now));
                        await Task.WhenAny(read, _waitCompletionSource.Task).ConfigureAwait(false);
                        cts.Cancel();
                    }
                }
            }
        }

        private void DoReceiveDatagram(DatagramInfo datagram)
        {
            _reader.Reset(datagram.Buffer.AsMemory(0, datagram.Length));

            var previousState = Connection.ConnectionState;
            _recvContext.Timestamp = Timestamp.Now;
            Connection.ReceiveData(_reader, datagram.RemoteEndpoint, _recvContext);
            // the array pools are shared
            ArrayPool.Return(datagram.Buffer);

            var newState = Connection.ConnectionState;
            if (newState != previousState)
            {
                _parent.OnConnectionStateChanged(Connection, newState);
            }
        }

        public void Start()
        {
            _backgroundWorkerTask = Task.Factory.StartNew(Run, CancellationToken.None, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            _parent.Start();
        }

        public void WakeUp()
        {
            _waitCompletionSource.TrySetResult();
            _needsUpdate = true;
        }

        public IPEndPoint LocalEndPoint => _parent.LocalEndPoint;
    }
}
