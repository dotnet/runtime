// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class CertificateSetup : IDisposable
    {
        public readonly X509Certificate2 serverCert;
        public readonly X509Certificate2Collection serverChain;

        public CertificateSetup()
        {
            TestHelper.CleanupCertificates(nameof(SslStreamNetworkStreamTest));
            (serverCert, serverChain) = TestHelper.GenerateCertificates("localhost", nameof(SslStreamNetworkStreamTest), longChain: true);
        }

        public void Dispose()
        {
            serverCert.Dispose();
            foreach (var c in serverChain)
            {
                c.Dispose();
            }
        }
    }

    public class SslStreamNetworkStreamTest : IClassFixture<CertificateSetup>
    {
        private static bool SupportsRenegotiation => TestConfiguration.SupportsRenegotiation;

        readonly ITestOutputHelper _output;
        readonly CertificateSetup _certificates;

        public SslStreamNetworkStreamTest(ITestOutputHelper output, CertificateSetup setup)
        {
            _output = output;
            _certificates = setup;
        }

        [ConditionalFact]
        [PlatformSpecific(TestPlatforms.Linux)] // This only applies where OpenSsl is used.
        public async Task SslStream_SendReceiveOverNetworkStream_AuthenticationException()
        {
            SslProtocols clientProtocol;
            SslProtocols serverProtocol;

            // Try to find protocol mismatch.
            if (PlatformDetection.SupportsTls12 && (PlatformDetection.SupportsTls10 || PlatformDetection.SupportsTls11))
            {
                // OpenSSL 1.0 where new is Tls12
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                clientProtocol = SslProtocols.Tls | SslProtocols.Tls11;
#pragma warning restore SYSLIB0039
                serverProtocol = SslProtocols.Tls12;
            }
            else if (PlatformDetection.SupportsTls12 && PlatformDetection.SupportsTls13)
            {
                // OpenSSl 1.1 where new is 1.3 and legacy is 1.2
                clientProtocol = SslProtocols.Tls13;
                serverProtocol = SslProtocols.Tls12;
            }
            else
            {
                throw new SkipTestException("Did not find disjoined sets");
            }

            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);

            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (TcpClient client = new TcpClient())
            {
                listener.Start();

                Task clientConnectTask = client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                Task<TcpClient> listenerAcceptTask = listener.AcceptTcpClientAsync();

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(clientConnectTask, listenerAcceptTask);

                TcpClient server = listenerAcceptTask.Result;
                using (SslStream clientStream = new SslStream(
                    client.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback(ValidateServerCertificate),
                    null,
                    EncryptionPolicy.RequireEncryption))
                using (SslStream serverStream = new SslStream(
                    server.GetStream(),
                    false,
                    null,
                    null,
                    EncryptionPolicy.RequireEncryption))
                {

                    Task clientAuthenticationTask = clientStream.AuthenticateAsClientAsync(
                        serverCertificate.GetNameInfo(X509NameType.SimpleName, false),
                        null,
                        clientProtocol,
                        false);

                    AuthenticationException e = await Assert.ThrowsAsync<AuthenticationException>(() => serverStream.AuthenticateAsServerAsync(
                        serverCertificate,
                        false,
                        serverProtocol,
                        false));

                    Assert.NotNull(e.InnerException);
                    Assert.Contains("SSL_ERROR_SSL", e.InnerException.Message);
                    Assert.NotNull(e.InnerException.InnerException);
                    Assert.Contains("protocol", e.InnerException.InnerException.Message);

                    e = await Assert.ThrowsAsync<AuthenticationException>(() => clientAuthenticationTask);

                    Assert.NotNull(e.InnerException);
                    Assert.Contains("SSL_ERROR_SSL", e.InnerException.Message);
                    Assert.NotNull(e.InnerException.InnerException);
                    Assert.Contains("protocol", e.InnerException.InnerException.Message);
                }
            }

            listener.Stop();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop] // Test hits external azure server.
        public async Task SslStream_NetworkStream_Renegotiation_Succeeds(bool useSync)
        {
            int validationCount = 0;

            var validationCallback = new RemoteCertificateValidationCallback((object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                validationCount++;
                return true;
            });

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await s.ConnectAsync(Configuration.Security.TlsRenegotiationServer, 443);
            using (NetworkStream ns = new NetworkStream(s))
            using (X509Certificate2 clientCert = Configuration.Certificates.GetClientCertificate())
            using (SslStream ssl = new SslStream(ns, true, validationCallback))
            {
                X509CertificateCollection certBundle = new X509CertificateCollection();
                certBundle.Add(clientCert);

                // Perform handshake to establish secure connection.
                await ssl.AuthenticateAsClientAsync(Configuration.Security.TlsRenegotiationServer, certBundle, SslProtocols.Tls12, false);
                Assert.True(ssl.IsAuthenticated);
                Assert.True(ssl.IsEncrypted);

                // Issue request that triggers regotiation from server.
                byte[] message = "GET /EchoClientCertificate.ashx HTTP/1.1\r\nHost: corefx-net-tls.azurewebsites.net\r\n\r\n"u8.ToArray();
                if (useSync)
                {
                    ssl.Write(message, 0, message.Length);
                }
                else
                {
                    await ssl.WriteAsync(message, 0, message.Length);
                }

                // Initiate Read operation, that results in starting renegotiation as per server response to the above request.
                int bytesRead = useSync ? ssl.Read(message, 0, message.Length) : await ssl.ReadAsync(message, 0, message.Length);

                Assert.Equal(1, validationCount);
                Assert.InRange(bytesRead, 1, message.Length);
                Assert.Contains("HTTP/1.1 200 OK", Encoding.UTF8.GetString(message));
            }
        }

        [ConditionalTheory(nameof(SupportsRenegotiation))]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_NegotiateClientCertificateAsync_Succeeds(bool sendClientCertificate)
        {
            bool negotiateClientCertificateCalled = false;
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TestConfiguration.PassingTestTimeout);

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate())
            {
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                    EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
#pragma warning restore SYSLIB0039
                };
                clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                clientOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) =>
                {
                    return sendClientCertificate ? clientCertificate : null;
                };

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = serverCertificate };
                serverOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (negotiateClientCertificateCalled && sendClientCertificate)
                    {
                        Assert.Equal(clientCertificate.GetCertHash(), certificate?.GetCertHash());
                    }
                    else
                    {
                        Assert.Null(certificate);
                    }

                    return true;
                };


                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions, cts.Token),
                                server.AuthenticateAsServerAsync(serverOptions, cts.Token));

                Assert.Null(server.RemoteCertificate);

                // Client needs to be reading for renegotiation to happen.
                byte[] buffer = new byte[TestHelper.s_ping.Length];
                ValueTask<int> t = client.ReadAsync(buffer, cts.Token);

                negotiateClientCertificateCalled = true;
                await server.NegotiateClientCertificateAsync(cts.Token);
                if (sendClientCertificate)
                {
                    Assert.NotNull(server.RemoteCertificate);
                }
                else
                {
                    Assert.Null(server.RemoteCertificate);
                }

                // Finish the client's read
                await server.WriteAsync(TestHelper.s_ping, cts.Token);
                await t;

                // verify that the session is usable with or without client's certificate
                await TestHelper.PingPong(client, server, cts.Token);
                await TestHelper.PingPong(server, client, cts.Token);

                server.RemoteCertificate?.Dispose();
            }
        }

        [ConditionalTheory(nameof(SupportsRenegotiation))]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_NegotiateClientCertificateAsyncNoRenego_Succeeds(bool sendClientCertificate)
        {
            bool negotiateClientCertificateCalled = false;
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TestConfiguration.PassingTestTimeout);

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate())
            {
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                    EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
#pragma warning restore SYSLIB0039
                };
                clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                clientOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) =>
                {
                    return sendClientCertificate ? clientCertificate : null;
                };

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions()
                {
                    ServerCertificate = serverCertificate,
                    AllowRenegotiation = false
                };
                serverOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (negotiateClientCertificateCalled && sendClientCertificate)
                    {
                        Assert.Equal(clientCertificate.GetCertHash(), certificate?.GetCertHash());
                    }
                    else
                    {
                        Assert.Null(certificate);
                    }

                    return true;
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions, cts.Token),
                                server.AuthenticateAsServerAsync(serverOptions, cts.Token));

                Assert.Null(server.RemoteCertificate);

                // Client needs to be reading for renegotiation to happen.
                byte[] buffer = new byte[TestHelper.s_ping.Length];
                ValueTask<int> t = client.ReadAsync(buffer, cts.Token);

                negotiateClientCertificateCalled = true;
                await server.NegotiateClientCertificateAsync(cts.Token);
                if (sendClientCertificate)
                {
                    Assert.NotNull(server.RemoteCertificate);
                }
                else
                {
                    Assert.Null(server.RemoteCertificate);
                }
                // Finish the client's read
                await server.WriteAsync(TestHelper.s_ping, cts.Token);
                await t;
                // verify that the session is usable with or without client's certificate
                await TestHelper.PingPong(client, server, cts.Token);
                await TestHelper.PingPong(server, client, cts.Token);

                server.RemoteCertificate?.Dispose();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_NegotiateClientCertificateAsync_ClientWriteData()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TestConfiguration.PassingTestTimeout);

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                using X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate();

                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                    EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
