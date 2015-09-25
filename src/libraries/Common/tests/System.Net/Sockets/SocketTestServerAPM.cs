// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Test.Common;

namespace System.Net.Sockets.Tests
{
    // Provides a dummy socket server that accepts connections and echoes data sent
    public class SocketTestServerAPM : SocketTestServer
    {
        private VerboseTestLogging _log;

        private Socket socket;
        private int _receiveBufferSize;
        private volatile bool disposed = false;

        public SocketTestServerAPM(int numConnections, int receiveBufferSize, EndPoint localEndPoint) 
        {
            _log = VerboseTestLogging.GetInstance();
            _receiveBufferSize = receiveBufferSize;

            socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(localEndPoint);
            socket.Listen(numConnections);
            
            socket.BeginAccept(OnAccept, null);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                socket.Dispose();
                disposed = true;
            }
        }

        private void OnAccept(IAsyncResult result)
        {
            Socket client = null;

            if (disposed)
            {
                return;
            }

            try
            {
                client = socket.EndAccept(result);
            }
            catch (SocketException e)
            {
                if (disposed ||
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
                socket.BeginAccept(OnAccept, null);
            }
            catch (SocketException e)
            {
                if (disposed ||
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

        // TODO: Cache and reuse ServerSocketState objects.
        private class ServerSocketState
        {
            private Socket _socket;
            private byte[] _buffer;

            public ServerSocketState(Socket socket, int bufferSize)
            {
                _socket = socket;
                _buffer = new byte[bufferSize];
            }

            public ServerSocketState(ServerSocketState original, int count)
            {
                _socket = original._socket;
                _buffer = new byte[count];
                Buffer.BlockCopy(original._buffer, 0, _buffer, 0, count);
            }

            public Socket Socket
            {
                get { return _socket; } 
            }

            public byte[] TransferBuffer
            {
                get { return _buffer; }
            }
        }
    }
}
