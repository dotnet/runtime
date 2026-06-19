// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/18837", ~(TestPlatforms.Windows | TestPlatforms.Linux))]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/18837", ~(TestPlatforms.Windows | TestPlatforms.Linux))]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/18837", ~(TestPlatforms.Windows | TestPlatforms.Linux))]
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
        [MemberData(nameof(SupportedSslProtocolsExcludingMacOSSsl3))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/18837", ~(TestPlatforms.Windows | TestPlatforms.Linux | TestPlatforms.OSX))]
        public async Task SslStream_NoCallback_UntrustedCert_SendsAlert(SslProtocols protocol)
        {
            // When no RemoteCertificateValidationCallback is set and the server's cert
            // is not trusted, the cert verify callback causes the TLS stack to send an
            // alert so the client sees a proper error.

            X509Certificate2 cert = Configuration.Certificates.GetSelfSignedServerCertificate();
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (RecordingReadStream recordingServerStream = new RecordingReadStream(serverStream))
            using (SslStream client = new SslStream(clientStream))
            using (SslStream server = new SslStream(recordingServerStream))
            using (cert)
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = cert,
                    EnabledSslProtocols = protocol,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = cert.GetNameInfo(X509NameType.DnsName, false),
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    EnabledSslProtocols = protocol,
                };

                Task serverTask = server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);
                Task clientTask = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);

                // Client should fail because the validation failed locally, and it should send an alert.
                AuthenticationException clientException = await Assert.ThrowsAsync<AuthenticationException>(() => clientTask).WaitAsync(TestConfiguration.PassingTestTimeout);

                // Server side should receive the alert and fail the handshake, the exact timing depends on the platform
                // Windows: after the handshake, during data exchange
                // Linux/macOS: during the handshake
                Exception exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
                {
                    await serverTask;
                    byte[] buffer = new byte[1];
                    await server.WriteAsync(buffer).AsTask().WaitAsync(TestConfiguration.PassingTestTimeout);
                    await server.ReadAsync(buffer).AsTask().WaitAsync(TestConfiguration.PassingTestTimeout);
                }).WaitAsync(TestConfiguration.PassingTestTimeout);

                if (PlatformDetection.IsWindows)
                {
                    Assert.IsType<IOException>(exception);
                    Assert.IsType<Win32Exception>(exception.InnerException);
                }
                else if (PlatformDetection.IsLinux)
                {
                    Assert.IsType<AuthenticationException>(exception);
                    Assert.NotNull(exception.InnerException);
                }
                else if (PlatformDetection.IsOSX)
                {
                    // Cross-platform parity: the cert validation exception should surface directly
                    // to the caller (matching Windows/Linux/Android behavior via SendAuthResetSignal),
                    // not wrapped in a generic AuthenticationException(SR.net_auth_SSPI, ...).
                    Assert.Null(clientException.InnerException);

                    // Verify the actual UnknownCA alert bytes reached the peer over the wire.
                    Assert.True(recordingServerStream.ContainsAlert(TlsAlertDescription.UnknownCA), recordingServerStream.GetRecordedAlerts());
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedSslProtocolsExcludingMacOSSsl3))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/18837", ~(TestPlatforms.Windows | TestPlatforms.Linux | TestPlatforms.OSX))]
        public async Task SslStream_NoCallback_UntrustedClientCert_ServerSendsAlert(SslProtocols protocol)
        {
            // When the server requires a client certificate and no
            // RemoteCertificateValidationCallback is set, the server should send
            // a TLS alert when the client's cert chain cannot be validated.

            X509Certificate2 cert = Configuration.Certificates.GetSelfSignedServerCertificate();
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (RecordingReadStream recordingClientStream = new RecordingReadStream(clientStream))
            using (serverStream)
            using (SslStream client = new SslStream(recordingClientStream))
            using (SslStream server = new SslStream(serverStream))
            using (cert)
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = cert,
                    ClientCertificateRequired = true,
                    EnabledSslProtocols = protocol,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = cert.GetNameInfo(X509NameType.DnsName, false),
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = delegate { return true; },
                    ClientCertificates = new X509CertificateCollection { cert },
                    EnabledSslProtocols = protocol,
                };

                Task serverTask = server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);
                Task clientTask = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);

                // Server should fail because the validation failed locally, and it should send an alert.
                AuthenticationException serverException = await Assert.ThrowsAsync<AuthenticationException>(() => serverTask).WaitAsync(TestConfiguration.PassingTestTimeout);

                // Client side should receive the alert and fail the handshake, the exact timing depends on the platform
                // Windows: after the handshake, during data exchange
                // Linux: during the handshake, TLS 1.3 sends the alert after the handshake
                // macOS: during the handshake
                Exception exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
                {
                    await clientTask;
                    byte[] buffer = new byte[1];
                    await client.WriteAsync(buffer).AsTask().WaitAsync(TestConfiguration.PassingTestTimeout);
                    await client.ReadAsync(buffer).AsTask().WaitAsync(TestConfiguration.PassingTestTimeout);
                }).WaitAsync(TestConfiguration.PassingTestTimeout);

                if (PlatformDetection.IsWindows)
                {
                    Assert.NotNull(exception.InnerException);
                    Assert.IsType<IOException>(exception);
                    Assert.IsType<Win32Exception>(exception.InnerException);
                }
                else if (PlatformDetection.IsLinux)
                {
                    Assert.NotNull(exception.InnerException);
                    if (protocol == SslProtocols.Tls13)
                    {
                        // failure during app data (read)
                        Assert.IsType<IOException>(exception);
                    }
                    else
                    {
                        // failure during handshake
                        Assert.IsType<AuthenticationException>(exception);
                    }

                    Assert.Contains("SslException", exception.InnerException.GetType().Name);
                    Assert.NotNull(exception.InnerException.InnerException);
                    Assert.Contains("alert", exception.InnerException.InnerException.Message);
                }
                else if (PlatformDetection.IsOSX)
                {
                    // Cross-platform parity: the cert validation exception should surface directly
                    // to the caller (matching Windows/Linux/Android behavior via SendAuthResetSignal),
                    // not wrapped in a generic AuthenticationException(SR.net_auth_SSPI, ...).
                    Assert.Null(serverException.InnerException);

                    // Verify a fatal TLS alert reached the peer. SecureTransport may surface
                    // an untrusted-chain failure as either UnknownCA or BadCertificate depending
                    // on the negotiated protocol; both are valid "cert rejected" signals.
                    Assert.True(
                        recordingClientStream.ContainsAlert(TlsAlertDescription.UnknownCA) ||
                        recordingClientStream.ContainsAlert(TlsAlertDescription.BadCertificate),
                        recordingClientStream.GetRecordedAlerts());
                }
            }
        }

        private bool FailClientCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return false;
        }

        public static IEnumerable<object[]> SupportedSslProtocolsExcludingMacOSSsl3()
        {
#pragma warning disable 0618 // SSL2/3 are deprecated
            // SecureTransport on modern macOS won't negotiate SSL 3.0, so handshakes hang
            // until they time out. Mask Ssl3 out on macOS so xunit reports the data point
            // as filtered rather than silently passing.
            SslProtocols mask = PlatformDetection.IsOSX ? ~SslProtocols.Ssl3 : (SslProtocols)~0;
#pragma warning restore 0618
            foreach (SslProtocols protocol in SslProtocolSupport.EnumerateSupportedProtocols(mask))
            {
                yield return new object[] { protocol };
            }
        }

        private bool AllowAnyServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private sealed class RecordingReadStream : DelegatingStream
        {
            private readonly List<byte> _readBytes = new List<byte>();

            public RecordingReadStream(Stream innerStream)
                : base(innerStream)
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRead = base.Read(buffer, offset, count);
                Record(new ReadOnlySpan<byte>(buffer, offset, bytesRead));
                return bytesRead;
            }

            public override int Read(Span<byte> buffer)
            {
                int bytesRead = base.Read(buffer);
                Record(buffer.Slice(0, bytesRead));
                return bytesRead;
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                int bytesRead = await base.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                Record(new ReadOnlySpan<byte>(buffer, offset, bytesRead));
                return bytesRead;
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                int bytesRead = await base.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                Record(buffer.Span.Slice(0, bytesRead));
                return bytesRead;
            }

            public bool ContainsAlert(TlsAlertDescription expectedDescription)
            {
                ReadOnlySpan<byte> remaining = GetRecordedBytes();
                while (remaining.Length >= TlsFrameHelper.HeaderSize)
                {
                    TlsFrameHeader header = default;
                    if (!TlsFrameHelper.TryGetFrameHeader(remaining, ref header) ||
                        header.Length <= 0 ||
                        header.Length > remaining.Length)
                    {
                        return false;
                    }

                    ReadOnlySpan<byte> frame = remaining.Slice(0, header.Length);
                    if (header.Type == TlsContentType.Alert)
                    {
                        TlsAlertLevel level = default;
                        TlsAlertDescription description = default;
                        if (TlsFrameHelper.TryGetAlertInfo(frame, ref level, ref description) &&
                            level == TlsAlertLevel.Fatal &&
                            description == expectedDescription)
                        {
                            return true;
                        }
                    }

                    remaining = remaining.Slice(header.Length);
                }

                return false;
            }

            public string GetRecordedAlerts()
            {
                List<string> alerts = new List<string>();
                ReadOnlySpan<byte> remaining = GetRecordedBytes();
                while (remaining.Length >= TlsFrameHelper.HeaderSize)
                {
                    TlsFrameHeader header = default;
                    if (!TlsFrameHelper.TryGetFrameHeader(remaining, ref header) ||
                        header.Length <= 0 ||
                        header.Length > remaining.Length)
                    {
                        break;
                    }

                    ReadOnlySpan<byte> frame = remaining.Slice(0, header.Length);
                    if (header.Type == TlsContentType.Alert)
                    {
                        TlsAlertLevel level = default;
                        TlsAlertDescription description = default;
                        if (TlsFrameHelper.TryGetAlertInfo(frame, ref level, ref description))
                        {
                            alerts.Add($"{level}:{description}");
                        }
                    }

                    remaining = remaining.Slice(header.Length);
                }

                return alerts.Count == 0 ? "No TLS alerts recorded." : "Recorded TLS alerts: " + string.Join(", ", alerts);
            }

            private void Record(ReadOnlySpan<byte> bytes)
            {
                lock (_readBytes)
                {
                    foreach (byte b in bytes)
                    {
                        _readBytes.Add(b);
                    }
                }
            }

            private byte[] GetRecordedBytes()
            {
                lock (_readBytes)
                {
                    return _readBytes.ToArray();
                }
            }
        }
    }
}
