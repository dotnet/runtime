// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamDisposeTest
    {
        [Fact]
        public async Task DisposeAsync_NotConnected_ClosesStream()
        {
            bool disposed = false;
            var stream = new SslStream(new DelegateStream(disposeFunc: _ => disposed = true, canReadFunc: () => true, canWriteFunc: () => true), false, delegate { return true; });

            Assert.False(disposed);
            await stream.DisposeAsync();
            Assert.True(disposed);
        }

        [Fact]
        public async Task DisposeAsync_Connected_ClosesStream()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            var trackingStream1 = new CallTrackingStream(stream1);
            var trackingStream2 = new CallTrackingStream(stream2);

            var clientStream = new SslStream(trackingStream1, false, delegate { return true; });
            var serverStream = new SslStream(trackingStream2, false, delegate { return true; });

            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    clientStream.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false)),
                    serverStream.AuthenticateAsServerAsync(certificate));
            }

            Assert.Equal(0, trackingStream1.TimesCalled(nameof(Stream.DisposeAsync)));
            await clientStream.DisposeAsync();
            Assert.NotEqual(0, trackingStream1.TimesCalled(nameof(Stream.DisposeAsync)));

            Assert.Equal(0, trackingStream2.TimesCalled(nameof(Stream.DisposeAsync)));
            await serverStream.DisposeAsync();
            Assert.NotEqual(0, trackingStream2.TimesCalled(nameof(Stream.DisposeAsync)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Dispose_PendingReadAsync_ThrowsODE(bool bufferedRead)
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TestConfiguration.PassingTestTimeout);

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams(leaveInnerStreamOpen: true);
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate())
            {
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                };
                clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions()
                {
                    ServerCertificate = serverCertificate,
                };
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions, default),
                                server.AuthenticateAsServerAsync(serverOptions, default));

                await TestHelper.PingPong(client, server, cts.Token);

                await server.WriteAsync("PINGPONG"u8.ToArray(), cts.Token);
                var readBuffer = new byte[1024];

                Task<int>? task = null;
                if (bufferedRead)
                {
                    // This will read everything into internal buffer. Following ReadAsync will not need IO.
                    await client.ReadAsync(readBuffer, 0, 1, cts.Token);
                    // Try to read the rest
                    task = client.ReadAsync(readBuffer, 0, 3, cts.Token);
                    client.Dispose();
                    int readLength = await task.ConfigureAwait(false);
                    Assert.Equal(3, readLength);
                }
                else
                {
                    client.Dispose();
                }

                await Assert.ThrowsAnyAsync<ObjectDisposedException>(() => client.ReadAsync(readBuffer, cts.Token).AsTask());
            }
        }
    }
}
