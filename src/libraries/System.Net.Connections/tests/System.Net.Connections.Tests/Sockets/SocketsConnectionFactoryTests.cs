// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Net.Connections;
using System.Net.Sockets;
using System.Net.Sockets.Tests;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Connections.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime))]
    public class SocketsConnectionFactoryTests
    {
        public static TheoryData<EndPoint, SocketType, ProtocolType> GetConnectData()
        {
            var result = new TheoryData<EndPoint, SocketType, ProtocolType>()
            {
                { new IPEndPoint(IPAddress.Loopback, 0), SocketType.Stream, ProtocolType.Tcp },
                { new IPEndPoint(IPAddress.IPv6Loopback, 0), SocketType.Stream, ProtocolType.Tcp },
            };

            if (Socket.OSSupportsUnixDomainSockets)
            {
                result.Add(new UnixDomainSocketEndPoint("/replaced/in/test"), SocketType.Stream, ProtocolType.Unspecified);
            }

            return result;
        }

        // to avoid random names in TheoryData, we replace the path in test code:
        private static EndPoint RecreateUdsEndpoint(EndPoint endPoint)
        {
            if (endPoint is UnixDomainSocketEndPoint)
            {
                endPoint = new UnixDomainSocketEndPoint($"{Path.GetTempPath()}/{Guid.NewGuid()}");
            }
            return endPoint;
        }

        private static Socket ValidateSocket(Connection connection, SocketType? socketType = null, ProtocolType? protocolType = null, AddressFamily? addressFamily = null)
        {
            Assert.True(connection.ConnectionProperties.TryGet(out Socket socket));
            Assert.True(socket.Connected);
            if (addressFamily != null) Assert.Equal(addressFamily, socket.AddressFamily);
            if (socketType != null) Assert.Equal(socketType, socket.SocketType);
            if (protocolType != null) Assert.Equal(protocolType, socket.ProtocolType);
            return socket;
        }

        [Theory]
        [MemberData(nameof(GetConnectData))]
        public async Task Constructor3_ConnectAsync_Success_PropagatesConstructorArgumentsToSocket(EndPoint endPoint, SocketType socketType, ProtocolType protocolType)
        {
            endPoint = RecreateUdsEndpoint(endPoint);
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, endPoint, protocolType);
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(endPoint.AddressFamily, socketType, protocolType);
            using Connection connection = await factory.ConnectAsync(server.EndPoint);

            ValidateSocket(connection, socketType, protocolType, endPoint.AddressFamily);
        }

        [Fact]
        public async Task Constructor2_ConnectAsync_Success_CreatesIPv6DualModeSocket()
        {
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.IPv6Loopback);
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(SocketType.Stream, ProtocolType.Tcp);
            using Connection connection = await factory.ConnectAsync(server.EndPoint);

            Socket socket = ValidateSocket(connection, SocketType.Stream, ProtocolType.Tcp, AddressFamily.InterNetworkV6);
            Assert.True(socket.DualMode);
        }

        [Fact]
        public async Task ConnectAsync_Success_SocketNoDelayIsTrue()
        {
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.Loopback);
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(SocketType.Stream, ProtocolType.Tcp);
            using Connection connection = await factory.ConnectAsync(server.EndPoint);

            connection.ConnectionProperties.TryGet(out Socket socket);
            Assert.True(socket.NoDelay);
        }

        [Fact]
        public void ConnectAsync_NullEndpoint_ThrowsArgumentNullException()
        {
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(SocketType.Stream, ProtocolType.Tcp);
            Assert.ThrowsAsync<ArgumentNullException>(() => factory.ConnectAsync(null).AsTask());
        }

        // TODO: On OSX and Windows7 connection failures seem to fail with unexpected SocketErrors that are mapped to NetworkError.Unknown. This needs an investigation.
        // Related: https://github.com/dotnet/runtime/pull/40565
        public static bool PlatformHasReliableConnectionFailures => !PlatformDetection.IsOSX && !PlatformDetection.IsWindows7 && !PlatformDetection.IsFreeBSD;

        [ConditionalFact(nameof(PlatformHasReliableConnectionFailures))]
        public async Task ConnectAsync_WhenRefused_ThrowsNetworkException()
        {
            using Socket notListening = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            int port = notListening.BindToAnonymousPort(IPAddress.Loopback);
            var endPoint = new IPEndPoint(IPAddress.Loopback, port);

            using SocketsConnectionFactory factory = new SocketsConnectionFactory(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            NetworkException ex = await Assert.ThrowsAsync<NetworkException>(() => factory.ConnectAsync(endPoint).AsTask());
            Assert.Equal(NetworkError.ConnectionRefused, ex.NetworkError);
        }

        [OuterLoop] // TimedOut and HostNotFound is slow on Windows
        [ConditionalFact(nameof(PlatformHasReliableConnectionFailures))]
        public async Task ConnectAsync_WhenHostNotFound_ThrowsNetworkException()
        {
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Unassigned as per https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.txt
            int unusedPort = 8;
            DnsEndPoint endPoint = new DnsEndPoint(System.Net.Test.Common.Configuration.Sockets.InvalidHost, unusedPort);

            NetworkException ex = await Assert.ThrowsAsync<NetworkException>(() => factory.ConnectAsync(endPoint).AsTask());
            Assert.Equal(NetworkError.HostNotFound, ex.NetworkError);
        }

        [OuterLoop] // TimedOut and HostNotFound is slow on Windows
        [ConditionalFact(nameof(PlatformHasReliableConnectionFailures))]
        public async Task ConnectAsync_TimedOut_ThrowsNetworkException()
        {
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint doesNotExist = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 23);

            NetworkException ex = await Assert.ThrowsAsync<NetworkException>(() => factory.ConnectAsync(doesNotExist).AsTask());
            Assert.Equal(NetworkError.TimedOut, ex.NetworkError);
        }

        // On Windows, connection timeout takes 21 seconds. Abusing this behavior to test the cancellation logic
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] 
        public async Task ConnectAsync_WhenCancelled_ThrowsTaskCancelledException()
        {
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint doesNotExist = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 23);

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(100);

            OperationCanceledException ex = await Assert.ThrowsAsync<OperationCanceledException>(() => factory.ConnectAsync(doesNotExist, cancellationToken: cts.Token).AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);
        }

        [Fact]
        public async Task ConnectAsync_WhenCancelledBeforeInvocation_ThrowsTaskCancelledException()
        {
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint doesNotExist = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 23);

            CancellationToken cancellationToken = new CancellationToken(true);

            OperationCanceledException ex = await Assert.ThrowsAsync<OperationCanceledException>(() => factory.ConnectAsync(doesNotExist, cancellationToken: cancellationToken).AsTask());
            Assert.Equal(cancellationToken, ex.CancellationToken);
        }

        [Theory]
        [MemberData(nameof(GetConnectData))]
        public async Task Connection_Stream_ReadWrite_Success(EndPoint endPoint, SocketType socketType, ProtocolType protocolType)
        {
            endPoint = RecreateUdsEndpoint(endPoint);
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, endPoint, protocolType);
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(endPoint.AddressFamily, socketType, protocolType);

            using Connection connection = await factory.ConnectAsync(server.EndPoint);

            Stream stream = connection.Stream;

            byte[] sendData = { 1, 2, 3 };
            byte[] receiveData = new byte[sendData.Length];

            await stream.WriteAsync(sendData);
            await stream.FlushAsync();
            await stream.ReadAsync(receiveData);

            // The test server should echo the data:
            Assert.Equal(sendData, receiveData);
        }

        [Theory]
        [MemberData(nameof(GetConnectData))]
        public async Task Connection_EndpointsAreCorrect(EndPoint endPoint, SocketType socketType, ProtocolType protocolType)
        {
            endPoint = RecreateUdsEndpoint(endPoint);
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, endPoint, protocolType);
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(endPoint.AddressFamily, socketType, protocolType);
            using Connection connection = await factory.ConnectAsync(server.EndPoint);

            // Checking for .ToString() result, because UnixDomainSocketEndPoint equality doesn't seem to be implemented
            Assert.Equal(server.EndPoint.ToString(), connection.RemoteEndPoint.ToString()); 
            Assert.IsType(endPoint.GetType(), connection.LocalEndPoint);
        }

        [Theory]
        [MemberData(nameof(GetConnectData))]
        public async Task Connection_Pipe_ReadWrite_Success(EndPoint endPoint, SocketType socketType, ProtocolType protocolType)
        {
            endPoint = RecreateUdsEndpoint(endPoint);
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, endPoint, protocolType);
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(endPoint.AddressFamily, socketType, protocolType);


            using Connection connection = await factory.ConnectAsync(server.EndPoint);

            IDuplexPipe pipe = connection.Pipe;

            byte[] sendData = { 1, 2, 3 };
            using MemoryStream receiveTempStream = new MemoryStream();

            await pipe.Output.WriteAsync(sendData);
            ReadResult rr = await pipe.Input.ReadAsync();

            // The test server should echo the data:
            Assert.True(rr.Buffer.FirstSpan.SequenceEqual(sendData));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Connection_Dispose_ClosesSocket(bool disposeAsync, bool usePipe)
        {
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.Loopback);
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Connection connection = await factory.ConnectAsync(server.EndPoint);

            Stream stream = usePipe ? null : connection.Stream;
            if (usePipe) _ = connection.Pipe;
            connection.ConnectionProperties.TryGet(out Socket socket);

            if (disposeAsync) await connection.DisposeAsync();
            else connection.Dispose();

            Assert.False(socket.Connected);

            if (!usePipe)
            {
                // In this case we can also verify if the stream is disposed
                Assert.Throws<ObjectDisposedException>(() => stream.Write(new byte[1]));
            }
        }

        [Theory]
        [InlineData(true, ConnectionCloseMethod.GracefulShutdown)]
        [InlineData(true, ConnectionCloseMethod.Immediate)]
        [InlineData(true, ConnectionCloseMethod.Abort)]
        [InlineData(false, ConnectionCloseMethod.GracefulShutdown)]
        [InlineData(false, ConnectionCloseMethod.Immediate)]
        [InlineData(false, ConnectionCloseMethod.Abort)]
        public async Task Connection_CloseAsync_ClosesSocket(bool usePipe, ConnectionCloseMethod method)
        {
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.Loopback);
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Connection connection = await factory.ConnectAsync(server.EndPoint);

            Stream stream = null;
            if (usePipe)
            {
                _ = connection.Pipe;
            }
            else
            {
                stream = connection.Stream;
            }

            connection.ConnectionProperties.TryGet(out Socket socket);

            await connection.CloseAsync(method);

            Assert.Throws<ObjectDisposedException>(() => socket.Send(new byte[1]));

            if (!usePipe) // No way to observe the stream if we work with the pipe
            {
                Assert.Throws<ObjectDisposedException>(() => stream.Write(new byte[1]));
            }
        }

        // Test scenario based on:
        // https://devblogs.microsoft.com/dotnet/system-io-pipelines-high-performance-io-in-net/
        [Theory(Timeout = 60000)] // Give 1 minute to fail, in case of a hang
        [InlineData(30)]
        [InlineData(500)]
        [OuterLoop("Might run long")]
        public async Task Connection_Pipe_ReadWrite_Integration(int totalLines)
        {
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            using SocketTestServer echoServer = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.Loopback);
            
            Socket serverSocket = null;
            echoServer.Accepted += s => serverSocket = s;

            using Connection connection = await factory.ConnectAsync(echoServer.EndPoint);

            IDuplexPipe pipe = connection.Pipe;

            ConcurrentQueue<string> linesSent = new ConcurrentQueue<string>();
            Task writerTask = Task.Factory.StartNew(async () =>
            {
                byte[] endl = Encoding.ASCII.GetBytes("\n");
                StringBuilder expectedLine = new StringBuilder();

                for (int i = 0; i < totalLines; i++)
                {
                    int words = i % 10 + 1;
                    for (int j = 0; j < words; j++)
                    {
                        string word = Guid.NewGuid() + " ";
                        Encoding.ASCII.GetBytes(word, pipe.Output);
                        expectedLine.Append(word);
                    }
                    linesSent.Enqueue(expectedLine.ToString());
                    await pipe.Output.WriteAsync(endl);
                    expectedLine.Clear();
                }

                await pipe.Output.FlushAsync();

                // This will also trigger completion on the reader. TODO: Fix
                // await pipe.Output.CompleteAsync();
            }, TaskCreationOptions.LongRunning);

            // The test server should echo the data sent to it                      

            PipeReader reader = pipe.Input;

            int lineIndex = 0;

            void ProcessLine(ReadOnlySequence<byte> lineBuffer)
            {
                string line = Encoding.ASCII.GetString(lineBuffer);
                Assert.True(linesSent.TryDequeue(out string expectedLine));
                Assert.Equal(expectedLine, line);
                lineIndex++;

                // Received everything, shut down the server, so next read will complete:
                if (lineIndex == totalLines) serverSocket.Shutdown(SocketShutdown.Both);
            }

            while (true)
            {
                try
                {
                    ReadResult result = await reader.ReadAsync();

                    ReadOnlySequence<byte> buffer = result.Buffer;
                    SequencePosition? position = null;

                    // Stop reading if there's no more data coming
                    if (result.IsCompleted)
                    {
                        break;
                    }

                    do
                    {
                        // Look for a EOL in the buffer
                        position = buffer.PositionOf((byte)'\n');

                        if (position != null)
                        {
                            // Process the line
                            ProcessLine(buffer.Slice(0, position.Value));

                            // Skip the line + the \n character (basically position)
                            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                        }
                    }
                    while (position != null);

                    // Tell the PipeReader how much of the buffer we have consumed
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
                catch (SocketException)
                {
                    // terminate
                }

            }

            // Mark the PipeReader as complete
            await reader.CompleteAsync();
            await writerTask;

            // TODO: If this is done in the end of writerTask the socket stops working
            Assert.Equal(totalLines, lineIndex);
        }
    }
}
