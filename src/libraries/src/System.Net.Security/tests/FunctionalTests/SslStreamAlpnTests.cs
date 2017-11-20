// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamAlpnTests
    {
        private async Task DoHandshakeWithOptions(SslStream clientSslStream, SslStream serverSslStream, SslClientAuthenticationOptions clientOptions, SslServerAuthenticationOptions serverOptions)
        {
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                clientOptions.RemoteCertificateValidationCallback = AllowAnyServerCertificate;
                clientOptions.TargetHost = certificate.GetNameInfo(X509NameType.SimpleName, false);
                serverOptions.ServerCertificate = certificate;

                Task t1 = clientSslStream.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = serverSslStream.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }

        protected bool AllowAnyServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            SslPolicyErrors expectedSslPolicyErrors = SslPolicyErrors.None;

            if (!Capability.IsTrustedRootCertificateInstalled())
            {
                expectedSslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
            }

            Assert.Equal(expectedSslPolicyErrors, sslPolicyErrors);

            if (sslPolicyErrors == expectedSslPolicyErrors)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_DuplicateOptions_Throws()
        {
            RemoteCertificateValidationCallback rCallback = (sender, certificate, chain, errors) => { return true; };
            LocalCertificateSelectionCallback lCallback = (sender, host, localCertificates, remoteCertificate, issuers) => { return null; };

            VirtualNetwork network = new VirtualNetwork();
            using (var clientStream = new VirtualNetworkStream(network, false))
            using (var serverStream = new VirtualNetworkStream(network, true))
            using (var client = new SslStream(clientStream, false, rCallback, lCallback, EncryptionPolicy.RequireEncryption))
            using (var server = new SslStream(serverStream, false, rCallback))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions();
                clientOptions.RemoteCertificateValidationCallback = AllowAnyServerCertificate;
                clientOptions.TargetHost = certificate.GetNameInfo(X509NameType.SimpleName, false);

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions();
                serverOptions.ServerCertificate = certificate;
                serverOptions.RemoteCertificateValidationCallback = AllowAnyServerCertificate;

                Task t1 = Assert.ThrowsAsync<InvalidOperationException>(() => client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None));
                Task t2 = Assert.ThrowsAsync<InvalidOperationException>(() => server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None));

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }

        [Theory]
        [MemberData(nameof(Alpn_TestData))]
        public async Task SslStream_StreamToStream_Alpn_Success(List<SslApplicationProtocol> clientProtocols, List<SslApplicationProtocol> serverProtocols, SslApplicationProtocol expected)
        {
            VirtualNetwork network = new VirtualNetwork();
            using (var clientStream = new VirtualNetworkStream(network, false))
            using (var serverStream = new VirtualNetworkStream(network, true))
            using (var client = new SslStream(clientStream, false))
            using (var server = new SslStream(serverStream, false))
            {
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    ApplicationProtocols = clientProtocols,
                };

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ApplicationProtocols = serverProtocols,
                };

                await DoHandshakeWithOptions(client, server, clientOptions, serverOptions);

                Assert.Equal(expected, client.NegotiatedApplicationProtocol);
                Assert.Equal(expected, server.NegotiatedApplicationProtocol);
            }
        }

        [Fact]
        [ActiveIssue(24853)]
        [PlatformSpecific(~TestPlatforms.OSX)]
        public async Task SslStream_StreamToStream_Alpn_NonMatchingProtocols_Fail()
        {
            VirtualNetwork network = new VirtualNetwork();
            using (var clientStream = new VirtualNetworkStream(network, false))
            using (var serverStream = new VirtualNetworkStream(network, true))
            using (var client = new SslStream(clientStream, false))
            using (var server = new SslStream(serverStream, false))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 },
                    RemoteCertificateValidationCallback = AllowAnyServerCertificate,
                    TargetHost = certificate.GetNameInfo(X509NameType.SimpleName, false),
                };

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 },
                    ServerCertificate = certificate,
                };

                Task t1 = Assert.ThrowsAsync<AuthenticationException>(() => client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None));
                Task t2 = Assert.ThrowsAsync<AuthenticationException>(() => server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None));

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }

        internal static IEnumerable<object[]> Alpn_TestData()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                yield return new object[] { new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 }, new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 }, null };
                yield return new object[] { new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 }, new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 }, null };
                yield return new object[] { new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 }, new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 }, null };
                yield return new object[] { null, new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 }, null };
                yield return new object[] { new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 }, null, null };
                yield return new object[] { new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 }, new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 }, null };
                yield return new object[] { null, null, null };
            }
            else
            {
                // Works on linux distros with openssl 1.0.2, CI machines Ubuntu14.04 and Debian 87 don't have openssl 1.0.2
                // Works on Windows OSes > 7.0
                bool featureWorks = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !(PlatformDetection.IsUbuntu1404 || PlatformDetection.IsDebian8)) ||
                                    (PlatformDetection.IsWindows && !PlatformDetection.IsWindows7);

                yield return new object[] { new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 }, new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 }, featureWorks ? SslApplicationProtocol.Http2 : default };
                yield return new object[] { new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 }, new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 }, featureWorks ? SslApplicationProtocol.Http11 : default };
                yield return new object[] { new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 }, new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 }, featureWorks ? SslApplicationProtocol.Http11 : default };
                yield return new object[] { null, new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 }, default(SslApplicationProtocol) };
                yield return new object[] { new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 }, null, default(SslApplicationProtocol) };
                yield return new object[] { null, null, default(SslApplicationProtocol) };
            }
        }
    }
}
