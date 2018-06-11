// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Pkcs.Tests;
using Xunit;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    public static partial class SubjectIdentifierTests
    {
        [Theory]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier)]
        [InlineData(SubjectIdentifierType.Unknown)]
        public static void SubjectIdentifier_MatchesCert(SubjectIdentifierType type)
        {
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer_ExplicitSki.GetCertificate())
            {
                EnvelopedCms cms = GetDocWithRecipient(type, cert);
                Assert.Equal(1, cms.RecipientInfos.Count);
                Assert.True(cms.RecipientInfos[0].RecipientIdentifier.MatchesCertificate(cert));
            }
        }

        [Theory]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier)]
        [InlineData(SubjectIdentifierType.Unknown)]
        public static void SubjectIdentifier_DoesNotMatchCert(SubjectIdentifierType type)
        {
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer_ExplicitSki.GetCertificate())
            using (X509Certificate2 notMatchingCert = Certificates.RSAKeyTransfer4_ExplicitSki.GetCertificate())
            {
                EnvelopedCms cms = GetDocWithRecipient(type, cert);
                Assert.Equal(1, cms.RecipientInfos.Count);
                Assert.False(cms.RecipientInfos[0].RecipientIdentifier.MatchesCertificate(notMatchingCert));
            }
        }

        private static EnvelopedCms GetDocWithRecipient(SubjectIdentifierType type, X509Certificate2 cert)
        {
            byte[] content = new byte[] { 1, 2, 3, 4 };
            EnvelopedCms cms = new EnvelopedCms(new ContentInfo(content));
            CmsRecipient recipient = new CmsRecipient(type, cert);
            cms.Encrypt(recipient);
            byte[] encoded = cms.Encode();
            cms = new EnvelopedCms();
            cms.Decode(encoded);
            return cms;
        }
    }
}
