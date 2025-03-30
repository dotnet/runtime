// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    [ConditionalClass(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
    public abstract class SlhDsaImplementationTestsBase : SlhDsaInstanceTestsBase
    {
        public static IEnumerable<object[]> NistSignTestVectorsData =>
            from vector in SlhDsaTestData.NistSignTestVectors
            select new object[] { vector };

        [Theory]
        [MemberData(nameof(NistSignTestVectorsData))]
        public void NistSignTest(SlhDsaTestData.SlhDsaTestVector vector)
        {
            byte[] sk = vector.SecretKey.HexToByteArray();
            byte[] msg = vector.Message.HexToByteArray();
            byte[] ctx = vector.Context.HexToByteArray();
            byte[] sig = new byte[vector.Algorithm.SignatureSizeInBytes];
            byte[] expectedSignature = vector.Signature.HexToByteArray();

            using SlhDsa slhDsa = ImportSlhDsaSecretKey(vector.Algorithm, sk);
            slhDsa.SignData(msg, sig, ctx);

            // Public key should be the same as the one in the test vector
            byte[] pk = new byte[vector.Algorithm.PublicKeySizeInBytes];
            slhDsa.ExportSlhDsaPublicKey(pk);
            byte[] expectedPublicKey = vector.PublicKey.HexToByteArray();
            Assert.Equal(expectedPublicKey, pk);

            // Verify should return true
            Assert.True(slhDsa.VerifyData(msg, sig, ctx));

            // Verify should return true when created with the public key
            using SlhDsa slhDsaPublic = SlhDsa.ImportSlhDsaPublicKey(vector.Algorithm, expectedPublicKey);
            Assert.True(slhDsaPublic.VerifyData(msg, sig, ctx));
        }
    }
}
