// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public static class ImportExportTests
    {
        [Fact]
        public static void TestImport()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                Assert.Equal(CngAlgorithm.ECDiffieHellmanP256, key.Algorithm);
                Assert.Equal(CngAlgorithmGroup.ECDiffieHellman, key.AlgorithmGroup);
                Assert.Equal(CngExportPolicies.None, key.ExportPolicy);
                Assert.Equal(true, key.IsEphemeral);
                Assert.Equal(false, key.IsMachineKey);
                Assert.Equal(null, key.KeyName);
                Assert.Equal(0x100, key.KeySize);
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
        }

        [Fact]
        public static void TestImportExportRoundTrip()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                byte[] reExported = key.Export(CngKeyBlobFormat.GenericPublicBlob);
                Assert.Equal<byte>(TestData.Key_ECDiffieHellmanP256, reExported);
            }
        }
    }
}
