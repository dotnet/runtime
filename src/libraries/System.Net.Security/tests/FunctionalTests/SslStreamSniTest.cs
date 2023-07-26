// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Net.Sockets;
using System.Security.Authentication;
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task SslStream_ClientSendsSNIServerReceives_Ok(string hostName)
        {
            using X509Certificate serverCert = Configuration.Certificates.GetSelfSignedServerCertificate();

            await WithVirtualConnection(async (server, client) =>
                {
                    Task clientJob = Task.Run(() => {
                        client.AuthenticateAsClient(hostName);
                    });

                    SslServerAuthenticationOptions options = DefaultServerOptions();

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
                    Assert.Equal(serverCert, certificate);
                    return true;
                }
            );
        }

        [Theory]
        [MemberData(nameof(HostNameData))]
        public async Task SslStream_ServerCallbackAndLocalCertificateSelectionSet_Throws(string hostName)
        {
            using X509Certificate serverCert = Configuration.Certificates.GetSelfSignedServerCertificate();

            int timesCallbackCalled = 0;

            var selectionCallback = new LocalCertificateSelectionCallback((object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] issuers) =>
            {
                Assert.True(false, "LocalCertificateSelectionCallback called when AuthenticateAsServerAsync was expected to fail.");
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
                Task clientJob = Task.Run(() => {
                    client.AuthenticateAsClient(hostName);
                    Assert.True(false, "RemoteCertificateValidationCallback called when AuthenticateAsServerAsync was expected to fail.");
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

            var selectionCallback = new LocalCertificateSelectionCallback((object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] issuers) =>
            {
                Assert.Equal(string.Empty, targetHost);
                Assert.True(localCertificates.Contains(serverCert));
                timesCallbackCalled++;
                return serverCert;
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
                Task clientJob = Task.Run(() => {
                    client.AuthenticateAsClient(hostName);
                });

                SslServerAuthenticationOptions options = DefaultServerOptions();
                options.ServerCertificate = serverCert;

                await TaskTimeoutExtensions.WhenAllOrAnyFailed(new[] { clientJob, server.AuthenticateAsServerAsync(options, CancellationToken.None) });

                Assert.Equal(1, timesCallbackCalled);
            }
        }

        [Fact]
        [SkipOnCoreClr("System.Net.Tests are flaky and/or long running: https://github.com/dotnet/runtime/issues/131", ~RuntimeConfiguration.Release)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/131", TestRuntimes.Mono)] // System.Net.Tests are flaky and/or long running
        public async Task SslStream_NoSniFromClient_CallbackReturnsNull()
        {
            await WithVirtualConnection(async (server, client) =>
            {
                Task clientJob = Task.Run(() => {
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
            Assert.Equal(string.Empty, client.TargetHostName);
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task UnencodedHostName_ValidatesCertificate()
        {
            string rawHostname = "räksmörgås.josefsson.org";
            string punycodeHostname = "xn--rksmrgs-5wao1o.josefsson.org";

            var (serverCert, serverChain) = TestHelper.GenerateCertificates(punycodeHostname);
            try
            {
                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions()
                {
                    ServerCertificateContext = SslStreamCertificateContext.Create(serverCert, serverChain),
                };

                SslClientAuthenticationOptions clientOptions = new ()
                {
                    TargetHost = rawHostname,
                    CertificateChainPolicy = new X509ChainPolicy()
                    {
                        RevocationMode = X509RevocationMode.NoCheck,
                        TrustMode = X509ChainTrustMode.CustomRootTrust,
                        CustomTrustStore = { serverChain[serverChain.Count - 1] }
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
            finally
            {
                serverCert.Dispose();
                foreach (var c in serverChain) c.Dispose();
                TestHelper.CleanupCertificates(rawHostname);
            }
        }

        [Theory]
        [InlineData("www-.volal.cz")]
        [InlineData("www-.colorhexa.com")]
        [InlineData("xn--www-7m0a.thegratuit.com")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task SslStream_SafeInvalidIdn_Success(string name)
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
            return async () => {
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
            yield return new object[] { "a" };
            yield return new object[] { "test" };
            // max allowed hostname length is 63
            yield return new object[] { new string('a', 63) };
            yield return new object[] { "\u017C\u00F3\u0142\u0107 g\u0119\u015Bl\u0105 ja\u017A\u0144. \u7EA2\u70E7. \u7167\u308A\u713C\u304D" };
        }
    }
}
