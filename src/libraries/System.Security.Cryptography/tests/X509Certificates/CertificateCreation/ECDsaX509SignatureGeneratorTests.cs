// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support asymmetric cryptography")]
    public static class ECDsaX509SignatureGeneratorTests
    {
        [Fact]
        public static void ECDsaX509SignatureGeneratorCtor_Exceptions()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => X509SignatureGenerator.CreateForECDsa(null));
        }

        [Theory]
        [MemberData(nameof(GetApplicableTestData))]
        public static void PublicKeyEncoding(EccTestData testData)
        {
            ECParameters keyParameters = testData.KeyParameters;

            using (ECDsa ecdsa = ECDsa.Create(keyParameters))
            {
                X509SignatureGenerator signatureGenerator =
                    X509SignatureGenerator.CreateForECDsa(ecdsa);

                PublicKey publicKey = signatureGenerator.PublicKey;

                Assert.Equal(
                    testData.CurveEncodedOidHex,
                    publicKey.EncodedParameters.RawData.ByteArrayToHex());

                string expectedKeyHex =
                    // Uncompressed Point
                    "04" +
                    // Qx
                    keyParameters.Q.X.ByteArrayToHex() +
                    // Qy
                    keyParameters.Q.Y.ByteArrayToHex();

                Assert.Equal(expectedKeyHex, publicKey.EncodedKeyValue.RawData.ByteArrayToHex());

                const string ecPublicKeyOid = "1.2.840.10045.2.1";
                Assert.Equal(ecPublicKeyOid, publicKey.Oid.Value);
                Assert.Equal(ecPublicKeyOid, publicKey.EncodedParameters.Oid.Value);
                Assert.Equal(ecPublicKeyOid, publicKey.EncodedKeyValue.Oid.Value);

                PublicKey publicKey2 = signatureGenerator.PublicKey;
                Assert.Same(publicKey, publicKey2);
            }
        }

        [Theory]
        [MemberData(nameof(SignatureDigestAlgorithms))]
        public static void SignatureAlgorithm_StableNotSame(HashAlgorithmName hashAlgorithm)
        {
            using (ECDsa ecdsa = ECDsa.Create(EccTestData.Secp256r1Data.KeyParameters))
            {
                var generator = X509SignatureGenerator.CreateForECDsa(ecdsa);

                byte[] sigAlg = generator.GetSignatureAlgorithmIdentifier(hashAlgorithm);
                byte[] sigAlg2 = generator.GetSignatureAlgorithmIdentifier(hashAlgorithm);

                Assert.NotSame(sigAlg, sigAlg2);
                Assert.Equal(sigAlg, sigAlg2);
            }
        }

        [Theory]
        [InlineData("MD5")]
        [InlineData("SHA1")]
        [InlineData("Potato")]
        public static void SignatureAlgorithm_NotSupported(string hashAlgorithmName)
        {
            using (ECDsa ecdsa = ECDsa.Create(EccTestData.Secp256r1Data.KeyParameters))
            {
                HashAlgorithmName hashAlgorithm = new HashAlgorithmName(hashAlgorithmName);
                var generator = X509SignatureGenerator.CreateForECDsa(ecdsa);

                Assert.Throws<ArgumentOutOfRangeException>(
                    "hashAlgorithm",
                    () => generator.GetSignatureAlgorithmIdentifier(hashAlgorithm));
            }
        }

        [Theory]
        [MemberData(nameof(SignatureDigestAlgorithms))]
        public static void SignatureAlgorithm_Encoding(HashAlgorithmName hashAlgorithm)
        {
            string expectedAlgOid;

            switch (hashAlgorithm.Name)
            {
                case "SHA256":
                    expectedAlgOid = "06082A8648CE3D040302";
                    break;
                case "SHA384":
                    expectedAlgOid = "06082A8648CE3D040303";
                    break;
                case "SHA512":
                    expectedAlgOid = "06082A8648CE3D040304";
                    break;
                case "SHA3-256":
                    expectedAlgOid = "060960864801650304030A";
                    break;
                case "SHA3-384":
                    expectedAlgOid = "060960864801650304030B";
                    break;
                case "SHA3-512":
                    expectedAlgOid = "060960864801650304030C";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hashAlgorithm));
            }

            EccTestData testData = EccTestData.Secp521r1Data;

            string expectedHex = $"30{(expectedAlgOid.Length / 2):X2}{expectedAlgOid}";

            using (ECDsa ecdsa = ECDsa.Create(testData.KeyParameters))
            {
                var generator = X509SignatureGenerator.CreateForECDsa(ecdsa);
                byte[] sigAlg = generator.GetSignatureAlgorithmIdentifier(hashAlgorithm);

                Assert.Equal(expectedHex, sigAlg.ByteArrayToHex());
            }
        }

        public static IEnumerable<object[]> GetApplicableTestData()
        {
            return EccTestData.EnumerateApplicableTests().Select(x => new object[] { x });
        }

        public static IEnumerable<object[]> SignatureDigestAlgorithms
        {
            get
            {
                yield return new object[] { HashAlgorithmName.SHA256 };
                yield return new object[] { HashAlgorithmName.SHA384 };
                yield return new object[] { HashAlgorithmName.SHA512 };
                yield return new object[] { HashAlgorithmName.SHA3_256 };
                yield return new object[] { HashAlgorithmName.SHA3_384 };
                yield return new object[] { HashAlgorithmName.SHA3_512 };
            }
        }
    }
}
