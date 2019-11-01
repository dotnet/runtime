// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;

namespace System.Net.Sockets.Tests
{
    // Provides a dummy _socket server that accepts connections and echoes data sent
    public class SocketTestServerAPM : SocketTestServer
    {
        private VerboseTestLogging _log;

        private Socket _socket;
        private int _receiveBufferSize;
        private volatile bool _disposed = false;

        protected sealed override int Port { get { return ((IPEndPoint)_socket.LocalEndPoint).Port; } }
        public sealed override EndPoint EndPoint { get { return _socket.LocalEndPoint; } }

        public SocketTestServerAPM(int numConnections, int receiveBufferSize, EndPoint localEndPoint)
        {
            _log = VerboseTestLogging.GetInstance();
            _receiveBufferSize = receiveBufferSize;

            _socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(localEndPoint);
            _socket.Listen(numConnections);

            _socket.BeginAccept(OnAccept, null);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _socket.Dispose();
                _disposed = true;
            }
        }

        private void OnAccept(IAsyncResult result)
        {
            Socket client = null;

            if (_disposed)
            {
                return;
            }

            try
            {
                client = _socket.EndAccept(result);
            }
            catch (SocketException e)
            {
                if (_disposed ||
                    e.SocketErrorCode == SocketError.OperationAborted ||
                    e.SocketErrorCode == SocketError.Interrupted)
                {
                    return;
                }

                throw;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            ServerSocketState state = new ServerSocketState(client, _receiveBufferSize);
            try
            {
                state.Socket.BeginReceive(state.TransferBuffer, 0, state.TransferBuffer.Length, SocketFlags.None, OnReceive, state);
            }
            catch (SocketException)
            {
            }

            try
            {
                _socket.BeginAccept(OnAccept, null);
            }
            catch (SocketException e)
            {
                if (_disposed ||
                    e.SocketErrorCode == SocketError.OperationAborted ||
                    e.SocketErrorCode == SocketError.Interrupted)
                {
                    return;
                }

                throw;
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void OnReceive(IAsyncResult result)
        {
            ServerSocketState recvState = (ServerSocketState)result.AsyncState;

            try
            {
                int bytesReceived = recvState.Socket.EndReceive(result);
                if (bytesReceived == 0)
                {
                    recvState.Socket.Dispose();
                    return;
                }

                ServerSocketState sendState = new ServerSocketState(recvState, bytesReceived);

                sendState.Socket.BeginSend(sendState.TransferBuffer, 0, bytesReceived, SocketFlags.None, OnSend, sendState);
                recvState.Socket.BeginReceive(
                    recvState.TransferBuffer,
                    0,
                    recvState.TransferBuffer.Length,
                    SocketFlags.None,
                    OnReceive,
                    recvState);
            }
            catch (SocketException)
            {
                recvState.Socket.Dispose();
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }

        private void OnSend(IAsyncResult result)
        {
            ServerSocketState sendState = (ServerSocketState)result.AsyncState;

            try
            {
                int bytesSent = sendState.Socket.EndSend(result);
                if (bytesSent != sendState.TransferBuffer.Length)
                {
                    _log.WriteLine("{2} APM: OnSend {0}bytes - expecting {1}bytes.", bytesSent, sendState.TransferBuffer.Length, sendState.Socket.GetHashCode());
                }
            }
            catch (SocketException)
            {
                sendState.Socket.Dispose();
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }

        private class ServerSocketState
        {
            private Socket __socket;
            private byte[] _buffer;

            public ServerSocketState(Socket _socket, int bufferSize)
            {
                __socket = _socket;
                _buffer = new byte[bufferSize];
            }

            public ServerSocketState(ServerSocketState original, int count)
            {
                __socket = original.__socket;
                _buffer = new byte[count];
                Buffer.BlockCopy(original._buffer, 0, _buffer, 0, count);
            }

            public Socket Socket
            {
                get { return __socket; }
            }

            public byte[] TransferBuffer
            {
                get { return _buffer; }
            }
        }
    }
}
