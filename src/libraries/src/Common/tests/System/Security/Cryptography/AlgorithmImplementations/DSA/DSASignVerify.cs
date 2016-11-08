// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    public partial class DSASignVerify
    {
        [Fact]
        public static void InvalidKeySize_DoesNotInvalidateKey()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                byte[] signature = dsa.SignData(DSATestData.HelloBytes, HashAlgorithmName.SHA1);

                // A 2049-bit key is hard to describe, none of the providers support it.
                Assert.ThrowsAny<CryptographicException>(() => dsa.KeySize = 2049);

                Assert.True(dsa.VerifyData(DSATestData.HelloBytes, signature, HashAlgorithmName.SHA1));
            }
        }

        [Fact]
        public static void SignAndVerifyDataNew1024()
        {
            using (DSA dsa = DSAFactory.Create(1024))
            {
                byte[] signature = dsa.SignData(DSATestData.HelloBytes, new HashAlgorithmName("SHA1"));
                bool signatureMatched = dsa.VerifyData(DSATestData.HelloBytes, signature, new HashAlgorithmName("SHA1"));
                Assert.True(signatureMatched);
            }
        }

        [Fact]
        public static void SignAndVerifyDataExplicit1024()
        {
            SignAndVerify(DSATestData.HelloBytes, "SHA1", DSATestData.GetDSA1024Params(), 40);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void SignAndVerifyDataExplicit2048()
        {
            SignAndVerify(DSATestData.HelloBytes, "SHA256", DSATestData.GetDSA2048Params(), 64);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void VerifyKnown_2048_SHA256()
        {
            byte[] signature =
            {
                0x92, 0x06, 0x0B, 0x57, 0xF1, 0x35, 0x20, 0x28,
                0xC6, 0x54, 0x4A, 0x0F, 0x08, 0x48, 0x5F, 0x5D,
                0x55, 0xA8, 0x42, 0xFB, 0x05, 0xA7, 0x3E, 0x32,
                0xCA, 0xC6, 0x91, 0x77, 0x70, 0x0A, 0x68, 0x44,
                0x60, 0x63, 0xF7, 0xE7, 0x96, 0x54, 0x8F, 0x4A,
                0x6D, 0x47, 0x10, 0xEE, 0x9A, 0x9F, 0xC2, 0xC8,
                0xDD, 0x74, 0xAE, 0x1A, 0x68, 0xF3, 0xA9, 0xB8,
                0x62, 0x14, 0x50, 0xA3, 0x01, 0x1D, 0x2A, 0x22,
            };

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.GetDSA2048Params());
                Assert.True(dsa.VerifyData(DSATestData.HelloBytes, signature, HashAlgorithmName.SHA256));
                Assert.False(dsa.VerifyData(DSATestData.HelloBytes, signature, HashAlgorithmName.SHA384));
                Assert.False(dsa.VerifyData(DSATestData.HelloBytes, signature, HashAlgorithmName.SHA512));
            }
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void VerifyKnown_2048_SHA384()
        {
            byte[] signature =
            {
                0x56, 0xBA, 0x70, 0x48, 0x18, 0xBA, 0xE3, 0x43,
                0xF0, 0x7F, 0x25, 0xFE, 0xEA, 0xF1, 0xDB, 0x49,
                0x37, 0x15, 0xD3, 0xD0, 0x5B, 0x9D, 0x57, 0x19,
                0x73, 0x44, 0xDA, 0x70, 0x8D, 0x44, 0x7D, 0xBA,
                0x83, 0xDB, 0x8E, 0x8F, 0x39, 0x0F, 0x83, 0xD5,
                0x0B, 0x73, 0x81, 0x77, 0x3D, 0x9B, 0x8D, 0xA4,
                0xAD, 0x94, 0x3C, 0xAB, 0x7A, 0x6C, 0x81, 0x48,
                0x2F, 0xCF, 0x50, 0xE3, 0x34, 0x0B, 0xEC, 0xF0,
            };

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.GetDSA2048Params());
                Assert.True(dsa.VerifyData(DSATestData.HelloBytes, signature, HashAlgorithmName.SHA384));
                Assert.False(dsa.VerifyData(DSATestData.HelloBytes, signature, HashAlgorithmName.SHA256));
                Assert.False(dsa.VerifyData(DSATestData.HelloBytes, signature, HashAlgorithmName.SHA512));
            }
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void VerifyKnown_2048_SHA512()
        {
            byte[] signature =
            {
                0x6F, 0x44, 0x68, 0x1F, 0x74, 0xF7, 0x90, 0x2F,
                0x38, 0x43, 0x9B, 0x00, 0x15, 0xDA, 0xF6, 0x8F,
                0x97, 0xB4, 0x4A, 0x52, 0xF7, 0xC1, 0xEC, 0x21,
                0xE2, 0x44, 0x48, 0x71, 0x0F, 0xEC, 0x5E, 0xB3,
                0xA1, 0xCB, 0xE4, 0x42, 0xC8, 0x1E, 0xCD, 0x3C,
                0xA8, 0x15, 0x51, 0xDE, 0x0C, 0xCC, 0xAE, 0x4D,
                0xEB, 0x2A, 0xE9, 0x13, 0xBB, 0x7F, 0x3C, 0xFB,
                0x69, 0x8A, 0x8E, 0x0F, 0x80, 0x87, 0x2E, 0xA6,
            };

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.GetDSA2048Params());
                Assert.True(dsa.VerifyData(DSATestData.HelloBytes, signature, HashAlgorithmName.SHA512));
                Assert.False(dsa.VerifyData(DSATestData.HelloBytes, signature, HashAlgorithmName.SHA256));
                Assert.False(dsa.VerifyData(DSATestData.HelloBytes, signature, HashAlgorithmName.SHA384));
            }
        }

        [Fact]
        public static void VerifyKnownSignature()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                byte[] data;
                byte[] signature;
                DSAParameters dsaParameters;
                DSATestData.GetDSA1024_186_2(out dsaParameters, out signature, out data);

                dsa.ImportParameters(dsaParameters);
                Assert.True(dsa.VerifyData(data, signature, HashAlgorithmName.SHA1));

                // Negative case
                signature[signature.Length - 1] ^= 0xff;
                Assert.False(dsa.VerifyData(data, signature, HashAlgorithmName.SHA1));
            }
        }

        private static void SignAndVerify(byte[] data, string hashAlgorithmName, DSAParameters dsaParameters, int expectedSignatureLength)
        {
            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(dsaParameters);
                byte[] signature = dsa.SignData(data, new HashAlgorithmName(hashAlgorithmName));
                Assert.Equal(expectedSignatureLength, signature.Length);
                bool signatureMatched = dsa.VerifyData(data, signature, new HashAlgorithmName(hashAlgorithmName));
                Assert.True(signatureMatched);
            }
        }

        static internal bool SupportsFips186_3
        {
            get
            {
                return DSAFactory.SupportsFips186_3;
            }
        }
    }
}
