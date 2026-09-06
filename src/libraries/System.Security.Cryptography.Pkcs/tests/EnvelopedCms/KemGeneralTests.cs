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
        public static TheoryData<byte[], string, int> MlKemDocuments { get; } = new TheoryData<byte[], string, int>
        {
            { KemTestDocuments.MlKem512, TestOids.MLKem512, 768 },
            { KemTestDocuments.MlKem768, TestOids.MLKem768, 1088 },
            { KemTestDocuments.MlKem1024, TestOids.MLKem1024, 1568 },
        };

        public static TheoryData<byte[], byte[]?> UserKeyingMaterialDocuments { get; } =
            new TheoryData<byte[], byte[]?>
            {
                { KemTestDocuments.MlKem768, null },
                { KemTestDocuments.MlKem768EmptyUkm, [] },
                { KemTestDocuments.MlKem768NonEmptyUkm, [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08] },
            };

        [Fact]
        public static void DecodeMlKem768()
        {
            KemRecipientInfo recipientInfo = Decode(KemTestDocuments.MlKem768);
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

        [Theory]
        [MemberData(nameof(MlKemDocuments))]
        public static void DecodeMlKemParameterSet(
            byte[] encodedMessage,
            string expectedAlgorithm,
            int expectedCiphertextLength)
        {
            KemRecipientInfo recipientInfo = Decode(encodedMessage);

            Assert.Equal(expectedAlgorithm, recipientInfo.KeyEncapsulationAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyEncapsulationAlgorithm.Parameters);
            Assert.Equal(expectedCiphertextLength, recipientInfo.KeyEncapsulationCiphertext.Length);
        }

        [Theory]
        [MemberData(nameof(UserKeyingMaterialDocuments))]
        public static void DecodeUserKeyingMaterial(byte[] encodedMessage, byte[]? expectedUserKeyingMaterial)
        {
            KemRecipientInfo recipientInfo = Decode(encodedMessage);
            ReadOnlyMemory<byte>? actualUserKeyingMaterial = recipientInfo.UserKeyingMaterial;

            if (expectedUserKeyingMaterial is null)
            {
                Assert.Null(actualUserKeyingMaterial);
            }
            else
            {
                Assert.True(actualUserKeyingMaterial.HasValue);
                Assert.Equal<byte>(expectedUserKeyingMaterial, actualUserKeyingMaterial.Value.ToArray());
            }
        }

        [Fact]
        public static void DecodeOtherRecipientInfoWithUnknownOid()
        {
            EnvelopedCms cms = new EnvelopedCms();

            Assert.Throws<CryptographicException>(
                () => cms.Decode(KemTestDocuments.UnsupportedOtherRecipientInfo));
        }

        private static KemRecipientInfo Decode(byte[] encodedMessage)
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(encodedMessage);
            return Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));
        }
    }
}
