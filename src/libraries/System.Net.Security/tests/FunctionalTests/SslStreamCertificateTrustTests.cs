// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Tests;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamCertificateTrustTest
    {
        [Fact]
        // not supported on Windows, not implemented elsewhere
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task SslStream_SendCertificateTrust_CertificateCollection()
        {
            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate())
            {
                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCertificate,
                    ClientCertificateRequired = true,
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
                    ServerCertificateContext = SslStreamCertificateContext.Create(
                        serverCertificate,
                        null,
                        trust: SslCertificateTrust.CreateForX509Collection(
                            new X509Certificate2Collection { serverCertificate, clientCertificate },
                            sendTrustInHandshake: true))
                };

                string[] acceptableIssuers = Array.Empty<string>();
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "localhost",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
                    LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, issuers) =>
                    {
                        if (remoteCertificate == null)
                        {
                            // ignore the first call, we should receive acceptable issuers in the next one
                            return null;
                        }

                        acceptableIssuers = issuers;
                        return clientCertificate;
                    },

                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions),
                                server.AuthenticateAsServerAsync(serverOptions));

                Assert.Equal(2, acceptableIssuers.Length);
                Assert.Contains(serverCertificate.Subject, acceptableIssuers);
                Assert.Contains(clientCertificate.Subject, acceptableIssuers);
            }
        }

        [Fact]
        public async Task SslStream_SendCertificateTrust_CertificateStore()
        {
            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate())
            using (X509Store store = new X509Store("Root", StoreLocation.LocalMachine))
            {
                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCertificate,
                    ClientCertificateRequired = true,
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
                    ServerCertificateContext = SslStreamCertificateContext.Create(
                        serverCertificate,
                        null,
                        trust: SslCertificateTrust.CreateForX509Store(store, sendTrustInHandshake: true))
                };

                string[] acceptableIssuers = Array.Empty<string>();
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "localhost",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
                    LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, issuers) =>
                    {
                        if (remoteCertificate == null)
                        {
                            // ignore the first call, we should receive acceptable issuers in the next one
                            return null;
                        }

                        acceptableIssuers = issuers;
                        return clientCertificate;
                    },

                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions),
                                server.AuthenticateAsServerAsync(serverOptions));

                // don't assert individual ellements, just that some issuers were sent
                // we use Root cert store which should always contain at least some certs
                Assert.NotEmpty(acceptableIssuers);
            }
        }
    }
}