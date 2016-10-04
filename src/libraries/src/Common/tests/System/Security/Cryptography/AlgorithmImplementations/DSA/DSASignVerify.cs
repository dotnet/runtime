// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    public class DSASignVerify
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
