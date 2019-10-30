// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public sealed class SslStreamStreamToStreamTest_MemoryAsync : SslStreamStreamToStreamTest_CancelableReadWriteAsync
    {
        protected override async Task DoHandshake(SslStream clientSslStream, SslStream serverSslStream, X509Certificate serverCertificate = null, X509Certificate clientCertificate = null)
        {
            X509CertificateCollection clientCerts = clientCertificate != null ? new X509CertificateCollection() { clientCertificate } : null;
            await WithServerCertificate(serverCertificate, async(certificate, name) =>
            {
                Task t1 = clientSslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions() { TargetHost = name, ClientCertificates = clientCerts }, CancellationToken.None);
                Task t2 = serverSslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions() { ServerCertificate = certificate, ClientCertificateRequired = clientCertificate != null }, CancellationToken.None);
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            });
        }

        protected override Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

        protected override Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();

        [Fact]
        public async Task Authenticate_Precanceled_ThrowsOperationCanceledException()
        {
            var network = new VirtualNetwork();
            using (var clientSslStream = new SslStream(new VirtualNetworkStream(network, isServer: false), false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(new VirtualNetworkStream(network, isServer: true)))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientSslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions() { TargetHost = certificate.GetNameInfo(X509NameType.SimpleName, false) }, new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverSslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions() { ServerCertificate = certificate }, new CancellationToken(true)));
            }
        }

        [Fact]
        public async Task AuthenticateAsClientAsync_VirtualNetwork_CanceledAfterStart_ThrowsOperationCanceledException()
        {
            var network = new VirtualNetwork();
            using (var clientSslStream = new SslStream(new VirtualNetworkStream(network, isServer: false), false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(new VirtualNetworkStream(network, isServer: true)))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                var cts = new CancellationTokenSource();
                Task t = clientSslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions() { TargetHost = certificate.GetNameInfo(X509NameType.SimpleName, false) }, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }

        [Fact]
        public async Task AuthenticateAsClientAsync_Sockets_CanceledAfterStart_ThrowsOperationCanceledException()
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                await client.ConnectAsync(listener.LocalEndPoint);
                using (Socket server = await listener.AcceptAsync())
                using (var clientSslStream = new SslStream(new NetworkStream(client), false, AllowAnyServerCertificate))
                using (var serverSslStream = new SslStream(new NetworkStream(server)))
                using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
                {
                    var cts = new CancellationTokenSource();
                    Task t = clientSslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions() { TargetHost = certificate.GetNameInfo(X509NameType.SimpleName, false) }, cts.Token);
                    cts.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
                }
            }
        }

        [Fact]
        public async Task AuthenticateAsServerAsync_VirtualNetwork_CanceledAfterStart_ThrowsOperationCanceledException()
        {
            var network = new VirtualNetwork();
            using (var clientSslStream = new SslStream(new VirtualNetworkStream(network, isServer: false), false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(new VirtualNetworkStream(network, isServer: true)))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                var cts = new CancellationTokenSource();
                Task t = serverSslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions() { ServerCertificate = certificate }, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }

        [Fact]
        public async Task AuthenticateAsServerAsync_Sockets_CanceledAfterStart_ThrowsOperationCanceledException()
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                await client.ConnectAsync(listener.LocalEndPoint);
                using (Socket server = await listener.AcceptAsync())
                using (var clientSslStream = new SslStream(new NetworkStream(client), false, AllowAnyServerCertificate))
                using (var serverSslStream = new SslStream(new NetworkStream(server)))
                using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
                {
                    var cts = new CancellationTokenSource();
                    Task t = serverSslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions() { ServerCertificate = certificate }, cts.Token);
                    cts.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
                }
            }
        }
    }
}
