// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                byte[] message = "781021abcd982139a8bc91387870ac01".HexToByteArray();
                byte[] hash = ("aecc72ccc8e3c3ff28ffd7acac7d3065225aa24e").HexToByteArray();
                byte[] signature =
                    ("3bde39974a884c4ecbc7296063a2a96edc778435b7b277b594a0712dcc0ddcd00b2970473b2f1359c98535dbae41fe2fb9e7"
                   + "42f77e1849c9746fd05c58d2e3f06c7290d96bbe53882391d76a73fd5f0650f1e46d5a81c83e617f05203f9ad6416957d06e"
                   + "49fe98a3d97359a3b7c1b80593b75265daa1a670aabf287d31a2f441").HexToByteArray();

                bool verified = rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pss);
                Assert.True(verified);
            }
        }
    }
}
