// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamCredentialCacheTest
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
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

                if (!PlatformDetection.IsWindows7 ||
                    Capability.IsTrustedRootCertificateInstalled())
                {
                    // https://technet.microsoft.com/en-us/library/hh831771.aspx#BKMK_Changes2012R2
                    // Starting with Windows 8, the "Management of trusted issuers for client authentication" has changed:
                    // The behavior to send the Trusted Issuers List by default is off.
                    //
                    // In Windows 7 the Trusted Issuers List is sent within the Server Hello TLS record. This list is built
                    // by the server using certificates from the Trusted Root Authorities certificate store.
                    // The client side will use the Trusted Issuers List, if not empty, to filter proposed certificates.

                    Assert.True(client.IsMutuallyAuthenticated);
                    Assert.True(server.IsMutuallyAuthenticated);
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
