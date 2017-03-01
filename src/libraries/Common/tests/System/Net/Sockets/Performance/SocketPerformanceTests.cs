// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Sockets.Tests;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Performance.Tests
{
    public class SocketPerformanceTests
    {
        private const string _format = "{0, -20}, {1, -25}, {2, 15}, {3, 15}, {4, 15}, {5, 15}, {6, 15}, {7, 15}, {8, 15}";
        private readonly ITestOutputHelper _log;

        public SocketPerformanceTests(ITestOutputHelper log)
        {
            _log = log;
        }

        public void ClientServerTest(
            int port,
            SocketImplementationType serverType,
            SocketImplementationType clientType,
            int iterations,
            int bufferSize,
            int socketInstances,
            long expectedMilliseconds = 0)
        {
            long milliseconds;

            int numConnections = socketInstances * 5;
            int receiveBufferSize = bufferSize * 2;
            IPAddress address = IPAddress.IPv6Loopback;

            SocketTestServer server;
            if (port == 0)
            {
                server = SocketTestServer.SocketTestServerFactory(serverType, numConnections, receiveBufferSize, address, out port);
            }
            else
            {
                server = SocketTestServer.SocketTestServerFactory(serverType, numConnections, receiveBufferSize, new IPEndPoint(address, port));
            }

            using (server)
            {
                milliseconds = RunClient(
                    clientType,
                    new IPEndPoint(address, port),
                    iterations,
                    bufferSize,
                    socketInstances);
            }

            if (expectedMilliseconds != 0)
            {
                Assert.True(
                    milliseconds < expectedMilliseconds,
                    "Test execution is expected to be shorter than " + expectedMilliseconds + " but was " + milliseconds);
            }
        }

        public void ClientServerTest(
            SocketImplementationType serverType,
            SocketImplementationType clientType,
            int iterations,
            int bufferSize,
            int socketInstances,
            long expectedMilliseconds = 0)
        {
            // NOTE: port '0' below indicates that the server should bind to an anonymous port.
            ClientServerTest(0, serverType, clientType, iterations, bufferSize, socketInstances, expectedMilliseconds);
        }

        public long RunClient(
            SocketImplementationType testType,
            EndPoint endpoint,
            int iterations,
            int bufferSize,
            int socketInstances)
        {
            _log.WriteLine(
                _format,
                "Implementation",
                "Type",
                "Buffer Size",
                "Iterations",
                "Init(ms)",
                "Connect(ms)",
                "SendRecv(ms)",
                "Close(ms)",
                "Total time");

            Task[] tasks = new Task[socketInstances];

            char[] charBuffer = new char[bufferSize];

            for (int i = 0; i < bufferSize; i++)
            {
                checked
                {
                    charBuffer[i] = (char)(i % 26 + 65);
                }
            }

            string message = new string(charBuffer);

            Stopwatch timeProgramStart = new Stopwatch();
            timeProgramStart.Start();

            Parallel.For(
                0,
                socketInstances,
                (i) =>
                {
                    var test = SocketTestClient.SocketTestClientFactory(
                        _log,
                        testType,
                        endpoint,
                        iterations,
                        message,
                        timeProgramStart);

                    tasks[i] = test.RunTest();
                });

            Task.WaitAll(tasks);

            timeProgramStart.Stop();

            return timeProgramStart.ElapsedMilliseconds;
        }
    }
}
