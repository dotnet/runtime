// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using System.Security.Cryptography.X509Certificates;

using Xunit;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    [PlatformSpecific(~TestPlatforms.Windows)]
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public static class KemEncryptTests
    {
        [Fact]
        public static void EncryptAndDecrypt()
        {
            byte[] content = "hello world!"u8.ToArray();

            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem))
            {
                EnvelopedCms cms = new EnvelopedCms(new ContentInfo(content));
                cms.Encrypt(new CmsRecipient(certificate));
                byte[] encoded = cms.Encode();

                cms = new EnvelopedCms();
                cms.Decode(encoded);

                KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

                using (MLKem privateKey = MLKem.ImportPkcs8PrivateKey(MLKemTestData.IetfMlKem768PrivateKeySeed))
                {
                    cms.Decrypt(recipientInfo, privateKey);
                }

                Assert.Equal(content, cms.ContentInfo.Content);
            }
        }
    }
}
