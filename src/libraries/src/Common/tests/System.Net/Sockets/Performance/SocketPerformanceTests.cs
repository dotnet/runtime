// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            int socket_instances, 
            long expectedMilliseconds)
        {
            long milliseconds;

#if arm
        iterations /= 100;
#endif

            using (SocketTestServer.SocketTestServerFactory(
                serverType,
                socket_instances * 5,
                bufferSize * 2,
                new IPEndPoint(IPAddress.IPv6Loopback, port)))
            {
                milliseconds = RunClient(
                    clientType,
                    "localhost",
                    port,
                    iterations,
                    bufferSize,
                    socket_instances);
            }

            Assert.True(
                milliseconds < expectedMilliseconds,
                "Test execution is expected to be shorter than " + expectedMilliseconds + " but was " + milliseconds);
        }

        public long RunClient(
            SocketImplementationType testType, 
            string server, 
            int port,
            int iterations, 
            int bufferSize,
            int socket_instances)
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

            Task[] tasks = new Task[socket_instances];

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
                socket_instances,
                (i) =>
                {
                    var test = SocketTestClient.SocketTestClientFactory(
                        _log,
                        testType, 
                        server,
                        port, 
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
