// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    public static class TripleDESCipherTests
    {
        [Fact]
        public static void TripleDESDefaults()
        {
            using (TripleDES des = TripleDESFactory.Create())
            {
                Assert.Equal(192, des.KeySize);
                Assert.Equal(64, des.BlockSize);
            }
        }

        [Fact]
        public static void TripleDESRoundTrip192BitsNoneECB()
        {
            byte[] key = "c5629363d957054eba793093b83739bb78711db221a82379".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                alg.Padding = PaddingMode.None;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "de7d2dddea96b691e979e647dc9d3ca27d7f1ad673ca9570".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = "e56f72478c7479d169d54c0548b744af5b53efb1cdd26037".HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "de7d2dddea96b691e979e647dc9d3ca27d7f1ad673ca9570".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Fact]
        public static void TripleDESRoundTrip192BitsNoneCBC()
        {
            byte[] key = "b43eaf0260813fb47c87ae073a146006d359ad04061eb0e6".HexToByteArray();
            byte[] iv = "5fbc5bc21b8597d8".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                alg.IV = iv;
                alg.Padding = PaddingMode.None;
                alg.Mode = CipherMode.CBC;

                byte[] plainText = "79a86903608e133e020e1dc68c9835250c2f17b0ebeed91b".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = "dea36279600f19c602b6ed9bf3ffdac5ebf25c1c470eb61c".HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "79a86903608e133e020e1dc68c9835250c2f17b0ebeed91b".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Fact]
        public static void TripleDESRoundTrip192BitsZerosECB()
        {
            byte[] key = "9da5b265179d65f634dfc95513f25094411e51bb3be877ef".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                alg.Padding = PaddingMode.Zeros;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = "149ec32f558b27c7e4151e340d8184f18b4e25d2518f69d9".HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8000000".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

#if netstandard17
        [Fact]
        public static void TripleDESRoundTrip192BitsISO10126ECB()
        {
            byte[] key = "9da5b265179d65f634dfc95513f25094411e51bb3be877ef".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                alg.Padding = PaddingMode.ISO10126;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);

                // the padding data for ISO10126 is made up of random bytes, so we cannot actually test
                // the full encrypted text. We need to strip the padding and then compare
                byte[] decrypted = alg.Decrypt(cipher);

                Assert.Equal<byte>(plainText, decrypted);
            }
        }

        [Fact]
        public static void TripleDESRoundTrip192BitsANSIX923ECB()
        {
            byte[] key = "9da5b265179d65f634dfc95513f25094411e51bb3be877ef".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                alg.Padding = PaddingMode.ANSIX923;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                
                byte[] expectedCipher = "149ec32f558b27c7e4151e340d8184f1c90f0a499e20fda9".HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                Assert.Equal<byte>(plainText, decrypted);
            }
        }

        [Fact]
        public static void TripleDES_FailureToRoundTrip192Bits_DifferentPadding_ANSIX923_ZerosECB()
        {
            byte[] key = "9da5b265179d65f634dfc95513f25094411e51bb3be877ef".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                alg.Padding = PaddingMode.ANSIX923;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);

                byte[] expectedCipher = "149ec32f558b27c7e4151e340d8184f1c90f0a499e20fda9".HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                alg.Padding = PaddingMode.Zeros;
                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();

                // They should not decrypt to the same value
                Assert.NotEqual<byte>(plainText, decrypted);
            }
        }

#endif

        [Fact]
        public static void TripleDESRoundTrip192BitsZerosCBC()
        {
            byte[] key = "5e970c0d2323d53b28fa3de507d6d20f9f0cd97123398b4d".HexToByteArray();
            byte[] iv = "95498b5bf570f4c8".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                alg.IV = iv;
                alg.Padding = PaddingMode.Zeros;
                alg.Mode = CipherMode.CBC;

                byte[] plainText = "f9e9a1385bf3bd056d6a06eac662736891bd3e6837".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = "65f3dc211876a9daad238aa7d0c7ed7a3662296faf77dff9".HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "f9e9a1385bf3bd056d6a06eac662736891bd3e6837000000".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Fact]
        public static void TripleDESRoundTrip192BitsPKCS7ECB()
        {
            byte[] key = "155425f12109cd89378795a4ca337b3264689dca497ba2fa".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                alg.Padding = PaddingMode.PKCS7;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "5bd3c4e16a723a17ac60dd0efdb158e269cddfd0fa".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = "7b8d982ee0c14821daf1b8cf4e407c2eb328627b696ac36e".HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "5bd3c4e16a723a17ac60dd0efdb158e269cddfd0fa".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Fact]
        public static void TripleDESRoundTrip192BitsPKCS7CBC()
        {
            byte[] key = "6b42da08f93e819fbd26fce0785b0eec3d0cb6bfa053c505".HexToByteArray();
            byte[] iv = "8fc67ce5e7f28cde".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                alg.IV = iv;
                alg.Padding = PaddingMode.PKCS7;
                alg.Mode = CipherMode.CBC;

                byte[] plainText = "e867f915e275eab27d6951165d26dec6dd0acafcfc".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = "446f57875e107702afde16b57eaf250b87b8110bef29af89".HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "e867f915e275eab27d6951165d26dec6dd0acafcfc".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }
    }
}
