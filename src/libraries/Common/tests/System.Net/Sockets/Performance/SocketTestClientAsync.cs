// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Xunit.Abstractions;

namespace System.Net.Sockets.Performance.Tests
{
    public class SocketTestClientAsync : SocketTestClient
    {
        SocketAsyncEventArgs sendEventArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs recvEventArgs = new SocketAsyncEventArgs();

        public SocketTestClientAsync(
            ITestOutputHelper log,
            string server, 
            int port, 
            int iterations, 
            string message, 
            Stopwatch timeProgramStart) : base(log, server, port, iterations, message, timeProgramStart)
        {
            sendEventArgs.Completed += IO_Complete;
            recvEventArgs.Completed += IO_Complete;
        }
        
        public override void Connect(Action<SocketError> onConnectCallback)
        {
            var connectEventArgs = new SocketAsyncEventArgs();
            connectEventArgs.RemoteEndPoint = _endpoint;
            connectEventArgs.UserToken = onConnectCallback;
            connectEventArgs.Completed += OnConnect;

            bool willRaiseEvent = _s.ConnectAsync(connectEventArgs);
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
            Action<SocketError> callback = (Action<SocketError>)e.UserToken;
            callback(e.SocketError);
        }

        public override void Send(Action<int, SocketError> onSendCallback)
        {
            sendEventArgs.SetBuffer(_sendBuffer, _sendBufferIndex, _sendBuffer.Length - _sendBufferIndex);
            sendEventArgs.UserToken = onSendCallback;

            bool willRaiseEvent = _s.SendAsync(sendEventArgs);
            if (!willRaiseEvent)
            {
                IO_Complete(this, sendEventArgs);
            }
        }

        public override void Receive(Action<int, SocketError> onReceiveCallback)
        {
            recvEventArgs.SetBuffer(_recvBuffer, _recvBufferIndex, _recvBuffer.Length - _recvBufferIndex);
            recvEventArgs.UserToken = onReceiveCallback;

            bool willRaiseEvent = _s.ReceiveAsync(recvEventArgs);
            if (!willRaiseEvent)
            {
                IO_Complete(this, recvEventArgs);
            }
        }

        private void IO_Complete(object sender, SocketAsyncEventArgs e)
        {
            Action<int, SocketError> callback = (Action<int, SocketError>)e.UserToken;
            callback(e.BytesTransferred, e.SocketError);
        }

        public override void Close(Action onCloseCallback)
        {
            _s.Dispose();
            onCloseCallback();
        }

        protected override string ImplementationName()
        {
            return "Async";
        }
    }
}
