// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Pkcs.Tests;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    public class DecryptTestsUsingExplicitPrivateKey : DecryptTests
    {
        public DecryptTestsUsingExplicitPrivateKey() : base(true) { }

        [Fact]
        public static void DecryptUsingWrongPrivateKeyType()
        {
            byte[] content = new byte[] { 1, 2, 3, 4 };
            ContentInfo contentInfo = new ContentInfo(content);
            AlgorithmIdentifier alg = new AlgorithmIdentifier(new Oid(Oids.Aes192));

            EnvelopedCms ecms = new EnvelopedCms(contentInfo, alg);

            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, cert);
                ecms.Encrypt(cmsRecipient);
            }

            byte[] encoded = ecms.Encode();
            ecms = new EnvelopedCms();
            ecms.Decode(encoded);

            using (ECDsa ecdsa = ECDsa.Create())
            {
                Assert.Throws<CryptographicException>(() => ecms.Decrypt(ecms.RecipientInfos[0], ecdsa));
            }
        }
    }
}
