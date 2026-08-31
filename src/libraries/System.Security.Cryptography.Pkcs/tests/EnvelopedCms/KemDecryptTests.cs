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
        public static TheoryData<byte[]> MlKem768Documents { get; } = new TheoryData<byte[]>
        {
            KemTestDocuments.MlKem768,
            KemTestDocuments.MlKem768HkdfSha3_384,
        };

        [Theory]
        [MemberData(nameof(MlKem768Documents))]
        public static void DecryptMlKem768(byte[] encodedMessage)
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(encodedMessage);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (MLKem privateKey = MLKem.ImportPkcs8PrivateKey(MLKemTestData.IetfMlKem768PrivateKeySeed))
            {
                cms.Decrypt(recipientInfo, privateKey);
            }

            Assert.Equal("hello world!"u8.ToArray(), cms.ContentInfo.Content);
        }
    }
}
