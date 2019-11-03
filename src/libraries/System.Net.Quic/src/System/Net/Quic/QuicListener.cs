// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Net.Security;
using System.Threading.Tasks;
using System.Threading;
using System.Buffers.Binary;

namespace System.Net.Quic
{
    public sealed class QuicListener : IDisposable
    {
        private bool _disposed = false;
        private SslServerAuthenticationOptions _sslOptions;
        private IPEndPoint _listenEndPoint;

        // !!! TEMPORARY FOR QUIC MOCK SUPPORT
        private bool _mock = false;
        private TcpListener _tcpListener = null;

        /// <summary>
        /// Create a QUIC listener on the specified local endpoint and start listening.
        /// </summary>
        /// <param name="listenEndPoint">The local endpoint to listen on.</param>
        /// <param name="sslServerAuthenticationOptions">TLS options for the listener.</param>
        /// <param name="mock">Use mock QUIC implementation.</param>
        // !!! TEMPORARY FOR QUIC MOCK SUPPORT: Remove "mock" parameter before shipping
        public QuicListener(IPEndPoint listenEndPoint, SslServerAuthenticationOptions sslServerAuthenticationOptions, bool mock = false)
        {
            if (sslServerAuthenticationOptions == null && !mock)
            {
                throw new ArgumentNullException(nameof(sslServerAuthenticationOptions));
            }

            if (listenEndPoint == null)
            {
                throw new ArgumentNullException(nameof(listenEndPoint));
            }

            _sslOptions = sslServerAuthenticationOptions;
            _listenEndPoint = listenEndPoint;

            _mock = mock;
            if (mock)
            {
                _tcpListener = new TcpListener(listenEndPoint);
                _tcpListener.Start();

                if (listenEndPoint.Port == 0)
                {
                    // Get auto-assigned port
                    _listenEndPoint = (IPEndPoint)_tcpListener.LocalEndpoint;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        // IPEndPoint is mutable, so we must create a new instance every time this is retrieved.
        public IPEndPoint ListenEndPoint => new IPEndPoint(_listenEndPoint.Address, _listenEndPoint.Port);

        /// <summary>
        /// Accept a connection.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<QuicConnection> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (_mock)
            {
                Socket socket = await _tcpListener.AcceptSocketAsync().ConfigureAwait(false);
                socket.NoDelay = true;

                // Read first 4 bytes to get client listen port
                byte[] buffer = new byte[4];
                int bytesRead = 0;
                do
                {
                    bytesRead += await socket.ReceiveAsync(buffer.AsMemory().Slice(bytesRead), SocketFlags.None).ConfigureAwait(false);
                } while (bytesRead != buffer.Length);

                int peerListenPort = BinaryPrimitives.ReadInt32LittleEndian(buffer);
                IPEndPoint peerListenEndPoint = new IPEndPoint(((IPEndPoint)socket.RemoteEndPoint).Address, peerListenPort);

                // Listen on a new local endpoint for inbound streams
                TcpListener inboundListener = new TcpListener(_listenEndPoint.Address, 0);
                inboundListener.Start();
                int inboundListenPort = ((IPEndPoint)inboundListener.LocalEndpoint).Port;

                // Write inbound listen port to socket so client can read it
                BinaryPrimitives.WriteInt32LittleEndian(buffer, inboundListenPort);
                await socket.SendAsync(buffer, SocketFlags.None).ConfigureAwait(false);

                return new QuicConnection(socket, peerListenEndPoint, inboundListener);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Stop listening and close the listener.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(QuicListener));
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _tcpListener?.Stop();
                    _tcpListener = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposed = true;
            }
        }

        ~QuicListener()
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
