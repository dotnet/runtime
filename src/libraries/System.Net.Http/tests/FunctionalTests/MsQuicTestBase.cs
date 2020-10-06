// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Security;
using System.Threading.Tasks;

namespace System.Net.Quic.Tests
{
    public class MsQuicTestBase
    {
        public SslServerAuthenticationOptions GetSslServerAuthenticationOptions()
        {
            return new SslServerAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") }
            };
        }

        public SslClientAuthenticationOptions GetSslClientAuthenticationOptions()
        {
            return new SslClientAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") }
            };
        }

        internal QuicConnection CreateQuicConnection(IPEndPoint endpoint)
        {
            return new QuicConnection(QuicImplementationProviders.MsQuic, endpoint, GetSslClientAuthenticationOptions());
        }

        internal QuicListener CreateQuicListener()
        {
            return CreateQuicListener(new IPEndPoint(IPAddress.Loopback, 0));
        }

        internal QuicListener CreateQuicListener(IPEndPoint endpoint)
        {
            QuicListener listener = new QuicListener(QuicImplementationProviders.MsQuic, endpoint, GetSslServerAuthenticationOptions());
            listener.Start();
            return listener;
        }

        internal async Task RunClientServer(Func<QuicConnection, Task> clientFunction, Func<QuicConnection, Task> serverFunction, int millisecondsTimeout = 10_000)
        {
            using QuicListener listener = CreateQuicListener();

            await new[]
            {
                Task.Run(async () =>
                {
                    using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
                    await serverFunction(serverConnection);
                }),
                Task.Run(async () =>
                {
                    using QuicConnection clientConnection = CreateQuicConnection(listener.ListenEndPoint);
                    await clientConnection.ConnectAsync();
                    await clientFunction(clientConnection);
                })
            }.WhenAllOrAnyFailed(millisecondsTimeout);
        }
    }
}
