// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamAlertsTest
    {
        private const uint SEC_E_ILLEGAL_MESSAGE = 0x80090326;
        private const uint SEC_E_CERT_UNKNOWN = 0x80090327;

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task SslStream_StreamToStream_HandshakeAlert_Ok()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new SslStream(stream1, true, AllowAnyServerCertificate))
            using (var server = new SslStream(stream2, true, FailClientCertificate))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                Task serverAuth = server.AuthenticateAsServerAsync(certificate);
                await client.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false)).WaitAsync(TestConfiguration.PassingTestTimeout);

                byte[] buffer = new byte[1024];

                // Schannel semantics require that Decrypt is called to receive an alert.
                await client.WriteAsync(buffer, 0, buffer.Length);
                var exception = await Assert.ThrowsAsync<IOException>(() => client.ReadAsync(buffer, 0, buffer.Length)).WaitAsync(TestConfiguration.PassingTestTimeout);

                Assert.IsType<Win32Exception>(exception.InnerException);
                var win32ex = (Win32Exception)exception.InnerException;

                // The Schannel HResults for each alert are documented here:
                // https://msdn.microsoft.com/en-us/library/windows/desktop/dd721886(v=vs.85).aspx
                Assert.Equal(SEC_E_CERT_UNKNOWN, unchecked((uint)win32ex.NativeErrorCode));

                await Assert.ThrowsAsync<AuthenticationException>(() => serverAuth).WaitAsync(TestConfiguration.PassingTestTimeout);

                await Assert.ThrowsAsync<AuthenticationException>(() => server.WriteAsync(buffer, 0, buffer.Length));
                await Assert.ThrowsAsync<AuthenticationException>(() => server.ReadAsync(buffer, 0, buffer.Length));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_StreamToStream_ServerInitiatedCloseNotify_Ok()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new SslStream(stream1, true, AllowAnyServerCertificate))
            using (var server = new SslStream(stream2))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                var handshake = new Task[2];

                handshake[0] = server.AuthenticateAsServerAsync(certificate);
                handshake[1] = client.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false));

                await Task.WhenAll(handshake).WaitAsync(TestConfiguration.PassingTestTimeout);

                var readBuffer = new byte[1024];

                await server.ShutdownAsync();
                int bytesRead = await client.ReadAsync(readBuffer, 0, readBuffer.Length);
                // close_notify received by the client.
                Assert.Equal(0, bytesRead);

                await client.ShutdownAsync();
                bytesRead = await server.ReadAsync(readBuffer, 0, readBuffer.Length);
                // close_notify received by the server.
                Assert.Equal(0, bytesRead);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_StreamToStream_ClientInitiatedCloseNotify_Ok(bool sendData)
        {
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (var client = new SslStream(clientStream, true, AllowAnyServerCertificate))
            using (var server = new SslStream(serverStream))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                var handshake = new Task[2];

                handshake[0] = server.AuthenticateAsServerAsync(certificate);
                handshake[1] = client.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false));

                await Task.WhenAll(handshake).WaitAsync(TestConfiguration.PassingTestTimeout);


                var readBuffer = new byte[1024];

                if (sendData)
                {
                    // Send some data before shutting down. This may matter for TLS13.
                    handshake[0] = server.WriteAsync(readBuffer, 0, 1);
                    handshake[1] = client.ReadAsync(readBuffer, 0, 1);
                    await Task.WhenAll(handshake).WaitAsync(TestConfiguration.PassingTestTimeout);
                }

                await client.ShutdownAsync();
                int bytesRead = await server.ReadAsync(readBuffer, 0, readBuffer.Length);
                // close_notify received by the server.
                Assert.Equal(0, bytesRead);
                await server.ShutdownAsync();
                bytesRead = await client.ReadAsync(readBuffer, 0, readBuffer.Length);
                // close_notify received by the client.
                Assert.Equal(0, bytesRead);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_StreamToStream_DataAfterShutdown_Fail()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new SslStream(stream1, true, AllowAnyServerCertificate))
            using (var server = new SslStream(stream2))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                var handshake = new Task[2];

                handshake[0] = server.AuthenticateAsServerAsync(certificate);
                handshake[1] = client.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false));

                await Task.WhenAll(handshake).WaitAsync(TestConfiguration.PassingTestTimeout);

                var buffer = new byte[1024];

                Assert.True(client.CanWrite);

                await client.ShutdownAsync();

                Assert.False(client.CanWrite);

                await Assert.ThrowsAsync<InvalidOperationException>(() => client.ShutdownAsync());
                await Assert.ThrowsAsync<InvalidOperationException>(() => client.WriteAsync(buffer, 0, buffer.Length));
            }
        }

        [Fact]
        public async Task SslStream_WriteAfterRemoteCloseNotify_MayThrowIOException()
        {
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (var client = new SslStream(clientStream, true, AllowAnyServerCertificate))
            using (var server = new SslStream(serverStream))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                string targetHost = certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = certificate,
                    EnabledSslProtocols = SslProtocols.Tls12
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = targetHost,
                    EnabledSslProtocols = SslProtocols.Tls12
                };

                await Task.WhenAll(
                    server.AuthenticateAsServerAsync(serverOptions),
                    client.AuthenticateAsClientAsync(clientOptions)
                ).WaitAsync(TestConfiguration.PassingTestTimeout);

                var buffer = new byte[1024];

                // Server initiates shutdown
                await server.ShutdownAsync().WaitAsync(TestConfiguration.PassingTestTimeout);

                // Client reads the close_notify
                int bytesRead = await client.ReadAsync(buffer, 0, buffer.Length).WaitAsync(TestConfiguration.PassingTestTimeout);
                Assert.Equal(0, bytesRead);

                // Client attempts to write after receiving close_notify
                // On macOS with Secure Transport, this throws immediately
                // On Linux/Windows, the first write may succeed, subsequent operations fail
                try
                {
                    await client.WriteAsync(buffer, 0, buffer.Length).WaitAsync(TestConfiguration.PassingTestTimeout);

                    // Write succeeded - this is expected on Linux/Windows
                    Assert.False(PlatformDetection.IsOSX, "Write after close_notify should throw on macOS");
                }
                catch (IOException)
                {
                    // IOException is expected on macOS, but also acceptable on other platforms
                }
            }
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_NoCallback_UntrustedCert_SendsAlert(SslProtocols protocol)
        {
            // When no RemoteCertificateValidationCallback is set and the server's cert
            // is not trusted, the cert verify callback causes the TLS stack to send an
            // alert so the client sees a proper error.

            X509Certificate2 cert = Configuration.Certificates.GetSelfSignedServerCertificate();
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream client = new SslStream(clientStream))
            using (SslStream server = new SslStream(serverStream))
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = cert,
                    EnabledSslProtocols = protocol,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "localhost",
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    EnabledSslProtocols = protocol,
                };

                Task serverTask = server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);
                Task clientTask = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);

                // Client should fail because the self-signed cert is not trusted.
                await Assert.ThrowsAsync<AuthenticationException>(() => clientTask);

                // Server should also fail — either from the handshake or from the
                // client's alert. In TLS 1.3, the alert may arrive later so we
                // attempt a data exchange to surface it.
                try
                {
                    await serverTask;
                    byte[] buffer = new byte[1];
                    await server.WriteAsync(buffer);
                    await server.ReadAsync(buffer);
                    Assert.Fail("Expected an exception from the server side.");
                }
                catch (AuthenticationException)
                {
                }
                catch (IOException)
                {
                }
            }
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_NoCallback_UntrustedClientCert_ServerSendsAlert(SslProtocols protocol)
        {
            // When the server requires a client certificate and no
            // RemoteCertificateValidationCallback is set, the server should send
            // a TLS alert when the client's cert chain cannot be validated.

            X509Certificate2 cert = Configuration.Certificates.GetSelfSignedServerCertificate();
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream client = new SslStream(clientStream))
            using (SslStream server = new SslStream(serverStream))
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = cert,
                    ClientCertificateRequired = true,
                    EnabledSslProtocols = protocol,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "localhost",
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = delegate { return true; },
                    ClientCertificates = new X509CertificateCollection { cert },
                    EnabledSslProtocols = protocol,
                };

                Task serverTask = server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);
                Task clientTask = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);

                // Server should fail because the client cert is not trusted.
                // In TLS 1.3, the failure may occur after the handshake during
                // post-handshake auth, so attempt data exchange to surface it.
                try
                {
                    await serverTask;
                    byte[] buffer = new byte[1];
                    await server.WriteAsync(buffer);
                    await server.ReadAsync(buffer);
                    Assert.Fail("Expected an exception from the server side.");
                }
                catch (AuthenticationException ex)
                {
                    Assert.Contains("chain", ex.ToString(), StringComparison.OrdinalIgnoreCase);
                }
                catch (IOException)
                {
                }

                // Client may or may not throw depending on timing.
                try
                {
                    await clientTask;
                }
                catch (AuthenticationException)
                {
                }
                catch (IOException)
                {
                }
            }
        }

        private bool FailClientCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return false;
        }

        private bool AllowAnyServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
