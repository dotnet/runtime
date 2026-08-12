// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamCredentialCacheTest
    {
        public static IEnumerable<object[]> CredentialReuseData()
        {
            foreach (SslProtocols protocol in SslProtocolSupport.EnumerateSupportedProtocols(SslProtocols.Tls12 | SslProtocols.Tls13, true))
            {
                foreach (bool requireClientCertificate in new[] { false, true })
                {
                    yield return new object[] { protocol, false, requireClientCertificate };

                    if (PlatformDetection.SupportsAlpn)
                    {
                        yield return new object[] { protocol, true, requireClientCertificate };
                    }
                }
            }
        }

        [Fact]
        public async Task SslStream_SameCertUsedForClientAndServer_Ok()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new SslStream(stream1, true, AllowAnyCertificate))
            using (var server = new SslStream(stream2, true, AllowAnyCertificate))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                // Using the same certificate for server and client auth.
                X509Certificate2Collection clientCertificateCollection =
                    new X509Certificate2Collection(certificate);

                Task t1 = server.AuthenticateAsServerAsync(certificate, true, false);
                Task t2 = client.AuthenticateAsClientAsync(
                                            certificate.GetNameInfo(X509NameType.SimpleName, false),
                                            clientCertificateCollection, false);


                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);

                if (Capability.IsTrustedRootCertificateInstalled())
                {
                    // https://technet.microsoft.com/en-us/library/hh831771.aspx#BKMK_Changes2012R2
                    // On Windows, the "Management of trusted issuers for client authentication" is configured
                    // such that the behavior to send the Trusted Issuers List by default is off.

                    Assert.True(client.IsMutuallyAuthenticated);
                    Assert.True(server.IsMutuallyAuthenticated);
                }
            }
        }

        [Theory]
        [MemberData(nameof(CredentialReuseData))]
        public async Task SslStream_ReusedCertificateContext_ConcurrentConnectionsAuthenticate(
            SslProtocols protocol,
            bool useAlpn,
            bool requireClientCertificate)
        {
            using X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate();
            using X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate();
            SslStreamCertificateContext serverCertificateContext = SslStreamCertificateContext.Create(serverCertificate, null, offline: true);
            SslStreamCertificateContext? clientCertificateContext = requireClientCertificate ?
                SslStreamCertificateContext.Create(clientCertificate, null, offline: true) :
                null;
            List<SslApplicationProtocol>? applicationProtocols = useAlpn ? [SslApplicationProtocol.Http2] : null;
            var serverOptions = new SslServerAuthenticationOptions
            {
                ServerCertificateContext = serverCertificateContext,
                ClientCertificateRequired = requireClientCertificate,
                EnabledSslProtocols = protocol,
                ApplicationProtocols = applicationProtocols,
                RemoteCertificateValidationCallback = AllowAnyCertificate,
            };
            var clientOptions = new SslClientAuthenticationOptions
            {
                TargetHost = serverCertificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false),
                ClientCertificateContext = requireClientCertificate ? clientCertificateContext : null,
                EnabledSslProtocols = protocol,
                ApplicationProtocols = applicationProtocols,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                RemoteCertificateValidationCallback = AllowAnyCertificate,
            };

            const int ConnectionCount = 16;
            var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int readyCount = 0;
            var tasks = new Task[ConnectionCount];

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = ConnectAsync();
            }

            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(tasks);

            async Task ConnectAsync()
            {
                (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
                using (client)
                using (server)
                {
                    if (Interlocked.Increment(ref readyCount) == ConnectionCount)
                    {
                        start.SetResult(true);
                    }

                    await start.Task;
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions));

                    if (protocol != SslProtocols.None)
                    {
                        Assert.Equal(protocol, client.SslProtocol);
                        Assert.Equal(protocol, server.SslProtocol);
                    }
                    Assert.Equal(useAlpn ? SslApplicationProtocol.Http2 : default, client.NegotiatedApplicationProtocol);
                    Assert.Equal(useAlpn ? SslApplicationProtocol.Http2 : default, server.NegotiatedApplicationProtocol);
                    Assert.Equal(requireClientCertificate, client.IsMutuallyAuthenticated);
                    Assert.Equal(requireClientCertificate, server.IsMutuallyAuthenticated);
                    await TestHelper.PingPong(client, server);
                }
            }
        }

        private static bool AllowAnyCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
