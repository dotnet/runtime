// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public static class RsaCngTests
    {
        public static bool KeySizeTrustsOSValue => !PlatformDetection.IsFullFramework || PlatformDetection.IsNetfx462OrNewer;

        [Fact]
        public static void SignVerifyHashRoundTrip()
        {
            byte[] message = "781021abcd982139a8bc91387870ac01".HexToByteArray();
            byte[] hash = SHA1.Create().ComputeHash(message);
            TestSignVerifyHashRoundTrip(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pss, 0x100);
        }

        private static void TestSignVerifyHashRoundTrip(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding paddingMode, int expectedSignatureLength)
        {
            using (RSA rsa = new RSACng())
            {
                byte[] signature = rsa.SignHash(hash, hashAlgorithm, paddingMode);

                // RSACng.SignHash() is intentionally non-deterministic so we can verify that we got back a signature of the right length
                // but nothing about the contents.
                Assert.Equal(expectedSignatureLength, signature.Length);

                bool verified = rsa.VerifyHash(hash, signature, hashAlgorithm, paddingMode);
                Assert.True(verified);
            }
        }

        [Fact]
        public static void SignVerifyDataRoundTrip()
        {
            byte[] message = "781021abcd982139a8bc91387870ac01".HexToByteArray();
            TestSignVerifyDataRoundTrip(message, HashAlgorithmName.SHA1, RSASignaturePadding.Pss, 0x100);
        }

        private static void TestSignVerifyDataRoundTrip(byte[] message, HashAlgorithmName hashAlgorithm, RSASignaturePadding paddingMode, int expectedSignatureLength)
        {
            using (RSA rsa = new RSACng())
            {
                byte[] signature = rsa.SignData(message, hashAlgorithm, paddingMode);

                // RSACng.SignHash() is intentionally non-deterministic so we can verify that we got back a signature of the right length
                // but nothing about the contents.
                Assert.Equal(expectedSignatureLength, signature.Length);

                bool verified = rsa.VerifyData(message, signature, hashAlgorithm, paddingMode);
                Assert.True(verified);
            }
        }

        [Fact]
        public static void VerifyHashPss()
        {
            using (RSA rsa = TestData.TestRsaKeyPair.CreateRsaCng())
            {
                byte[] hash = ("aecc72ccc8e3c3ff28ffd7acac7d3065225aa24e").HexToByteArray();
                byte[] signature =
                    ("3bde39974a884c4ecbc7296063a2a96edc778435b7b277b594a0712dcc0ddcd00b2970473b2f1359c98535dbae41fe2fb9e7"
                   + "42f77e1849c9746fd05c58d2e3f06c7290d96bbe53882391d76a73fd5f0650f1e46d5a81c83e617f05203f9ad6416957d06e"
                   + "49fe98a3d97359a3b7c1b80593b75265daa1a670aabf287d31a2f441").HexToByteArray();

                bool verified = rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pss);
                Assert.True(verified);
            }
        }

        [ConditionalFact(nameof(KeySizeTrustsOSValue))]
        public static void RSACng_Ctor_UnusualKeysize_384()
        {
            const int ExpectedKeySize = 384;

            byte[] keyBlob = (
                "525341328001000003000000300000001800000018000000010001DACC22D86E" +
                "671575032E31F206DCFC192C65E2D51089E5112D096F2882AFDB5B78CDB6572F" +
                "D2F61DB390472232E3D9F5FADBD7F8A18B3A75A4F6DFAEE3426FD0FF8BAC74B6" +
                "722DEFDF48144A6D88A780144FCEA66BDCDA50D6071C54E5D0DA5B").HexToByteArray();

            RSAParameters expected = System.Security.Cryptography.Rsa.Tests.TestData.RSA384Parameters;

            try
            {
                RSACng_Ctor_UnusualKeysize(ExpectedKeySize, keyBlob, expected);

                Assert.True(Rsa.Tests.RSAFactory.Supports384PrivateKey, "RSAFactory.Supports384PrivateKey");
            }
            catch (CryptographicException)
            {
                // If the provider is not known to fail loading a 384-bit key, let the exception be the
                // test failure. (If it is known to fail loading that key, we've now suppressed the throw,
                // and the test will pass.)
                if (Rsa.Tests.RSAFactory.Supports384PrivateKey)
                {
                    throw;
                }
            }
        }

        [ConditionalFact(nameof(KeySizeTrustsOSValue))]
        public static void RSACng_Ctor_UnusualKeysize_1032()
        {
            const int ExpectedKeySize = 1032;

            byte[] keyBlob = (
                "525341320804000003000000810000004100000041000000010001BCACB1A534" +
                "9D7B35A580AC3B3998EB15EBF900ECB329BF1F75717A00B2199C8A18D791B592" +
                "B7EC52BD5AF2DB0D3B635F0595753DFF7BA7C9872DBF7E3226DEF44A07CA568D" +
                "1017992C2B41BFE5EC3570824CF1F4B15919FED513FDA56204AF2034A2D08FF0" +
                "4C2CCA49D168FA03FA2FA32FCCD3484C15F0A2E5467C76FC760B55090E15300A" +
                "9D34BA37B6BDA831BC6727B2F7F6D0EFB7B33A99C9AF28CFD625E245A54F251B" +
                "784C4791ADA585ADB711D9300A3D52B450CC307F55D31E1217B9FFD7450D65C6" +
                "0DE8B6F54A7756FD1CCBA76CE41EF446D024031EE9C5A40931B07336CFED35A8" +
                "EE580E19DB8592CB0F266EC69028EB9E98E3E84FF1A459A8A26860A610F5").HexToByteArray();

            RSAParameters expected = System.Security.Cryptography.Rsa.Tests.TestData.RSA1032Parameters;
            RSACng_Ctor_UnusualKeysize(ExpectedKeySize, keyBlob, expected);
        }

        private static void RSACng_Ctor_UnusualKeysize(
            int expectedKeySize,
            byte[] keyBlob,
            RSAParameters expectedParameters)
        {
            // Pre-condition: Creating a key of this size will fail
            Assert.Throws<CryptographicException>(() => new RSACng(expectedKeySize));

            using (CngKey cngKey = CngKey.Import(keyBlob, new CngKeyBlobFormat("RSAPRIVATEBLOB")))
            using (RSACng rsaCng = new RSACng(cngKey))
            {
                Assert.Equal(expectedKeySize, rsaCng.KeySize);

                RSAParameters exported = rsaCng.ExportParameters(false);

                Assert.Equal(expectedParameters.Modulus, exported.Modulus);
                Assert.Equal(expectedParameters.Exponent, exported.Exponent);
            }
        }
    }
}
