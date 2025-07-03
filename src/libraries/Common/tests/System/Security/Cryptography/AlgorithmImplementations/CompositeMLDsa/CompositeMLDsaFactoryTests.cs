// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class CompositeMLDsaFactoryTests
    {
        // TODO test doesn't belong here, move to different class
        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportExportVerify(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            using (CompositeMLDsa key = CompositeMLDsa.ImportCompositeMLDsaPrivateKey(vector.Algorithm, vector.SecretKey))
            {
                byte[] exportedSecretKey = key.ExportCompositeMLDsaPrivateKey();
                // TODO 'D' values differ, so we cannot compare keys directly
                //Assert.Equal(vector.SecretKey, exportedSecretKey);

                byte[] exportedPublicKey = key.ExportCompositeMLDsaPublicKey();
                Assert.Equal(vector.PublicKey, exportedPublicKey);

                AssertExtensions.TrueExpression(key.VerifyData(vector.Message, vector.Signature));
            }

            using (CompositeMLDsa key = CompositeMLDsa.ImportCompositeMLDsaPublicKey(vector.Algorithm, vector.PublicKey))
            {
                Assert.Throws<CryptographicException>(key.ExportCompositeMLDsaPrivateKey);

                byte[] exportedPublicKey = key.ExportCompositeMLDsaPublicKey();
                Assert.Equal(vector.PublicKey, exportedPublicKey);

                AssertExtensions.TrueExpression(key.VerifyData(vector.Message, vector.Signature));
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportSignVerify(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            byte[] signature;

            using (CompositeMLDsa key = CompositeMLDsa.ImportCompositeMLDsaPrivateKey(vector.Algorithm, vector.SecretKey))
            {
                signature = key.SignData(vector.Message, null);

                Assert.Equal(vector.Signature.Length, signature.Length);

                AssertExtensions.TrueExpression(key.VerifyData(vector.Message, signature));
            }

            using (CompositeMLDsa key = CompositeMLDsa.ImportCompositeMLDsaPublicKey(vector.Algorithm, vector.PublicKey))
            {
                AssertExtensions.TrueExpression(key.VerifyData(vector.Message, signature));
            }
        }

        [Fact]
        public static void MessageRepresentative_NoContext()
        {
            // TODO permalink to draft spec

            byte[] M = Convert.FromHexString("00010203040506070809");
            byte[] ctx = [];
            byte[] r = Convert.FromHexString("e7c3052838e7b07a46d8f89c794ddedcd16f9c108ccfc2a2ba0467d36c1493ec");
            byte[] expectedMPrime = Convert.FromHexString(
                "436f6d706f73697465416c676f726974686d5369676e6174757265733230323506" +
                "0b6086480186fa6b5009010800e7c3052838e7b07a46d8f89c794ddedcd16f9c108ccf" +
                "c2a2ba0467d36c1493ec0f89ee1fcb7b0a4f7809d1267a029719004c5a5e5ec323a7c3" +
                "523a20974f9a3f202f56fadba4cd9e8d654ab9f2e96dc5c795ea176fa20ede8d854c34" +
                "2f903533");

            ReadOnlySpan<byte> MPrime;

            using (CompositeMLDsaMessageEncoder encoder = new(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256, ctx, r))
            {
                encoder.AppendData(M);
                MPrime = encoder.GetMessageRepresentativeAndDispose();
            }

            Assert.Equal(expectedMPrime, MPrime);
        }

        [Fact]
        public static void MessageRepresentative_WithContext()
        {
            // TODO permalink to draft spec

            byte[] M = Convert.FromHexString("00010203040506070809");
            byte[] ctx = Convert.FromHexString("0813061205162623");
            byte[] r = Convert.FromHexString("d735d53cdbc2b82e4c116b97e06daa6185da4ba805f6cef0759eea2d2f03af09");
            byte[] expectedMPrime = Convert.FromHexString(
                "436f6d706f73697465416c676f726974686d5369676e6174757265733230323506" +
                "0b6086480186fa6b50090108080813061205162623d735d53cdbc2b82e4c116b97e06d" +
                "aa6185da4ba805f6cef0759eea2d2f03af090f89ee1fcb7b0a4f7809d1267a02971900" +
                "4c5a5e5ec323a7c3523a20974f9a3f202f56fadba4cd9e8d654ab9f2e96dc5c795ea17" +
                "6fa20ede8d854c342f903533");

            ReadOnlySpan<byte> MPrime;

            using (CompositeMLDsaMessageEncoder encoder = new(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256, ctx, r))
            {
                encoder.AppendData(M);
                MPrime = encoder.GetMessageRepresentativeAndDispose();
            }

            Assert.Equal(expectedMPrime, MPrime);
        }

        // TODO This is a temporary test to validate the KATs. The spec is not finalized, so the KATs might not be correct.
        // This is a quick way to validate them without using CompositeMLDsa directly.
        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void KATValidation(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            MLDsaAlgorithm mldsaAlgorithm = CompositeMLDsaTestHelpers.MLDsaAlgorithms[vector.Algorithm];
            ReadOnlySpan<byte> mldsaKey = vector.PublicKey.AsSpan(0, mldsaAlgorithm.PublicKeySizeInBytes);
            byte[] tradKey = vector.PublicKey.AsSpan(mldsaAlgorithm.PublicKeySizeInBytes).ToArray();

            using MLDsa mldsa = MLDsa.ImportMLDsaPublicKey(mldsaAlgorithm, mldsaKey);
            using AsymmetricAlgorithm trad =
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    vector.Algorithm,
                    () => { RSA rsa = RSA.Create(); rsa.ImportRSAPublicKey(tradKey, out _); return rsa; },
                    () => throw new NotImplementedException(),
                    () => throw new NotImplementedException()
                );

            ReadOnlySpan<byte> r = vector.Signature.AsSpan(0, 32);
            ReadOnlySpan<byte> mldsaSig = vector.Signature.AsSpan(32, mldsaAlgorithm.SignatureSizeInBytes);
            byte[] tradSig = vector.Signature.AsSpan(32 + mldsaAlgorithm.SignatureSizeInBytes).ToArray();

            byte[] mPrime;

            using (CompositeMLDsaMessageEncoder encoder = new(vector.Algorithm, context: [], r))
            {
                encoder.AppendData(vector.Message);
                mPrime = encoder.GetMessageRepresentativeAndDispose().ToArray();
            }

            AssertExtensions.TrueExpression(mldsa.VerifyData(mPrime, mldsaSig, context: CompositeMLDsaTestHelpers.DomainSeparators[vector.Algorithm]));

            CompositeMLDsaTestHelpers.ExecuteComponentAction(
                vector.Algorithm,
                () => AssertExtensions.TrueExpression(((RSA)trad).VerifyData(
                    mPrime,
                    tradSig,
                    CompositeMLDsaTestHelpers.TradHashAlgorithms[vector.Algorithm],
                    CompositeMLDsaTestHelpers.RsaPadding[vector.Algorithm])),
                () => throw new NotImplementedException(),
                () => throw new NotImplementedException()
            );
        }
    }
}
