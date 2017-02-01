// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Sockets.Tests;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Performance.Tests
{
    public abstract class SocketTestClient
    {
        protected readonly ITestOutputHelper _log;

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

        private int _current_bytes;

        private Stopwatch _timeConnect = new Stopwatch();
        private Stopwatch _timeSendRecv = new Stopwatch();
        private Stopwatch _timeClose = new Stopwatch();

        private Random _random = new Random();

        public SocketTestClient(
            ITestOutputHelper log,
            EndPoint endpoint,
            int iterations,
            string message,
            Stopwatch timeProgramStart)
        {
            _log = log;

            _endpoint = endpoint;

            _sendString = message;
            _sendBuffer = Encoding.UTF8.GetBytes(_sendString);

            _bufferLen = _sendBuffer.Length;
            _recvBuffer = new byte[_bufferLen];

            _timeProgramStart = timeProgramStart;

            _iterations = iterations;
        }

        public static SocketTestClient SocketTestClientFactory(
            ITestOutputHelper log,
            SocketImplementationType type,
            EndPoint endpoint,
            int iterations,
            string message,
            Stopwatch timeProgramStart)
        {
            switch (type)
            {
                case SocketImplementationType.APM:
                    var socketAPM = new SocketTestClientAPM(log, endpoint, iterations, message, timeProgramStart);
                    log.WriteLine(socketAPM.GetHashCode() + " SocketTestClientAPM(..)");
                    return socketAPM;
                case SocketImplementationType.Async:
                    var socketAsync = new SocketTestClientAsync(log, endpoint, iterations, message, timeProgramStart);
                    log.WriteLine(socketAsync.GetHashCode() + " SocketTestClientAsync(..)");
                    return socketAsync;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public abstract void Connect(Action<SocketError> onConnectCallback);

        private Task ConnectHelper()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            Connect(socketError => {
                if (socketError == SocketError.Success)
                {
                    tcs.SetResult(true);
                }
                else
                {
                    tcs.SetException(new SocketException((int)socketError));
                }
            });

            return tcs.Task;
        }

        public async Task RunTest()
        {
            int connectionAttempts = 0;
            while (true)
            {
                try 
                {
                    await ConnectHelper();

                    // Success, break out of loop
                    break;
                }
                catch (SocketException e)
                {
                    // If we got ConnectionRefused, we want to fall through and retry
                    // Otherwise, rethrow
                    if (e.SocketErrorCode != SocketError.ConnectionRefused)
                    {
                        throw;
                    }

                    // Limit connection attempts
                    if (connectionAttempts == 3)
                    {
                        throw;
                    }
                }

                // Connection was refused.  
                // The server may be temporarily overloaded, and the server OS is rejecting connections.
                // Wait a bit and then retry.
                await Task.Delay(200);

                connectionAttempts++;
            }

            _timeSendRecv.Start();

            // Loop over iterations, doing sends and receives
            for (int i = 0; i < _iterations; i++)
            {
                // Kick off another iteration.
                // First, we pick a random # of Sends to perform.

                // Generate a random floating point number between 0 and 10
                double d = ((double)_random.Next()) / int.MaxValue * 10;

                // Use this as the base 2 exponent to determine # of Sends to do.
                // In other words, this scales exponentially from 1 (2^0) to 1024 (2^10),
                // with a median of 32 (2^5).
                int sends = (int) Math.Floor(Math.Pow(2.0, d));

                // We actually track bytes overall, since the receives are not
                // necessarily always on buffer length boundaries.
                _current_bytes = sends * _bufferLen;

                // 
                // TODO: It might be more efficient to have more than one outstanding Send/Receive.

                // IMPORTANT: The code currently assumes one outstanding Send and one Receive. Interlocked operations
                //            are required to handle re-entrancy.

                Task t1 = Task.Run(() => DoSend());
                Task t2 = Task.Run(() => DoReceive());

                await t1;
                await t2;
            }

            await CloseHelper();
        }

        public abstract bool Send(out int bytesTransferred, out SocketError socketError, Action<int, SocketError> onSendCallback);

        private Task<int> SendHelper()
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            Action<int, SocketError> onSend = (bytesTransferred, socketError) => {
                if (socketError == SocketError.Success)
                {
                    tcs.SetResult(bytesTransferred);
                }
                else
                {
                    tcs.SetException(new SocketException((int)socketError));
                }
            };

            int bytes;
            SocketError error;
            bool pending = Send(out bytes, out error, onSend);
            if (!pending)
            {
                onSend(bytes, error);
            }

            return tcs.Task;
        }

        private Task<int> ReceiveHelper()
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            Action<int, SocketError> onReceive = (bytesTransferred, socketError) => {
                if (socketError == SocketError.Success)
                {
                    tcs.SetResult(bytesTransferred);
                }
                else
                {
                    tcs.SetException(new SocketException((int)socketError));
                }
            };

            int bytes;
            SocketError error;
            bool pending = Receive(out bytes, out error, onReceive);
            if (!pending)
            {
                onReceive(bytes, error);
            }

            return tcs.Task;
        }

        private async Task DoSend()
        {
            int total_bytes_sent = 0;
            while (total_bytes_sent < _current_bytes)
            {
                int bytesSent = await SendHelper();

                if (bytesSent != _sendBuffer.Length)
                {
                    _log.WriteLine(
                        "OnSend: Unexpected bytesSent={0}, expected {1}",
                        bytesSent,
                        _sendBuffer.Length);
                    throw new Exception("Unexpected bytesSent");
                }

                total_bytes_sent += bytesSent;
                Assert.True(total_bytes_sent <= _current_bytes);
            }
        }

        public abstract bool Receive(out int bytesTransferred, out SocketError socketError, Action<int, SocketError> onSendCallback);

        private async Task DoReceive()
        {
            int total_bytes_received = 0;
            while (total_bytes_received < _current_bytes)
            {
                int receivedBytes = await ReceiveHelper();
                if (receivedBytes == 0)
                {
                    _log.WriteLine("Socket unexpectedly closed.");
                    throw new Exception("Socket unexpectedly closed");
                }

                total_bytes_received += receivedBytes;
                Assert.True(total_bytes_received <= _current_bytes);

                _recvBufferIndex += receivedBytes;
                Assert.True(_recvBufferIndex <= _bufferLen);
                if (_recvBufferIndex == _bufferLen)
                {
                    _recvBufferIndex = 0;

                    // Compare the bytes we sent to the bytes we received;
                    // They should be the same since the server is an echo server.
                    if (!SocketTestMemcmp.Compare(_sendBuffer, _recvBuffer))
                    {
                        _log.WriteLine("Received different data from echo server");
                        throw new Exception("Received different data from echo server");
                    }

                    Array.Clear(_recvBuffer, 0, _bufferLen);
                }
            }
        }

        public abstract void Close(Action onCloseCallback);

        private Task CloseHelper()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            Close(() => { tcs.SetResult(true); });

            return tcs.Task;
        }

        protected abstract string ImplementationName();
    }
}
