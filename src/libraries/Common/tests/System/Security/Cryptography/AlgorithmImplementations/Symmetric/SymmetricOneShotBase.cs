// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public abstract class SymmetricOneShotBase
    {
        protected abstract byte[] Key { get; }
        protected abstract byte[] IV { get; }
        protected abstract SymmetricAlgorithm CreateAlgorithm();

        protected void OneShotRoundtripTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;

                // Set the instance to use a different mode and padding than what will be used
                // in the one-shots to test that the one shot "wins".
                alg.FeedbackSize = 8;
                alg.Mode = mode == CipherMode.ECB ? CipherMode.CBC : CipherMode.ECB;
                alg.Padding = padding == PaddingMode.None ? PaddingMode.PKCS7 : PaddingMode.None;

                byte[] encrypted = mode switch
                {
                    CipherMode.ECB => alg.EncryptEcb(plaintext, padding),
                    CipherMode.CBC => alg.EncryptCbc(plaintext, IV, padding),
                    _ => throw new NotImplementedException(),
                };
                byte[] decrypted = mode switch
                {
                    CipherMode.ECB => alg.DecryptEcb(encrypted, padding),
                    CipherMode.CBC => alg.DecryptCbc(encrypted, IV, padding),
                    _ => throw new NotImplementedException(),
                };

                AssertPlaintexts(plaintext, decrypted, padding);
                AssertCiphertexts(encrypted, ciphertext, padding, alg.BlockSize / 8);

                decrypted = mode switch
                {
                    CipherMode.ECB => alg.DecryptEcb(ciphertext, padding),
                    CipherMode.CBC => alg.DecryptCbc(ciphertext, IV, padding),
                    _ => throw new NotImplementedException(),
                };
                encrypted = mode switch
                {
                    CipherMode.ECB => alg.EncryptEcb(decrypted, padding),
                    CipherMode.CBC => alg.EncryptCbc(decrypted, IV, padding),
                    _ => throw new NotImplementedException(),
                };

                AssertPlaintexts(plaintext, decrypted, padding);
                AssertCiphertexts(ciphertext, encrypted, padding, alg.BlockSize / 8);
            }
        }

        protected void TryDecryptOneShot_DestinationTooSmallTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            if (plaintext.Length == 0)
            {
                // Can't have a ciphertext length shorter than zero.
                return;
            }

            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;

                Span<byte> destinationBuffer = new byte[plaintext.Length - 1];

                int bytesWritten;
                bool result = mode switch
                {
                    CipherMode.ECB => alg.TryDecryptEcb(ciphertext, destinationBuffer, padding, out bytesWritten),
                    CipherMode.CBC => alg.TryDecryptCbc(ciphertext, IV, destinationBuffer, out bytesWritten, padding),
                    _ => throw new NotImplementedException(),
                };

                Assert.False(result, "TryDecrypt");
                Assert.Equal(0, bytesWritten);
            }
        }

        protected void TryEncryptOneShot_DestinationTooSmallTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            if (ciphertext.Length == 0)
            {
                // Can't have a too small buffer for zero.
                return;
            }

            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;

                Span<byte> destinationBuffer = new byte[ciphertext.Length - 1];

                int bytesWritten;
                bool result = mode switch
                {
                    CipherMode.ECB => alg.TryEncryptEcb(plaintext, destinationBuffer, padding, out bytesWritten),
                    CipherMode.CBC => alg.TryEncryptCbc(plaintext, IV, destinationBuffer, out bytesWritten, padding),
                    _ => throw new NotImplementedException(),
                };
                Assert.False(result, "TryEncrypt");
                Assert.Equal(0, bytesWritten);
            }
        }

        protected void TryDecryptOneShot_DestinationJustRightTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;

                int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;
                Span<byte> destinationBuffer = new byte[expectedPlaintextSize];

                int bytesWritten;
                bool result = mode switch
                    {
                        CipherMode.ECB => alg.TryDecryptEcb(ciphertext, destinationBuffer, padding, out bytesWritten),
                        CipherMode.CBC => alg.TryDecryptCbc(ciphertext, IV, destinationBuffer, out bytesWritten, padding),
                        _ => throw new NotImplementedException(),
                    };
                Assert.True(result, "TryDecrypt");
                Assert.Equal(destinationBuffer.Length, bytesWritten);

                AssertPlaintexts(plaintext, destinationBuffer, padding);
            }
        }

        protected void TryEncryptOneShot_DestinationJustRightTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;

                int expectedCiphertextSize = mode switch
                {
                    CipherMode.ECB => alg.GetCiphertextLengthEcb(plaintext.Length, padding),
                    CipherMode.CBC => alg.GetCiphertextLengthCbc(plaintext.Length, padding),
                    _ => throw new NotImplementedException(),
                };
                Span<byte> destinationBuffer = new byte[expectedCiphertextSize];

                int bytesWritten;
                bool result = mode switch
                {
                    CipherMode.ECB => alg.TryEncryptEcb(plaintext, destinationBuffer, padding, out bytesWritten),
                    CipherMode.CBC => alg.TryEncryptCbc(plaintext, IV, destinationBuffer, out bytesWritten, padding),
                    _ => throw new NotImplementedException(),
                };
                Assert.True(result, "TryEncrypt");
                Assert.Equal(expectedCiphertextSize, bytesWritten);

                AssertCiphertexts(ciphertext, destinationBuffer, padding, alg.BlockSize / 8);
            }
        }

        protected void TryDecryptOneShot_DestinationLargerTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;
                int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;

                Span<byte> largeBuffer = new byte[expectedPlaintextSize + 10];
                Span<byte> destinationBuffer = largeBuffer.Slice(0, expectedPlaintextSize);
                largeBuffer.Fill(0xCC);

                int bytesWritten;
                bool result = mode switch
                {
                    CipherMode.ECB => alg.TryDecryptEcb(ciphertext, destinationBuffer, padding, out bytesWritten),
                    CipherMode.CBC => alg.TryDecryptCbc(ciphertext, IV, destinationBuffer, out bytesWritten, padding),
                    _ => throw new NotImplementedException(),
                };

                Assert.True(result, "TryDecrypt");
                Assert.Equal(destinationBuffer.Length, bytesWritten);

                AssertPlaintexts(plaintext, destinationBuffer, padding);

                Span<byte> excess = largeBuffer.Slice(destinationBuffer.Length);
                AssertExtensions.FilledWith<byte>(0xCC, excess);
            }
        }

        protected void TryEncryptOneShot_DestinationLargerTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;

                Span<byte> largeBuffer = new byte[ciphertext.Length + 10];
                Span<byte> destinationBuffer = largeBuffer.Slice(0, ciphertext.Length);
                largeBuffer.Fill(0xCC);

                int bytesWritten;
                bool result = mode switch
                {
                    CipherMode.ECB => alg.TryEncryptEcb(plaintext, destinationBuffer, padding, out bytesWritten),
                    CipherMode.CBC => alg.TryEncryptCbc(plaintext, IV, destinationBuffer, out bytesWritten, padding),
                    _ => throw new NotImplementedException(),
                };

                Assert.True(result, "TryEncrypt");
                Assert.Equal(destinationBuffer.Length, bytesWritten);

                AssertCiphertexts(ciphertext, destinationBuffer, padding, alg.BlockSize / 8);
                AssertExtensions.FilledWith<byte>(0xCC, largeBuffer.Slice(ciphertext.Length));
            }
        }

        protected void TryDecryptOneShot_OverlapsTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            (int plaintextOffset, int ciphertextOffset)[] offsets =
            {
                (0, 0), (8, 0), (0, 8), (8, 8),
            };

            foreach ((int plaintextOffset, int ciphertextOffset) in offsets)
            {
                using (SymmetricAlgorithm alg = CreateAlgorithm())
                {
                    alg.Key = Key;

                    int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;
                    int destinationSize = Math.Max(expectedPlaintextSize, ciphertext.Length) + Math.Max(plaintextOffset, ciphertextOffset);
                    Span<byte> buffer = new byte[destinationSize];
                    Span<byte> destinationBuffer = buffer.Slice(plaintextOffset, expectedPlaintextSize);
                    Span<byte> ciphertextBuffer = buffer.Slice(ciphertextOffset, ciphertext.Length);
                    ciphertext.AsSpan().CopyTo(ciphertextBuffer);

                    int bytesWritten;
                    bool result = mode switch
                    {
                        CipherMode.ECB => alg.TryDecryptEcb(ciphertextBuffer, destinationBuffer, padding, out bytesWritten),
                        CipherMode.CBC => alg.TryDecryptCbc(ciphertextBuffer, IV, destinationBuffer, out bytesWritten, padding),
                        _ => throw new NotImplementedException(),
                    };
                    Assert.True(result, "TryDecrypt");
                    Assert.Equal(destinationBuffer.Length, bytesWritten);

                    AssertPlaintexts(plaintext, destinationBuffer, padding);
                    Assert.True(destinationBuffer.Overlaps(ciphertextBuffer) || plaintext.Length == 0 || ciphertext.Length == 0);
                }
            }
        }

        protected void TryEncryptOneShot_OverlapsTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            (int plaintextOffset, int ciphertextOffset)[] offsets =
            {
                (0, 0), (8, 0), (0, 8), (8, 8),
            };

            foreach ((int plaintextOffset, int ciphertextOffset) in offsets)
            {
                using (SymmetricAlgorithm alg = CreateAlgorithm())
                {
                    alg.Key = Key;

                    int destinationSize = ciphertext.Length + Math.Max(plaintextOffset, ciphertextOffset);
                    Span<byte> buffer = new byte[destinationSize];
                    Span<byte> destinationBuffer = buffer.Slice(ciphertextOffset, ciphertext.Length);
                    Span<byte> plaintextBuffer = buffer.Slice(plaintextOffset, plaintext.Length);
                    plaintext.AsSpan().CopyTo(plaintextBuffer);

                    int bytesWritten;
                    bool result = mode switch
                    {
                        CipherMode.ECB => alg.TryEncryptEcb(plaintextBuffer, destinationBuffer, padding, out bytesWritten),
                        CipherMode.CBC => alg.TryEncryptCbc(plaintextBuffer, IV, destinationBuffer, out bytesWritten, padding),
                        _ => throw new NotImplementedException(),
                    };
                    Assert.True(result, "TryEncrypt");
                    Assert.Equal(destinationBuffer.Length, bytesWritten);

                    AssertCiphertexts(ciphertext, destinationBuffer, padding, alg.BlockSize / 8);
                    Assert.True(destinationBuffer.Overlaps(plaintextBuffer) || plaintext.Length == 0 || ciphertext.Length == 0);
                }
            }
        }

        protected void DecryptOneShot_SpanTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;
                byte[] decrypted = mode switch
                {
                    CipherMode.ECB => alg.DecryptEcb(ciphertext.AsSpan(), padding),
                    CipherMode.CBC => alg.DecryptCbc(ciphertext.AsSpan(), IV.AsSpan(), padding),
                    _ => throw new NotImplementedException(),
                };

                AssertPlaintexts(plaintext, decrypted, padding);
            }
        }

        protected void EncryptOneShot_SpanTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;
                byte[] encrypted = mode switch
                {
                    CipherMode.ECB => alg.EncryptEcb(plaintext.AsSpan(), padding),
                    CipherMode.CBC => alg.EncryptCbc(plaintext.AsSpan(), IV.AsSpan(), padding),
                    _ => throw new NotImplementedException(),
                };

                AssertCiphertexts(ciphertext, encrypted, padding, alg.BlockSize / 8);
            }
        }

        protected void DecryptOneShot_ArrayTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;
                byte[] decrypted = mode switch
                {
                    CipherMode.ECB => alg.DecryptEcb(ciphertext, padding),
                    CipherMode.CBC => alg.DecryptCbc(ciphertext, IV, padding),
                    _ => throw new NotImplementedException(),
                };

                AssertPlaintexts(plaintext, decrypted, padding);
            }
        }

        protected void EncryptOneShot_ArrayTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;
                byte[] encrypted = mode switch
                {
                    CipherMode.ECB => alg.EncryptEcb(plaintext, padding),
                    CipherMode.CBC => alg.EncryptCbc(plaintext, IV, padding),
                    _ => throw new NotImplementedException(),
                };

                AssertCiphertexts(ciphertext, encrypted, padding, alg.BlockSize / 8);
            }
        }

        [Fact]
        public void DerivedTypesDefineTest()
        {
            const string TestSuffix = "Test";
            Type implType = GetType();
            Type defType = typeof(SymmetricOneShotBase);
            List<string> missingMethods = new List<string>();

            foreach (MethodInfo info in defType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (info.IsFamily && info.Name.EndsWith(TestSuffix, StringComparison.Ordinal))
                {
                    string targetMethodName = info.Name[..^TestSuffix.Length];

                    MethodInfo info2 = implType.GetMethod(
                        targetMethodName,
                        BindingFlags.Instance | BindingFlags.Public);

                    if (info2 is null)
                    {
                        missingMethods.Add(targetMethodName);
                    }
                }
            }

            Assert.Empty(missingMethods);
        }

        private static void AssertPlaintexts(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual, PaddingMode padding)
        {
            if (padding == PaddingMode.Zeros)
            {
                AssertExtensions.SequenceEqual(expected, actual.Slice(0, expected.Length));
                AssertExtensions.FilledWith<byte>(0, actual.Slice(actual.Length));
            }
            else
            {
                AssertExtensions.SequenceEqual(expected, actual);
            }
        }

        private static void AssertCiphertexts(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual, PaddingMode padding, int blockSizeBytes)
        {
            if (padding == PaddingMode.ISO10126)
            {
                // The padding is random, so we can't check the exact ciphertext.
                AssertExtensions.SequenceEqual(expected[..^blockSizeBytes], actual[..^blockSizeBytes]);
            }
            else
            {
                AssertExtensions.SequenceEqual(expected, actual);
            }
        }
    }
}
