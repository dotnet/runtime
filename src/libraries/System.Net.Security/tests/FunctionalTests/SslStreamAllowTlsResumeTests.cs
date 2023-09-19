// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;
using Microsoft.DotNet.XUnitExtensions;

#if DEBUG
namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamTlsResumeTests
    {
        private static FieldInfo connectionInfo = typeof(SslStream).GetField(
                                    "_connectionInfo",
                                    BindingFlags.Instance | BindingFlags.NonPublic);

        private bool CheckResumeFlag(SslStream ssl)
        {
            // This works only on Debug build where SslStream has extra property so we can validate.
            object info = connectionInfo.GetValue(ssl);
            return (bool)info.GetType().GetProperty("TlsResumed").GetValue(info);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_ClientDisableTlsResume_Succeeds(bool testClient)
        {
            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null, false)
                };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    EnabledSslProtocols = SslProtocols.Tls12,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                };

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions));

            Assert.True(client.IsAuthenticated);
            Assert.True(client.IsEncrypted);
            await client.ShutdownAsync();
            await server.ShutdownAsync();
            client.Dispose();
            server.Dispose();

            // create new TLS to the same server. This should resume TLS.
            (client, server) = TestHelper.GetConnectedSslStreams();
            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions));

            //Assert.True(CheckResumeFlag(client));
            if (!CheckResumeFlag(client))
            {
                throw new SkipTestException("Unable to resume test session");
            }
            Assert.True(CheckResumeFlag(server));
            await client.ShutdownAsync();
            await server.ShutdownAsync();
            client.Dispose();
            server.Dispose();

            // Disable TLS resumption and try it again.
            if (testClient)
            {
                clientOptions.AllowTlsResume = false;
            }
            else
            {
                serverOptions.AllowTlsResume = false;
            }

            // We do multiple loops to also cover credential cache.
            for (int i=0; i < 3; i++)
            {
                (client, server) = TestHelper.GetConnectedSslStreams();
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                    client.AuthenticateAsClientAsync(clientOptions),
                                    server.AuthenticateAsServerAsync(serverOptions));

                Assert.False(CheckResumeFlag(client), $"TLS session resumed in round ${i}");
                Assert.False(CheckResumeFlag(server), $"TLS session resumed in round ${i}");
                await client.ShutdownAsync();
                await server.ShutdownAsync();
                client.Dispose();
                server.Dispose();
            }

            // TLS resume still should be possible
            if (testClient)
            {
                clientOptions.AllowTlsResume = true;
            }
            else
            {
                serverOptions.AllowTlsResume = true;
            }

            // On Windows it may take extra round to refresh the session cache.
            (client, server) = TestHelper.GetConnectedSslStreams();
            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions),
                                server.AuthenticateAsServerAsync(serverOptions));
            client.Dispose();
            server.Dispose();

            (client, server) = TestHelper.GetConnectedSslStreams();
            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions),
                                server.AuthenticateAsServerAsync(serverOptions));

            Assert.True(CheckResumeFlag(client));
            Assert.True(CheckResumeFlag(server));
            client.Dispose();
            server.Dispose();
        }
    }
}
#endif
