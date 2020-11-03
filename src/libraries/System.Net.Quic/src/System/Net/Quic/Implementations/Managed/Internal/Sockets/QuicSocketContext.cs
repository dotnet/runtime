// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed.Internal.Sockets
{
    /// <summary>
    ///     Class responsible for serving a socket for QUIC connections.
    /// </summary>
    internal abstract class QuicSocketContext
    {
        private readonly EndPoint? _localEndPoint;
        private readonly EndPoint? _remoteEndPoint;
        private readonly bool _isServer;
        private readonly CancellationTokenSource _socketTaskCts;

        private bool _started;

        private readonly Socket _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly SocketAsyncEventArgs _socketReceiveEventArgs = new SocketAsyncEventArgs();

        protected QuicSocketContext(EndPoint? localEndPoint, EndPoint? remoteEndPoint, bool isServer)
        {
            _localEndPoint = localEndPoint;
            _remoteEndPoint = remoteEndPoint;

            _isServer = isServer;

            _socketTaskCts = new CancellationTokenSource();

            SetupSocket(localEndPoint, remoteEndPoint);

            _socketReceiveEventArgs.Completed += (sender, args) => OnReceiveFinished(args);
        }

        private void SetupSocket(EndPoint? localEndPoint, EndPoint? remoteEndPoint)
        {
            if (_socket.AddressFamily == AddressFamily.InterNetwork)
            {
                _socket.DontFragment = true;
            }

            if (localEndPoint != null)
            {
                _socket.Bind(localEndPoint);
            }

            if (remoteEndPoint != null)
            {
                _socket.Connect(remoteEndPoint);
            }

#if WINDOWS
            // disable exception when client forcibly closes the socket.
            // https://stackoverflow.com/questions/38191968/c-sharp-udp-an-existing-connection-was-forcibly-closed-by-the-remote-host

            const int SIO_UDP_CONNRESET = -1744830452;
            _socket.IOControl(
                (IOControlCode)SIO_UDP_CONNRESET,
                new byte[] {0, 0, 0, 0},
                null
            );
#endif
        }

        public IPEndPoint LocalEndPoint => (IPEndPoint)_socket.LocalEndPoint!;

        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;

            var args = _socketReceiveEventArgs;
            while (!ReceiveFromAsync(args))
            {
                // this should not really happen, as the socket should be just opened, but we want to be sure and don't
                // miss any incoming datagrams
                DoReceive(ExtractDatagram(args));
            }
        }

        private static DatagramInfo ExtractDatagram(SocketAsyncEventArgs args)
        {
            return new DatagramInfo(args.Buffer!, args.SocketError == SocketError.Success ? args.BytesTransferred : 0,
                args.RemoteEndPoint!);
        }

        private void OnReceiveFinished(SocketAsyncEventArgs args)
        {
            if (_socketTaskCts.IsCancellationRequested)
                return;

            bool pending = false;
            do
            {
                DatagramInfo datagram = ExtractDatagram(args);

                // immediately issue another async receive, this achieves the leader-follower thread pattern
                pending = !ReceiveFromAsync(args);

                DoReceive(datagram);
            } while (pending);
        }

        protected void SignalStop()
        {
            _socketTaskCts.Cancel();
            Dispose();
        }

        protected abstract void OnDatagramReceived(in DatagramInfo datagram);

        private void DoReceive(in DatagramInfo datagram)
        {
            // process only datagrams big enough to contain valid QUIC packets
            if (datagram.Length < QuicConstants.MinimumPacketSize)
            {
                return;
            }

            OnDatagramReceived(datagram);
        }

        private void DoReceive(byte[] datagram, int length, EndPoint sender)
        {
            // process only datagrams big enough to contain valid QUIC packets
            if (datagram.Length < QuicConstants.MinimumPacketSize)
            {
                return;
            }

            OnDatagramReceived(new DatagramInfo(datagram, length, sender));
        }

        /// <summary>
        ///     Called when a connections <see cref="ManagedQuicConnection.ConnectionState"/> changes.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="newState">The new state of the connection.</param>
        /// <returns>True if the processing of the connection should be stopped.</returns>
        protected internal abstract bool
            OnConnectionStateChanged(ManagedQuicConnection connection, QuicConnectionState newState);

        protected class ReceiveOperationAsyncSocketArgs : SocketAsyncEventArgs
        {
            public ResettableCompletionSource<SocketReceiveFromResult> CompletionSource { get; } =
                new ResettableCompletionSource<SocketReceiveFromResult>();

            protected override void OnCompleted(SocketAsyncEventArgs e)
            {
                CompletionSource.Complete(
                    new SocketReceiveFromResult()
                    {
                        ReceivedBytes = e.SocketError == SocketError.Success ? e.BytesTransferred : 0,
                        RemoteEndPoint = e.RemoteEndPoint!
                    });
            }
        }

        internal ArrayPool<byte> ArrayPool { get; } = ArrayPool<byte>.Shared;

        private bool ReceiveFromAsync(SocketAsyncEventArgs args)
        {
            if (_socketTaskCts.IsCancellationRequested)
                return true;

            // we need a new buffer, because the one which was received into last time may be still read from by the
            // connection
            var buffer = ArrayPool.Rent(QuicConstants.MaximumAllowedDatagramSize);
            args.SetBuffer(buffer, 0, buffer.Length);

            if (_remoteEndPoint != null)
            {
                // we are using connected sockets -> use Receive(...). We also have to set the expected
                // receiver address so that the receiving code later uses it

                args.RemoteEndPoint = _remoteEndPoint!;
                return _socket.ReceiveAsync(args);
            }

            Debug.Assert(_isServer);
            Debug.Assert(_localEndPoint != null);

            args.RemoteEndPoint = _localEndPoint!;
            return _socket.ReceiveFromAsync(args);
        }

        protected abstract void OnException(Exception e);

        /// <summary>
        ///     Detaches the given connection from this context, the connection will no longer be updated from the
        ///     thread running at this socket.
        /// </summary>
        /// <param name="connection"></param>
        protected internal abstract void DetachConnection(ManagedQuicConnection connection);

        internal class ContextBase
        {
            public ContextBase(ObjectPool<SentPacket> sentPacketPool) => SentPacketPool = sentPacketPool;

            /// <summary>
            ///     Timestamp when the next tick of internal processing was requested.
            /// </summary>
            internal long Timestamp { get; set; }

            internal ObjectPool<SentPacket> SentPacketPool { get; }

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

        private void Dispose()
        {
            _socket.Dispose();
        }

        internal void SendDatagram(in DatagramInfo datagram)
        {
            if (_remoteEndPoint != null)
            {
                _socket.Send(datagram.Buffer.AsSpan(0, datagram.Length), SocketFlags.None);
            }
            else
            {
                _socket.SendTo(datagram.Buffer, 0, datagram.Length, SocketFlags.None, datagram.RemoteEndpoint);
            }
        }
    }
}
