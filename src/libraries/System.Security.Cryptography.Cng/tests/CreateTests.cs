// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public static class CreateTests
    {
        [Fact]
        public static void CreateEmphemeral_Default()
        {
            CngAlgorithm alg = CngAlgorithm.ECDiffieHellmanP256;
            using (CngKey key = CngKey.Create(alg, null, null))
            {
            }
        }

        [Fact]
        public static void CreateEphemeral_WithParameters()
        {
            CngAlgorithm alg = CngAlgorithm.ECDiffieHellmanP256;
            CngKeyCreationParameters p = new CngKeyCreationParameters();
            p.ExportPolicy = CngExportPolicies.AllowExport;
            p.KeyUsage = CngKeyUsages.KeyAgreement;
            p.UIPolicy = new CngUIPolicy(CngUIProtectionLevels.None, "MyFriendlyName", "MyDescription", "MyUseContext", "MyCreationTitle");
            byte[] myPropValue1 = "23afbc".HexToByteArray();
            p.Parameters.Add(new CngProperty("MyProp1", myPropValue1, CngPropertyOptions.CustomProperty));
            byte[] myPropValue2 = "8765".HexToByteArray();
            p.Parameters.Add(new CngProperty("MyProp2", myPropValue2, CngPropertyOptions.CustomProperty));

            using (CngKey key = CngKey.Create(alg, null, p))
            {
                Assert.Equal(CngAlgorithm.ECDiffieHellmanP256, key.Algorithm);
                Assert.Equal(CngExportPolicies.AllowExport, key.ExportPolicy);
                Assert.Equal(CngKeyUsages.KeyAgreement, key.KeyUsage);
                CngUIPolicy uiPolicy = key.UIPolicy;
                Assert.Equal(CngUIProtectionLevels.None, uiPolicy.ProtectionLevel);
                Assert.Equal("MyFriendlyName", uiPolicy.FriendlyName);
                Assert.Equal("MyDescription", uiPolicy.Description);
                Assert.Equal("MyUseContext", uiPolicy.UseContext);
                Assert.Equal("MyCreationTitle", uiPolicy.CreationTitle);

                byte[] propValue1Actual = key.GetProperty("MyProp1", CngPropertyOptions.CustomProperty).GetValue();
                Assert.Equal<byte>(myPropValue1, propValue1Actual);

                byte[] propValue2Actual = key.GetProperty("MyProp2", CngPropertyOptions.CustomProperty).GetValue();
                Assert.Equal<byte>(myPropValue2, propValue2Actual);
            }
        }
    }
}
