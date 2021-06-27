// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Tests;
using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    public sealed class MockQuicStreamConformanceTests : QuicStreamConformanceTests
    {
        protected override QuicImplementationProvider Provider => QuicImplementationProviders.Mock;
    }

    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(QuicTestBase<MsQuicProviderFactory>.IsSupported))]
    public sealed class MsQuicQuicStreamConformanceTests : QuicStreamConformanceTests
    {
        protected override QuicImplementationProvider Provider => QuicImplementationProviders.MsQuic;
        protected override bool UsableAfterCanceledReads => false;
        protected override bool BlocksOnZeroByteReads => true;

        // TODO: new additions, find out the actual reason for hanging
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public override Task ReadAsync_DuringReadAsync_ThrowsIfUnsupported() => base.ReadAsync_DuringReadAsync_ThrowsIfUnsupported();
    }

    public abstract class QuicStreamConformanceTests : ConnectedStreamConformanceTests
    {
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

        protected abstract QuicImplementationProvider Provider { get; }

        protected override async Task<StreamPair> CreateConnectedStreamsAsync()
        {
            QuicImplementationProvider provider = Provider;
            var listener = new QuicListener(
                provider,
                new IPEndPoint(IPAddress.Loopback, 0),
                GetSslServerAuthenticationOptions());

            byte[] buffer = new byte[1] { 42 };
            QuicConnection connection1 = null, connection2 = null;
            QuicStream stream1 = null, stream2 = null;
            await WhenAllOrAnyFailed(
                Task.Run(async () =>
                {
                    connection1 = await listener.AcceptConnectionAsync();
                    stream1 = await connection1.AcceptStreamAsync();
                    Assert.Equal(1, await stream1.ReadAsync(buffer));
                }),
                Task.Run(async () =>
                {
                    connection2 = new QuicConnection(
                        provider,
                        listener.ListenEndPoint,
                        GetSslClientAuthenticationOptions());
                    await connection2.ConnectAsync();
                    stream2 = connection2.OpenBidirectionalStream();
                    // OpenBidirectionalStream only allocates ID. We will force stream opening
                    // by Writing there and receiving data on the other side.
                    await stream2.WriteAsync(buffer);
                }));

            var result = new StreamPairWithOtherDisposables(stream1, stream2);
            result.Disposables.Add(connection1);
            result.Disposables.Add(connection2);
            result.Disposables.Add(listener);

            return result;
        }

        private sealed class StreamPairWithOtherDisposables : StreamPair
        {
            public readonly List<IDisposable> Disposables = new List<IDisposable>();

            public StreamPairWithOtherDisposables(Stream stream1, Stream stream2) : base(stream1, stream2) { }

            public override void Dispose()
            {
                base.Dispose();
                Disposables.ForEach(d => d.Dispose());
            }
        }
    }
}
