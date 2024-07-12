// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class SslStreamAlpnTestBase
    {
        private static bool BackendSupportsAlpn => PlatformDetection.SupportsAlpn;
        private static bool ClientSupportsAlpn => PlatformDetection.SupportsClientAlpn;
        readonly ITestOutputHelper _output;
        public static readonly object[][] Http2Servers = Configuration.Http.Http2Servers;

        // Whether AuthenticateAs(Client/Server) or AuthenticateAs(Client/Server)Async will be called
        public abstract bool TestAuthenticateAsync { get; }

        protected SslStreamAlpnTestBase(ITestOutputHelper output)
        {
            _output = output;
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

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
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

                Task t1 = Assert.ThrowsAsync<InvalidOperationException>(() => client.AuthenticateAsClientAsync(TestAuthenticateAsync, clientOptions));
                Task t2 = Assert.ThrowsAsync<InvalidOperationException>(() => server.AuthenticateAsServerAsync(TestAuthenticateAsync, serverOptions));

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }

        [Theory]
        [MemberData(nameof(Alpn_TestData))]
        [ActiveIssue("ABC", TestPlatforms.OSX)]
        public async Task SslStream_StreamToStream_Alpn_Success(SslProtocols protocol, List<SslApplicationProtocol> clientProtocols, List<SslApplicationProtocol> serverProtocols, SslApplicationProtocol expected)
        {
            using X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate();

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
            {
                ApplicationProtocols = clientProtocols,
                EnabledSslProtocols = protocol,
                RemoteCertificateValidationCallback = delegate { return true; },
                TargetHost = Guid.NewGuid().ToString("N"),
            };

            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
            {
                ApplicationProtocols = serverProtocols,
                EnabledSslProtocols = protocol,
                ServerCertificateContext = SslStreamCertificateContext.Create(certificate, null)
            };

            // We do multiple loops to also cover credential cache and TLS resume.
            for (int i = 0; i < 3; i++)
            {
                (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
                using (clientStream)
                using (serverStream)
                using (var client = new SslStream(clientStream, false))
                using (var server = new SslStream(serverStream, false))
                {
                    Task t1 = client.AuthenticateAsClientAsync(TestAuthenticateAsync, clientOptions);
                    Task t2 = server.AuthenticateAsServerAsync(TestAuthenticateAsync, serverOptions);

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);

                    Assert.Equal(expected, client.NegotiatedApplicationProtocol);
                    Assert.Equal(expected, server.NegotiatedApplicationProtocol);

                    await TestHelper.PingPong(client, server);
                    await TestHelper.PingPong(server, client);

                    Assert.Equal(expected, client.NegotiatedApplicationProtocol);
                    Assert.Equal(expected, server.NegotiatedApplicationProtocol);
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task SslStream_StreamToStream_Alpn_NonMatchingProtocols_Fail()
        {
            (SslStream clientStream, SslStream serverStream) = TestHelper.GetConnectedSslStreams();

            using (serverStream)
            using (clientStream)
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 },
                    ServerCertificate = certificate,
                };
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 },
                    RemoteCertificateValidationCallback = AllowAnyServerCertificate,
                    TargetHost = certificate.GetNameInfo(X509NameType.SimpleName, false),
                };

                // Test ALPN failure only on platforms that supports ALPN.
                if (BackendSupportsAlpn)
                {
                    Task t1 = Assert.ThrowsAsync<AuthenticationException>(() => clientStream.AuthenticateAsClientAsync(TestAuthenticateAsync, clientOptions));
                    await Assert.ThrowsAsync<AuthenticationException>(() => serverStream.AuthenticateAsServerAsync(TestAuthenticateAsync, serverOptions).WaitAsync(TestConfiguration.PassingTestTimeout));
                    serverStream.Dispose();

                    await t1.WaitAsync(TestConfiguration.PassingTestTimeout);
                }
                else
                {
                    Task t1 = clientStream.AuthenticateAsClientAsync(TestAuthenticateAsync, clientOptions);
                    Task t2 = serverStream.AuthenticateAsServerAsync(TestAuthenticateAsync, serverOptions);

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);

                    Assert.Equal(default(SslApplicationProtocol), clientStream.NegotiatedApplicationProtocol);
                    Assert.Equal(default(SslApplicationProtocol), serverStream.NegotiatedApplicationProtocol);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(ClientSupportsAlpn))]
        [MemberData(nameof(Http2Servers))]
        public async Task SslStream_Http2_Alpn_Success(Uri server)
        {
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(server.Host, server.Port);
                    using (SslStream clientStream = new SslStream(client.GetStream(), leaveInnerStreamOpen: false))
                    {
                        SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                        {
                            ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 },
                            TargetHost = server.Host
                        };

                        await clientStream.AuthenticateAsClientAsync(TestAuthenticateAsync, clientOptions);
                        Assert.Equal("h2", clientStream.NegotiatedApplicationProtocol.ToString());
                    }
                }
                catch (Exception e)
                {
                    // Failures to connect do not cause test failure.
                    _output.WriteLine("Unable to connect: {0}", e);
                }
            }
        }

        public static IEnumerable<object[]> Alpn_TestData()
        {
            SslApplicationProtocol h1 = SslApplicationProtocol.Http11;
            SslApplicationProtocol h2 = SslApplicationProtocol.Http2;
            List<SslApplicationProtocol> list_empty = [];
            List<SslApplicationProtocol> list_h1 = [h1];
            List<SslApplicationProtocol> list_h2 = [h2];
            List<SslApplicationProtocol> list_both = [h1, h2];

            foreach (var protocol in new SslProtocolSupport.SupportedSslProtocolsTestData().Concat(new[] { new object[] { SslProtocols.None } }))
            {
                var proto = protocol[0];
#pragma warning disable 0618 // SSL2/3 are deprecated
                if (proto.Equals(SslProtocols.Ssl3) || proto.Equals(SslProtocols.Ssl2))
#pragma warning restore 0618
                {
                    // ALPN not supported by this protocol
                    continue;
                }

                yield return new object[] { proto, list_both, list_h2, BackendSupportsAlpn ? h2 : default };
                yield return new object[] { proto, list_h1, list_both, BackendSupportsAlpn ? h1 : default };

                yield return new object[] { proto, list_both, list_both, BackendSupportsAlpn ? h1 : default };
                yield return new object[] { proto, null, list_both, default(SslApplicationProtocol) };
                yield return new object[] { proto, list_both, list_empty, default(SslApplicationProtocol) };
                yield return new object[] { proto, list_both, null, default(SslApplicationProtocol) };

                yield return new object[] { proto, list_empty, list_empty, default(SslApplicationProtocol) };
                yield return new object[] { proto, null, list_empty, default(SslApplicationProtocol) };
                yield return new object[] { proto, list_empty, null, default(SslApplicationProtocol) };
                yield return new object[] { proto, null, null, default(SslApplicationProtocol) };
            }
        }
    }

    public sealed class SslStreamAlpnTest_Async : SslStreamAlpnTestBase
    {
        public override bool TestAuthenticateAsync => true;

        public SslStreamAlpnTest_Async(ITestOutputHelper output)
            : base(output) { }
    }

    public sealed class SslStreamAlpnTest_Sync : SslStreamAlpnTestBase
    {
        public override bool TestAuthenticateAsync => false;

        public SslStreamAlpnTest_Sync(ITestOutputHelper output)
            : base(output) { }
    }
}
