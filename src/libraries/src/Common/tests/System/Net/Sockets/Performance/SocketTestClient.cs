// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Sockets.Tests;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Xunit.Abstractions;

namespace System.Net.Sockets.Performance.Tests
{
    public abstract class SocketTestClient
    {
        protected readonly ITestOutputHelper _log;

        protected string _server;
        protected int _port;
        protected EndPoint _endpoint;

        protected Socket _s;

        protected byte[] _sendBuffer;
        protected int _sendBufferIndex = 0;

        protected byte[] _recvBuffer;
        protected int _recvBufferIndex = 0;

        protected int _iterations;

        private string _sendString;
        private Stopwatch _timeInit = new Stopwatch();
        private Stopwatch _timeProgramStart;
        private const string _format = "{0, -20}, {1, -25}, {2, 15}, {3, 15}, {4, 15}, {5, 15}, {6, 15}, {7, 15}, {8, 15}";
        private int _bufferLen;

        private int _send_iterations = 0;
        private int _receive_iterations = 0;

        private Stopwatch _timeConnect = new Stopwatch();
        private Stopwatch _timeSendRecv = new Stopwatch();
        private Stopwatch _timeClose = new Stopwatch();

        private TaskCompletionSource<long> _tcs = new TaskCompletionSource<long>();

        public SocketTestClient(
            ITestOutputHelper log,
            string server,
            int port,
            int iterations,
            string message,
            Stopwatch timeProgramStart)
        {
            _log = log;

            _server = server;
            _port = port;
            _endpoint = new DnsEndPoint(server, _port);

            _sendString = message;
            _sendBuffer = Encoding.UTF8.GetBytes(_sendString);

            _bufferLen = _sendBuffer.Length;
            _recvBuffer = new byte[_bufferLen];

            _timeProgramStart = timeProgramStart;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // on Unix, socket will be created in Socket.ConnectAsync
            {
                _timeInit.Start();
                _s = new Socket(SocketType.Stream, ProtocolType.Tcp);
                _timeInit.Stop();
            }

            _iterations = iterations;
        }

        public static SocketTestClient SocketTestClientFactory(
            ITestOutputHelper log,
            SocketImplementationType type,
            string server,
            int port,
            int iterations,
            string message,
            Stopwatch timeProgramStart)
        {
            switch (type)
            {
                case SocketImplementationType.APM:
                    var socketAPM = new SocketTestClientAPM(log, server, port, iterations, message, timeProgramStart);
                    log.WriteLine(socketAPM.GetHashCode() + " SocketTestClientAPM(..)");
                    return socketAPM;
                case SocketImplementationType.Async:
                    var socketAsync = new SocketTestClientAsync(log, server, port, iterations, message, timeProgramStart);
                    log.WriteLine(socketAsync.GetHashCode() + " SocketTestClientAsync(..)");
                    return socketAsync;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public abstract void Connect(Action<SocketError> onConnectCallback);

        private void OnConnect(SocketError error)
        {
            _timeConnect.Stop();
            _log.WriteLine(this.GetHashCode() + " OnConnect({0}) _timeConnect={1}", error, _timeConnect.ElapsedMilliseconds);

            // TODO: an error should fail the test.
            if (error != SocketError.Success)
            {
                _timeClose.Start();
                Close(OnClose);
                return;
            }

            _timeSendRecv.Start();

            // TODO: It might be more efficient to have more than one outstanding Send/Receive.

            // IMPORTANT: The code currently assumes one outstanding Send and one Receive. Interlocked operations
            //            are required to handle re-entrancy.
            Send(OnSend);
            Receive(OnReceive);
        }

        public abstract void Send(Action<int, SocketError> onSendCallback);

        // Called when the entire _sendBuffer has been sent.
        private void OnSend(int bytesSent, SocketError error)
        {
            _log.WriteLine(this.GetHashCode() + " OnSend({0}, {1})", bytesSent, error);

            // TODO: an error should fail the test.
            if (error != SocketError.Success)
            {
                _timeClose.Start();
                Close(OnClose);
                return;
            }

            if (bytesSent == _sendBuffer.Length)
            {
                OnSendMessage();
            }
            else
            {
                _log.WriteLine(
                    "OnSend: Unexpected bytesSent={0}, expected {1}",
                    bytesSent,
                    _sendBuffer.Length);
            }
        }

        private void OnSendMessage()
        {
            _send_iterations++;
            _log.WriteLine(this.GetHashCode() + " OnSendMessage() _send_iterations={0}", _send_iterations);

            if (_send_iterations < _iterations)
            {
                Send(OnSend);
            }

            //TODO: _s.Shutdown(SocketShutdown.Send);
        }

        public abstract void Receive(Action<int, SocketError> onReceiveCallback);

        // Called when the entire _recvBuffer has been received.
        private void OnReceive(int receivedBytes, SocketError error)
        {
            _log.WriteLine(this.GetHashCode() + " OnSend({0}, {1})", receivedBytes, error);
            _recvBufferIndex += receivedBytes;

            // TODO: an error should fail the test.
            if (error != SocketError.Success)
            {
                _timeClose.Start();
                Close(OnClose);
                return;
            }

            if (_recvBufferIndex == _recvBuffer.Length)
            {
                OnReceiveMessage();
            }
            else if (receivedBytes == 0)
            {
                _log.WriteLine("Socket unexpectedly closed.");
            }
            else
            {
                Receive(OnReceive);
            }
        }

        private void OnReceiveMessage()
        {
            _receive_iterations++;
            _log.WriteLine(this.GetHashCode() + " OnReceiveMessage() _receive_iterations={0}", _receive_iterations);

            _recvBufferIndex = 0;

            // Expect echo server.
            if (!SocketTestMemcmp.Compare(_sendBuffer, _recvBuffer))
            {
                _log.WriteLine("Received different data from echo server");
            }

            if (_receive_iterations >= _iterations)
            {
                _timeSendRecv.Stop();
                _timeClose.Start();
                Close(OnClose);
            }
            else
            {
                Array.Clear(_recvBuffer, 0, _recvBuffer.Length);
                Receive(OnReceive);
            }
        }

        public abstract void Close(Action onCloseCallback);

        private void OnClose()
        {
            _timeClose.Stop();
            _log.WriteLine(this.GetHashCode() + " OnClose() _timeClose={0}", _timeClose.ElapsedMilliseconds);

            try
            {
                _log.WriteLine(
                    _format,
                    "Socket",
                    ImplementationName(),
                    _bufferLen,
                    _receive_iterations,
                    _timeInit.ElapsedMilliseconds, // only relevant on Windows
                    _timeConnect.ElapsedMilliseconds, // on Unix this includes socket creation time
                    _timeSendRecv.ElapsedMilliseconds,
                    _timeClose.ElapsedMilliseconds,
                    _timeProgramStart.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _log.WriteLine("Exception while writing the report: {0}", ex);
            }

            _log.WriteLine(
                this.GetHashCode() + " OnClose() setting tcs result : {0}",
                _timeSendRecv.ElapsedMilliseconds);

            _tcs.TrySetResult(_timeSendRecv.ElapsedMilliseconds);
        }

        public Task<long> RunTest()
        {
            _timeConnect.Start();
            Connect(OnConnect);

            return _tcs.Task;
        }

        protected abstract string ImplementationName();
    }
}
