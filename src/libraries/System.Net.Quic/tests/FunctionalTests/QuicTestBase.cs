﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    public abstract class QuicTestBase<T>
        where T : IQuicImplProviderFactory, new()
    {
        private static readonly byte[] s_ping = Encoding.UTF8.GetBytes("PING");
        private static readonly byte[] s_pong = Encoding.UTF8.GetBytes("PONG");
        private static readonly IQuicImplProviderFactory s_factory = new T();

        public static QuicImplementationProvider ImplementationProvider { get; } = s_factory.GetProvider();
        public static bool IsSupported => ImplementationProvider.IsSupported;

        public static SslApplicationProtocol ApplicationProtocol { get; } = new SslApplicationProtocol("quictest");

        public X509Certificate2 ServerCertificate = System.Net.Test.Common.Configuration.Certificates.GetServerCertificate();
        public X509Certificate2 ClientCertificate = System.Net.Test.Common.Configuration.Certificates.GetClientCertificate();

        public bool RemoteCertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            Assert.Equal(ServerCertificate.GetCertHash(), certificate?.GetCertHash());
            return true;
        }

        public SslServerAuthenticationOptions GetSslServerAuthenticationOptions()
        {
            return new SslServerAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { ApplicationProtocol },
                ServerCertificate = ServerCertificate
            };
        }

        public SslClientAuthenticationOptions GetSslClientAuthenticationOptions()
        {
            return new SslClientAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { ApplicationProtocol },
                RemoteCertificateValidationCallback = RemoteCertificateValidationCallback
            };
        }

        internal QuicConnection CreateQuicConnection(IPEndPoint endpoint)
        {
            return new QuicConnection(ImplementationProvider, endpoint, GetSslClientAuthenticationOptions());
        }

        internal QuicListener CreateQuicListener(int maxUnidirectionalStreams = 100, int maxBidirectionalStreams = 100)
        {
            var options = new QuicListenerOptions()
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0),
                ServerAuthenticationOptions = GetSslServerAuthenticationOptions(),
                MaxUnidirectionalStreams = maxUnidirectionalStreams,
                MaxBidirectionalStreams = maxBidirectionalStreams
            };
            return CreateQuicListener(options);
        }

        internal QuicListener CreateQuicListener(IPEndPoint endpoint)
        {
            var options = new QuicListenerOptions()
            {
                ListenEndPoint = endpoint,
                ServerAuthenticationOptions = GetSslServerAuthenticationOptions()
            };
            return CreateQuicListener(options);
        }

        internal async Task PingPong(QuicConnection client, QuicConnection server)
        {
            using QuicStream clientStream = client.OpenBidirectionalStream();
            ValueTask t = clientStream.WriteAsync(s_ping);
            using QuicStream serverStream = await server.AcceptStreamAsync();

            byte[] buffer = new byte[s_ping.Length];
            int remains = s_ping.Length;
            while (remains > 0)
            {
                int readLength = await serverStream.ReadAsync(buffer, buffer.Length - remains, remains);
                Assert.True(readLength > 0);
                remains -= readLength;
            }
            Assert.Equal(s_ping, buffer);
            await t;

            t = serverStream.WriteAsync(s_pong);
            remains = s_pong.Length;
            while (remains > 0)
            {
                int readLength = await clientStream.ReadAsync(buffer, buffer.Length - remains, remains);
                Assert.True(readLength > 0);
                remains -= readLength;
            }

            Assert.Equal(s_pong, buffer);
            await t;
        }

        private QuicListener CreateQuicListener(QuicListenerOptions options) => new QuicListener(ImplementationProvider, options);

        internal async Task RunClientServer(Func<QuicConnection, Task> clientFunction, Func<QuicConnection, Task> serverFunction, int iterations = 1, int millisecondsTimeout = 10_000)
        {
            using QuicListener listener = CreateQuicListener();

            var serverFinished = new ManualResetEventSlim();
            var clientFinished = new ManualResetEventSlim();

            for (int i = 0; i < iterations; ++i)
            {
                serverFinished.Reset();
                clientFinished.Reset();

                await new[]
                {
                    Task.Run(async () =>
                    {
                        using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
                        await serverFunction(serverConnection);
                        serverFinished.Set();
                        clientFinished.Wait();
                        await serverConnection.CloseAsync(0);
                    }),
                    Task.Run(async () =>
                    {
                        using QuicConnection clientConnection = CreateQuicConnection(listener.ListenEndPoint);
                        await clientConnection.ConnectAsync();
                        await clientFunction(clientConnection);
                        clientFinished.Set();
                        serverFinished.Wait();
                        await clientConnection.CloseAsync(0);
                    })
                }.WhenAllOrAnyFailed(millisecondsTimeout);
            }
        }

        internal static async Task<int> ReadAll(QuicStream stream, byte[] buffer)
        {
            Memory<byte> memory = buffer;
            int bytesRead = 0;
            while (true)
            {
                int res = await stream.ReadAsync(memory);
                if (res == 0)
                {
                    break;
                }
                bytesRead += res;
                memory = memory[res..];
            }

            return bytesRead;
        }

        internal static async Task<int> WriteForever(QuicStream stream)
        {
            Memory<byte> buffer = new byte[] { 123 };
            while (true)
            {
                await stream.WriteAsync(buffer);
            }
        }
    }

    public interface IQuicImplProviderFactory
    {
        QuicImplementationProvider GetProvider();
    }

    public sealed class MsQuicProviderFactory : IQuicImplProviderFactory
    {
        public QuicImplementationProvider GetProvider() => QuicImplementationProviders.MsQuic;
    }

    public sealed class MockProviderFactory : IQuicImplProviderFactory
    {
        public QuicImplementationProvider GetProvider() => QuicImplementationProviders.Mock;
    }
}
