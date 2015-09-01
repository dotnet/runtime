// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    /// <summary>
    /// Tests that apply to the filesystem/cache portions of the X509 infrastructure on Unix implementations.
    /// </summary>
    [Collection("X509Filesystem")]
    public static class X509FilesystemTests
    {
        [Fact]
        [OuterLoop]
        public static void VerifyCrlCache()
        {
            string crlDirectory = PersistedFiles.GetUserFeatureDirectory("cryptography", "crls");
            string crlFile = Path.Combine(crlDirectory,MicrosoftDotComRootCrlFilename);

            Directory.CreateDirectory(crlDirectory);
            File.Delete(crlFile);

            using (var microsoftDotComIssuer = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            using (var microsoftDotComRoot = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
            using (var unrelated = new X509Certificate2(TestData.DssCer))
            {
                X509Chain chain = new X509Chain();

                chain.ChainPolicy.ExtraStore.Add(unrelated);
                chain.ChainPolicy.ExtraStore.Add(microsoftDotComRoot);
                
                // The very start of the CRL period.
                chain.ChainPolicy.VerificationTime = new DateTime(2015, 6, 17, 0, 0, 0, DateTimeKind.Utc);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

                bool valid = chain.Build(microsoftDotComIssuer);
                Assert.True(valid, "Precondition: Chain builds with no revocation checks");

                chain.ChainPolicy.RevocationMode = X509RevocationMode.Offline;

                valid = chain.Build(microsoftDotComIssuer);
                Assert.False(valid, "Chain should not build validly");

                Assert.Equal(1, chain.ChainStatus.Length);
                Assert.Equal(X509ChainStatusFlags.RevocationStatusUnknown, chain.ChainStatus[0].Status);

                File.WriteAllText(crlFile, MicrosoftDotComRootCrlPem, Encoding.ASCII);

                valid = chain.Build(microsoftDotComIssuer);
                Assert.True(valid, "Chain should build validly now");
                Assert.Equal(0, chain.ChainStatus.Length);

                // Rewind one second, the CRL is not "not yet valid"
                chain.ChainPolicy.VerificationTime = chain.ChainPolicy.VerificationTime.Subtract(TimeSpan.FromSeconds(1));

                valid = chain.Build(microsoftDotComIssuer);
                Assert.False(valid, "Chain should not build validly, CRL is not yet valid");

                Assert.Equal(1, chain.ChainStatus.Length);
                Assert.Equal(X509ChainStatusFlags.RevocationStatusUnknown, chain.ChainStatus[0].Status);
            }
        }

        // `openssl crl -in [MicrosoftDotComRootCrlPem] -noout -hash`.crl
        private const string MicrosoftDotComRootCrlFilename = "b204d74a.crl";

        // This CRL was downloaded 2015-08-31 20:31 PDT
        // It is valid from Jun 17 00:00:00 2015 GMT to Sep 30 23:59:59 2015 GMT
        private const string MicrosoftDotComRootCrlPem =
            @"-----BEGIN X509 CRL-----
MIICETCB+jANBgkqhkiG9w0BAQUFADCByjELMAkGA1UEBhMCVVMxFzAVBgNVBAoT
DlZlcmlTaWduLCBJbmMuMR8wHQYDVQQLExZWZXJpU2lnbiBUcnVzdCBOZXR3b3Jr
MTowOAYDVQQLEzEoYykgMjAwNiBWZXJpU2lnbiwgSW5jLiAtIEZvciBhdXRob3Jp
emVkIHVzZSBvbmx5MUUwQwYDVQQDEzxWZXJpU2lnbiBDbGFzcyAzIFB1YmxpYyBQ
cmltYXJ5IENlcnRpZmljYXRpb24gQXV0aG9yaXR5IC0gRzUXDTE1MDYxNzAwMDAw
MFoXDTE1MDkzMDIzNTk1OVowDQYJKoZIhvcNAQEFBQADggEBAFxqobObEqKNSAe+
A9cHCYI7sw+Vc8HuE7E+VZc6ni3a2UHiprYuXDsvD18+cyv/nFSLpLqLmExZrsf/
dzH8GH2HgBTt5aO/nX08EBrDgcjHo9b0VI6ZuOOaEeS0NsRh28Jupfn1Xwcsbdw9
nVh1OaExpHwxgg7pJr4pXzaAjbl3b4QfCPyTd5aaOQOEmqvJtRrMwCna4qQ3p4r6
QYe19/pXqK9my7lSmH1vZ0CmNvQeNPmnx+YmFXYTBgap+Xi2cs6GX/qI04CDzjWi
sm6L0+S1Zx2wMhiYOi0JvrRizf+rIyKkDbPMoYEyXZqcCwSnv6mJQY81vmKRKU5N
WKo2mLw=
-----END X509 CRL-----";
    }
}
