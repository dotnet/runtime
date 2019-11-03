// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    public sealed class QuicConnection : IDisposable
    {
        private readonly bool _isClient;
        private bool _disposed = false;
        private IPEndPoint _remoteEndPoint;
        private IPEndPoint _localEndPoint;
        private object _syncObject = new object();

        // !!! TEMPORARY FOR QUIC MOCK SUPPORT
        private readonly bool _mock = false;
        private Socket _socket = null;
        private IPEndPoint _peerListenEndPoint = null;
        private TcpListener _inboundListener = null;
        private long _nextOutboundBidirectionalStream;
        private long _nextOutboundUnidirectionalStream;

        /// <summary>
        /// Create an outbound QUIC connection.
        /// </summary>
        /// <param name="remoteEndPoint">The remote endpoint to connect to.</param>
        /// <param name="sslClientAuthenticationOptions">TLS options</param>
        /// <param name="localEndPoint">The local endpoint to connect from.</param>
        /// <param name="mock">Use mock QUIC implementation.</param>
        // !!! TEMPORARY FOR QUIC MOCK SUPPORT: Remove "mock" parameter before shipping
        public QuicConnection(IPEndPoint remoteEndPoint, SslClientAuthenticationOptions sslClientAuthenticationOptions, IPEndPoint localEndPoint = null, bool mock = false)
        {
            // TODO: TLS handling

            _mock = mock;
            _remoteEndPoint = remoteEndPoint;
            _localEndPoint = localEndPoint;

            _isClient = true;
            _nextOutboundBidirectionalStream = 0;
            _nextOutboundUnidirectionalStream = 2;
        }

        // Constructor for accepted inbound QuicConnections
        // !!! TEMPORARY FOR QUIC MOCK SUPPORT
        internal QuicConnection(Socket socket, IPEndPoint peerListenEndPoint, TcpListener inboundListener)
        {
            _mock = true;
            _isClient = false;
            _nextOutboundBidirectionalStream = 1;
            _nextOutboundUnidirectionalStream = 3;
            _socket = socket;
            _peerListenEndPoint = peerListenEndPoint;
            _inboundListener = inboundListener;
            _localEndPoint = (IPEndPoint)socket.LocalEndPoint;
            _remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
        }

        /// <summary>
        /// Indicates whether the QuicConnection is connected.
        /// </summary>
        public bool Connected
        {
            get
            {
                CheckDisposed();

                if (_mock)
                {
                    return _socket != null;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public IPEndPoint LocalEndPoint => new IPEndPoint(_localEndPoint.Address, _localEndPoint.Port);

        public IPEndPoint RemoteEndPoint => new IPEndPoint(_remoteEndPoint.Address, _remoteEndPoint.Port);

        /// <summary>
        /// Connect to the remote endpoint.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (_mock)
            {
                if (Connected)
                {
                    // TODO: Exception text
                    throw new InvalidOperationException("Already connected");
                }

                Socket socket = new Socket(_remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(_remoteEndPoint).ConfigureAwait(false);
                socket.NoDelay = true;

                _localEndPoint = (IPEndPoint)socket.LocalEndPoint;

                // Listen on a new local endpoint for inbound streams
                TcpListener inboundListener = new TcpListener(_localEndPoint.Address, 0);
                inboundListener.Start();
                int inboundListenPort = ((IPEndPoint)inboundListener.LocalEndpoint).Port;

                // Write inbound listen port to socket so server can read it
                byte[] buffer = new byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(buffer, inboundListenPort);
                await socket.SendAsync(buffer, SocketFlags.None).ConfigureAwait(false);

                // Read first 4 bytes to get server listen port
                int bytesRead = 0;
                do
                {
                    bytesRead += await socket.ReceiveAsync(buffer.AsMemory().Slice(bytesRead), SocketFlags.None).ConfigureAwait(false);
                } while (bytesRead != buffer.Length);

                int peerListenPort = BinaryPrimitives.ReadInt32LittleEndian(buffer);
                IPEndPoint peerListenEndPoint = new IPEndPoint(((IPEndPoint)socket.RemoteEndPoint).Address, peerListenPort);

                _socket = socket;
                _peerListenEndPoint = peerListenEndPoint;
                _inboundListener = inboundListener;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Create an outbound unidirectional stream.
        /// </summary>
        /// <returns></returns>
        public QuicStream OpenUnidirectionalStream()
        {
            if (_mock)
            {
                long streamId;
                lock (_syncObject)
                {
                    streamId = _nextOutboundUnidirectionalStream;
                    _nextOutboundUnidirectionalStream += 4;
                }

                return new QuicStream(this, streamId, bidirectional: false);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Create an outbound bidirectional stream.
        /// </summary>
        /// <returns></returns>
        public QuicStream OpenBidirectionalStream()
        {
            if (_mock)
            {
                long streamId;
                lock (_syncObject)
                {
                    streamId = _nextOutboundBidirectionalStream;
                    _nextOutboundBidirectionalStream += 4;
                }

                return new QuicStream(this, streamId, bidirectional: true);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        // !!! TEMPORARY FOR QUIC MOCK SUPPORT
        internal async Task<Socket> CreateOutboundMockStreamAsync(long streamId)
        {
            Debug.Assert(_mock);
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(_peerListenEndPoint).ConfigureAwait(false);
            socket.NoDelay = true;

            // Write stream ID to socket so server can read it
            byte[] buffer = new byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, streamId);
            await socket.SendAsync(buffer, SocketFlags.None).ConfigureAwait(false);

            return socket;
        }

        /// <summary>
        /// Accept an incoming stream.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<QuicStream> AcceptStreamAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (_mock)
            {
                Socket socket = await _inboundListener.AcceptSocketAsync().ConfigureAwait(false);

                // Read first bytes to get stream ID
                byte[] buffer = new byte[8];
                int bytesRead = 0;
                do
                {
                    bytesRead += await socket.ReceiveAsync(buffer.AsMemory().Slice(bytesRead), SocketFlags.None).ConfigureAwait(false);
                } while (bytesRead != buffer.Length);

                long streamId = BinaryPrimitives.ReadInt64LittleEndian(buffer);

                bool clientInitiated = ((streamId & 0b01) == 0);
                if (clientInitiated == _isClient)
                {
                    throw new Exception($"Wrong initiator on accepted stream??? streamId={streamId}, _isClient={_isClient}");
                }

                bool bidirectional = ((streamId & 0b10) == 0);
                return new QuicStream(socket, streamId, bidirectional: bidirectional);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Close the connection and terminate any active streams.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(QuicConnection));
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_mock)
                    {
                        _socket?.Dispose();
                        _socket = null;

                        _inboundListener?.Stop();
                        _inboundListener = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposed = true;
            }
        }

        ~QuicConnection()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
