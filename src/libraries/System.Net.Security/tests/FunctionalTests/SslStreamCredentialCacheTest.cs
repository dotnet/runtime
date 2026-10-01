// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamCredentialCacheTest
    {
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

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task SslStream_ClientCertificateContext_DoesNotPolluteAnonymousCredentialCache(SslProtocols protocol)
        {
            await RemoteExecutor.Invoke(async protocolString =>
            {
                SslProtocols protocol = (SslProtocols)int.Parse(protocolString);
                using X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate();
                using X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate();

                var serverOptions = new SslServerAuthenticationOptions
                {
                    ClientCertificateRequired = true,
                    EnabledSslProtocols = protocol,
                    RemoteCertificateValidationCallback = AllowAnyCertificate,
                    ServerCertificateContext = SslStreamCertificateContext.Create(serverCertificate, null, false),
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    ClientCertificateContext = SslStreamCertificateContext.Create(clientCertificate, null, false),
                    EnabledSslProtocols = protocol,
                    RemoteCertificateValidationCallback = AllowAnyCertificate,
                    TargetHost = Guid.NewGuid().ToString("N"),
                };

                await RunConnectionAsync(clientOptions, serverOptions, clientCertificate);

                clientOptions.ClientCertificateContext = null;
                clientOptions.TargetHost = Guid.NewGuid().ToString("N");

                await RunConnectionAsync(clientOptions, serverOptions, expectedClientCertificate: null);
            }, ((int)protocol).ToString()).DisposeAsync();

            static async Task RunConnectionAsync(
                SslClientAuthenticationOptions clientOptions,
                SslServerAuthenticationOptions serverOptions,
                X509Certificate2? expectedClientCertificate)
            {
                (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
                using (client)
                using (server)
                {
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions));

                    if (expectedClientCertificate is null)
                    {
                        Assert.Null(server.RemoteCertificate);
                    }
                    else
                    {
                        Assert.Equal(expectedClientCertificate, server.RemoteCertificate);
                    }
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
