// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Algorithms.Tests
{
    public class AesCcmTests : AesAEADTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(0, 30)]
        [InlineData(1, 1)]
        [InlineData(1, 100)]
        [InlineData(7, 12)]
        [InlineData(16, 16)]
        [InlineData(17, 29)]
        [InlineData(32, 7)]
        [InlineData(41, 25)]
        [InlineData(48, 22)]
        [InlineData(50, 5)]
        public static void EncryptTamperAADDecrypt(int dataLength, int additionalDataLength)
        {
            byte[] additionalData = new byte[additionalDataLength];
            RandomNumberGenerator.Fill(additionalData);

            byte[] plaintext = Enumerable.Range(1, dataLength).Select((x) => (byte)x).ToArray();
            byte[] ciphertext = new byte[dataLength];
            byte[] key = new byte[16];
            byte[] nonce = new byte[AesCcm.NonceByteSizes.MinSize];
            byte[] tag = new byte[AesCcm.TagByteSizes.MinSize];
            RandomNumberGenerator.Fill(key);
            RandomNumberGenerator.Fill(nonce);

            using (var aesCcm = new AesCcm(key))
            {
                aesCcm.Encrypt(nonce, plaintext, ciphertext, tag, additionalData);

                additionalData[0] ^= 1;

                byte[] decrypted = new byte[dataLength];
                Assert.Throws<CryptographicException>(
                    () => aesCcm.Decrypt(nonce, ciphertext, tag, decrypted, additionalData));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(17)]
        [InlineData(29)]
        [InlineData(33)]
        public static void InvalidKeyLength(int keyLength)
        {
            byte[] key = new byte[keyLength];
            Assert.Throws<CryptographicException>(() => new AesCcm(key));
        }

        [Theory]
        [MemberData(nameof(GetInvalidNonceSizes))]
        public static void InvalidNonceSize(int nonceSize)
        {
            int dataLength = 30;
            byte[] plaintext = Enumerable.Range(1, dataLength).Select((x) => (byte)x).ToArray();
            byte[] ciphertext = new byte[dataLength];
            byte[] key = new byte[16];
            byte[] nonce = new byte[nonceSize];
            byte[] tag = new byte[AesCcm.TagByteSizes.MinSize];
            RandomNumberGenerator.Fill(key);
            RandomNumberGenerator.Fill(nonce);

            using (var aesCcm = new AesCcm(key))
            {
                Assert.Throws<ArgumentException>("nonce", () => aesCcm.Encrypt(nonce, plaintext, ciphertext, tag));
            }
        }

        [Theory]
        [MemberData(nameof(GetValidNonceSizes))]
        public static void ValidNonceSize(int nonceSize)
        {
            const int dataLength = 35;
            byte[] plaintext = Enumerable.Range(1, dataLength).Select((x) => (byte)x).ToArray();
            byte[] ciphertext = new byte[dataLength];
            byte[] key = new byte[16];
            byte[] nonce = new byte[nonceSize];
            byte[] tag = new byte[AesCcm.TagByteSizes.MinSize];
            RandomNumberGenerator.Fill(key);
            RandomNumberGenerator.Fill(nonce);

            using (var aesCcm = new AesCcm(key))
            {
                aesCcm.Encrypt(nonce, plaintext, ciphertext, tag);

                byte[] decrypted = new byte[dataLength];
                aesCcm.Decrypt(nonce, ciphertext, tag, decrypted);
                Assert.Equal(plaintext, decrypted);
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidTagSizes))]
        public static void InvalidTagSize(int tagSize)
        {
            int dataLength = 30;
            byte[] plaintext = Enumerable.Range(1, dataLength).Select((x) => (byte)x).ToArray();
            byte[] ciphertext = new byte[dataLength];
            byte[] key = new byte[16];
            byte[] nonce = new byte[12];
            byte[] tag = new byte[tagSize];
            RandomNumberGenerator.Fill(key);
            RandomNumberGenerator.Fill(nonce);

            using (var aesCcm = new AesCcm(key))
            {
                Assert.Throws<ArgumentException>("tag", () => aesCcm.Encrypt(nonce, plaintext, ciphertext, tag));
            }
        }

        [Theory]
        [MemberData(nameof(GetValidTagSizes))]
        public static void ValidTagSize(int tagSize)
        {
            const int dataLength = 35;
            byte[] plaintext = Enumerable.Range(1, dataLength).Select((x) => (byte)x).ToArray();
            byte[] ciphertext = new byte[dataLength];
            byte[] key = new byte[16];
            byte[] nonce = new byte[12];
            byte[] tag = new byte[tagSize];
            RandomNumberGenerator.Fill(key);
            RandomNumberGenerator.Fill(nonce);

            using (var aesCcm = new AesCcm(key))
            {
                aesCcm.Encrypt(nonce, plaintext, ciphertext, tag);

                byte[] decrypted = new byte[dataLength];
                aesCcm.Decrypt(nonce, ciphertext, tag, decrypted);
                Assert.Equal(plaintext, decrypted);
            }
        }

        [Fact]
        public static void TwoEncryptionsAndDecryptionsUsingOneInstance()
        {
            byte[] key = "d5a194ed90cfe08abecd4691997ceb2c".HexToByteArray();
            byte[] originalData1 = Enumerable.Range(1, 15).Select((x) => (byte)x).ToArray();
            byte[] originalData2 = Enumerable.Range(14, 97).Select((x) => (byte)x).ToArray();
            byte[] associatedData2 = Enumerable.Range(100, 109).Select((x) => (byte)x).ToArray();
            byte[] nonce1 = "b41329dd64af2c3036661b46".HexToByteArray();
            byte[] nonce2 = "8ba10892e8b87d031196".HexToByteArray();

            byte[] expectedCiphertext1 = "58457f5939e029b99637c9aef266aa".HexToByteArray();
            byte[] expectedTag1 = "ac5ccedc".HexToByteArray();

            byte[] expectedCiphertext2 = (
                "be4a5174fc244002d8614652d75cad9b464d86709cd9e8c58061add9a7546a1d" +
                "8165b375011bd4d8e188d4d2782ae890aa7ddf335c9759267f813148903c47d1" +
                "de0278c772dc2295bef9bba3bbfde319edbb54b71288c1fd1ddefb4a9b12534d" +
                "15").HexToByteArray();
            byte[] expectedTag2 = "f564b8439dc79ddf7aa1a497bc1f780e".HexToByteArray();

            using (var aesCcm = new AesCcm(key))
            {
                byte[] ciphertext1 = new byte[originalData1.Length];
                byte[] tag1 = new byte[expectedTag1.Length];
                aesCcm.Encrypt(nonce1, originalData1, ciphertext1, tag1);
                Assert.Equal(expectedCiphertext1, ciphertext1);
                Assert.Equal(expectedTag1, tag1);

                byte[] ciphertext2 = new byte[originalData2.Length];
                byte[] tag2 = new byte[expectedTag2.Length];
                aesCcm.Encrypt(nonce2, originalData2, ciphertext2, tag2, associatedData2);
                Assert.Equal(expectedCiphertext2, ciphertext2);
                Assert.Equal(expectedTag2, tag2);

                byte[] plaintext1 = new byte[originalData1.Length];
                aesCcm.Decrypt(nonce1, ciphertext1, tag1, plaintext1);
                Assert.Equal(originalData1, plaintext1);

                byte[] plaintext2 = new byte[originalData2.Length];
                aesCcm.Decrypt(nonce2, ciphertext2, tag2, plaintext2, associatedData2);
                Assert.Equal(originalData2, plaintext2);
            }
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(3, 4)]
        [InlineData(4, 3)]
        [InlineData(20, 120)]
        [InlineData(120, 20)]
        public static void PlaintextAndCiphertextSizeDiffer(int ptLen, int ctLen)
        {
            byte[] key = new byte[16];
            byte[] nonce = new byte[12];
            byte[] plaintext = new byte[ptLen];
            byte[] ciphertext = new byte[ctLen];
            byte[] tag = new byte[16];

            using (var aesCcm = new AesCcm(key))
            {
                Assert.Throws<ArgumentException>(() => aesCcm.Encrypt(nonce, plaintext, ciphertext, tag));
                Assert.Throws<ArgumentException>(() => aesCcm.Decrypt(nonce, ciphertext, tag, plaintext));
            }
        }

        [Fact]
        public static void NullKey()
        {
            Assert.Throws<ArgumentNullException>("key", () => new AesCcm((byte[])null));
        }

        [Fact]
        public static void EncryptDecryptNullNonce()
        {
            byte[] key = "d5a194ed90cfe08abecd4691997ceb2c".HexToByteArray();
            byte[] plaintext = new byte[0];
            byte[] ciphertext = new byte[0];
            byte[] tag = new byte[16];

            using (var aesCcm = new AesCcm(key))
            {
                Assert.Throws<ArgumentNullException>("nonce", () => aesCcm.Encrypt((byte[])null, plaintext, ciphertext, tag));
                Assert.Throws<ArgumentNullException>("nonce", () => aesCcm.Decrypt((byte[])null, ciphertext, tag, plaintext));
            }
        }

        [Fact]
        public static void EncryptDecryptNullPlaintext()
        {
            byte[] key = "d5a194ed90cfe08abecd4691997ceb2c".HexToByteArray();
            byte[] nonce = new byte[12];
            byte[] ciphertext = new byte[0];
            byte[] tag = new byte[16];

            using (var aesCcm = new AesCcm(key))
            {
                Assert.Throws<ArgumentNullException>("plaintext", () => aesCcm.Encrypt(nonce, (byte[])null, ciphertext, tag));
                Assert.Throws<ArgumentNullException>("plaintext", () => aesCcm.Decrypt(nonce, ciphertext, tag, (byte[])null));
            }
        }

        [Fact]
        public static void EncryptDecryptNullCiphertext()
        {
            byte[] key = "d5a194ed90cfe08abecd4691997ceb2c".HexToByteArray();
            byte[] nonce = new byte[12];
            byte[] plaintext = new byte[0];
            byte[] tag = new byte[16];

            using (var aesCcm = new AesCcm(key))
            {
                Assert.Throws<ArgumentNullException>("ciphertext", () => aesCcm.Encrypt(nonce, plaintext, (byte[])null, tag));
                Assert.Throws<ArgumentNullException>("ciphertext", () => aesCcm.Decrypt(nonce, (byte[])null, tag, plaintext));
            }
        }

        [Fact]
        public static void EncryptDecryptNullTag()
        {
            byte[] key = "d5a194ed90cfe08abecd4691997ceb2c".HexToByteArray();
            byte[] nonce = new byte[12];
            byte[] plaintext = new byte[0];
            byte[] ciphertext = new byte[0];

            using (var aesCcm = new AesCcm(key))
            {
                Assert.Throws<ArgumentNullException>("tag", () => aesCcm.Encrypt(nonce, plaintext, ciphertext, (byte[])null));
                Assert.Throws<ArgumentNullException>("tag", () => aesCcm.Decrypt(nonce, ciphertext, (byte[])null, plaintext));
            }
        }

        [Fact]
        public static void InplaceEncryptDecrypt()
        {
            byte[] key = "d5a194ed90cfe08abecd4691997ceb2c".HexToByteArray();
            byte[] nonce = new byte[12];
            byte[] originalPlaintext = new byte[] { 1, 2, 8, 12, 16, 99, 0 };
            byte[] data = (byte[])originalPlaintext.Clone();
            byte[] tag = new byte[16];
            RandomNumberGenerator.Fill(nonce);

            using (var aesCcm = new AesCcm(key))
            {
                aesCcm.Encrypt(nonce, data, data, tag);
                Assert.NotEqual(originalPlaintext, data);

                aesCcm.Decrypt(nonce, data, tag, data);
                Assert.Equal(originalPlaintext, data);
            }
        }

        [ActiveIssue(32710, TestPlatforms.AnyUnix)] 
        [Fact]
        public static void InplaceEncryptTamperTagDecrypt()
        {
            byte[] key = "d5a194ed90cfe08abecd4691997ceb2c".HexToByteArray();
            byte[] nonce = new byte[12];
            byte[] originalPlaintext = new byte[] { 1, 2, 8, 12, 16, 99, 0 };
            byte[] data = (byte[])originalPlaintext.Clone();
            byte[] tag = new byte[16];
            RandomNumberGenerator.Fill(nonce);

            using (var aesCcm = new AesCcm(key))
            {
                aesCcm.Encrypt(nonce, data, data, tag);
                Assert.NotEqual(originalPlaintext, data);

                tag[0] ^= 1;

                Assert.Throws<CryptographicException>(
                    () => aesCcm.Decrypt(nonce, data, tag, data));
                Assert.Equal(new byte[data.Length], data);
            }
        }

        [Theory]
        [MemberData(nameof(GetNistCcmTestCases))]
        public static void AesCcmNistTests(AEADTest testCase)
        {
            using (var aesCcm = new AesCcm(testCase.Key))
            {
                byte[] ciphertext = new byte[testCase.Plaintext.Length];
                byte[] tag = new byte[testCase.Tag.Length];
                aesCcm.Encrypt(testCase.Nonce, testCase.Plaintext, ciphertext, tag, testCase.AssociatedData);
                Assert.Equal(testCase.Ciphertext, ciphertext);
                Assert.Equal(testCase.Tag, tag);

                byte[] plaintext = new byte[testCase.Plaintext.Length];
                aesCcm.Decrypt(testCase.Nonce, ciphertext, tag, plaintext, testCase.AssociatedData);
                Assert.Equal(testCase.Plaintext, plaintext);
            }
        }

        [ActiveIssue(32710, TestPlatforms.AnyUnix)] 
        [Theory]
        [MemberData(nameof(GetNistCcmTestCases))]
        public static void AesCcmNistTestsTamperTag(AEADTest testCase)
        {
            using (var aesCcm = new AesCcm(testCase.Key))
            {
                byte[] ciphertext = new byte[testCase.Plaintext.Length];
                byte[] tag = new byte[testCase.Tag.Length];
                aesCcm.Encrypt(testCase.Nonce, testCase.Plaintext, ciphertext, tag, testCase.AssociatedData);
                Assert.Equal(testCase.Ciphertext, ciphertext);
                Assert.Equal(testCase.Tag, tag);

                tag[0] ^= 1;

                byte[] plaintext = new byte[testCase.Plaintext.Length];
                RandomNumberGenerator.Fill(plaintext);
                Assert.Throws<CryptographicException>(
                    () => aesCcm.Decrypt(testCase.Nonce, ciphertext, tag, plaintext, testCase.AssociatedData));
                Assert.Equal(new byte[plaintext.Length], plaintext);
            }
        }

        [ActiveIssue(32710, TestPlatforms.AnyUnix)] 
        [Theory]
        [MemberData(nameof(GetNistCcmTestCasesWithNonEmptyPT))]
        public static void AesCcmNistTestsTamperCiphertext(AEADTest testCase)
        {
            using (var aesCcm = new AesCcm(testCase.Key))
            {
                byte[] ciphertext = new byte[testCase.Plaintext.Length];
                byte[] tag = new byte[testCase.Tag.Length];
                aesCcm.Encrypt(testCase.Nonce, testCase.Plaintext, ciphertext, tag, testCase.AssociatedData);
                Assert.Equal(testCase.Ciphertext, ciphertext);
                Assert.Equal(testCase.Tag, tag);

                ciphertext[0] ^= 1;

                byte[] plaintext = new byte[testCase.Plaintext.Length];
                RandomNumberGenerator.Fill(plaintext);
                Assert.Throws<CryptographicException>(
                    () => aesCcm.Decrypt(testCase.Nonce, ciphertext, tag, plaintext, testCase.AssociatedData));
                Assert.Equal(new byte[plaintext.Length], plaintext);
            }
        }

        public static IEnumerable<object[]> GetValidNonceSizes()
        {
            return GetValidSizes(AesCcm.NonceByteSizes);
        }

        public static IEnumerable<object[]> GetInvalidNonceSizes()
        {
            return GetInvalidSizes(AesCcm.NonceByteSizes);
        }

        public static IEnumerable<object[]> GetValidTagSizes()
        {
            return GetValidSizes(AesCcm.TagByteSizes);
        }

        public static IEnumerable<object[]> GetInvalidTagSizes()
        {
            return GetInvalidSizes(AesCcm.TagByteSizes);
        }

        public static IEnumerable<object[]> GetNistCcmTestCases()
        {
            foreach (AEADTest test in s_nistCcmTestVectorsSelectedCases)
            {
                yield return new object[] { test };
            }
        }

        public static IEnumerable<object[]> GetNistCcmTestCasesWithNonEmptyPT()
        {
            foreach (AEADTest test in s_nistCcmTestVectorsSelectedCases)
            {
                if (test.Plaintext.Length > 0)
                    yield return new object[] { test };
            }
        }

        // https://csrc.nist.gov/CSRC/media/Projects/Cryptographic-Algorithm-Validation-Program/documents/mac/ccmtestvectors.zip
        private const string NistCcmTestVectors = "NIST CCM Test Vectors";

        private static readonly AEADTest[] s_nistCcmTestVectorsSelectedCases = new AEADTest[]
        {
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - DVPT128.rsp",
                CaseId = 0,
                Key = "4ae701103c63deca5b5a3939d7d05992".HexToByteArray(),
                Nonce = "5a8aa485c316e9".HexToByteArray(),
                Plaintext = Array.Empty<byte>(),
                AssociatedData = null,
                Ciphertext = Array.Empty<byte>(),
                Tag = "02209f55".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - DVPT128.rsp",
                CaseId = 60,
                Key = "19ebfde2d5468ba0a3031bde629b11fd".HexToByteArray(),
                Nonce = "5a8aa485c316e9".HexToByteArray(),
                Plaintext = "3796cf51b8726652a4204733b8fbb047cf00fb91a9837e22".HexToByteArray(),
                AssociatedData = Array.Empty<byte>(),
                Ciphertext = "a90e8ea44085ced791b2fdb7fd44b5cf0bd7d27718029bb7".HexToByteArray(),
                Tag = "03e1fa6b".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - DVPT128.rsp",
                CaseId = 120,
                Key = "90929a4b0ac65b350ad1591611fe4829".HexToByteArray(),
                Nonce = "5a8aa485c316e9".HexToByteArray(),
                Plaintext = Array.Empty<byte>(),
                AssociatedData = "3796cf51b8726652a4204733b8fbb047cf00fb91a9837e22ec22b1a268f88e2c".HexToByteArray(),
                Ciphertext = Array.Empty<byte>(),
                Tag = "782e4318".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - DVPT192.rsp",
                CaseId = 0,
                Key = "c98ad7f38b2c7e970c9b965ec87a08208384718f78206c6c".HexToByteArray(),
                Nonce = "5a8aa485c316e9".HexToByteArray(),
                Plaintext = Array.Empty<byte>(),
                AssociatedData = null,
                Ciphertext = Array.Empty<byte>(),
                Tag = "9d4b7f3b".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - DVPT192.rsp",
                CaseId = 60,
                Key = "19ebfde2d5468ba0a3031bde629b11fd4094afcb205393fa".HexToByteArray(),
                Nonce = "5a8aa485c316e9".HexToByteArray(),
                Plaintext = "3796cf51b8726652a4204733b8fbb047cf00fb91a9837e22".HexToByteArray(),
                AssociatedData = null,
                Ciphertext = "411986d04d6463100bff03f7d0bde7ea2c3488784378138c".HexToByteArray(),
                Tag = "ddc93a54".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - DVPT192.rsp",
                CaseId = 120,
                Key = "90929a4b0ac65b350ad1591611fe48297e03956f6083e451".HexToByteArray(),
                Nonce = "5a8aa485c316e9".HexToByteArray(),
                Plaintext = Array.Empty<byte>(),
                AssociatedData = "3796cf51b8726652a4204733b8fbb047cf00fb91a9837e22ec22b1a268f88e2c".HexToByteArray(),
                Ciphertext = Array.Empty<byte>(),
                Tag = "1d089a5f".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - DVPT256.rsp",
                CaseId = 0,
                Key = "eda32f751456e33195f1f499cf2dc7c97ea127b6d488f211ccc5126fbb24afa6".HexToByteArray(),
                Nonce = "a544218dadd3c1".HexToByteArray(),
                Plaintext = Array.Empty<byte>(),
                AssociatedData = null,
                Ciphertext = Array.Empty<byte>(),
                Tag = "469c90bb".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - DVPT256.rsp",
                CaseId = 60,
                Key = "af063639e66c284083c5cf72b70d8bc277f5978e80d9322d99f2fdc718cda569".HexToByteArray(),
                Nonce = "a544218dadd3c1".HexToByteArray(),
                Plaintext = "d3d5424e20fbec43ae495353ed830271515ab104f8860c98".HexToByteArray(),
                AssociatedData = null,
                Ciphertext = "64a1341679972dc5869fcf69b19d5c5ea50aa0b5e985f5b7".HexToByteArray(),
                Tag = "22aa8d59".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - DVPT256.rsp",
                CaseId = 120,
                Key = "1b0e8df63c57f05d9ac457575ea764524b8610ae5164e6215f426f5a7ae6ede4".HexToByteArray(),
                Nonce = "a544218dadd3c1".HexToByteArray(),
                Plaintext = Array.Empty<byte>(),
                AssociatedData = "d3d5424e20fbec43ae495353ed830271515ab104f8860c988d15b6d36c038eab".HexToByteArray(),
                Ciphertext = Array.Empty<byte>(),
                Tag = "92d00fbe".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT128.rsp",
                CaseId = 0,
                Key = "c0425ed20cd28fda67a2bcc0ab342a49".HexToByteArray(),
                Nonce = "37667f334dce90".HexToByteArray(),
                Plaintext = "4f065a23eeca6b18d118e1de4d7e5ca1a7c0e556d786d407".HexToByteArray(),
                AssociatedData = "0b3e8d9785c74c8f41ea257d4d87495ffbbb335542b12e0d62bb177ec7a164d9".HexToByteArray(),
                Ciphertext = "768fccdf4898bca099e33c3d40565497dec22dd6e33dcf43".HexToByteArray(),
                Tag = "84d71be8565c21a455db45816da8158c".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT128.rsp",
                CaseId = 50,
                Key = "005e8f4d8e0cbf4e1ceeb5d87a275848".HexToByteArray(),
                Nonce = "0ec3ac452b547b9062aac8fa".HexToByteArray(),
                Plaintext = "b6f345204526439daf84998f380dcfb4b4167c959c04ff65".HexToByteArray(),
                AssociatedData = "2f1821aa57e5278ffd33c17d46615b77363149dbc98470413f6543a6b749f2ca".HexToByteArray(),
                Ciphertext = "9575e16f35da3c88a19c26a7b762044f4d7bbbafeff05d75".HexToByteArray(),
                Tag = "4829e2a7752fa3a14890972884b511d8".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT128.rsp",
                CaseId = 51,
                Key = "005e8f4d8e0cbf4e1ceeb5d87a275848".HexToByteArray(),
                Nonce = "472711261a9262bef077c0b7".HexToByteArray(),
                Plaintext = "9d63df773b3799e361c5328d44bbb12f4154747ecf7cc667".HexToByteArray(),
                AssociatedData = "17c87889a2652636bcf712d111c86b9d68d64d18d531928030a5ec97c59931a4".HexToByteArray(),
                Ciphertext = "53323b82d7a754d82cebf0d4bc930ef06d11e162c5c027c4".HexToByteArray(),
                Tag = "715a641834bbb75bb6572ca5a45c3183".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT128.rsp",
                CaseId = 52,
                Key = "005e8f4d8e0cbf4e1ceeb5d87a275848".HexToByteArray(),
                Nonce = "6a7b80b6738ff0a23ad58fb2".HexToByteArray(),
                Plaintext = "ba1978d58492c7f827cafef87d00f1a137f3f05a2dedb14d".HexToByteArray(),
                AssociatedData = "26c12e5cdfe225a5be56d7a8aaf9fd4eb327d2f29c2ebc7396022f884f33ce54".HexToByteArray(),
                Ciphertext = "aa1d9eacabdcdd0f54681653ac44042a3dd47e338d15604e".HexToByteArray(),
                Tag = "86a0e926daf21d17b359253d0d5d5d00".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT128.rsp",
                CaseId = 60,
                Key = "ac87fef3b76e725d66d905625a387e82".HexToByteArray(),
                Nonce = "61bf06b9fa5a450d094f3ddcb5".HexToByteArray(),
                Plaintext = "959403e0771c21a416bd03f3898390e90d0a0899f69f9552".HexToByteArray(),
                AssociatedData = "0245484bcd987787fe97fda6c8ffb6e7058d7b8f7064f27514afaac4048767fd".HexToByteArray(),
                Ciphertext = "cabf8aa613d5357aa3e70173d43f1f202b628a61d18e8b57".HexToByteArray(),
                Tag = "2eb66bb8213a515aa61e5f0945cd57f4".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT192.rsp",
                CaseId = 0,
                Key = "ceb009aea4454451feadf0e6b36f45555dd04723baa448e8".HexToByteArray(),
                Nonce = "764043c49460b7".HexToByteArray(),
                Plaintext = "c8d275f919e17d7fe69c2a1f58939dfe4d403791b5df1310".HexToByteArray(),
                AssociatedData = "6e80dd7f1badf3a1c9ab25c75f10bde78c23fa0eb8f9aaa53adefbf4cbf78fe4".HexToByteArray(),
                Ciphertext = "8a0f3d8229e48e7487fd95a28ad392c80b3681d4fbc7bbfd".HexToByteArray(),
                Tag = "2dd6ef1c45d4ccb723dc074414db506d".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT192.rsp",
                CaseId = 50,
                Key = "d49b255aed8be1c02eb6d8ae2bac6dcd7901f1f61df3bbf5".HexToByteArray(),
                Nonce = "1af29e721c98e81fb6286370".HexToByteArray(),
                Plaintext = "062eafb0cd09d26e65108c0f56fcc7a305f31c34e0f3a24c".HexToByteArray(),
                AssociatedData = "64f8a0eee5487a4958a489ed35f1327e2096542c1bdb2134fb942ca91804c274".HexToByteArray(),
                Ciphertext = "721344e2fd05d2ee50713531052d75e4071103ab0436f65f".HexToByteArray(),
                Tag = "0af2a663da51bac626c9f4128ba5ec0b".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT192.rsp",
                CaseId = 51,
                Key = "d49b255aed8be1c02eb6d8ae2bac6dcd7901f1f61df3bbf5".HexToByteArray(),
                Nonce = "ca650ed993c4010c1b0bd1f2".HexToByteArray(),
                Plaintext = "fc375d984fa13af4a5a7516f3434365cd9473cd316e8964c".HexToByteArray(),
                AssociatedData = "4efbd225553b541c3f53cabe8a1ac03845b0e846c8616b3ea2cc7d50d344340c".HexToByteArray(),
                Ciphertext = "5b300c718d5a64f537f6cbb4d212d0f903b547ab4b21af56".HexToByteArray(),
                Tag = "ef7662525021c5777c2d74ea239a4c44".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT192.rsp",
                CaseId = 52,
                Key = "d49b255aed8be1c02eb6d8ae2bac6dcd7901f1f61df3bbf5".HexToByteArray(),
                Nonce = "318adeb8d8df47878ca59117".HexToByteArray(),
                Plaintext = "610a52216f47a544ec562117e0741e5f8b2e02bc9bc9122e".HexToByteArray(),
                AssociatedData = "feccf08d8c3a9be9a2c0f93f888e486b0076e2e9e2fd068c04b2db735cbeb23a".HexToByteArray(),
                Ciphertext = "83f14f6ba09a6e6b50f0d94d7d79376561f891f9a6162d0f".HexToByteArray(),
                Tag = "8925c37cc35c1c8530b0be4817814a8e".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT192.rsp",
                CaseId = 60,
                Key = "36ad1e3fb630d1b1fbccfd685f44edd8984427b78deae7a9".HexToByteArray(),
                Nonce = "3af625df8be9d7685a842f260e".HexToByteArray(),
                Plaintext = "8b9db1c8f9b4892a5654c85467bcffa2e15e28392c938952".HexToByteArray(),
                AssociatedData = "308443033ecd4a814475672b814b7c6d813d0ec2a0caeecbcaba18a2840cdb6c".HexToByteArray(),
                Ciphertext = "6bc6890fee299c712fb8d9df9c141f24ee1572b8f15112c2".HexToByteArray(),
                Tag = "f8c99ccf2d82788cf613a61d60dae458".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT256.rsp",
                CaseId = 0,
                Key = "553521a765ab0c3fd203654e9916330e189bdf951feee9b44b10da208fee7acf".HexToByteArray(),
                Nonce = "aaa23f101647d8".HexToByteArray(),
                Plaintext = "644eb34b9a126e437b5e015eea141ca1a88020f2d5d6cc2c".HexToByteArray(),
                AssociatedData = "a355d4c611812e5f9258d7188b3df8851477094ffc2af2cf0c8670db903fbbe0".HexToByteArray(),
                Ciphertext = "27ed90668174ebf8241a3c74b35e1246b6617e4123578f15".HexToByteArray(),
                Tag = "3bdb67062a13ef4e986f5bb3d0bb4307".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT256.rsp",
                CaseId = 50,
                Key = "d6ff67379a2ead2ca87aa4f29536258f9fb9fc2e91b0ed18e7b9f5df332dd1dc".HexToByteArray(),
                Nonce = "2f1d0717a822e20c7cd28f0a".HexToByteArray(),
                Plaintext = "98626ffc6c44f13c964e7fcb7d16e988990d6d063d012d33".HexToByteArray(),
                AssociatedData = "d50741d34c8564d92f396b97be782923ff3c855ea9757bde419f632c83997630".HexToByteArray(),
                Ciphertext = "50e22db70ac2bab6d6af7059c90d00fbf0fb52eee5eb650e".HexToByteArray(),
                Tag = "08aca7dec636170f481dcb9fefb85c05".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT256.rsp",
                CaseId = 51,
                Key = "d6ff67379a2ead2ca87aa4f29536258f9fb9fc2e91b0ed18e7b9f5df332dd1dc".HexToByteArray(),
                Nonce = "819ecbe71f851743871163cc".HexToByteArray(),
                Plaintext = "8d164f598ea141082b1069776fccd87baf6a2563cbdbc9d1".HexToByteArray(),
                AssociatedData = "48e06c3b2940819e58eb24122a2988c997697347a6e34c21267d76049febdcf8".HexToByteArray(),
                Ciphertext = "70fd9d3c7d9e8af610edb3d329f371cf3052d820e79775a9".HexToByteArray(),
                Tag = "32d42f9954f9d35d989a09e4292949fc".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT256.rsp",
                CaseId = 52,
                Key = "d6ff67379a2ead2ca87aa4f29536258f9fb9fc2e91b0ed18e7b9f5df332dd1dc".HexToByteArray(),
                Nonce = "22168c66967d545823ea0b7a".HexToByteArray(),
                Plaintext = "b28a5bc814e7f71ae94586b58281ff05a71191c92e45db74".HexToByteArray(),
                AssociatedData = "7f596bc7a815d103ed9f6dc428b60e72aeadcb9382ccde4ac9f3b61e7e8047fd".HexToByteArray(),
                Ciphertext = "30254fe7c249c0125c56c90bad3983c7f852df91fa4e828b".HexToByteArray(),
                Tag = "7522efcd96cd4de4cf41e9b67c708f9f".HexToByteArray(),
            },
            new AEADTest
            {
                Source = $"{NistCcmTestVectors} - VNT256.rsp",
                CaseId = 60,
                Key = "4a75ff2f66dae2935403cce27e829ad8be98185c73f8bc61d3ce950a83007e11".HexToByteArray(),
                Nonce = "46eb390b175e75da6193d7edb6".HexToByteArray(),
                Plaintext = "205f2a664a8512e18321a91c13ec13b9e6b633228c57cc1e".HexToByteArray(),
                AssociatedData = "282f05f734f249c0535ee396282218b7c4913c39b59ad2a03ffaf5b0e9b0f780".HexToByteArray(),
                Ciphertext = "58f1584f761983bef4d0060746b5d5ee610ecfda31101a7f".HexToByteArray(),
                Tag = "5460e9b7856d60a5ad9803c0762f8176".HexToByteArray(),
            },
        };
    }
}
