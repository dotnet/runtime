// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Security;
using System.Threading.Tasks;
using System.Net.Quic.Implementations;
using Xunit;
using System.Threading;
using System.Text;

namespace System.Net.Quic.Tests
{
    public abstract class QuicTestBase<T>
        where T : IQuicImplProviderFactory, new()
    {
        private static readonly IQuicImplProviderFactory s_factory = new T();

        public static QuicImplementationProvider ImplementationProvider { get; } = s_factory.GetProvider();
        public static bool IsSupported => ImplementationProvider.IsSupported;

        public static SslApplicationProtocol ApplicationProtocol { get; } = new SslApplicationProtocol("quictest");

        public SslServerAuthenticationOptions GetSslServerAuthenticationOptions()
        {
            return new SslServerAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { ApplicationProtocol },
                ServerCertificate = System.Net.Test.Common.Configuration.Certificates.GetServerCertificate()
            };
        }

        public SslClientAuthenticationOptions GetSslClientAuthenticationOptions()
        {
            return new SslClientAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { ApplicationProtocol },
                RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => { return true; }
            };
        }

        internal QuicConnection CreateQuicConnection(IPEndPoint endpoint)
        {
            return new QuicConnection(ImplementationProvider, endpoint, GetSslClientAuthenticationOptions());
        }

        internal QuicListener CreateQuicListener()
        {
            return CreateQuicListener(new IPEndPoint(IPAddress.Loopback, 0));
        }

        internal QuicListener CreateQuicListener(IPEndPoint endpoint)
        {
            return new QuicListener(ImplementationProvider, endpoint, GetSslServerAuthenticationOptions());
        }

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

        internal static void AssertArrayEqual(byte[] expected, byte[] actual)
        {
            for (int i = 0; i < expected.Length; ++i)
            {
                if (expected[i] == actual[i])
                {
                    continue;
                }

                var message = $"Wrong data starting from idx={i}\n" +
                    $"Expected: {ToStringAroundIndex(expected, i)}\n" +
                    $"Actual:   {ToStringAroundIndex(actual, i)}";

                Assert.True(expected[i] == actual[i], message);
            }
        }

        private static string ToStringAroundIndex(byte[] arr, int idx, int dl = 3, int dr = 7)
        {
            var sb = new StringBuilder(idx - (dl+1) >= 0 ? "[..., " : "[");

            for (int i = idx - dl; i <= idx + dr; ++i)
            {
                if (i >= 0 && i < arr.Length)
                {
                    sb.Append($"{arr[i]}, ");
                }
            }

            sb.Append(idx + (dr+1) < arr.Length ? "...]" : "]");

            return sb.ToString();
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
