// Licensed to the .NET Foundation under one or more agreements.
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
        private static readonly IQuicImplProviderFactory s_factory = new T();

        public static QuicImplementationProvider ImplementationProvider { get; } = s_factory.GetProvider();
        public static bool IsSupported => ImplementationProvider.IsSupported;

        public static SslApplicationProtocol ApplicationProtocol { get; } = new SslApplicationProtocol("quictest");

        public X509Certificate2 ServerCertificate = System.Net.Test.Common.Configuration.Certificates.GetServerCertificate();

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

        private QuicListener CreateQuicListener(QuicListenerOptions options) => new QuicListener(ImplementationProvider, options);

        internal Task RunUnidirectionalClientServer(Func<QuicStream, Task> clientFunction, Func<QuicStream, Task> serverFunction, int iterations = 1, int millisecondsTimeout = 10_000)
            => RunClientServerStream(clientFunction, serverFunction, iterations, millisecondsTimeout, bidi: false);

        internal Task RunBidirectionalClientServer(Func<QuicStream, Task> clientFunction, Func<QuicStream, Task> serverFunction, int iterations = 1, int millisecondsTimeout = 10_000)
            => RunClientServerStream(clientFunction, serverFunction, iterations, millisecondsTimeout, bidi: true);

        private async Task RunClientServerStream(Func<QuicStream, Task> clientFunction, Func<QuicStream, Task> serverFunction, int iterations, int millisecondsTimeout, bool bidi)
        {
            const long ClientThrewAbortCode = 1234567890;
            const long ServerThrewAbortCode = 2345678901;

            await RunClientServer(
                async clientConnection =>
                {
                    await using QuicStream clientStream = bidi ? clientConnection.OpenBidirectionalStream() : clientConnection.OpenUnidirectionalStream();
                    try
                    {
                        await clientFunction(clientStream);
                    }
                    catch
                    {
                        try
                        {
                            // abort the stream to give the peer a chance to tear down.
                            clientStream.Abort(ClientThrewAbortCode);
                        }
                        catch(ObjectDisposedException)
                        {
                            // do nothing.
                        }

                        throw;
                    }
                },
                async serverConnection =>
                {
                    await using QuicStream serverStream = await serverConnection.AcceptStreamAsync();
                    try
                    {
                        await serverFunction(serverStream);
                    }
                    catch
                    {
                        try
                        {
                            // abort the stream to give the peer a chance to tear down.
                            serverStream.Abort(ServerThrewAbortCode);
                        }
                        catch (ObjectDisposedException)
                        {
                            // do nothing.
                        }
                        throw;
                    }
                }, iterations, millisecondsTimeout);
        }

        internal async Task RunClientServer(Func<QuicConnection, Task> clientFunction, Func<QuicConnection, Task> serverFunction, int iterations = 1, int millisecondsTimeout = 10_000)
        {
            using QuicListener listener = CreateQuicListener();

            using var serverFinished = new SemaphoreSlim(0);
            using var clientFinished = new SemaphoreSlim(0);

            for (int i = 0; i < iterations; ++i)
            {
                await new[]
                {
                    Task.Run(async () =>
                    {
                        using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
                        await serverFunction(serverConnection);
                        serverFinished.Release();
                        await clientFinished.WaitAsync();
                        await serverConnection.CloseAsync(0);
                    }),
                    Task.Run(async () =>
                    {
                        using QuicConnection clientConnection = CreateQuicConnection(listener.ListenEndPoint);
                        await clientConnection.ConnectAsync();
                        await clientFunction(clientConnection);
                        clientFinished.Release();
                        await serverFinished.WaitAsync();
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
