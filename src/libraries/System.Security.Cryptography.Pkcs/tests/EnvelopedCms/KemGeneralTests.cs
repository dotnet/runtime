// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using TestOids = System.Security.Cryptography.Pkcs.Tests.Oids;
using X509IssuerSerial = System.Security.Cryptography.Xml.X509IssuerSerial;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    [PlatformSpecific(~TestPlatforms.Windows)]
    public static class KemGeneralTests
    {
        [Fact]
        public static void DecodeMlKem768()
        {
            EnvelopedCms cms = new EnvelopedCms();

            cms.Decode(KemTestDocuments.MlKem768);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));
            Assert.Equal(RecipientInfoType.KeyEncapsulation, recipientInfo.Type);
            Assert.Equal(0, recipientInfo.Version);
            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, recipientInfo.RecipientIdentifier.Type);
            X509IssuerSerial issuerSerial = Assert.IsType<X509IssuerSerial>(recipientInfo.RecipientIdentifier.Value);
            Assert.Equal("159FFE6F22FD5CC42C524DF6FD5E28D0DE38F34F", issuerSerial.SerialNumber);
            Assert.Equal(TestOids.MLKem768, recipientInfo.KeyEncapsulationAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyEncapsulationAlgorithm.Parameters);
            Assert.Equal(1088, recipientInfo.KeyEncapsulationCiphertext.Length);
            Assert.Equal(TestOids.HkdfSha384, recipientInfo.KeyDerivationAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyDerivationAlgorithm.Parameters);
            Assert.Equal(32, recipientInfo.KeyEncryptionKeyLengthInBytes);
            Assert.Null(recipientInfo.UserKeyingMaterial);
            Assert.Equal(TestOids.Aes256Wrap, recipientInfo.KeyEncryptionAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyEncryptionAlgorithm.Parameters);
            Assert.Equal(40, recipientInfo.EncryptedKey.Length);
        }

        [Fact]
        public static void DecodeOtherRecipientInfoWithUnknownOid()
        {
            EnvelopedCms cms = new EnvelopedCms();

            Assert.Throws<CryptographicException>(
                () => cms.Decode(KemTestDocuments.UnsupportedOtherRecipientInfo));
        }
    }
}