#pragma warning restore SYSLIB0039
                };
                clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = serverCertificate };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions, cts.Token),
                                server.AuthenticateAsServerAsync(serverOptions, cts.Token));

                Assert.Null(server.RemoteCertificate);

                var t = server.NegotiateClientCertificateAsync(cts.Token);

                // Send application data instead of Client hello.
                await client.WriteAsync(new byte[500], cts.Token);
                // Fail as it is not allowed to receive non handshake frames during handshake.
                await Assert.ThrowsAsync<InvalidOperationException>(() => t);
            }
        }

        [ConditionalFact(nameof(SupportsRenegotiation))]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_NegotiateClientCertificateAsync_IncompleteIncomingTlsFrame_Throws()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TestConfiguration.PassingTestTimeout);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();

            // use ManualChunkingStream in the middle to enforce partial TLS frame receive later
            ManualChunkingStream clientChunkingStream = new ManualChunkingStream(clientStream, false);

            using (SslStream server = new SslStream(serverStream))
            using (SslStream client = new SslStream(clientChunkingStream))
            {
                using X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate();
                using X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate();

                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                    EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
#pragma warning restore SYSLIB0039
                };
                clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                clientOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) =>
                {
                    return clientCertificate;
                };
                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = serverCertificate };
                serverOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions, cts.Token),
                                server.AuthenticateAsServerAsync(serverOptions, cts.Token));
                Assert.Null(server.RemoteCertificate);

                // manually approve all future writes
                clientChunkingStream.SetWriteChunking(true);

                // TLS packets are maximum 16 kB, sending 20 kB of data guarantees at least 2 packets to be sent
                byte[] buffer = new byte[20 * 1024];
                client.Write(buffer);

                // delay receiving last few B so that only an incomplete TLS frame is received
                await clientChunkingStream.CommitWriteAsync(clientChunkingStream.PendingWriteLength - 100);
                int read = await server.ReadAsync(buffer, cts.Token);

                // Fail as there are still some undrained data (incomplete incoming TLS frame)
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    server.NegotiateClientCertificateAsync(cts.Token)
                );

                // no more delaying needed, drain client data.
                clientChunkingStream.SetWriteChunking(false);
                while (read < buffer.Length)
                {
                    read += await server.ReadAsync(buffer);
                }

                // Verify that the session is usable even renego request failed.
                await TestHelper.PingPong(client, server, cts.Token);
                await TestHelper.PingPong(server, client, cts.Token);
            }
        }

        [ConditionalFact(nameof(SupportsRenegotiation))]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_NegotiateClientCertificateAsync_PendingDecryptedData_Throws()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TestConfiguration.PassingTestTimeout);

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                using X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate();
                using X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate();

                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    ClientCertificates = new X509CertificateCollection(new X509Certificate2[] { clientCertificate })
                };
                clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = serverCertificate };
                serverOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions, cts.Token),
                                server.AuthenticateAsServerAsync(serverOptions, cts.Token));

                await TestHelper.PingPong(client, server, cts.Token);
                Assert.Null(server.RemoteCertificate);

                // This should go out in single TLS frame
                await client.WriteAsync(new byte[200], cts.Token);
                byte[] readBuffer = new byte[10];
                // when we read part of the frame, remaining part should left decrypted
                int read = await server.ReadAsync(readBuffer, cts.Token);

                await Assert.ThrowsAsync<InvalidOperationException>(() => server.NegotiateClientCertificateAsync(cts.Token));

                while (read < 200)
                {
                    read += await server.ReadAsync(readBuffer, cts.Token);
                }

                // verify that the session is usable with or without client's certificate
                await TestHelper.PingPong(client, server, cts.Token);
                await TestHelper.PingPong(server, client, cts.Token);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls13))]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.Android, "SslStream Renegotiate is not supported in SslStream on Android")]
        public async Task SslStream_NegotiateClientCertificateAsyncTls13_Succeeds(bool sendClientCertificate)
        {
            bool negotiateClientCertificateCalled = false;
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TestConfiguration.PassingTestTimeout);

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate())
            {
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    EnabledSslProtocols = SslProtocols.Tls13,
                };
                clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                clientOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) =>
                {
                    return sendClientCertificate ? clientCertificate : null;
                };

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = serverCertificate };
                serverOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (negotiateClientCertificateCalled && sendClientCertificate)
                    {
                        Assert.Equal(clientCertificate.GetCertHash(), certificate?.GetCertHash());
                    }
                    else
                    {
                        Assert.Null(certificate);
                    }

                    return true;
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions, cts.Token),
                                server.AuthenticateAsServerAsync(serverOptions, cts.Token));
                // need this to complete TLS 1.3 handshake
                await TestHelper.PingPong(client, server);
                Assert.Null(server.RemoteCertificate);

                // Client needs to be reading for renegotiation to happen.
                byte[] buffer = new byte[TestHelper.s_ping.Length];
                ValueTask<int> t = client.ReadAsync(buffer, cts.Token);

                negotiateClientCertificateCalled = true;
                await server.NegotiateClientCertificateAsync(cts.Token);
                if (sendClientCertificate)
                {
                    Assert.NotNull(server.RemoteCertificate);
                }
                else
                {
                    Assert.Null(server.RemoteCertificate);
                }
                // Finish the client's read
                await server.WriteAsync(TestHelper.s_ping, cts.Token);
                await t;
                // verify that the session is usable with or without client's certificate
                await TestHelper.PingPong(client, server, cts.Token);
                await TestHelper.PingPong(server, client, cts.Token);

                server.RemoteCertificate?.Dispose();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task SslStream_SecondNegotiateClientCertificateAsync_Throws(bool sendClientCertificate)
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TestConfiguration.PassingTestTimeout);

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate())
            {
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                    EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
#pragma warning restore SYSLIB0039
                };
                clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                clientOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) =>
                {
                    return sendClientCertificate ? clientCertificate : null;
                };

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = serverCertificate };
                serverOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions, cts.Token),
                                server.AuthenticateAsServerAsync(serverOptions, cts.Token));

                await TestHelper.PingPong(client, server, cts.Token);
                Assert.Null(server.RemoteCertificate);

                // Client needs to be reading for renegotiation to happen.
                byte[] buffer = new byte[TestHelper.s_ping.Length];
                ValueTask<int> t = client.ReadAsync(buffer, cts.Token);

                await server.NegotiateClientCertificateAsync(cts.Token);
                if (sendClientCertificate)
                {
                    Assert.NotNull(server.RemoteCertificate);
                }
                else
                {
                    Assert.Null(server.RemoteCertificate);
                }
                // Finish the client's read
                await server.WriteAsync(TestHelper.s_ping, cts.Token);
                await t;

                await Assert.ThrowsAsync<InvalidOperationException>(() => server.NegotiateClientCertificateAsync());

                server.RemoteCertificate?.Dispose();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task SslStream_NegotiateClientCertificateAsyncConcurrentIO_Throws(bool doRead)
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TestConfiguration.PassingTestTimeout);

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate())
            {
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    ClientCertificates = new X509CertificateCollection(new X509Certificate2[] { clientCertificate })
                };
                clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = serverCertificate };
                serverOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions, cts.Token),
                                server.AuthenticateAsServerAsync(serverOptions, cts.Token));

                await TestHelper.PingPong(client, server, cts.Token);
                Assert.Null(server.RemoteCertificate);

                Task t = server.NegotiateClientCertificateAsync(cts.Token);
                if (doRead)
                {
                    byte[] buffer = new byte[TestHelper.s_ping.Length];
                    await Assert.ThrowsAsync<NotSupportedException>(() => server.ReadAsync(buffer).AsTask());
                }
                else
                {
                    await Assert.ThrowsAsync<NotSupportedException>(() => server.WriteAsync(TestHelper.s_ping).AsTask());
                }
            }
        }

        [Fact]
        public async Task SslStream_NestedAuth_Throws()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var ssl = new SslStream(stream1))
            using (stream2)
            {
                // Start handshake.
                Task task = ssl.AuthenticateAsClientAsync("foo.com", null, SslProtocols.Tls12, false);
                // Do it again without waiting for previous one to finish.
                await Assert.ThrowsAsync<InvalidOperationException>(() => ssl.AuthenticateAsClientAsync("foo.com", null, SslProtocols.Tls12, false));
            }
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task SslStream_TargetHostName_Succeeds(bool useEmptyName, bool useCallback)
        {
            string targetName = useEmptyName ? string.Empty : Guid.NewGuid().ToString("N");
            int count = 0;

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (var client = new SslStream(clientStream))
            using (var server = new SslStream(serverStream))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                // It should be empty before handshake.
                Assert.Equal(string.Empty, client.TargetHostName);
                Assert.Equal(string.Empty, server.TargetHostName);

                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions() { TargetHost = targetName };
                clientOptions.RemoteCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        SslStream stream = (SslStream)sender;
                        Assert.Equal(targetName, stream.TargetHostName);
                        count++;
                        return true;
                    };

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions();
                if (useCallback)
                {
                    serverOptions.ServerCertificateSelectionCallback =
                        (sender, name) =>
                        {
                            SslStream stream = (SslStream)sender;
                            Assert.Equal(targetName, stream.TargetHostName);

                            return certificate;
                        };
                }
                else
                {
                    serverOptions.ServerCertificate = certificate;
                }

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions),
                                server.AuthenticateAsServerAsync(serverOptions));

                await TestHelper.PingPong(client, server);

                Assert.Equal(targetName, client.TargetHostName);
                Assert.Equal(targetName, server.TargetHostName);
                Assert.Equal(1, count);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.Android, "Self-signed certificates are rejected by Android before the .NET validation is reached")]
        public async Task SslStream_UntrustedCaWithCustomTrust_OK(bool usePartialChain)
        {
            int split = Random.Shared.Next(0, _certificates.serverChain.Count - 1);

            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = "localhost" };
            clientOptions.CertificateChainPolicy = new X509ChainPolicy()
            {
                RevocationMode = X509RevocationMode.NoCheck,
                TrustMode = X509ChainTrustMode.CustomRootTrust
            };
            clientOptions.CertificateChainPolicy.CustomTrustStore.Add(_certificates.serverChain[_certificates.serverChain.Count - 1]);
            // Add only one CA to verify that peer did send intermediate CA cert.
            // In case of partial chain, we need to make missing certs available.
            if (usePartialChain)
            {
                for (int i = split; i < _certificates.serverChain.Count - 1; i++)
                {
                    clientOptions.CertificateChainPolicy.ExtraStore.Add(_certificates.serverChain[i]);
                }
            }

            var serverOptions = new SslServerAuthenticationOptions();
            X509Certificate2Collection serverChain;
            if (usePartialChain)
            {
                // give first few certificates without root CA
                serverChain = new X509Certificate2Collection();
                for (int i = 0; i < split; i++)
                {
                    serverChain.Add(_certificates.serverChain[i]);
                }
            }
            else
            {
                serverChain = _certificates.serverChain;
            }

            // TODO: line below is wrong, but it breaks on Mac, it should be
            // serverOptions.ServerCertificateContext = SslStreamCertificateContext.Create(_certificates.serverCert, serverChain);
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/73295")]
            serverOptions.ServerCertificateContext = SslStreamCertificateContext.Create(_certificates.serverCert, _certificates.serverChain);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream client = new SslStream(clientStream))
            using (SslStream server = new SslStream(serverStream))
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.Android, "Self-signed certificates are rejected by Android before the .NET validation is reached")]
        public async Task SslStream_UntrustedCaWithCustomCallback_Throws(bool customCallback)
        {
            string errorMessage;
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = "localhost" };
            if (customCallback)
            {
                clientOptions.RemoteCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        // Add only root CA to verify that peer did send intermediate CA cert.
                        chain.ChainPolicy.CustomTrustStore.Add(_certificates.serverChain[_certificates.serverChain.Count - 1]);
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        // This should work and we should be able to trust the chain.
                        Assert.True(chain.Build((X509Certificate2)certificate));
                        // Reject it in custom callback to simulate for example pinning.
                        return false;
                    };

                errorMessage = "RemoteCertificateValidationCallback";
            }
            else
            {
                // On Windows we hand whole chain to OS so they can always see the root CA.
                errorMessage = PlatformDetection.IsWindows ? "UntrustedRoot" : "PartialChain";
            }

            var serverOptions = new SslServerAuthenticationOptions();
            serverOptions.ServerCertificateContext = SslStreamCertificateContext.Create(_certificates.serverCert, _certificates.serverChain);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream client = new SslStream(clientStream))
            using (SslStream server = new SslStream(serverStream))
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);

                var e = await Assert.ThrowsAsync<AuthenticationException>(() => t1);
                Assert.Contains(errorMessage, e.Message);
                // Server side should finish since we run custom callback after handshake is done.
                await t2;
            }
        }

        [ConditionalFact]
        [SkipOnPlatform(TestPlatforms.Android, "Self-signed certificates are rejected by Android before the .NET validation is reached")]
        public async Task SslStream_ClientCertificate_SendsChain()
        {
            List<SslStream> streams = new List<SslStream>();
            TestHelper.CleanupCertificates();
            (X509Certificate2 clientCertificate, X509Certificate2Collection clientChain) = TestHelper.GenerateCertificates("SslStream_ClinetCertificate_SendsChain", serverCertificate: false);

            using (X509Store store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser))
            {
                // add chain certificate so we can construct chain since there is no way how to pass intermediates directly.
                store.Open(OpenFlags.ReadWrite);
                store.AddRange(clientChain);
                store.Close();
            }

            using (var chain = new X509Chain())
            {
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.DisableCertificateDownloads = false;
                bool chainStatus = chain.Build(clientCertificate);
                // Verify we can construct full chain
                if (chain.ChainElements.Count < clientChain.Count)
                {
                    throw new SkipTestException($"chain cannot be built {chain.ChainElements.Count}");
                }
            }

            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = "localhost" };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            clientOptions.LocalCertificateSelectionCallback = (sender, target, certificates, remoteCertificate, issuers) => clientCertificate;

            var serverOptions = new SslServerAuthenticationOptions() { ClientCertificateRequired = true };
            serverOptions.ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null);
            serverOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                // Client should send chain without root CA. There is no good way how to know if the chain was built from certificates
                // from wire or from system store. However, SslStream adds certificates from wire to ExtraStore in RemoteCertificateValidationCallback.
                // So we verify the operation by checking the ExtraStore. On Windows, that includes leaf itself.
                _output.WriteLine("RemoteCertificateValidationCallback called with {0} and {1} extra certificates", sslPolicyErrors, chain.ChainPolicy.ExtraStore.Count);
                foreach (X509Certificate c in chain.ChainPolicy.ExtraStore)
                {
                    _output.WriteLine("received {0}", c.Subject);
                }

                Assert.Equal(clientChain.Count - 1, chain.ChainPolicy.ExtraStore.Count);
                Assert.Contains(clientChain[0], chain.ChainPolicy.ExtraStore);
                return true;
            };

            // run the test multiple times while holding established SSL so we could hit credential cache.
            for (int i = 0; i < 3; i++)
            {
                (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
                SslStream client = new SslStream(clientStream);
                SslStream server = new SslStream(serverStream);

                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);

                // hold to the streams so they stay in credential cache
                streams.Add(client);
                streams.Add(server);
            }

            TestHelper.CleanupCertificates();
            clientCertificate.Dispose();
            foreach (X509Certificate c in clientChain)
            {
                c.Dispose();
            }

            foreach (SslStream s in streams)
            {
                s.Dispose();
            }
        }

        [Theory]
        [InlineData(16384 * 100, 4096, 1024, false)]
        [InlineData(16384 * 100, 4096, 1024, true)]
        [InlineData(16384 * 100, 1024 * 20, 1024, true)]
        [InlineData(16384, 3, 3, true)]
        public async Task SslStream_RandomSizeWrites_OK(int bufferSize, int readBufferSize, int writeBufferSize, bool useAsync)
        {
            byte[] dataToCopy = RandomNumberGenerator.GetBytes(bufferSize);
            byte[] dataReceived = new byte[dataToCopy.Length + readBufferSize]; // make the buffer bigger to have chance to read more

            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = "localhost" };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            var serverOptions = new SslServerAuthenticationOptions();
            serverOptions.ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedTcpStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream client = new SslStream(new RandomReadWriteSizeStream(clientStream, readBufferSize)))
            using (SslStream server = new SslStream(new RandomReadWriteSizeStream(serverStream)))
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);

                Task writer = Task.Run(async () =>
                {
                    Memory<byte> data = new Memory<byte>(dataToCopy);
                    while (data.Length > 0)
                    {
                        int writeLength = Math.Min(data.Length, writeBufferSize);
                        if (useAsync)
                        {
                            await server.WriteAsync(data.Slice(0, writeLength));
                        }
                        else
                        {
                            server.Write(data.Span.Slice(0, writeLength));
                            await Task.CompletedTask;
                        }

                        data = data.Slice(Math.Min(writeBufferSize, data.Length));
                    }

                    server.ShutdownAsync().GetAwaiter().GetResult();
                });

                Task reader = Task.Run(async () =>
                {
                    Memory<byte> readBuffer = new Memory<byte>(dataReceived);
                    int totalLength = 0;
                    int readLength;

                    while (true)
                    {
                        if (useAsync)
                        {
                            readLength = await client.ReadAsync(readBuffer.Slice(totalLength, readBufferSize));
                        }
                        else
                        {
                            readLength = client.Read(readBuffer.Span.Slice(totalLength, readBufferSize));
                            await Task.CompletedTask;
                        }

                        if (readLength == 0)
                        {
                            break;
                        }

                        totalLength += readLength;
                        Assert.True(totalLength <= bufferSize);
                    }

                    Assert.Equal(bufferSize, totalLength);
                    AssertExtensions.SequenceEqual(dataToCopy.AsSpan(), dataReceived.AsSpan().Slice(0, totalLength));
                });

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(writer, reader);
            }

        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls10))]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task SslStream_UnifiedHello_Ok(bool useOptionCallback)
        {
            (Stream client, Stream server) = TestHelper.GetConnectedTcpStreams();
            SslStream ssl = new SslStream(server);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TestConfiguration.PassingTestTimeout);
            bool callbackCalled = false;
            Task serverTask;

            var options = new SslServerAuthenticationOptions();
            if (useOptionCallback)
            {
                serverTask = ssl.AuthenticateAsServerAsync((ssl, info, o, ct) =>
                {
                    callbackCalled = true;
                    options.ServerCertificate = Configuration.Certificates.GetServerCertificate();
                    return new ValueTask<SslServerAuthenticationOptions>(options);
                }, null, cts.Token);

            }
            else
            {
                options.ServerCertificateSelectionCallback = (o, name) =>
                {
                    callbackCalled = true;
                    return Configuration.Certificates.GetServerCertificate();
                };

                serverTask = ssl.AuthenticateAsServerAsync(options, cts.Token);
            }

            Task.WaitAny(client.WriteAsync(TlsFrameHelperTests.s_UnifiedHello).AsTask(), serverTask);
            if (serverTask.IsCompleted)
            {
                // Something failed. Raise exception.
                await serverTask;
            }

            byte[] buffer = new byte[1024];
            Task<int> readTask = client.ReadAsync(buffer, cts.Token).AsTask();
            Task.WaitAny(readTask, serverTask);
            if (serverTask.IsCompleted)
            {
                // Something failed. Raise exception.
                await serverTask;
            }

            int readLength = await readTask;
            // We should get back ServerHello
            Assert.True(readLength > 0);
            Assert.True(callbackCalled);
            Assert.Equal(22, buffer[0]); // Handshake Protocol
            Assert.Equal(2, buffer[5]);  // ServerHello

            // Handshake should not be finished at this point.
            Assert.False(serverTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverTask);
        }

        private static bool ValidateServerCertificate(
            object sender,
            X509Certificate retrievedServerPublicCertificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            // Accept any certificate.
            return true;
        }
    }
}
