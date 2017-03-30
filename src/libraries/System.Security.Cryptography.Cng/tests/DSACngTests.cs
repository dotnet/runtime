// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Cng.Tests;
using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    public static class DsaCngTests
    {
        [Fact]
        public static void TestImportV1Key()
        {
            using (CngKey key = CngKey.Import(TestData.Key_DSA1024Key, CngKeyBlobFormat.GenericPrivateBlob))
            {
                VerifyImportedKey(key);
                Assert.Equal(1024, key.KeySize);
            }
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void TestImportV2Key()
        {
            using (CngKey key = CngKey.Import(TestData.Key_DSA2048Key, CngKeyBlobFormat.GenericPrivateBlob))
            {
                VerifyImportedKey(key);
                Assert.Equal(2048, key.KeySize);
            }
        }

        private static void VerifyImportedKey(CngKey key)
        {
            Assert.Equal(new CngAlgorithm("DSA"), key.Algorithm);
            Assert.Equal(CngAlgorithmGroup.Dsa, key.AlgorithmGroup);
            Assert.Equal(CngExportPolicies.None, key.ExportPolicy);
            Assert.Equal(true, key.IsEphemeral);
            Assert.Equal(false, key.IsMachineKey);
            Assert.Equal(null, key.KeyName);
            Assert.Equal(CngKeyUsages.AllUsages, key.KeyUsage);
            Assert.Equal(IntPtr.Zero, key.ParentWindowHandle);
            Assert.Equal(CngProvider.MicrosoftSoftwareKeyStorageProvider, key.Provider);

            CngUIPolicy policy = key.UIPolicy;
            Assert.Equal(null, policy.CreationTitle);
            Assert.Equal(null, policy.Description);
            Assert.Equal(null, policy.FriendlyName);
            Assert.Equal(null, policy.UseContext);
            Assert.Equal(CngUIProtectionLevels.None, policy.ProtectionLevel);
            Assert.Equal(null, key.UniqueName);
        }

        [Fact]
        public static void TestImportExportRoundTrip()
        {
            using (CngKey key = CngKey.Import(TestData.Key_DSA1024PublicKey, CngKeyBlobFormat.GenericPublicBlob))
            {
                byte[] reExported = key.Export(CngKeyBlobFormat.GenericPublicBlob);
                Assert.Equal<byte>(TestData.Key_DSA1024PublicKey, reExported);
            }
        }

        [Fact]
        public static void VerifyDefaultKeySize_Cng()
        {
            using (DSA dsa = new DSACng())
            {
                // DSACng detects OS version and selects appropriate default key size
                Assert.Equal(DSAFactory.SupportsFips186_3 ? 2048 : 1024, dsa.KeySize);
            }
        }

        internal static bool SupportsFips186_3
        {
            get
            {
                return DSAFactory.SupportsFips186_3;
            }
        }
    }
}
