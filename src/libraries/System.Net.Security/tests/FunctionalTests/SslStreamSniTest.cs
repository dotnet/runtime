// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamSniTest
    {
        [Theory]
        [MemberData(nameof(HostNameData))]
        public async Task SslStream_ClientSendsSNIServerReceives_Ok(string hostName)
        {
            using X509Certificate serverCert = Configuration.Certificates.GetSelfSignedServerCertificate();
            bool validationCallbackCalled = false;

            await WithVirtualConnection(async (server, client) =>
                {
                    Task clientJob = Task.Run(() =>
                    {
                        client.AuthenticateAsClient(hostName);
                    });

                    SslServerAuthenticationOptions options = DefaultServerOptions();
                    options.AllowTlsResume = false;

                    int timesCallbackCalled = 0;
                    options.ServerCertificateSelectionCallback = (sender, actualHostName) =>
                    {
                        timesCallbackCalled++;
                        Assert.Equal(hostName, actualHostName);
                        return serverCert;
                    };

                    await TaskTimeoutExtensions.WhenAllOrAnyFailed(new[] { clientJob, server.AuthenticateAsServerAsync(options, CancellationToken.None) });

                    Assert.Equal(1, timesCallbackCalled);
                    Assert.Equal(hostName, server.TargetHostName);
                    Assert.Equal(hostName, client.TargetHostName);
                },
                (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
                {
                    validationCallbackCalled = true;
                    Assert.Equal(serverCert, certificate);
                    return true;
                }
            );
            Assert.True(validationCallbackCalled);
        }

        [Theory]
        [MemberData(nameof(HostNameData))]
        public async Task SslStream_ServerCallbackAndLocalCertificateSelectionSet_Throws(string hostName)
        {
            using X509Certificate serverCert = Configuration.Certificates.GetSelfSignedServerCertificate();

            int timesCallbackCalled = 0;

            var selectionCallback = new LocalCertificateSelectionCallback((object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] issuers) =>
            {
                Assert.Fail("LocalCertificateSelectionCallback called when AuthenticateAsServerAsync was expected to fail.");
                return null;
            });

            var validationCallback = new RemoteCertificateValidationCallback((object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                Assert.Equal(serverCert, certificate);
                return true;
            });

            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (SslStream server = new SslStream(stream1, false, null, selectionCallback),
                             client = new SslStream(stream2, leaveInnerStreamOpen: false, validationCallback))
            {
                Task clientJob = Task.Run(() =>
                {
                    client.AuthenticateAsClient(hostName);
                    Assert.Fail("RemoteCertificateValidationCallback called when AuthenticateAsServerAsync was expected to fail.");
                });

                SslServerAuthenticationOptions options = DefaultServerOptions();
                options.ServerCertificateSelectionCallback = (sender, actualHostName) =>
                {
                    timesCallbackCalled++;
                    Assert.Equal(hostName, actualHostName);
                    return serverCert;
                };

                await Assert.ThrowsAsync<InvalidOperationException>(() => server.AuthenticateAsServerAsync(options, CancellationToken.None));

                Assert.Equal(0, timesCallbackCalled);
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(HostNameData))]
        public async Task SslStream_ServerCallbackNotSet_UsesLocalCertificateSelection(string hostName)
        {
            if (PlatformDetection.IsAndroid && hostName.ToCharArray().Any(c => !char.IsAscii(c)))
                throw new SkipTestException("Android does not support non-ASCII host names");

            using X509Certificate serverCert = Configuration.Certificates.GetSelfSignedServerCertificate();

            int timesCallbackCalled = 0;
            bool validationCallbackCalled = false;

            var selectionCallback = new LocalCertificateSelectionCallback((object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] issuers) =>
            {
                Assert.Equal(string.Empty, targetHost);
                Assert.True(localCertificates.Contains(serverCert));
                timesCallbackCalled++;
                return serverCert;
            });

            var validationCallback = new RemoteCertificateValidationCallback((object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                validationCallbackCalled = true;
                Assert.Equal(serverCert, certificate);
                return true;
            });

            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (SslStream server = new SslStream(stream1, false, null, selectionCallback),
                             client = new SslStream(stream2, leaveInnerStreamOpen: false, validationCallback))
            {
                Task clientJob = Task.Run(() =>
                {
                    client.AuthenticateAsClient(hostName);
                });

                SslServerAuthenticationOptions options = DefaultServerOptions();
                options.AllowTlsResume = false;
                options.ServerCertificate = serverCert;

                await TaskTimeoutExtensions.WhenAllOrAnyFailed(new[] { clientJob, server.AuthenticateAsServerAsync(options, CancellationToken.None) });

                Assert.Equal(1, timesCallbackCalled);
                Assert.True(validationCallbackCalled);
            }
        }

        [Fact]
        [SkipOnCoreClr("System.Net.Tests are flaky and/or long running: https://github.com/dotnet/runtime/issues/131", ~RuntimeConfiguration.Release)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/131", TestRuntimes.Mono)] // System.Net.Tests are flaky and/or long running
        public async Task SslStream_NoSniFromClient_CallbackReturnsNull()
        {
            await WithVirtualConnection(async (server, client) =>
            {
                Task clientJob = Task.Run(() =>
                {
                    Assert.Throws<IOException>(() =>
                        client.AuthenticateAsClient("test")
                    );
                });

                int timesCallbackCalled = 0;
                SslServerAuthenticationOptions options = DefaultServerOptions();
                options.ServerCertificateSelectionCallback = (sender, actualHostName) =>
                {
                    timesCallbackCalled++;
                    return null;
                };

                var cts = new CancellationTokenSource();
                await Assert.ThrowsAsync<AuthenticationException>(WithAggregateExceptionUnwrapping(async () =>
                    await server.AuthenticateAsServerAsync(options, cts.Token)
                ));

                // to break connection so that client is not waiting
                server.Dispose();

                Assert.Equal(1, timesCallbackCalled);

                await clientJob;
            },
            (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                return true;
            });
        }
        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("::1")]
        [InlineData("2001:11:22::1")]
        [InlineData("fe80::9c3a:b64d:6249:1de8%2")]
        [InlineData("fe80::9c3a:b64d:6249:1de8")]
        public async Task SslStream_IpLiteral_NotSend(string target)
        {
            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
            {
                TargetHost = target,
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
            };
            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions()
            {
                ServerCertificate = Configuration.Certificates.GetServerCertificate(),
            };

            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions, default),
                        server.AuthenticateAsServerAsync(serverOptions, default));

            Assert.Equal(string.Empty, server.TargetHostName);
            Assert.Equal(target, client.TargetHostName);
        }

        public static TheoryData<string, string, string?, string?, string, bool> IpLiteralCertificateNameValidationData()
        {
            TheoryData<string, string, string?, string?, string, bool> data = new();

            // Without a DNS SAN, a matching CN is accepted even if an IP SAN contains a different address.
            AddCase("IPv4: CN matches, no SAN",
                commonName: "192.0.2.1", targetHost: "192.0.2.1", expectNameMatch: true);
            AddCase("IPv4: CN mismatches, no SAN",
                commonName: "192.0.2.1", targetHost: "192.0.2.2", expectNameMatch: false);
            AddCase("IPv4: CN matches, IP SAN mismatches",
                commonName: "192.0.2.1", targetHost: "192.0.2.1", expectNameMatch: true,
                sanIpAddress: "192.0.2.2");
            AddCase("IPv4: IP SAN matches, CN mismatches",
                commonName: "192.0.2.1", targetHost: "192.0.2.2", expectNameMatch: true,
                sanIpAddress: "192.0.2.2");
            AddCase("IPv4: neither CN nor IP SAN matches",
                commonName: "192.0.2.1", targetHost: "192.0.2.3", expectNameMatch: false,
                sanIpAddress: "192.0.2.2");

            // A DNS SAN suppresses CN fallback, but does not prevent an IP SAN from matching.
            AddCase("IPv4: DNS SAN blocks matching CN",
                commonName: "192.0.2.1", targetHost: "192.0.2.1", expectNameMatch: false,
                sanDnsName: "example.test");
            AddCase("IPv4: DNS SAN blocks CN; IP SAN mismatches",
                commonName: "192.0.2.1", targetHost: "192.0.2.1", expectNameMatch: false,
                sanIpAddress: "192.0.2.2", sanDnsName: "example.test");
            AddCase("IPv4: numeric DNS SAN blocks CN; IP SAN mismatches",
                commonName: "192.168.2.1", targetHost: "192.168.2.1", expectNameMatch: false,
                sanIpAddress: "192.168.2.3", sanDnsName: "192.168.2.2");
            AddCase("IPv4: IP SAN matches, DNS SAN mismatches",
                commonName: "192.0.2.1", targetHost: "192.0.2.2", expectNameMatch: true,
                sanIpAddress: "192.0.2.2", sanDnsName: "example.test");
            AddCase("IPv4: IP SAN matches, numeric DNS SAN mismatches",
                commonName: "192.168.2.1", targetHost: "192.168.2.3", expectNameMatch: true,
                sanIpAddress: "192.168.2.3", sanDnsName: "192.168.2.2");

            // A numeric DNS SAN can match the IP target as text, independently of CN.
            AddCase("IPv4: numeric DNS SAN and CN match",
                commonName: "192.0.2.1", targetHost: "192.0.2.1", expectNameMatch: true,
                sanDnsName: "192.0.2.1");
            AddCase("IPv4: numeric DNS SAN matches, CN unrelated",
                commonName: "other.test", targetHost: "192.0.2.1", expectNameMatch: true,
                sanDnsName: "192.0.2.1");
            AddCase("IPv4: numeric DNS SAN matches, IP SAN and CN mismatch",
                commonName: "192.168.2.1", targetHost: "192.168.2.2", expectNameMatch: true,
                sanIpAddress: "192.168.2.3", sanDnsName: "192.168.2.2");
            AddCase("IPv4: wildcard DNS SAN does not match",
                commonName: "192.0.2.1", targetHost: "192.0.2.1", expectNameMatch: false,
                sanDnsName: "*.0.2.1");
            AddCase("IPv4: wildcard CN does not match",
                commonName: "*.0.2.1", targetHost: "192.0.2.1", expectNameMatch: false);

            // IPv6 follows the same CN and SAN precedence rules.
            AddCase("IPv6: CN matches, no SAN",
                commonName: "2001:db8::1", targetHost: "2001:db8::1", expectNameMatch: true);
            AddCase("IPv6: CN mismatches, no SAN",
                commonName: "2001:db8::1", targetHost: "2001:db8::2", expectNameMatch: false);
            AddCase("IPv6: CN matches, IP SAN mismatches",
                commonName: "2001:db8::1", targetHost: "2001:db8::1", expectNameMatch: true,
                sanIpAddress: "2001:db8::2");
            AddCase("IPv6: IP SAN matches, CN mismatches",
                commonName: "2001:db8::1", targetHost: "2001:db8::2", expectNameMatch: true,
                sanIpAddress: "2001:db8::2");
            AddCase("IPv6: neither CN nor IP SAN matches",
                commonName: "2001:db8::1", targetHost: "2001:db8::3", expectNameMatch: false,
                sanIpAddress: "2001:db8::2");
            AddCase("IPv6: DNS SAN blocks matching CN",
                commonName: "2001:db8::1", targetHost: "2001:db8::1", expectNameMatch: false,
                sanDnsName: "example.test");
            AddCase("IPv6: DNS SAN blocks CN; IP SAN mismatches",
                commonName: "2001:db8::1", targetHost: "2001:db8::1", expectNameMatch: false,
                sanIpAddress: "2001:db8::2", sanDnsName: "example.test");
            AddCase("IPv6: IP SAN matches, DNS SAN mismatches",
                commonName: "2001:db8::1", targetHost: "2001:db8::2", expectNameMatch: true,
                sanIpAddress: "2001:db8::2", sanDnsName: "example.test");
            AddCase("IPv6: numeric DNS SAN and CN match",
                commonName: "2001:db8::1", targetHost: "2001:db8::1", expectNameMatch: true,
                sanDnsName: "2001:db8::1");
            AddCase("IPv6: numeric DNS SAN matches, CN unrelated",
                commonName: "other.test", targetHost: "2001:db8::1", expectNameMatch: true,
                sanDnsName: "2001:db8::1");

            // Equivalent IPv6 addresses with different spellings do not match in numeric DNS SANs.
            AddCase("IPv6: expanded DNS vs compressed target mismatch",
                commonName: "other.test", targetHost: "2001:db8::1", expectNameMatch: false,
                sanDnsName: "2001:0db8:0:0:0:0:0:1");
            AddCase("IPv6: compressed DNS vs expanded target mismatch",
                commonName: "other.test", targetHost: "2001:0db8:0:0:0:0:0:1", expectNameMatch: false,
                sanDnsName: "2001:db8::1");

            return data;

            void AddCase(
                string scenario, string commonName, string targetHost, bool expectNameMatch,
                string? sanIpAddress = null, string? sanDnsName = null)
            {
                data.Add(scenario, commonName, sanIpAddress, sanDnsName, targetHost, expectNameMatch);
            }
        }

        [Theory]
        [MemberData(nameof(IpLiteralCertificateNameValidationData))]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_IpLiteral_CertificateNameValidation(
            string scenario, string commonName, string? sanIpAddress, string? sanDnsName, string targetHost, bool expectNameMatch)
        {
            // The scenario describes the rule being checked and appears in the test case name.
            _ = scenario;

            using RSA key = RSA.Create(2048);
            CertificateRequest request = new CertificateRequest(
                $"CN={commonName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            if (sanIpAddress is not null || sanDnsName is not null)
            {
                SubjectAlternativeNameBuilder san = new SubjectAlternativeNameBuilder();

                // Preserve numeric DNS SANs as text; only IP SANs are encoded as address bytes.
                if (sanDnsName is not null)
                {
                    san.AddDnsName(sanDnsName);
                }

                if (sanIpAddress is not null)
                {
                    san.AddIpAddress(IPAddress.Parse(sanIpAddress));
                }

                request.CertificateExtensions.Add(san.Build());
            }

            using X509Certificate2 certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            using X509Certificate2 serverCertificate = X509CertificateLoader.LoadPkcs12(
                certificate.Export(X509ContentType.Pkcs12), (string?)null);

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                SslPolicyErrors? observedErrors = null;
                // Disable resumption on both peers so every case validates its own certificate.
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    // Validate this IP literal, independently of the underlying transport.
                    TargetHost = targetHost,
                    AllowTlsResume = false,
                    // Trust the test certificate so chain errors do not obscure name-matching results.
                    CertificateChainPolicy = new X509ChainPolicy
                    {
                        TrustMode = X509ChainTrustMode.CustomRootTrust,
                        CustomTrustStore = { certificate },
                        RevocationMode = X509RevocationMode.NoCheck,
                    },
                    RemoteCertificateValidationCallback = (sender, remoteCertificate, chain, errors) =>
                    {
                        observedErrors = errors;
                        // Let mismatch cases complete TLS and payload checks; assert the exact errors below.
                        return true;
                    },
                };
                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCertificate,
                    AllowTlsResume = false,
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions));

                Assert.Equal(
                    expectNameMatch ? SslPolicyErrors.None : SslPolicyErrors.RemoteCertificateNameMismatch,
                    observedErrors);
                await TestHelper.PingPong(client, server);
            }
        }

        [Theory]
        [InlineData("\u00E1b\u00E7d\u00EB.com")]
        [InlineData("\u05D1\u05F1.com")]
        [InlineData("\u30B6\u30C7\u30D8.com")]
        public async Task SslStream_ValidIdn_Success(string name)
        {
            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                using X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate();
                using X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate();

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = serverCertificate };
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
                {
                    TargetHost = name,
                    CertificateChainPolicy = new X509ChainPolicy() { VerificationFlags = X509VerificationFlags.IgnoreInvalidName },
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions, default),
                                server.AuthenticateAsServerAsync(serverOptions, default));

                await TestHelper.PingPong(client, server, default);
                Assert.Equal(name, server.TargetHostName);
            }
        }

        [Fact]
        public async Task UnencodedHostName_ValidatesCertificate()
        {
            string rawHostname = "räksmörgås.josefsson.org";
            string punycodeHostname = "xn--rksmrgs-5wao1o.josefsson.org";

            using Configuration.Certificates.PkiHolder pkiHolder = Configuration.Certificates.GenerateCertificates(punycodeHostname);

            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions()
            {
                ServerCertificateContext = pkiHolder.CreateSslStreamCertificateContext(),
            };

            SslClientAuthenticationOptions clientOptions = new()
            {
                TargetHost = rawHostname,
                CertificateChainPolicy = new X509ChainPolicy()
                {
                    RevocationMode = X509RevocationMode.NoCheck,
                    TrustMode = X509ChainTrustMode.CustomRootTrust,
                    CustomTrustStore = { pkiHolder.IssuerChain[^1] }
                }
            };

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();

            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                            client.AuthenticateAsClientAsync(clientOptions, default),
                            server.AuthenticateAsServerAsync(serverOptions, default));

            await TestHelper.PingPong(client, server, default);
            Assert.Equal(rawHostname, server.TargetHostName);
            Assert.Equal(rawHostname, client.TargetHostName);
        }

        [ConditionalTheory]
        [InlineData("www-.volal.cz")]
        [InlineData("www-.colorhexa.com")]
        [InlineData("xn--www-7m0a.thegratuit.com")]
        [SkipOnPlatform(TestPlatforms.Android, "Safe invalid IDN hostnames are not supported on Android")]
        public async Task SslStream_SafeInvalidIdn_Success(string name)
        {
            if (PlatformDetection.IsNetworkFrameworkEnabled())
            {
                throw new SkipTestException("Safe invalid IDN hostnames are not supported on Network.framework");
            }

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                using X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate();
                using X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate();

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = serverCertificate };
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
                {
                    TargetHost = name,
                    CertificateChainPolicy = new X509ChainPolicy() { VerificationFlags = X509VerificationFlags.IgnoreInvalidName },
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions, default),
                                server.AuthenticateAsServerAsync(serverOptions, default));

                await TestHelper.PingPong(client, server, default);
                Assert.Equal(name, server.TargetHostName);
                Assert.Equal(name, client.TargetHostName);
            }
        }

        [Theory]
        [InlineData("\u0000\u00E7d\u00EB.com")]
        public async Task SslStream_UnsafeInvalidIdn_Throws(string name)
        {
            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                using X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate();

                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions()
                {
                    TargetHost = name,
                    CertificateChainPolicy = new X509ChainPolicy() { VerificationFlags = X509VerificationFlags.IgnoreInvalidName },
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
                };

                await Assert.ThrowsAsync<ArgumentException>(() => client.AuthenticateAsClientAsync(clientOptions, default));
            }
        }

        private static Func<Task> WithAggregateExceptionUnwrapping(Func<Task> a)
        {
            return async () =>
            {
                try
                {
                    await a();
                }
                catch (AggregateException e)
                {
                    throw e.InnerException;
                }
            };
        }

        private static SslServerAuthenticationOptions DefaultServerOptions()
        {
            return new SslServerAuthenticationOptions()
            {
                ClientCertificateRequired = false,
                EnabledSslProtocols = SslProtocols.None,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
            };
        }

        private async Task WithVirtualConnection(Func<SslStream, SslStream, Task> serverClientConnection, RemoteCertificateValidationCallback clientCertValidate)
        {
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (SslStream server = new SslStream(serverStream, leaveInnerStreamOpen: false),
                             client = new SslStream(clientStream, leaveInnerStreamOpen: false, clientCertValidate))
            {
                await serverClientConnection(server, client);
            }
        }

        public static IEnumerable<object[]> HostNameData()
        {
            if (OperatingSystem.IsAndroid())
            {
                yield return new object[] { "localhost" };
                yield return new object[] { "dot.net" };
                // max allowed hostname length is 63
                yield return new object[] { $"{new string('a', 59)}.net" };
                yield return new object[] { "\u017C\u00F3\u0142\u0107g\u0119\u015Bl\u0105ja\u017A\u0144.\u7EA2\u70E7.\u7167\u308A\u713C\u304D" };
                yield break;
            }

            yield return new object[] { "a" };
            yield return new object[] { "test" };
            // max allowed hostname length is 63
            yield return new object[] { new string('a', 63) };
            yield return new object[] { "\u017C\u00F3\u0142\u0107g\u0119\u015Bl\u0105ja\u017A\u0144.\u7EA2\u70E7.\u7167\u308A\u713C\u304D" };
        }
    }
}
