// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public partial class RSASignatureFormatterTests : AsymmetricSignatureFormatterTests
    {
        [ConditionalFact(typeof(RSAFactory), nameof(RSAFactory.SupportsSha1Signatures))]
        public static void VerifySignature_SHA1()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA2048Params);

                var formatter = new RSAPKCS1SignatureFormatter(rsa);
                var deformatter = new RSAPKCS1SignatureDeformatter(rsa);

                using (SHA1 alg = SHA1.Create())
                {
                    VerifySignature(formatter, deformatter, alg, "SHA1");
                    VerifySignature(formatter, deformatter, alg, "sha1");
                }
            }
        }

        [Fact]
        public static void VerifySignature_SHA256()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA2048Params);

                var formatter = new RSAPKCS1SignatureFormatter(rsa);
                var deformatter = new RSAPKCS1SignatureDeformatter(rsa);

                using (SHA256 alg = SHA256.Create())
                {
                    VerifySignature(formatter, deformatter, alg, "SHA256");
                    VerifySignature(formatter, deformatter, alg, "sha256");
                }
            }
        }

        [Fact]
        public static void InvalidHashAlgorithm()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                var formatter = new RSAPKCS1SignatureFormatter(rsa);
                var deformatter = new RSAPKCS1SignatureDeformatter(rsa);

                // Exception is deferred until VerifySignature
                formatter.SetHashAlgorithm("INVALIDVALUE");
                deformatter.SetHashAlgorithm("INVALIDVALUE");

                using (SHA1 alg = SHA1.Create())
                {
                    Assert.Throws<CryptographicUnexpectedOperationException>(() =>
                        VerifySignature(formatter, deformatter, alg, "INVALIDVALUE"));
                }
            }
        }

        [ConditionalFact(typeof(RSAFactory), nameof(RSAFactory.SupportsSha1Signatures))]
        public static void VerifyKnownSignature()
        {
            byte[] hash = "012d161304fa0c6321221516415813022320620c".HexToByteArray();
            byte[] sig;

            using (RSA key = RSAFactory.Create())
            {
                key.ImportParameters(TestData.RSA1024Params);
                RSAPKCS1SignatureFormatter formatter = new RSAPKCS1SignatureFormatter(key);
                formatter.SetHashAlgorithm("SHA1");
                sig = formatter.CreateSignature(hash);

                byte[] expectedSig =
                    ("566390012605b1c4c01c3c2f91ce27d19476ab7131d9ee9cd1b811afb2be02ab6b498b862f0b2368ed6b09ccc9e0ec0d4f97a4f318f4f11" +
                     "ae882a1131012dc35d2e0b810a38e05da71d291e88b306605c9d34815091641370bd7db7a87b115bd427fcb9993bc5ba2bd518745aef80c" +
                     "a4557cfa1d827ff1610595d8eeb4c15073").HexToByteArray();
                Assert.Equal(expectedSig, sig);
            }

            using (RSA key = RSAFactory.Create()) // Test against a different instance
            {
                key.ImportParameters(TestData.RSA1024Params);
                RSAPKCS1SignatureDeformatter deformatter = new RSAPKCS1SignatureDeformatter(key);
                deformatter.SetHashAlgorithm("SHA1");
                bool verified = deformatter.VerifySignature(hash, sig);
                Assert.True(verified);

                sig[3] ^= 0xff;
                verified = deformatter.VerifySignature(hash, sig);
                Assert.False(verified);
            }
        }
    }
}
