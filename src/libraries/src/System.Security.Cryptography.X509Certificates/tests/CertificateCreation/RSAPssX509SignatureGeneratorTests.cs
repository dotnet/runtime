// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    public static class RSAPssX509SignatureGeneratorTests
    {
        [Fact]
        public static void RsaPssSignatureGeneratorCtor_Exceptions()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => X509SignatureGenerator.CreateForRSA(null, RSASignaturePadding.Pss));
        }

        [Fact]
        public static void PublicKeyEncoding()
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(TestData.RsaBigExponentParams);

                X509SignatureGenerator signatureGenerator = X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pss);
                PublicKey publicKey = signatureGenerator.PublicKey;

                // Irrespective of what the current key thinks, the PublicKey value we encode for RSA will always write
                // DER-NULL parameters, by the guidance of RFC 3447:
                //
                //    The object identifier rsaEncryption identifies RSA public and private
                //    keys as defined in Appendices A.1.1 and A.1.2.  The parameters field
                //    associated with this OID in a value of type AlgorithmIdentifier shall
                //    have a value of type NULL.
                Assert.Equal(new byte[] { 5, 0 }, publicKey.EncodedParameters.RawData);

                string expectedKeyHex =
                    // SEQUENCE
                    "3082010C" +
                    //   INTEGER (modulus)
                    "0282010100" + TestData.RsaBigExponentParams.Modulus.ByteArrayToHex() +
                    //   INTEGER (exponent)
                    "0205" + TestData.RsaBigExponentParams.Exponent.ByteArrayToHex();

                Assert.Equal(expectedKeyHex, publicKey.EncodedKeyValue.RawData.ByteArrayToHex());

                const string rsaEncryptionOid = "1.2.840.113549.1.1.1";
                Assert.Equal(rsaEncryptionOid, publicKey.Oid.Value);
                Assert.Equal(rsaEncryptionOid, publicKey.EncodedParameters.Oid.Value);
                Assert.Equal(rsaEncryptionOid, publicKey.EncodedKeyValue.Oid.Value);

                PublicKey publicKey2 = signatureGenerator.PublicKey;
                Assert.Same(publicKey, publicKey2);
            }
        }

        [Theory]
        [InlineData("SHA256")]
        [InlineData("SHA384")]
        [InlineData("SHA512")]
        public static void SignatureAlgorithm_StableNotSame(string hashAlgorithmName)
        {
            using (RSA rsa = RSA.Create())
            {
                RSAParameters parameters = TestData.RsaBigExponentParams;
                rsa.ImportParameters(parameters);

                var signatureGenerator = X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pss);

                HashAlgorithmName hashAlgorithm = new HashAlgorithmName(hashAlgorithmName);

                byte[] sigAlg = signatureGenerator.GetSignatureAlgorithmIdentifier(hashAlgorithm);
                byte[] sigAlg2 = signatureGenerator.GetSignatureAlgorithmIdentifier(hashAlgorithm);

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
            using (RSA rsa = RSA.Create())
            {
                RSAParameters parameters = TestData.RsaBigExponentParams;
                rsa.ImportParameters(parameters);

                var signatureGenerator = X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pss);

                HashAlgorithmName hashAlgorithm = new HashAlgorithmName(hashAlgorithmName);

                Assert.Throws<ArgumentOutOfRangeException>(
                    "hashAlgorithm",
                    () => signatureGenerator.GetSignatureAlgorithmIdentifier(hashAlgorithm));
            }
        }

        [Theory]
        [InlineData("SHA256")]
        [InlineData("SHA384")]
        [InlineData("SHA512")]
        public static void SignatureAlgorithm_Encoding(string hashAlgorithmName)
        {

            // PSS-MGF1-with-SHA-2-* end up differing in only three bytes:
            // 1) hashAlgorithm.algorithmId's last byte
            // 2) mgf.parameters.algorithmId's last byte
            // 3) saltLen.

            byte lastOidByte;
            byte saltLenByte;

            switch (hashAlgorithmName)
            {
                case "SHA256":
                    lastOidByte = 0x01;
                    saltLenByte = 256 / 8;
                    break;
                case "SHA384":
                    lastOidByte = 0x02;
                    saltLenByte = 384 / 8;
                    break;
                case "SHA512":
                    lastOidByte = 0x03;
                    saltLenByte = 512 / 8;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hashAlgorithmName));
            }

            string expectedHex =
                // SEQUENCE (AlgorithmIdentifier)
                "303D" +
                    // OBJECT IDENTIFIER (AlgorithmIdentifier.algorithm == Oids.RsaPss)
                    "06092A864886F70D01010A" +
                    // SEQUENCE (AlgorithmIdentifier.params == RSASSA-PSS-params)
                    "3030" +
                        // CONSTRUCTED CONTEXT SPECIFIC 0 (params.hashAlgorithm)
                        "A00D" +
                            // SEQUENCE (AlgorithmIdentifier)
                            "300B" +
                                // OBJECT IDENTIFIER (params.hashAlgorithm.algorithm)
                                "06096086480165030402" + lastOidByte.ToString("X2") +
                        // CONSTRUCTED CONTEXT SPECIFIC 1 (params.maskGenAlgorithm)
                        "A11A" +
                            // SEQUENCE (MaskGenAlgorithm)
                            "3018" +
                                // OBJECT IDENTIFIER (MaskGenAlgorithm.algorithm == Oids.Mgf1)
                                "06092A864886F70D010108" +
                                // SEQUENCE (MaskGenAlgorithm.params)
                                "300B" +
                                    // OBJECT IDENTIFIER (MGF PRF == same hash algorithm as above)
                                    "06096086480165030402" + lastOidByte.ToString("X2") +
                        // CONSTRUCTED CONTEXT SPECIFIC 2 (params.saltLength)
                        "A203" +
                            // INTEGER (saltLength == size of hash)
                            "0201" + saltLenByte.ToString("X2");

            using (RSA rsa = RSA.Create())
            {
                RSAParameters parameters = TestData.RsaBigExponentParams;
                rsa.ImportParameters(parameters);

                HashAlgorithmName hashAlgorithm = new HashAlgorithmName(hashAlgorithmName);
                var signatureGenerator = X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pss);
                byte[] sigAlg = signatureGenerator.GetSignatureAlgorithmIdentifier(hashAlgorithm);

                Assert.Equal(expectedHex, sigAlg.ByteArrayToHex());
            }
        }
    }
}
