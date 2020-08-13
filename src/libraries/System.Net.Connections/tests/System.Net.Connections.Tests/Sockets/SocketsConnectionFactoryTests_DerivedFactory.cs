// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Net.Connections;
using System.Net.Sockets;
using System.Net.Sockets.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Connections.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime))]
    public class SocketsConnectionFactoryTests_DerivedFactory
    {
        private class CustomConnectionOptionsValues
        {
            public bool NoDelay { get; set; }

            public bool DualMode { get; set; }
        }

        private class CustomConnectionOptions : IConnectionProperties
        {
            public CustomConnectionOptionsValues Values { get; } = new CustomConnectionOptionsValues();

            public CustomConnectionOptions()
            {
            }

            public bool TryGet(Type propertyKey, [NotNullWhen(true)] out object property)
            {
                if (propertyKey == typeof(CustomConnectionOptionsValues))
                {
                    property = Values;
                    return true;
                }

                property = null;
                return false;
            }
        }

        class CustomNetworkStream : NetworkStream
        {
            public CustomConnectionOptionsValues OptionValues { get; }

            public CustomNetworkStream(Socket socket, CustomConnectionOptionsValues options) : base(socket, ownsSocket: true)
            {
                OptionValues = options;
            }
        }

        class CustomPipe : IDuplexPipe
        {
            private static readonly StreamPipeReaderOptions s_readerOpts = new StreamPipeReaderOptions(leaveOpen: true);
            private static readonly StreamPipeWriterOptions s_writerOpts = new StreamPipeWriterOptions(leaveOpen: true);

            public CustomPipe(Stream stream, CustomConnectionOptionsValues optionValues)
            {
                Input = PipeReader.Create(stream, s_readerOpts);
                Output = PipeWriter.Create(stream, s_writerOpts);
                OptionValues = optionValues;
            }

            public PipeReader Input { get; }
            public PipeWriter Output { get; }
            public CustomConnectionOptionsValues OptionValues { get; }
        }

        private sealed class CustomFactory : SocketsConnectionFactory
        {
            public bool UseCustomPipe { get; set; } = true;

            public event Action<NetworkStream> OnCreateNetworkStream;

            public CustomFactory() : base(SocketType.Stream, ProtocolType.Tcp)
            {
            }

            protected override Socket CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, EndPoint endPoint, IConnectionProperties options)
            {
                Socket socket = new Socket(addressFamily, socketType, protocolType);

                if (options.TryGet(out CustomConnectionOptionsValues vals))
                {
                    socket.NoDelay = vals.NoDelay;
                    socket.DualMode = vals.DualMode;
                }

                return socket;
            }

            protected override Stream CreateStream(Socket socket, IConnectionProperties options)
            {
                options.TryGet(out CustomConnectionOptionsValues vals);
                var stream = new CustomNetworkStream(socket, vals);
                OnCreateNetworkStream?.Invoke(stream);
                return stream;
            }

            protected override IDuplexPipe CreatePipe(Socket socket, IConnectionProperties options)
            {
                if (UseCustomPipe)
                {
                    options.TryGet(out CustomConnectionOptionsValues vals);
                    return new CustomPipe(CreateStream(socket, options), vals);
                }
                else
                {
                    return base.CreatePipe(socket, options);
                }
            }
        }

        private readonly CustomFactory _factory = new CustomFactory();
        private readonly CustomConnectionOptions _options = new CustomConnectionOptions();
        private readonly IPEndPoint _endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 0);

        [Fact]
        public async Task DerivedFactory_CanShimSocket()
        {
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _endPoint);
            using Connection connection = await _factory.ConnectAsync(server.EndPoint, _options);
            connection.ConnectionProperties.TryGet(out Socket socket);

            Assert.Equal(_options.Values.NoDelay, socket.NoDelay);
            Assert.Equal(_options.Values.DualMode, socket.DualMode);
        }

        [Fact]
        public async Task DerivedFactory_CanShimStream()
        {
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _endPoint);
            using Connection connection = await _factory.ConnectAsync(server.EndPoint, _options);

            CustomNetworkStream stream = Assert.IsType<CustomNetworkStream>(connection.Stream);
            Assert.Same(_options.Values, stream.OptionValues);
        }

        [Fact]
        public async Task DerivedFactory_CanShimPipe()
        {
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _endPoint);
            using Connection connection = await _factory.ConnectAsync(server.EndPoint, _options);

            CustomPipe pipe = Assert.IsType<CustomPipe>(connection.Pipe);
            Assert.Same(_options.Values, pipe.OptionValues);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UsePipe_ConnectionClose_ShouldDisposeSocket(bool useCustomPipe)
        {
            _factory.UseCustomPipe = useCustomPipe;
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _endPoint);
            NetworkStream stream = null;
            _factory.OnCreateNetworkStream += ns => stream = ns;

            Connection connection = await _factory.ConnectAsync(server.EndPoint, _options);
            connection.ConnectionProperties.TryGet(out Socket socket);

            _ = connection.Pipe;
            await connection.CloseAsync();

            Assert.Throws<ObjectDisposedException>(() => socket.Send(new byte[1]));

            // In case of a custom pipe that was created from a stream, we do not guarantee that disposal of the stream, only the socket:
            if (!useCustomPipe)
            {
                Assert.Throws<ObjectDisposedException>(() => stream.Write(new byte[1]));
            }
        }
    }
}
