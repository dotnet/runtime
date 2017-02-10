// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Xunit.Abstractions;

namespace System.Net.Sockets.Performance.Tests
{
    public class SocketTestClientAsync : SocketTestClient
    {
        private SocketAsyncEventArgs _sendEventArgs = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs _recvEventArgs = new SocketAsyncEventArgs();

        public SocketTestClientAsync(
            ITestOutputHelper log,
            EndPoint endpoint,
            int iterations,
            string message,
            Stopwatch timeProgramStart) : base(log, endpoint, iterations, message, timeProgramStart)
        {
            _sendEventArgs.Completed += IO_Complete;
            _recvEventArgs.Completed += IO_Complete;
        }

        public override void Connect(Action<SocketError> onConnectCallback)
        {
            var connectEventArgs = new SocketAsyncEventArgs();
            connectEventArgs.RemoteEndPoint = _endpoint;
            connectEventArgs.UserToken = onConnectCallback;
            connectEventArgs.Completed += OnConnect;

            bool willRaiseEvent = Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, connectEventArgs);
            if (!willRaiseEvent)
            {
                ProcessConnect(connectEventArgs);
            }
        }

        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            ProcessConnect(e);
        }

        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            _s = e.ConnectSocket;
            Action<SocketError> callback = (Action<SocketError>)e.UserToken;
            callback(e.SocketError);
        }

        public override bool Send(out int bytesSent, out SocketError socketError, Action<int, SocketError> onSendCallback)
        {
            bytesSent = 0;
            socketError = SocketError.Success;

            _sendEventArgs.SetBuffer(_sendBuffer, _sendBufferIndex, _sendBuffer.Length - _sendBufferIndex);
            _sendEventArgs.UserToken = onSendCallback;

            bool pending = _s.SendAsync(_sendEventArgs);
            if (!pending)
            {
                bytesSent = _sendEventArgs.BytesTransferred;
                socketError = _sendEventArgs.SocketError;
            }

            return pending;
        }

        public override bool Receive(out int bytesReceived, out SocketError socketError, Action<int, SocketError> onReceiveCallback)
        {
            bytesReceived = 0;
            socketError = SocketError.Success;

            _recvEventArgs.SetBuffer(_recvBuffer, _recvBufferIndex, _recvBuffer.Length - _recvBufferIndex);
            _recvEventArgs.UserToken = onReceiveCallback;

            bool pending = _s.ReceiveAsync(_recvEventArgs);
            if (!pending)
            {
                bytesReceived = _recvEventArgs.BytesTransferred;
                socketError = _recvEventArgs.SocketError;
            }

            return pending;
        }

        private void IO_Complete(object sender, SocketAsyncEventArgs e)
        {
            Action<int, SocketError> callback = (Action<int, SocketError>)e.UserToken;
            callback(e.BytesTransferred, e.SocketError);
        }

        public override void Close(Action onCloseCallback)
        {
            _s?.Dispose();
            onCloseCallback();
        }

        protected override string ImplementationName()
        {
            return "Async";
        }
    }
}
