// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Connections;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
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
        public void DerivedFactory_CanShimPipe()
        {
            // TODO
        }
    }
}
