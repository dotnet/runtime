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
        public static void EncryptDecryptRoundtrip()
        {
            {
                byte[] plainText = "87abc91203fa927823".HexToByteArray();
                TestEncryptDecryptRoundTrip(plainText, RSAEncryptionPadding.Pkcs1, 0x100);
                TestEncryptDecryptRoundTrip(plainText, RSAEncryptionPadding.OaepSHA1, 0x100);
                TestEncryptDecryptRoundTrip(plainText, RSAEncryptionPadding.OaepSHA256, 0x100);
                TestEncryptDecryptRoundTrip(plainText, RSAEncryptionPadding.OaepSHA384, 0x100);
                TestEncryptDecryptRoundTrip(plainText, RSAEncryptionPadding.OaepSHA512, 0x100);
            }
            {
                byte[] plainText = "1298999adbbc".HexToByteArray();
                TestEncryptDecryptRoundTrip(plainText, RSAEncryptionPadding.Pkcs1, 0x100);
                TestEncryptDecryptRoundTrip(plainText, RSAEncryptionPadding.OaepSHA1, 0x100);
                TestEncryptDecryptRoundTrip(plainText, RSAEncryptionPadding.OaepSHA256, 0x100);
                TestEncryptDecryptRoundTrip(plainText, RSAEncryptionPadding.OaepSHA384, 0x100);
                TestEncryptDecryptRoundTrip(plainText, RSAEncryptionPadding.OaepSHA512, 0x100);
            }
        }

        private static void TestEncryptDecryptRoundTrip(byte[] plainText, RSAEncryptionPadding paddingMode, int expectedCipherSize)
        {
            using (RSA rsaCng = new RSACng())
            {
                byte[] cipher = rsaCng.Encrypt(plainText, paddingMode);

                // RSACng.Encrypt() is intentionally non-deterministic so we can verify that we got back a cipher of the right length
                // but nothing about the contents.
                Assert.Equal(expectedCipherSize, cipher.Length);

                // But we can test to see that it decrypts back to the original.
                byte[] plainTextAgain = rsaCng.Decrypt(cipher, paddingMode);
                Assert.Equal<byte>(plainText, plainTextAgain);
            }
        }

        [Fact]
        public static void DecryptPkcs1()
        {
            byte[] cipher =
                ("49d4247afc62226a48c8fc0983fe5c774dcffa57a2d44f68bbcd40af0111ab5b9e8683f994f89bdd1c295e276666f810e343"
               + "583882acad9349810368204c5e3e24e196f7fc3fc1621e8c2843006e7b80732043faaa4ef873941a626e716943f2f5addd3e"
               + "5b3d39c6e856bdca84d7040b711315794b612808a51b587df244d539").HexToByteArray();

            using (RSA rsa = TestData.TestRsaKeyPair.CreateRsaCng())
            {
                byte[] expectedPlainText = "87abc91203fa927823".HexToByteArray();
                byte[] actualPlainText = rsa.Decrypt(cipher, RSAEncryptionPadding.Pkcs1);
                Assert.Equal<byte>(expectedPlainText, actualPlainText);
            }
        }

        [Fact]
        public static void DecryptOaepSHA1()
        {
            byte[] cipher =
                ("0b9293720a2d171c3c0754611a83180086199cff3531e4849eb3da4ce5e9a19fc1de619cceae2b5eb341e2311b4651baa01a"
               + "8ecdd9b1e5c5baf0181bf31595407f2730e24a952ebef506ee7812d17658ac13de37033a8c94aef2839d3c528f438f5f5e3b"
               + "290d1a12af586c64073f99d1fbd4a406cdae46f7059ee4300d334855").HexToByteArray();

            using (RSA rsa = TestData.TestRsaKeyPair.CreateRsaCng())
            {
                byte[] expectedPlainText = "87abc91203fa927823".HexToByteArray();
                byte[] actualPlainText = rsa.Decrypt(cipher, RSAEncryptionPadding.OaepSHA1);
                Assert.Equal<byte>(expectedPlainText, actualPlainText);
            }
        }

        [Fact]
        public static void SignVerifyHashRoundTrip()
        {
            {
                byte[] message = "781021abcd982139a8bc91387870ac01".HexToByteArray();
                byte[] hash = SHA1.Create().ComputeHash(message);
                TestSignVerifyHashRoundTrip(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pss, 0x100);
            }

            {
                byte[] message = "abcd992377".HexToByteArray();
                byte[] hash = SHA256.Create().ComputeHash(message);
                TestSignVerifyHashRoundTrip(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1, 0x100);
            }
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
            {
                byte[] message = "781021abcd982139a8bc91387870ac01".HexToByteArray();
                TestSignVerifyDataRoundTrip(message, HashAlgorithmName.SHA1, RSASignaturePadding.Pss, 0x100);
            }

            {
                byte[] message = "abcd992377".HexToByteArray();
                TestSignVerifyDataRoundTrip(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1, 0x100);
            }
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
        public static void VerifyHashPkcs1()
        {
            using (RSA rsa = TestData.TestRsaKeyPair.CreateRsaCng())
            {
                byte[] message = "781021abcd982139a8bc91387870ac01".HexToByteArray();
                byte[] hash = ("aecc72ccc8e3c3ff28ffd7acac7d3065225aa24e").HexToByteArray();
                byte[] signature =
                    ("a35477a7db1f5d47fdca660eb7ab31520a6267b45498c5fb52b9a0a634545f96d289da27f8f8f21cee71271fecfb1d013ed9"
                   + "6f12f1a17fbb29190bf1bb4bbeb14f6eb7c4703982ac9c3ad355f30064596cfebc5c2dae59962035fbbaa073af4bf2774195"
                   + "74a2937736ccdec4c9b6f6b930f9787ca5be9e8edfe493473903e965").HexToByteArray();

                bool verified = rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
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

        [Fact]
        public static void VerifyData()
        {
            using (RSA rsa = TestData.TestRsaKeyPair.CreateRsaCng())
            {
                byte[] message = "781021abcd982139a8bc91387870ac01".HexToByteArray();
                byte[] signature =
                    ("a35477a7db1f5d47fdca660eb7ab31520a6267b45498c5fb52b9a0a634545f96d289da27f8f8f21cee71271fecfb1d013ed9"
                   + "6f12f1a17fbb29190bf1bb4bbeb14f6eb7c4703982ac9c3ad355f30064596cfebc5c2dae59962035fbbaa073af4bf2774195"
                   + "74a2937736ccdec4c9b6f6b930f9787ca5be9e8edfe493473903e965").HexToByteArray();

                bool verified = rsa.VerifyData(message, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                Assert.True(verified);


            }
        }

        [Fact]
        public static void SignAndVerifyDataFromStream()
        {
            TestSignAndVerifyDataFromStream(100);
            TestSignAndVerifyDataFromStream(4096);
            TestSignAndVerifyDataFromStream(4097);
            TestSignAndVerifyDataFromStream(4096 * 2);
            TestSignAndVerifyDataFromStream(4096 * 2 + 1);
            TestSignAndVerifyDataFromStream(0);
        }

        private static void TestSignAndVerifyDataFromStream(int messageSize)
        {
            RSASignaturePadding padding = RSASignaturePadding.Pkcs1;
            byte[] message = new byte[messageSize];
            byte b = 5;
            for (int i = 0; i < message.Length; i++)
            {
                message[i] = b;
                b = (byte)((b << 4) | (i & 0xf));
            }

            byte[] hash = SHA1.Create().ComputeHash(message);
            Stream stream = new MemoryStream(message);

            using (RSA rsa = new RSACng())
            {
                byte[] signature = rsa.SignData(stream, HashAlgorithmName.SHA1, padding);

                // Since the unique codepath being tested here is HashData(Stream...), the interesting test is to see if HashData(Stream...)
                // computed the right hash. The easiest way to test that is to compute the hash ourselves and call VerifyHash.
                bool verified = rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA1, padding);
                Assert.True(verified);

                stream = new MemoryStream(message);
                verified = rsa.VerifyData(stream, signature, HashAlgorithmName.SHA1, padding);
                Assert.True(verified);
            }
        }

        [Fact]
        public static void ImportExport()
        {
            using (RSA rsa = new RSACng())
            {
                rsa.ImportParameters(TestData.TestRsaKeyPair);
                RSAParameters reExported;

                // This is the current 4.6 behavior.
                Assert.Throws<CryptographicException>(() => reExported = rsa.ExportParameters(includePrivateParameters: true));
                //AssertRSAParametersEquals(TestData.TestRsaKeyPair, reExported);
            }
        }

        [Fact]
        public static void ImportExportPublicOnly()
        {
            using (RSA rsa = new RSACng())
            {
                rsa.ImportParameters(TestData.TestRsaKeyPair);
                RSAParameters reExported = rsa.ExportParameters(includePrivateParameters: false);
                Assert.Null(reExported.D);
                Assert.Null(reExported.DP);
                Assert.Null(reExported.DQ);
                Assert.Null(reExported.InverseQ);
                Assert.Null(reExported.P);
                Assert.Null(reExported.Q);
                Assert.Equal<byte>(TestData.TestRsaKeyPair.Exponent, reExported.Exponent);
                Assert.Equal<byte>(TestData.TestRsaKeyPair.Modulus, reExported.Modulus);
            }
        }

        private static void AssertRSAParametersEquals(RSAParameters expected, RSAParameters actual)
        {
            Assert.Equal<byte>(expected.D, actual.D);
            Assert.Equal<byte>(expected.DP, actual.DP);
            Assert.Equal<byte>(expected.DQ, actual.DQ);
            Assert.Equal<byte>(expected.Exponent, actual.Exponent);
            Assert.Equal<byte>(expected.InverseQ, actual.InverseQ);
            Assert.Equal<byte>(expected.Modulus, actual.Modulus);
            Assert.Equal<byte>(expected.P, actual.P);
            Assert.Equal<byte>(expected.Q, actual.Q);
        }
    }
}
