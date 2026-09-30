// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using System.Security.Cryptography.X509Certificates;

using Xunit;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    [PlatformSpecific(~TestPlatforms.Windows)]
    public static class KemNotSupportedTests
    {
        public static bool IsMLKemNotSupported => !MLKem.IsSupported;

        [ConditionalTheory(typeof(KemNotSupportedTests), nameof(IsMLKemNotSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public static void Encrypt_PlatformNotSupported(bool useFactory)
        {
            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem))
            {
                CmsRecipient recipient = useFactory ?
                    CmsRecipient.CreateForKeyEncapsulation(certificate, []) :
                    new CmsRecipient(certificate);
                EnvelopedCms cms = new EnvelopedCms(new ContentInfo("hello world!"u8.ToArray()));

                Assert.Throws<PlatformNotSupportedException>(() => cms.Encrypt(recipient));
            }
        }

    }
}
