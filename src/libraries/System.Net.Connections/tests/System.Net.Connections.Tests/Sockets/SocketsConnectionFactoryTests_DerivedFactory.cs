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
    }
}
