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

        class CustomDuplexPipe : IDuplexPipe
        {
            private static readonly StreamPipeReaderOptions s_readerOpts = new StreamPipeReaderOptions(leaveOpen: true);
            private static readonly StreamPipeWriterOptions s_writerOpts = new StreamPipeWriterOptions(leaveOpen: true);

            public CustomDuplexPipe(Stream stream, CustomConnectionOptionsValues optionValues)
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
                return new CustomNetworkStream(socket, vals);
            }

            protected override IDuplexPipe CreatePipe(Socket socket, IConnectionProperties options)
            {
                options.TryGet(out CustomConnectionOptionsValues vals);
                return new CustomDuplexPipe(CreateStream(socket, options), vals);
            }
        }

        private readonly CustomFactory _factory = new CustomFactory();
        private readonly CustomConnectionOptions _options = new CustomConnectionOptions();
        private readonly IPEndPoint _endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 0);

        [Fact]
        public async Task DerivedFactory_CanShimSocket()
        {
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _endPoint);
            Connection connection = await _factory.ConnectAsync(server.EndPoint, _options);
            connection.ConnectionProperties.TryGet(out Socket socket);

            Assert.Equal(_options.Values.NoDelay, socket.NoDelay);
            Assert.Equal(_options.Values.DualMode, socket.DualMode);
        }

        [Fact]
        public async Task DerivedFactory_CanShimStream()
        {
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _endPoint);
            Connection connection = await _factory.ConnectAsync(server.EndPoint, _options);

            CustomNetworkStream stream = Assert.IsType<CustomNetworkStream>(connection.Stream);
            Assert.Same(_options.Values, stream.OptionValues);
        }

        [Fact]
        public async Task DerivedFactory_CanShimPipe()
        {
            using var server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _endPoint);
            Connection connection = await _factory.ConnectAsync(server.EndPoint, _options);

            CustomDuplexPipe pipe = Assert.IsType<CustomDuplexPipe>(connection.Pipe);
            Assert.Same(_options.Values, pipe.OptionValues);
        }
    }
}
