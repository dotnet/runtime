// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Diagnostics.Tracing;
using System.Net.Sockets;

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

        public static bool IsMockProvider => typeof(T) == typeof(MockProviderFactory);
        public static bool IsMsQuicProvider => typeof(T) == typeof(MsQuicProviderFactory);

        public static SslApplicationProtocol ApplicationProtocol { get; } = new SslApplicationProtocol("quictest");

        public X509Certificate2 ServerCertificate = System.Net.Test.Common.Configuration.Certificates.GetServerCertificate();
        public X509Certificate2 ClientCertificate = System.Net.Test.Common.Configuration.Certificates.GetClientCertificate();

        public ITestOutputHelper _output;
        public const int PassingTestTimeoutMilliseconds = 4 * 60 * 1000;
        public static TimeSpan PassingTestTimeout => TimeSpan.FromMilliseconds(PassingTestTimeoutMilliseconds);

        public QuicTestBase(ITestOutputHelper output)
        {
            _output = output;
        }
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
                RemoteCertificateValidationCallback = RemoteCertificateValidationCallback,
                TargetHost = "localhost"
            };
        }

        public QuicClientConnectionOptions CreateQuicClientOptions()
        {
            return new QuicClientConnectionOptions()
            {
                ClientAuthenticationOptions = GetSslClientAuthenticationOptions()
            };
        }

        internal QuicConnection CreateQuicConnection(IPEndPoint endpoint)
        {
            return new QuicConnection(ImplementationProvider, endpoint, GetSslClientAuthenticationOptions());
        }

        internal QuicConnection CreateQuicConnection(QuicClientConnectionOptions clientOptions)
        {
            return new QuicConnection(ImplementationProvider, clientOptions);
        }

        internal QuicListenerOptions CreateQuicListenerOptions()
        {
            return new QuicListenerOptions()
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0),
                ServerAuthenticationOptions = GetSslServerAuthenticationOptions()
            };
        }

        internal QuicListener CreateQuicListener(int maxUnidirectionalStreams = 100, int maxBidirectionalStreams = 100)
        {
            var options = CreateQuicListenerOptions();
            options.MaxUnidirectionalStreams = maxUnidirectionalStreams;
            options.MaxBidirectionalStreams = maxBidirectionalStreams;

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

        internal QuicListener CreateQuicListener(QuicListenerOptions options) => new QuicListener(ImplementationProvider, options);

        internal Task<(QuicConnection, QuicConnection)> CreateConnectedQuicConnection(QuicListener listener) => CreateConnectedQuicConnection(null, listener);
        internal async Task<(QuicConnection, QuicConnection)> CreateConnectedQuicConnection(QuicClientConnectionOptions? clientOptions, QuicListenerOptions listenerOptions)
        {
            using (QuicListener listener = CreateQuicListener(listenerOptions))
            {
                clientOptions ??= new QuicClientConnectionOptions()
                {
                    ClientAuthenticationOptions = GetSslClientAuthenticationOptions()
                };
                clientOptions.RemoteEndPoint = listener.ListenEndPoint;
                return await CreateConnectedQuicConnection(clientOptions, listener);
            }
        }

        internal async Task<(QuicConnection, QuicConnection)> CreateConnectedQuicConnection(QuicClientConnectionOptions? clientOptions = null, QuicListener? listener = null)
        {
            int retry = 3;
            int delay = 25;
            bool disposeListener = false;

            if (listener == null)
            {
                listener = CreateQuicListener();
                disposeListener = true;
            }

            clientOptions ??= CreateQuicClientOptions();
            if (clientOptions.RemoteEndPoint == null)
            {
                clientOptions.RemoteEndPoint = listener.ListenEndPoint;
            }

            QuicConnection clientConnection = null;
            ValueTask<QuicConnection> serverTask = listener.AcceptConnectionAsync();
            while (retry > 0)
            {
                clientConnection = CreateQuicConnection(clientOptions);
                retry--;
                try
                {
                    await clientConnection.ConnectAsync().ConfigureAwait(false);
                    break;
                }
                catch (QuicException ex) when (ex.HResult == (int)SocketError.ConnectionRefused)
                {
                    _output.WriteLine($"ConnectAsync to {clientConnection.RemoteEndPoint} failed with {ex.Message}");
                    await Task.Delay(delay);
                    delay *= 2;

                    if (retry == 0)
                    {
                        Debug.Fail($"ConnectAsync to {clientConnection.RemoteEndPoint} failed with {ex.Message}");
                        throw ex;
                    }
                }
            }

            QuicConnection serverConnection = await serverTask.ConfigureAwait(false);
            if (disposeListener)
            {
                listener.Dispose();
            }

            Assert.True(serverConnection.Connected);
            Assert.True(clientConnection.Connected);

            return (clientConnection, serverTask.Result);
        }

        internal async Task PingPong(QuicConnection client, QuicConnection server)
        {
            using QuicStream clientStream = await client.OpenBidirectionalStreamAsync();
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

        internal async Task RunClientServer(Func<QuicConnection, Task> clientFunction, Func<QuicConnection, Task> serverFunction, int iterations = 1, int millisecondsTimeout = PassingTestTimeoutMilliseconds, QuicListenerOptions listenerOptions = null)
        {
            const long ClientCloseErrorCode = 11111;
            const long ServerCloseErrorCode = 22222;

            using QuicListener listener = CreateQuicListener(listenerOptions ?? CreateQuicListenerOptions());

            using var serverFinished = new SemaphoreSlim(0);
            using var clientFinished = new SemaphoreSlim(0);

            for (int i = 0; i < iterations; ++i)
            {
                (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(listener);
                using (clientConnection)
                using (serverConnection)
                {
                    await new[]
                    {
                        Task.Run(async () =>
                        {
                            await serverFunction(serverConnection);
                            serverFinished.Release();
                            await clientFinished.WaitAsync();
                        }),
                        Task.Run(async () =>
                        {
                            await clientFunction(clientConnection);
                            clientFinished.Release();
                            await serverFinished.WaitAsync();
                        })
                    }.WhenAllOrAnyFailed(millisecondsTimeout);
                    await serverConnection.CloseAsync(ServerCloseErrorCode);
                    await clientConnection.CloseAsync(ClientCloseErrorCode);
                }
            }
        }

        internal async Task RunStreamClientServer(Func<QuicStream, Task> clientFunction, Func<QuicStream, Task> serverFunction, bool bidi, int iterations, int millisecondsTimeout)
        {
            byte[] buffer = new byte[1] { 42 };

            await RunClientServer(
                clientFunction: async connection =>
                {
                    await using QuicStream stream = bidi ? await connection.OpenBidirectionalStreamAsync() : await connection.OpenUnidirectionalStreamAsync();
                    // Open(Bi|Uni)directionalStream only allocates ID. We will force stream opening
                    // by Writing there and receiving data on the other side.
                    await stream.WriteAsync(buffer);

                    await clientFunction(stream);

                    stream.Shutdown();
                    await stream.ShutdownCompleted();
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptStreamAsync();
                    Assert.Equal(1, await stream.ReadAsync(buffer));

                    await serverFunction(stream);

                    stream.Shutdown();
                    await stream.ShutdownCompleted();
                },
                iterations,
                millisecondsTimeout
            );
        }

        internal Task RunBidirectionalClientServer(Func<QuicStream, Task> clientFunction, Func<QuicStream, Task> serverFunction, int iterations = 1, int millisecondsTimeout = PassingTestTimeoutMilliseconds)
            => RunStreamClientServer(clientFunction, serverFunction, bidi: true, iterations, millisecondsTimeout);

        internal Task RunUnirectionalClientServer(Func<QuicStream, Task> clientFunction, Func<QuicStream, Task> serverFunction, int iterations = 1, int millisecondsTimeout = PassingTestTimeoutMilliseconds)
            => RunStreamClientServer(clientFunction, serverFunction, bidi: false, iterations, millisecondsTimeout);

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

        internal static async Task<int> WriteForever(QuicStream stream, int size = 1)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                while (true)
                {
                    await stream.WriteAsync(buffer);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
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
