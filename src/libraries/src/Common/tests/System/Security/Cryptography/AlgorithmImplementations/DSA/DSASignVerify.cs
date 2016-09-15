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
            // The parameters and signature come from FIPS 186-2 APPENDIX 5. EXAMPLE OF THE DSA
            using (DSA dsa = DSAFactory.Create())
            {
                DSAParameters dsaParameters = new DSAParameters
                {
                    P = (
                    "8df2a494492276aa3d25759bb06869cbeac0d83afb8d0cf7" +
                    "cbb8324f0d7882e5d0762fc5b7210eafc2e9adac32ab7aac" +
                    "49693dfbf83724c2ec0736ee31c80291").HexToByteArray(),

                    Q = ("c773218c737ec8ee993b4f2ded30f48edace915f").HexToByteArray(),

                    G = (
                    "626d027839ea0a13413163a55b4cb500299d5522956cefcb" +
                    "3bff10f399ce2c2e71cb9de5fa24babf58e5b79521925c9c" +
                    "c42e9f6f464b088cc572af53e6d78802").HexToByteArray(),

                    X = ("2070b3223dba372fde1c0ffc7b2e3b498b260614").HexToByteArray(),

                    Y = (
                    "19131871d75b1612a819f29d78d1b0d7346f7aa77bb62a85" +
                    "9bfd6c5675da9d212d3a36ef1672ef660b8c7c255cc0ec74" +
                    "858fba33f44c06699630a76b030ee333").HexToByteArray(),
                };

                byte[] signature = (
                    // r
                    "8bac1ab66410435cb7181f95b16ab97c92b341c0" +
                    // s
                    "41e2345f1f56df2458f426d155b4ba2db6dcd8c8"
                    ).HexToByteArray();

                byte[] data = Encoding.ASCII.GetBytes("abc");

                dsa.ImportParameters(dsaParameters);

                Assert.True(dsa.VerifyData(data, signature, HashAlgorithmName.SHA1));

                // Negative case
                unchecked
                {
                    --signature[signature.Length - 1];
                }
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
