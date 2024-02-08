// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Tests;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    [Collection(nameof(DisableParallelization))]
    [Collection(nameof(QuicCountersListener))]
    [ConditionalClass(typeof(QuicTestBase), nameof(QuicTestBase.IsSupported), nameof(QuicTestBase.IsNotArm32CoreClrStressTest))]
    public sealed class QuicStreamConformanceTests : ConnectedStreamConformanceTests
    {
        protected override bool UsableAfterCanceledReads => false;
        protected override bool BlocksOnZeroByteReads => true;
        protected override bool CanTimeout => true;

        public readonly X509Certificate2 ServerCertificate = System.Net.Test.Common.Configuration.Certificates.GetServerCertificate();
        public ITestOutputHelper _output;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerCertificate.Dispose();
            }
            base.Dispose(disposing);
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
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") },
                ServerCertificate = ServerCertificate
            };
        }

        public SslClientAuthenticationOptions GetSslClientAuthenticationOptions()
        {
            return new SslClientAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") },
                RemoteCertificateValidationCallback = RemoteCertificateValidationCallback
            };
        }

        protected override async Task<StreamPair> CreateConnectedStreamsAsync()
        {
            var listener = await QuicListener.ListenAsync(new QuicListenerOptions()
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0),
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") },
                ConnectionOptionsCallback = (_, _, _) => ValueTask.FromResult(new QuicServerConnectionOptions()
                {
                    DefaultStreamErrorCode = QuicTestBase.DefaultStreamErrorCodeServer,
                    DefaultCloseErrorCode = QuicTestBase.DefaultCloseErrorCodeServer,
                    ServerAuthenticationOptions = GetSslServerAuthenticationOptions()
                })
            });

            byte[] buffer = new byte[1] { 42 };
            QuicConnection connection1 = null, connection2 = null;
            QuicStream stream1 = null, stream2 = null;
            try
            {
                await WhenAllOrAnyFailed(
                    Task.Run(async () =>
                    {
                        connection1 = await listener.AcceptConnectionAsync();
                        stream1 = await connection1.AcceptInboundStreamAsync();
                        Assert.Equal(1, await stream1.ReadAsync(buffer));
                    }),
                    Task.Run(async () =>
                    {
                        try
                        {
                            connection2 = await QuicConnection.ConnectAsync(new QuicClientConnectionOptions()
                            {
                                DefaultStreamErrorCode = QuicTestBase.DefaultStreamErrorCodeClient,
                                DefaultCloseErrorCode = QuicTestBase.DefaultCloseErrorCodeClient,
                                RemoteEndPoint = listener.LocalEndPoint,
                                ClientAuthenticationOptions = GetSslClientAuthenticationOptions()
                            });
                            stream2 = await connection2.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                            // OpenBidirectionalStream only allocates ID. We will force stream opening
                            // by Writing there and receiving data on the other side.
                            await stream2.WriteAsync(buffer);
                        }
                        catch (Exception ex)
                        {
                            _output?.WriteLine($"Failed to connect: {ex.Message}");
                            throw;
                        }
                    }));

                // No need to keep the listener once we have connected connection and streams
                await listener.DisposeAsync();

                var result = new StreamPairWithOtherDisposables(stream1, stream2);
                result.Disposables.Add(connection1);
                result.Disposables.Add(connection2);

                return result;
            }
            catch
            {
                if (stream1 is not null)
                {
                    await stream1.DisposeAsync();
                }
                if (stream2 is not null)
                {
                    await stream2.DisposeAsync();
                }
                if (connection1 is not null)
                {
                    await connection1.DisposeAsync();
                }
                if (connection2 is not null)
                {
                    await connection2.DisposeAsync();
                }
                throw;
            }
        }

        private sealed class StreamPairWithOtherDisposables : StreamPair
        {
            public readonly List<IAsyncDisposable> Disposables = new List<IAsyncDisposable>();

            public StreamPairWithOtherDisposables(Stream stream1, Stream stream2) : base(stream1, stream2) { }

            public override void Dispose()
            {
                base.Dispose();
                foreach (IAsyncDisposable disposable in Disposables)
                {
                    disposable.DisposeAsync().GetAwaiter().GetResult();
                }
            }
        }
    }
}
