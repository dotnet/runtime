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
        readonly CertificateSetup certificates;

        public SslStreamNetworkStreamTest(ITestOutputHelper output, CertificateSetup setup)
        {
            _output = output;
            certificates = setup;
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
                clientProtocol = SslProtocols.Tls | SslProtocols.Tls11;
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
            using (SslStream ssl = new SslStream(ns, true, validationCallback))
            {
                X509CertificateCollection certBundle = new X509CertificateCollection();
                certBundle.Add(Configuration.Certificates.GetClientCertificate());

                // Perform handshake to establish secure connection.
                await ssl.AuthenticateAsClientAsync(Configuration.Security.TlsRenegotiationServer, certBundle, SslProtocols.Tls12, false);
                Assert.True(ssl.IsAuthenticated);
                Assert.True(ssl.IsEncrypted);

                // Issue request that triggers regotiation from server.
                byte[] message = Encoding.UTF8.GetBytes("GET /EchoClientCertificate.ashx HTTP/1.1\r\nHost: corefx-net-tls.azurewebsites.net\r\n\r\n");
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
                    EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
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
                    EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
                };
                clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                clientOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) =>
                {
                    return sendClientCertificate ? clientCertificate : null;
                };

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = serverCertificate,
                                                                                                      AllowRenegotiation = false  };
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
                    EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
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
                // Fail as it is not allowed to receive non hnadshake frames during handshake.
                await Assert.ThrowsAsync<InvalidOperationException>(()=> t);
            }
        }

        [ConditionalFact(nameof(SupportsRenegotiation))]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_NegotiateClientCertificateAsync_ServerDontDrainClientData()
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
                    EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
                };
                clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = serverCertificate };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions, cts.Token),
                                server.AuthenticateAsServerAsync(serverOptions, cts.Token));

                Assert.Null(server.RemoteCertificate);

                // Send application data instead of Client hello.
                await client.WriteAsync(new byte[500], cts.Token);
                // Server don't drain the client data
                await server.ReadAsync(new byte[1]);
                // Fail as it is not allowed to receive non hnadshake frames during handshake.
                await Assert.ThrowsAsync<InvalidOperationException>(()=>
                    server.NegotiateClientCertificateAsync(cts.Token)
                );
            }
        }


        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls13))]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
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
                    EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
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
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task NegotiateClientCertificateAsync_PendingData_Throws()
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

                // This should go out in single TLS frame
                await client.WriteAsync(new byte[200], cts.Token);
                byte[] readBuffer = new byte[10];
                // when we read part of the frame, remaining part should left decrypted
                await server.ReadAsync(readBuffer, cts.Token);

                await Assert.ThrowsAsync<InvalidOperationException>(() => server.NegotiateClientCertificateAsync(cts.Token));
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
        [InlineData(false)]
        [InlineData(true)]
        public async Task SslStream_TargetHostName_Succeeds(bool useEmptyName)
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
                serverOptions.ServerCertificateSelectionCallback =
                    (sender, name) =>
                    {
                        SslStream stream = (SslStream)sender;
                        Assert.Equal(targetName, stream.TargetHostName);

                        return certificate;
                    };

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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/46837", TestPlatforms.OSX)]
        public async Task SslStream_UntrustedCaWithCustomCallback_OK(bool usePartialChain)
        {
            int split = Random.Shared.Next(0, certificates.serverChain.Count - 1);

            var clientOptions = new  SslClientAuthenticationOptions() { TargetHost = "localhost" };
            clientOptions.RemoteCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) =>
                {
                    // add our custom root CA
                    chain.ChainPolicy.CustomTrustStore.Add(certificates.serverChain[certificates.serverChain.Count - 1]);
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    // Add only one CA to verify that peer did send intermediate CA cert.
                    // In case of partial chain, we need to make missing certs available.
                    if (usePartialChain)
                    {
                        for (int i = split; i < certificates.serverChain.Count - 1; i++)
                        {
                            chain.ChainPolicy.ExtraStore.Add(certificates.serverChain[i]);
                        }
                    }

                    bool result = chain.Build((X509Certificate2)certificate);
                    Assert.True(result);

                    return result;
                };

            var serverOptions = new SslServerAuthenticationOptions();
            X509Certificate2Collection serverChain;
            if (usePartialChain)
            {
                // give first few certificates without root CA
                serverChain = new X509Certificate2Collection();
                for (int i = 0; i < split; i++)
                {
                    serverChain.Add(certificates.serverChain[i]);
                }
            }
            else
            {
                serverChain = certificates.serverChain;
            }

            serverOptions.ServerCertificateContext = SslStreamCertificateContext.Create(certificates.serverCert, certificates.serverChain);

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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/46837", TestPlatforms.OSX)]
        public async Task SslStream_UntrustedCaWithCustomCallback_Throws(bool customCallback)
        {
            string errorMessage;
            var clientOptions = new  SslClientAuthenticationOptions() { TargetHost = "localhost" };
            if (customCallback)
            {
                clientOptions.RemoteCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        // Add only root CA to verify that peer did send intermediate CA cert.
                        chain.ChainPolicy.CustomTrustStore.Add(certificates.serverChain[certificates.serverChain.Count - 1]);
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
            serverOptions.ServerCertificateContext = SslStreamCertificateContext.Create(certificates.serverCert, certificates.serverChain);

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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/46837", TestPlatforms.OSX)]
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

            var clientOptions = new  SslClientAuthenticationOptions() { TargetHost = "localhost",  };
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

                Assert.True(chain.ChainPolicy.ExtraStore.Count >= clientChain.Count - 1, "client did not sent expected chain");
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
                await Task.WhenAll(t1, t2).WaitAsync(TestConfiguration.PassingTestTimeout);

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

            foreach (SslStream s in  streams)
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

                Task writer = Task.Run(() =>
                {
                    Memory<byte> data = new Memory<byte>(dataToCopy);
                    while (data.Length > 0)
                    {
                        int writeLength = Math.Min(data.Length, writeBufferSize);
                        if (useAsync)
                        {
                            server.WriteAsync(data.Slice(0, writeLength)).GetAwaiter().GetResult();
                        }
                        else
                        {
                            server.Write(data.Span.Slice(0, writeLength));
                        }

                        data = data.Slice(Math.Min(writeBufferSize, data.Length));
                    }

                    server.ShutdownAsync().GetAwaiter().GetResult();
                });

                Task reader = Task.Run(() =>
                {
                    Memory<byte> readBuffer = new Memory<byte>(dataReceived);
                    int totalLength = 0;
                    int readLength;

                    while (true)
                    {
                        if (useAsync)
                        {
                            readLength = client.ReadAsync(readBuffer.Slice(totalLength, readBufferSize)).GetAwaiter().GetResult();
                        }
                        else
                        {
                            readLength = client.Read(readBuffer.Span.Slice(totalLength, readBufferSize));
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
