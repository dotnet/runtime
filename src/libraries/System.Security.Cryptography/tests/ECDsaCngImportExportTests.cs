// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.EcDsa.Tests;
using System.Security.Cryptography.Tests;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public sealed class ECDsaCngImportExportTests : ECDsaTestsBase
    {
        protected override ECDsaProvider ECDsaFactory { get; } = ECDsaCngProvider.Instance;

        [Fact]
        public static void TestImportKey()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                Assert.Equal(CngAlgorithm.ECDiffieHellmanP256, key.Algorithm);
                Assert.Equal(CngAlgorithmGroup.ECDiffieHellman, key.AlgorithmGroup);
                Assert.Equal(CngExportPolicies.None, key.ExportPolicy);
                Assert.True(key.IsEphemeral);
                Assert.False(key.IsMachineKey);
                Assert.Null(key.KeyName);
                Assert.Equal(0x100, key.KeySize);
                Assert.Equal(CngKeyUsages.AllUsages, key.KeyUsage);
                Assert.Equal(IntPtr.Zero, key.ParentWindowHandle);
                Assert.Equal(CngProvider.MicrosoftSoftwareKeyStorageProvider, key.Provider);

                CngUIPolicy policy = key.UIPolicy;
                Assert.Null(policy.CreationTitle);
                Assert.Null(policy.Description);
                Assert.Null(policy.FriendlyName);
                Assert.Null(policy.UseContext);
                Assert.Equal(CngUIProtectionLevels.None, policy.ProtectionLevel);

                Assert.Null(key.UniqueName);
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

        [ConditionalTheory]
        [MemberData(nameof(TestCurves))]
        public static void TestHashRoundTrip(CurveDef curveDef)
        {
            SkipTestException.ThrowUnless(ECDsaCngProvider.Instance.ExplicitCurvesSupported);

            if (!curveDef.IsCurveValidOnPlatform(ECDsaCngProvider.Instance))
                return;

            // This test is in the cng only tests because OpenSsl does not provide the hash algorithm
            using (var cng = new ECDsaCng(curveDef.Curve))
            {
                ECParameters param = cng.ExportExplicitParameters(false);

                // Add some dummy values and import
                Assert.True(param.Curve.IsExplicit);
                var curve = param.Curve;
                curve.Hash = HashAlgorithmName.SHA1;
                curve.Seed = new byte[1] { 0xFF }; // Hash should have a seed
                param.Curve = curve;
                cng.ImportParameters(param);

                // Export to see if the hash is there
                ECParameters param2 = cng.ExportExplicitParameters(false);
                Assert.Equal(HashAlgorithmName.SHA1.Name.ToUpper(), param2.Curve.Hash.Value.Name.ToUpper());
                Assert.Equal(0xFF, param2.Curve.Seed[0]);
            }
        }
    }
}
