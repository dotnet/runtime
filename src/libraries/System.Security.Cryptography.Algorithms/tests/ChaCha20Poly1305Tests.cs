// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Algorithms.Tests
{
    [ConditionalClass(typeof(ChaCha20Poly1305), nameof(ChaCha20Poly1305.IsSupported))]
    public class ChaCha20Poly1305Tests : CommonAEADTests
    {
        private const int KeySizeInBytes = 256 / 8;
        private const int NonceSizeInBytes = 96 / 8;
        private const int TagSizeInBytes = 128 / 8;

        [Theory]
        [MemberData(nameof(EncryptTamperAADDecryptTestInputs))]
        public static void EncryptTamperAADDecrypt(int dataLength, int additionalDataLength)
        {
            byte[] additionalData = new byte[additionalDataLength];
            RandomNumberGenerator.Fill(additionalData);

            byte[] plaintext = Enumerable.Range(1, dataLength).Select((x) => (byte)x).ToArray();
            byte[] ciphertext = new byte[dataLength];
            byte[] key = RandomNumberGenerator.GetBytes(KeySizeInBytes);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
            byte[] tag = new byte[TagSizeInBytes];

            using (var chaChaPoly = new ChaCha20Poly1305(key))
            {
                chaChaPoly.Encrypt(nonce, plaintext, ciphertext, tag, additionalData);

                additionalData[0] ^= 1;

                byte[] decrypted = new byte[dataLength];
                Assert.Throws<CryptographicException>(
                    () => chaChaPoly.Decrypt(nonce, ciphertext, tag, decrypted, additionalData));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)] // 128-bit keys disallowed
        [InlineData(17)]
        [InlineData(24)] // 192-bit keys disallowed
        [InlineData(29)]
        [InlineData(33)]
        public static void InvalidKeyLength(int keyLength)
        {
            byte[] key = new byte[keyLength];
            Assert.Throws<CryptographicException>(() => new ChaCha20Poly1305(key));
        }

        [Theory]
        [MemberData(nameof(GetInvalidNonceSizes))]
        public static void InvalidNonceSize(int nonceSize)
        {
            int dataLength = 30;
            byte[] plaintext = Enumerable.Range(1, dataLength).Select((x) => (byte)x).ToArray();
            byte[] ciphertext = new byte[dataLength];
            byte[] key = RandomNumberGenerator.GetBytes(KeySizeInBytes);
            byte[] nonce = RandomNumberGenerator.GetBytes(nonceSize);
            byte[] tag = new byte[TagSizeInBytes];

            using (var chaChaPoly = new ChaCha20Poly1305(key))
            {
                Assert.Throws<ArgumentException>("nonce", () => chaChaPoly.Encrypt(nonce, plaintext, ciphertext, tag));
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidTagSizes))]
        public static void InvalidTagSize(int tagSize)
        {
            int dataLength = 30;
            byte[] plaintext = Enumerable.Range(1, dataLength).Select((x) => (byte)x).ToArray();
            byte[] ciphertext = new byte[dataLength];
            byte[] key = RandomNumberGenerator.GetBytes(KeySizeInBytes);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
            byte[] tag = new byte[tagSize];

            using (var chaChaPoly = new ChaCha20Poly1305(key))
            {
                Assert.Throws<ArgumentException>("tag", () => chaChaPoly.Encrypt(nonce, plaintext, ciphertext, tag));
            }
        }

        [Fact]
        public static void ValidNonceAndTagSize()
        {
            const int dataLength = 35;
            byte[] plaintext = Enumerable.Range(1, dataLength).Select((x) => (byte)x).ToArray();
            byte[] ciphertext = new byte[dataLength];
            byte[] key = RandomNumberGenerator.GetBytes(KeySizeInBytes);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
            byte[] tag = new byte[TagSizeInBytes];

            using (var chaChaPoly = new ChaCha20Poly1305(key))
            {
                chaChaPoly.Encrypt(nonce, plaintext, ciphertext, tag);

                byte[] decrypted = new byte[dataLength];
                chaChaPoly.Decrypt(nonce, ciphertext, tag, decrypted);
                Assert.Equal(plaintext, decrypted);
            }
        }

        [Fact]
        public static void TwoEncryptionsAndDecryptionsUsingOneInstance()
        {
            byte[] key = "fde37f01fe9ca260f432e0ed98b3e0bb23895ca1ca1ce2cfcaaca2ccc98889d7".HexToByteArray();
            byte[] originalData1 = Enumerable.Range(1, 15).Select((x) => (byte)x).ToArray();
            byte[] originalData2 = Enumerable.Range(14, 97).Select((x) => (byte)x).ToArray();
            byte[] associatedData2 = Enumerable.Range(100, 109).Select((x) => (byte)x).ToArray();
            byte[] nonce1 = "b41329dd64af2c3036661b46".HexToByteArray();
            byte[] nonce2 = "8ba10892e8b87d031196bf99".HexToByteArray();

            byte[] expectedCiphertext1 = "75f5aafbbabab80a3cfa2ecfd1bc58".HexToByteArray();
            byte[] expectedTag1 = "1ed70acc454fba01f0354e93eba9b428".HexToByteArray();

            byte[] expectedCiphertext2 = (
                "f95cc19929463ba96a2cfc21fac5345ec308e2748995ba285af6b21ca3d665bc" +
                "00144604b38e9645fb2d5f5893fc78871bd8f5fc91caaa013eac5f80397fd65c" +
                "358c239f013f3c75da17ddbd14de01eb67f5204dfa787986fb27a098fe21b2c5" +
                "07").HexToByteArray();
            byte[] expectedTag2 = "9877f87f29f68b5f9efb071c1351ccf6".HexToByteArray();

            using (var chaChaPoly = new ChaCha20Poly1305(key))
            {
                byte[] ciphertext1 = new byte[originalData1.Length];
                byte[] tag1 = new byte[expectedTag1.Length];
                chaChaPoly.Encrypt(nonce1, originalData1, ciphertext1, tag1);
                Assert.Equal(expectedCiphertext1, ciphertext1);
                Assert.Equal(expectedTag1, tag1);

                byte[] ciphertext2 = new byte[originalData2.Length];
                byte[] tag2 = new byte[expectedTag2.Length];
                chaChaPoly.Encrypt(nonce2, originalData2, ciphertext2, tag2, associatedData2);
                Assert.Equal(expectedCiphertext2, ciphertext2);
                Assert.Equal(expectedTag2, tag2);

                byte[] plaintext1 = new byte[originalData1.Length];
                chaChaPoly.Decrypt(nonce1, ciphertext1, tag1, plaintext1);
                Assert.Equal(originalData1, plaintext1);

                byte[] plaintext2 = new byte[originalData2.Length];
                chaChaPoly.Decrypt(nonce2, ciphertext2, tag2, plaintext2, associatedData2);
                Assert.Equal(originalData2, plaintext2);
            }
        }

        [Theory]
        [MemberData(nameof(PlaintextAndCiphertextSizeDifferTestInputs))]
        public static void PlaintextAndCiphertextSizeDiffer(int ptLen, int ctLen)
        {
            byte[] key = new byte[KeySizeInBytes];
            byte[] nonce = new byte[NonceSizeInBytes];
            byte[] plaintext = new byte[ptLen];
            byte[] ciphertext = new byte[ctLen];
            byte[] tag = new byte[TagSizeInBytes];

            using (var chaChaPoly = new ChaCha20Poly1305(key))
            {
                Assert.Throws<ArgumentException>(() => chaChaPoly.Encrypt(nonce, plaintext, ciphertext, tag));
                Assert.Throws<ArgumentException>(() => chaChaPoly.Decrypt(nonce, ciphertext, tag, plaintext));
            }
        }

        [Fact]
        public static void NullKey()
        {
            Assert.Throws<ArgumentNullException>(() => new ChaCha20Poly1305((byte[])null));
        }

        [Fact]
        public static void EncryptDecryptNullNonce()
        {
            byte[] key = "fde37f01fe9ca260f432e0ed98b3e0bb23895ca1ca1ce2cfcaaca2ccc98889d7".HexToByteArray();
            byte[] plaintext = new byte[0];
            byte[] ciphertext = new byte[0];
            byte[] tag = new byte[TagSizeInBytes];

            using (var chaChaPoly = new ChaCha20Poly1305(key))
            {
                Assert.Throws<ArgumentNullException>(() => chaChaPoly.Encrypt((byte[])null, plaintext, ciphertext, tag));
                Assert.Throws<ArgumentNullException>(() => chaChaPoly.Decrypt((byte[])null, ciphertext, tag, plaintext));
            }
        }

        [Fact]
        public static void EncryptDecryptNullPlaintext()
        {
            byte[] key = "fde37f01fe9ca260f432e0ed98b3e0bb23895ca1ca1ce2cfcaaca2ccc98889d7".HexToByteArray();
            byte[] nonce = new byte[NonceSizeInBytes];
            byte[] ciphertext = new byte[0];
            byte[] tag = new byte[TagSizeInBytes];

            using (var chaChaPoly = new ChaCha20Poly1305(key))
            {
                Assert.Throws<ArgumentNullException>(() => chaChaPoly.Encrypt(nonce, (byte[])null, ciphertext, tag));
                Assert.Throws<ArgumentNullException>(() => chaChaPoly.Decrypt(nonce, ciphertext, tag, (byte[])null));
            }
        }

        [Fact]
        public static void EncryptDecryptNullCiphertext()
        {
            byte[] key = "fde37f01fe9ca260f432e0ed98b3e0bb23895ca1ca1ce2cfcaaca2ccc98889d7".HexToByteArray();
            byte[] nonce = new byte[NonceSizeInBytes];
            byte[] plaintext = new byte[0];
            byte[] tag = new byte[TagSizeInBytes];

            using (var chaChaPoly = new ChaCha20Poly1305(key))
            {
                Assert.Throws<ArgumentNullException>(() => chaChaPoly.Encrypt(nonce, plaintext, (byte[])null, tag));
                Assert.Throws<ArgumentNullException>(() => chaChaPoly.Decrypt(nonce, (byte[])null, tag, plaintext));
            }
        }

        [Fact]
        public static void EncryptDecryptNullTag()
        {
            byte[] key = "fde37f01fe9ca260f432e0ed98b3e0bb23895ca1ca1ce2cfcaaca2ccc98889d7".HexToByteArray();
            byte[] nonce = new byte[NonceSizeInBytes];
            byte[] plaintext = new byte[0];
            byte[] ciphertext = new byte[0];

            using (var chaChaPoly = new ChaCha20Poly1305(key))
            {
                Assert.Throws<ArgumentNullException>(() => chaChaPoly.Encrypt(nonce, plaintext, ciphertext, (byte[])null));
                Assert.Throws<ArgumentNullException>(() => chaChaPoly.Decrypt(nonce, ciphertext, (byte[])null, plaintext));
            }
        }

        [Fact]
        public static void InplaceEncryptDecrypt()
        {
            byte[] key = "fde37f01fe9ca260f432e0ed98b3e0bb23895ca1ca1ce2cfcaaca2ccc98889d7".HexToByteArray();
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
            byte[] originalPlaintext = new byte[] { 1, 2, 8, 12, 16, 99, 0 };
            byte[] data = (byte[])originalPlaintext.Clone();
            byte[] tag = new byte[TagSizeInBytes];

            using (var chaChaPoly = new ChaCha20Poly1305(key))
            {
                chaChaPoly.Encrypt(nonce, data, data, tag);
                Assert.NotEqual(originalPlaintext, data);

                chaChaPoly.Decrypt(nonce, data, tag, data);
                Assert.Equal(originalPlaintext, data);
            }
        }

        [Fact]
        public static void InplaceEncryptTamperTagDecrypt()
        {
            byte[] key = "fde37f01fe9ca260f432e0ed98b3e0bb23895ca1ca1ce2cfcaaca2ccc98889d7".HexToByteArray();
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
            byte[] originalPlaintext = new byte[] { 1, 2, 8, 12, 16, 99, 0 };
            byte[] data = (byte[])originalPlaintext.Clone();
            byte[] tag = new byte[TagSizeInBytes];

            using (var chaChaPoly = new ChaCha20Poly1305(key))
            {
                chaChaPoly.Encrypt(nonce, data, data, tag);
                Assert.NotEqual(originalPlaintext, data);

                tag[0] ^= 1;

                Assert.Throws<CryptographicException>(
                    () => chaChaPoly.Decrypt(nonce, data, tag, data));
                Assert.Equal(new byte[data.Length], data);
            }
        }

        [Theory]
        [MemberData(nameof(GetRfc8439TestCases))]
        public static void Rfc8439Tests(AEADTest testCase)
        {
            using (var chaChaPoly = new ChaCha20Poly1305(testCase.Key))
            {
                byte[] ciphertext = new byte[testCase.Plaintext.Length];
                byte[] tag = new byte[testCase.Tag.Length];
                chaChaPoly.Encrypt(testCase.Nonce, testCase.Plaintext, ciphertext, tag, testCase.AssociatedData);
                Assert.Equal(testCase.Ciphertext, ciphertext);
                Assert.Equal(testCase.Tag, tag);

                byte[] plaintext = new byte[testCase.Plaintext.Length];
                chaChaPoly.Decrypt(testCase.Nonce, ciphertext, tag, plaintext, testCase.AssociatedData);
                Assert.Equal(testCase.Plaintext, plaintext);
            }
        }

        [Theory]
        [MemberData(nameof(GetRfc8439TestCases))]
        public static void Rfc8439TestsTamperTag(AEADTest testCase)
        {
            using (var chaChaPoly = new ChaCha20Poly1305(testCase.Key))
            {
                byte[] ciphertext = new byte[testCase.Plaintext.Length];
                byte[] tag = new byte[testCase.Tag.Length];
                chaChaPoly.Encrypt(testCase.Nonce, testCase.Plaintext, ciphertext, tag, testCase.AssociatedData);
                Assert.Equal(testCase.Ciphertext, ciphertext);
                Assert.Equal(testCase.Tag, tag);

                tag[0] ^= 1;

                byte[] plaintext = RandomNumberGenerator.GetBytes(testCase.Plaintext.Length);
                Assert.Throws<CryptographicException>(
                    () => chaChaPoly.Decrypt(testCase.Nonce, ciphertext, tag, plaintext, testCase.AssociatedData));
                Assert.Equal(new byte[plaintext.Length], plaintext);
            }
        }

        public static IEnumerable<object[]> GetInvalidNonceSizes()
        {
            yield return new object[] { 0 };
            yield return new object[] { 8 };
            yield return new object[] { 11 };
            yield return new object[] { 13 };
            yield return new object[] { 16 };
        }

        public static IEnumerable<object[]> GetInvalidTagSizes()
        {
            yield return new object[] { 0 };
            yield return new object[] { 8 };
            yield return new object[] { 12 };
            yield return new object[] { 15 };
            yield return new object[] { 17 };
        }

        // https://tools.ietf.org/html/rfc8439
        private const string Rfc8439TestVectors = "RFC 8439 Test Vectors";

        public static IEnumerable<object[]> GetRfc8439TestCases()
        {
            foreach (AEADTest test in s_rfc8439TestVectors)
            {
                yield return new object[] { test };
            }
        }

        // CaseId is unique per test case
        private static readonly AEADTest[] s_rfc8439TestVectors = new AEADTest[]
        {
            new AEADTest
            {
                Source = Rfc8439TestVectors,
                CaseId = 1, // RFC 8439, Sec. 2.8.2
                Key = "808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f".HexToByteArray(),
                Nonce = "070000004041424344454647".HexToByteArray(),
                Plaintext = (
                    "4c616469657320616e642047656e746c" +
                    "656d656e206f662074686520636c6173" +
                    "73206f66202739393a20496620492063" +
                    "6f756c64206f6666657220796f75206f" +
                    "6e6c79206f6e652074697020666f7220" +
                    "746865206675747572652c2073756e73" +
                    "637265656e20776f756c642062652069" +
                    "742e").HexToByteArray(),
                AssociatedData = "50515253c0c1c2c3c4c5c6c7".HexToByteArray(),
                Ciphertext = (
                    "d31a8d34648e60db7b86afbc53ef7ec2" +
                    "a4aded51296e08fea9e2b5a736ee62d6" +
                    "3dbea45e8ca9671282fafb69da92728b" +
                    "1a71de0a9e060b2905d6a5b67ecd3b36" +
                    "92ddbd7f2d778b8c9803aee328091b58" +
                    "fab324e4fad675945585808b4831d7bc" +
                    "3ff4def08e4b7a9de576d26586cec64b" +
                    "6116").HexToByteArray(),
                Tag = "1ae10b594f09e26a7e902ecbd0600691".HexToByteArray()
            },
            new AEADTest
            {
                Source = Rfc8439TestVectors,
                CaseId = 2, // RFC 8439, Appendix A.5
                Key = "1c9240a5eb55d38af333888604f6b5f0473917c1402b80099dca5cbc207075c0".HexToByteArray(),
                Nonce = "000000000102030405060708".HexToByteArray(),
                Plaintext = (
                    "496e7465726e65742d44726166747320" +
                    "61726520647261667420646f63756d65" +
                    "6e74732076616c696420666f72206120" +
                    "6d6178696d756d206f6620736978206d" +
                    "6f6e74687320616e64206d6179206265" +
                    "20757064617465642c207265706c6163" +
                    "65642c206f72206f62736f6c65746564" +
                    "206279206f7468657220646f63756d65" +
                    "6e747320617420616e792074696d652e" +
                    "20497420697320696e617070726f7072" +
                    "6961746520746f2075736520496e7465" +
                    "726e65742d4472616674732061732072" +
                    "65666572656e6365206d617465726961" +
                    "6c206f7220746f206369746520746865" +
                    "6d206f74686572207468616e20617320" +
                    "2fe2809c776f726b20696e2070726f67" +
                    "726573732e2fe2809d").HexToByteArray(),
                AssociatedData = "f33388860000000000004e91".HexToByteArray(),
                Ciphertext = (
                    "64a0861575861af460f062c79be643bd" +
                    "5e805cfd345cf389f108670ac76c8cb2" +
                    "4c6cfc18755d43eea09ee94e382d26b0" +
                    "bdb7b73c321b0100d4f03b7f355894cf" +
                    "332f830e710b97ce98c8a84abd0b9481" +
                    "14ad176e008d33bd60f982b1ff37c855" +
                    "9797a06ef4f0ef61c186324e2b350638" +
                    "3606907b6a7c02b0f9f6157b53c867e4" +
                    "b9166c767b804d46a59b5216cde7a4e9" +
                    "9040c5a40433225ee282a1b0a06c523e" +
                    "af4534d7f83fa1155b0047718cbc546a" +
                    "0d072b04b3564eea1b422273f548271a" +
                    "0bb2316053fa76991955ebd63159434e" +
                    "cebb4e466dae5a1073a6727627097a10" +
                    "49e617d91d361094fa68f0ff77987130" +
                    "305beaba2eda04df997b714d6c6f2c29" +
                    "a6ad5cb4022b02709b").HexToByteArray(),
                Tag = "eead9d67890cbb22392336fea1851f38".HexToByteArray()
            }
        };
    }

    public class ChaCha20Poly1305IsSupportedTests
    {
        public static bool RuntimeSaysIsNotSupported => !ChaCha20Poly1305.IsSupported;

        [ConditionalFact(nameof(RuntimeSaysIsNotSupported))]
        public static void CtorThrowsPNSEIfNotSupported()
        {
            byte[] key = RandomNumberGenerator.GetBytes(256 / 8);

            Assert.Throws<PlatformNotSupportedException>(() => new ChaCha20Poly1305(key));
            Assert.Throws<PlatformNotSupportedException>(() => new ChaCha20Poly1305(key.AsSpan()));
        }

        [Fact]
        public static void CheckIsSupported()
        {
            bool expectedIsSupported = false; // assume not supported unless environment advertises support

            if (PlatformDetection.IsWindows)
            {
                // Runtime uses a hardcoded OS version to determine support.
                // The test queries the OS directly to ensure our version check is correct.
                expectedIsSupported = CngUtility.IsAlgorithmSupported("CHACHA20_POLY1305");
            }
            else if (PlatformDetection.IsAndroid)
            {
                // Android with API Level 28 is the minimum API Level support for ChaChaPoly1305.
                expectedIsSupported = OperatingSystem.IsAndroidVersionAtLeast(28);
            }
            else if (PlatformDetection.OpenSslPresentOnSystem &&
                (PlatformDetection.IsOSX || PlatformDetection.IsOpenSslSupported))
            {
                const int OpenSslChaChaMinimumVersion = 0x1_01_00_00_F; //major_minor_fix_patch_status
                expectedIsSupported = SafeEvpPKeyHandle.OpenSslVersion >= OpenSslChaChaMinimumVersion;
            }

            Assert.Equal(expectedIsSupported, ChaCha20Poly1305.IsSupported);
        }
    }
}
