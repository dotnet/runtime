// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Xunit.Abstractions;

namespace System.Net.Sockets.Performance.Tests
{
    public class SocketTestClientAPM : SocketTestClient
    {
        public SocketTestClientAPM(
            ITestOutputHelper log,
            string server,
            int port,
            int iterations,
            string message,
            Stopwatch timeProgramStart) : base(log, server, port, iterations, message, timeProgramStart)
        {
        }

        public override void Close(Action onCloseCallback)
        {
            _s.Dispose();
            onCloseCallback();
        }

        public override void Connect(Action<SocketError> onConnectCallback)
        {
            _s.BeginConnect(_endpoint, OnConnect, onConnectCallback);
        }

        private void OnConnect(IAsyncResult result)
        {
            Action<SocketError> callback = (Action<SocketError>)result.AsyncState;

            SocketError error = SocketError.Success;
            try
            {
                _s.EndConnect(result);
            }
            catch (SocketException ex)
            {
                error = ex.SocketErrorCode;
            }

            callback(error);
        }

        public override void Receive(Action<int, SocketError> onReceiveCallback)
        {
            _s.BeginReceive(
                _recvBuffer,
                _recvBufferIndex,
                _recvBuffer.Length - _recvBufferIndex,
                SocketFlags.None,
                OnReceive,
                onReceiveCallback);
        }

        private void OnReceive(IAsyncResult result)
        {
            Action<int, SocketError> callback = (Action<int, SocketError>)result.AsyncState;

            int recvBytes = 0;
            SocketError error = SocketError.Success;
            try
            {
                recvBytes = _s.EndReceive(result);
            }
            catch (SocketException ex)
            {
                error = ex.SocketErrorCode;
            }

            callback(recvBytes, error);
        }

        public override void Send(Action<int, SocketError> onSendCallback)
        {
            _s.BeginSend(
                _sendBuffer,
                _sendBufferIndex,
                _sendBuffer.Length - _sendBufferIndex,
                SocketFlags.None,
                OnSend,
                onSendCallback);
        }

        private void OnSend(IAsyncResult result)
        {
            Action<int, SocketError> callback = (Action<int, SocketError>)result.AsyncState;

            int sentBytes = 0;
            SocketError error = SocketError.Success;
            try
            {
                sentBytes = _s.EndSend(result);
            }
            catch (SocketException ex)
            {
                error = ex.SocketErrorCode;
            }

            callback(sentBytes, error);
        }

        protected override string ImplementationName()
        {
            return "APM";
        }
    }
}
