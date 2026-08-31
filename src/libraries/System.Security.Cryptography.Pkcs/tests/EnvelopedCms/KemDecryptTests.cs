// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;

using Xunit;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    [PlatformSpecific(~TestPlatforms.Windows)]
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public static class KemDecryptTests
    {
        public static TheoryData<byte[]> AesKeyWrapDocuments { get; } = new TheoryData<byte[]>
        {
            KemTestDocuments.MlKem768Aes128Wrap,
            KemTestDocuments.MlKem768Aes192Wrap,
            KemTestDocuments.MlKem768,
        };

        public static TheoryData<byte[]> HkdfDocuments { get; } = new TheoryData<byte[]>
        {
            KemTestDocuments.MlKem768HkdfSha256,
            KemTestDocuments.MlKem768,
            KemTestDocuments.MlKem768HkdfSha512,
            KemTestDocuments.MlKem768HkdfSha3_256,
            KemTestDocuments.MlKem768HkdfSha3_384,
            KemTestDocuments.MlKem768HkdfSha3_512,
        };

        public static TheoryData<byte[], byte[]> MlKemParameterSetDocuments { get; } = new TheoryData<byte[], byte[]>
        {
            { KemTestDocuments.MlKem512, MLKemTestData.IetfMlKem512PrivateKeySeed },
            { KemTestDocuments.MlKem768, MLKemTestData.IetfMlKem768PrivateKeySeed },
            { KemTestDocuments.MlKem1024, MLKemTestData.IetfMlKem1024PrivateKeySeed },
        };

        [Theory]
        [MemberData(nameof(AesKeyWrapDocuments))]
        public static void DecryptAesKeyWrapAlgorithm(byte[] encodedMessage)
        {
            Decrypt(encodedMessage, MLKemTestData.IetfMlKem768PrivateKeySeed);
        }

        [Theory]
        [MemberData(nameof(HkdfDocuments))]
        public static void DecryptHkdfAlgorithm(byte[] encodedMessage)
        {
            Decrypt(encodedMessage, MLKemTestData.IetfMlKem768PrivateKeySeed);
        }

        [Theory]
        [MemberData(nameof(MlKemParameterSetDocuments))]
        public static void DecryptMlKemParameterSet(byte[] encodedMessage, byte[] privateKey)
        {
            Decrypt(encodedMessage, privateKey);
        }

        private static void Decrypt(byte[] encodedMessage, byte[] privateKey)
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(encodedMessage);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (MLKem mlKem = MLKem.ImportPkcs8PrivateKey(privateKey))
            {
                cms.Decrypt(recipientInfo, mlKem);
            }

            Assert.Equal("hello world!"u8.ToArray(), cms.ContentInfo.Content);
        }
    }
}
