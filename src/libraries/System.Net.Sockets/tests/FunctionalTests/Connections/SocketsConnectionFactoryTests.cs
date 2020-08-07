// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Pipelines;
using System.Net.Connections;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SocketsConnectionFactoryTests
    {
        public static readonly TheoryData<EndPoint, SocketType, ProtocolType> ConnectData = new TheoryData<EndPoint, SocketType, ProtocolType>()
        {
            { new IPEndPoint(IPAddress.Loopback, 0), SocketType.Stream, ProtocolType.Tcp },
            { new IPEndPoint(IPAddress.IPv6Loopback, 0), SocketType.Stream, ProtocolType.Tcp },
            { new UnixDomainSocketEndPoint("/replaced/in/test"), SocketType.Stream, ProtocolType.Unspecified }
        };

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
        [MemberData(nameof(ConnectData))]
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

        [Fact]
        public async Task ConnectAsync_WhenRefused_ThrowsNetworkException()
        {
            using Socket notListening = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            int port = notListening.BindToAnonymousPort(IPAddress.Loopback);
            var endPoint = new IPEndPoint(IPAddress.Loopback, port);

            using SocketsConnectionFactory factory = new SocketsConnectionFactory(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            NetworkException ex = await Assert.ThrowsAsync<NetworkException>(() => factory.ConnectAsync(endPoint).AsTask());
            Assert.Equal(NetworkError.ConnectionRefused, ex.NetworkError);
        }

        // On Windows, failing connections take > 1 sec when the destination address is not found,
        // making it easy to test the cancellation logic
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] 
        public async Task ConnectAsync_WhenCancelled_ThrowsTaskCancelledException()
        {
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint doesNotExist = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 23);

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(100);

            await Assert.ThrowsAsync<TaskCanceledException>(() => factory.ConnectAsync(doesNotExist, cancellationToken: cts.Token).AsTask());
        }

        [Theory]
        [MemberData(nameof(ConnectData))]
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
        [MemberData(nameof(ConnectData))]
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
        [MemberData(nameof(ConnectData))]
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
        [InlineData(false)]
        [InlineData(true)]
        public async Task Connection_Dispose_ClosesSocket(bool disposeAsync)
        {
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.Loopback);
            using SocketsConnectionFactory factory = new SocketsConnectionFactory(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Connection connection = await factory.ConnectAsync(server.EndPoint);
            Stream stream = connection.Stream;
            connection.ConnectionProperties.TryGet(out Socket socket);

            if (disposeAsync) await connection.DisposeAsync();
            else connection.Dispose();

            Assert.False(socket.Connected);
            Assert.Throws<ObjectDisposedException>(() => stream.Write(new byte[1]));
        }
    }
}
