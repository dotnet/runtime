// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using Microsoft.DotNet.XUnitExtensions;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public abstract class SymmetricOneShotBase
    {
        protected abstract byte[] Key { get; }
        protected abstract byte[] IV { get; }
        protected abstract SymmetricAlgorithm CreateAlgorithm();

        protected void OneShotRoundtripTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                int paddingSizeBytes = mode == CipherMode.CFB ? feedbackSize / 8 : alg.BlockSize / 8;
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
                    CipherMode.CFB => alg.EncryptCfb(plaintext, IV, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };
                byte[] decrypted = mode switch
                {
                    CipherMode.ECB => alg.DecryptEcb(encrypted, padding),
                    CipherMode.CBC => alg.DecryptCbc(encrypted, IV, padding),
                    CipherMode.CFB => alg.DecryptCfb(encrypted, IV, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };

                AssertPlaintexts(plaintext, decrypted, padding);
                AssertCiphertexts(encrypted, ciphertext, padding, paddingSizeBytes);

                decrypted = mode switch
                {
                    CipherMode.ECB => alg.DecryptEcb(ciphertext, padding),
                    CipherMode.CBC => alg.DecryptCbc(ciphertext, IV, padding),
                    CipherMode.CFB => alg.DecryptCfb(ciphertext, IV, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };
                encrypted = mode switch
                {
                    CipherMode.ECB => alg.EncryptEcb(decrypted, padding),
                    CipherMode.CBC => alg.EncryptCbc(decrypted, IV, padding),
                    CipherMode.CFB => alg.EncryptCfb(decrypted, IV, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };

                AssertPlaintexts(plaintext, decrypted, padding);
                AssertCiphertexts(ciphertext, encrypted, padding, paddingSizeBytes);
            }
        }

        protected void TryDecryptOneShot_DestinationTooSmallTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
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
                    CipherMode.CFB => alg.TryDecryptCfb(ciphertext, IV, destinationBuffer, out bytesWritten, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };

                Assert.False(result, "TryDecrypt");
                Assert.Equal(0, bytesWritten);
            }
        }

        protected void TryEncryptOneShot_DestinationTooSmallTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
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
                    CipherMode.CFB => alg.TryEncryptCfb(plaintext, IV, destinationBuffer, out bytesWritten, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };
                Assert.False(result, "TryEncrypt");
                Assert.Equal(0, bytesWritten);
            }
        }

        protected void TryDecryptOneShot_DestinationJustRightTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
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
                    CipherMode.CFB => alg.TryDecryptCfb(ciphertext, IV, destinationBuffer, out bytesWritten, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };
                Assert.True(result, "TryDecrypt");
                Assert.Equal(destinationBuffer.Length, bytesWritten);

                AssertPlaintexts(plaintext, destinationBuffer, padding);
            }
        }

        protected void TryEncryptOneShot_DestinationJustRightTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                int paddingSizeBytes = mode == CipherMode.CFB ? feedbackSize / 8 : alg.BlockSize / 8;
                alg.Key = Key;

                int expectedCiphertextSize = mode switch
                {
                    CipherMode.ECB => alg.GetCiphertextLengthEcb(plaintext.Length, padding),
                    CipherMode.CBC => alg.GetCiphertextLengthCbc(plaintext.Length, padding),
                    CipherMode.CFB => alg.GetCiphertextLengthCfb(plaintext.Length, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };
                Span<byte> destinationBuffer = new byte[expectedCiphertextSize];

                int bytesWritten;
                bool result = mode switch
                {
                    CipherMode.ECB => alg.TryEncryptEcb(plaintext, destinationBuffer, padding, out bytesWritten),
                    CipherMode.CBC => alg.TryEncryptCbc(plaintext, IV, destinationBuffer, out bytesWritten, padding),
                    CipherMode.CFB => alg.TryEncryptCfb(plaintext, IV, destinationBuffer, out bytesWritten, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };
                Assert.True(result, "TryEncrypt");
                Assert.Equal(expectedCiphertextSize, bytesWritten);

                AssertCiphertexts(ciphertext, destinationBuffer, padding, paddingSizeBytes);
            }
        }

        protected void TryDecryptOneShot_DestinationLargerTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
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
                    CipherMode.CFB => alg.TryDecryptCfb(ciphertext, IV, destinationBuffer, out bytesWritten, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };

                Assert.True(result, "TryDecrypt");
                Assert.Equal(destinationBuffer.Length, bytesWritten);

                AssertPlaintexts(plaintext, destinationBuffer, padding);

                Span<byte> excess = largeBuffer.Slice(destinationBuffer.Length);
                AssertExtensions.FilledWith<byte>(0xCC, excess);
            }
        }

        protected void TryEncryptOneShot_DestinationLargerTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                int paddingSizeBytes = mode == CipherMode.CFB ? feedbackSize / 8 : alg.BlockSize / 8;
                alg.Key = Key;

                Span<byte> largeBuffer = new byte[ciphertext.Length + 10];
                Span<byte> destinationBuffer = largeBuffer.Slice(0, ciphertext.Length);
                largeBuffer.Fill(0xCC);

                int bytesWritten;
                bool result = mode switch
                {
                    CipherMode.ECB => alg.TryEncryptEcb(plaintext, destinationBuffer, padding, out bytesWritten),
                    CipherMode.CBC => alg.TryEncryptCbc(plaintext, IV, destinationBuffer, out bytesWritten, padding),
                    CipherMode.CFB => alg.TryEncryptCfb(plaintext, IV, destinationBuffer, out bytesWritten, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };

                Assert.True(result, "TryEncrypt");
                Assert.Equal(destinationBuffer.Length, bytesWritten);

                AssertCiphertexts(ciphertext, destinationBuffer, padding, paddingSizeBytes);
                AssertExtensions.FilledWith<byte>(0xCC, largeBuffer.Slice(ciphertext.Length));
            }
        }

        protected void TryDecryptOneShot_OverlapsTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
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
                        CipherMode.CFB => alg.TryDecryptCfb(ciphertextBuffer, IV, destinationBuffer, out bytesWritten, padding, feedbackSize),
                        _ => throw new NotImplementedException(),
                    };
                    Assert.True(result, "TryDecrypt");
                    Assert.Equal(destinationBuffer.Length, bytesWritten);

                    AssertPlaintexts(plaintext, destinationBuffer, padding);
                    Assert.True(destinationBuffer.Overlaps(ciphertextBuffer) || plaintext.Length == 0 || ciphertext.Length == 0);
                }
            }
        }

        protected void TryEncryptOneShot_OverlapsTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
        {
            (int plaintextOffset, int ciphertextOffset)[] offsets =
            {
                (0, 0), (8, 0), (0, 8), (8, 8),
            };

            foreach ((int plaintextOffset, int ciphertextOffset) in offsets)
            {
                using (SymmetricAlgorithm alg = CreateAlgorithm())
                {
                    int paddingSizeBytes = mode == CipherMode.CFB ? feedbackSize / 8 : alg.BlockSize / 8;
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
                        CipherMode.CFB => alg.TryEncryptCfb(plaintextBuffer, IV, destinationBuffer, out bytesWritten, padding, feedbackSize),
                        _ => throw new NotImplementedException(),
                    };
                    Assert.True(result, "TryEncrypt");
                    Assert.Equal(destinationBuffer.Length, bytesWritten);

                    AssertCiphertexts(ciphertext, destinationBuffer, padding, paddingSizeBytes);
                    Assert.True(destinationBuffer.Overlaps(plaintextBuffer) || plaintext.Length == 0 || ciphertext.Length == 0);
                }
            }
        }

        protected void DecryptOneShot_SpanTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;
                byte[] decrypted = mode switch
                {
                    CipherMode.ECB => alg.DecryptEcb(ciphertext.AsSpan(), padding),
                    CipherMode.CBC => alg.DecryptCbc(ciphertext.AsSpan(), IV.AsSpan(), padding),
                    CipherMode.CFB => alg.DecryptCfb(ciphertext.AsSpan(), IV.AsSpan(), padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };

                AssertPlaintexts(plaintext, decrypted, padding);
            }
        }

        protected void EncryptOneShot_SpanTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;
                int paddingSizeBytes = mode == CipherMode.CFB ? feedbackSize / 8 : alg.BlockSize / 8;
                byte[] encrypted = mode switch
                {
                    CipherMode.ECB => alg.EncryptEcb(plaintext.AsSpan(), padding),
                    CipherMode.CBC => alg.EncryptCbc(plaintext.AsSpan(), IV.AsSpan(), padding),
                    CipherMode.CFB => alg.EncryptCfb(plaintext.AsSpan(), IV.AsSpan(), padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };

                AssertCiphertexts(ciphertext, encrypted, padding, paddingSizeBytes);
            }
        }

        protected void DecryptOneShot_ArrayTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;
                byte[] decrypted = mode switch
                {
                    CipherMode.ECB => alg.DecryptEcb(ciphertext, padding),
                    CipherMode.CBC => alg.DecryptCbc(ciphertext, IV, padding),
                    CipherMode.CFB => alg.DecryptCfb(ciphertext, IV, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };

                AssertPlaintexts(plaintext, decrypted, padding);
            }
        }

        protected void EncryptOneShot_ArrayTest(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;
                int paddingSizeBytes = mode == CipherMode.CFB ? feedbackSize / 8 : alg.BlockSize / 8;
                byte[] encrypted = mode switch
                {
                    CipherMode.ECB => alg.EncryptEcb(plaintext, padding),
                    CipherMode.CBC => alg.EncryptCbc(plaintext, IV, padding),
                    CipherMode.CFB => alg.EncryptCfb(plaintext, IV, padding, feedbackSize),
                    _ => throw new NotImplementedException(),
                };

                AssertCiphertexts(ciphertext, encrypted, padding, paddingSizeBytes);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public void DecryptOneShot_Cfb8_ToleratesExtraPadding()
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                if (alg is RC2)
                {
                    throw new SkipTestException("RC2 does not support CFB.");
                }

                alg.Key = Key;

                // This is a plaintext that is manually padded. It contains one byte of
                // data, padded up to the block size, in bytes, of the algorithm.
                Span<byte> excessPadding = new byte[alg.BlockSize / 8];
                excessPadding.Fill((byte)(alg.BlockSize / 8 - 1));
                excessPadding[0] = 0x42;

                // Use paddingMode: None since we manually padded our data
                byte[] ciphertext = alg.EncryptCfb(excessPadding, IV, paddingMode: PaddingMode.None, feedbackSizeInBits: 8);

                // Now decrypt it with PKCS7 padding mode. It should remove all but the first byte since it is not padding.
                byte[] decrypted = alg.DecryptCfb(ciphertext, IV, paddingMode: PaddingMode.PKCS7, feedbackSizeInBits: 8);
                Assert.Equal(new byte[] { 0x42 }, decrypted);
            }
        }

        [Theory]
        [InlineData(PaddingMode.PKCS7, 0)]
        [InlineData(PaddingMode.ANSIX923, 0)]
        [InlineData(PaddingMode.ISO10126, 0)]
        [InlineData(PaddingMode.PKCS7, 2048)]
        [InlineData(PaddingMode.ANSIX923, 2048)]
        [InlineData(PaddingMode.ISO10126, 2048)]
        public void DecryptOneShot_Cbc_InvalidPadding_DoesNotContainPlaintext(PaddingMode paddingMode, int ciphertextSize)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;
                int size = ciphertextSize > 0 ? ciphertextSize : alg.BlockSize / 8;

                byte plaintextByte = 0xFE;
                byte[] invalidPadding = new byte[size];
                invalidPadding.AsSpan().Fill(plaintextByte);
                byte[] destination = new byte[size];

                // Use paddingMode: None since we manually padded our data
                byte[] ciphertext = alg.EncryptCbc(invalidPadding, IV, paddingMode: PaddingMode.None);
                Assert.Throws<CryptographicException>(() => alg.DecryptCbc(ciphertext, IV, destination, paddingMode: paddingMode));
                Assert.True(destination.AsSpan().IndexOf(plaintextByte) < 0, "does not contain plaintext data");
            }
        }

        [Theory]
        [InlineData(PaddingMode.PKCS7, 0)]
        [InlineData(PaddingMode.ANSIX923, 0)]
        [InlineData(PaddingMode.ISO10126, 0)]
        [InlineData(PaddingMode.PKCS7, 2048)]
        [InlineData(PaddingMode.ANSIX923, 2048)]
        [InlineData(PaddingMode.ISO10126, 2048)]
        public void DecryptOneShot_Ecb_InvalidPadding_DoesNotContainPlaintext(PaddingMode paddingMode, int ciphertextSize)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;
                int size = ciphertextSize > 0 ? ciphertextSize : alg.BlockSize / 8;

                byte plaintextByte = 0xFE;
                byte[] invalidPadding = new byte[size];
                invalidPadding.AsSpan().Fill(plaintextByte);
                byte[] destination = new byte[size];

                // Use paddingMode: None since we manually padded our data
                byte[] ciphertext = alg.EncryptEcb(invalidPadding, paddingMode: PaddingMode.None);
                Assert.Throws<CryptographicException>(() => alg.DecryptEcb(ciphertext, destination, paddingMode: paddingMode));
                Assert.True(destination.AsSpan().IndexOf(plaintextByte) < 0, "does not contain plaintext data");
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData(PaddingMode.PKCS7, 0)]
        [InlineData(PaddingMode.ANSIX923, 0)]
        [InlineData(PaddingMode.ISO10126, 0)]
        [InlineData(PaddingMode.PKCS7, 2048)]
        [InlineData(PaddingMode.ANSIX923, 2048)]
        [InlineData(PaddingMode.ISO10126, 2048)]
        public void DecryptOneShot_Cfb_InvalidPadding_DoesNotContainPlaintext(PaddingMode paddingMode, int ciphertextSize)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                if (alg is RC2)
                {
                    throw new SkipTestException("RC2 does not support CFB.");
                }

                alg.Key = Key;
                int size = ciphertextSize > 0 ? ciphertextSize : alg.BlockSize / 8;

                byte plaintextByte = 0xFE;
                byte[] invalidPadding = new byte[size];
                invalidPadding.AsSpan().Fill(plaintextByte);
                byte[] destination = new byte[size];

                // Use paddingMode: None since we manually padded our data
                byte[] ciphertext = alg.EncryptCfb(invalidPadding, IV, paddingMode: PaddingMode.None);
                Assert.Throws<CryptographicException>(() => alg.DecryptCfb(ciphertext, IV, destination, paddingMode: paddingMode));
                Assert.True(destination.AsSpan().IndexOf(plaintextByte) < 0, "does not contain plaintext data");
            }
        }

        [Theory]
        [InlineData(PaddingMode.PKCS7)]
        [InlineData(PaddingMode.ANSIX923)]
        [InlineData(PaddingMode.ISO10126)]
        public void DecryptOneShot_Cbc_TooShortDoesNotContainPlaintext(PaddingMode paddingMode)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                alg.Key = Key;
                int size = 2048;

                byte plaintextByte = 0xFE;
                Span<byte> plaintext = new byte[size + 1];
                plaintext.Fill(plaintextByte);
                Span<byte> destination = new byte[size];
                byte[] ciphertext = alg.EncryptCbc(plaintext, IV, paddingMode: paddingMode);

                bool success = alg.TryDecryptCbc(ciphertext, IV, destination, out int bytesWritten, paddingMode: paddingMode);
                Assert.False(success, nameof(alg.TryDecryptCbc));
                Assert.True(destination.IndexOf(plaintextByte) < 0, "does not contain plaintext data");
                Assert.Equal(0, bytesWritten);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData(PaddingMode.PKCS7)]
        [InlineData(PaddingMode.ANSIX923)]
        [InlineData(PaddingMode.ISO10126)]
        public void DecryptOneShot_Cfb8_TooShortDoesNotContainPlaintext(PaddingMode paddingMode)
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                if (alg is RC2)
                {
                    throw new SkipTestException("RC2 does not support CFB.");
                }

                alg.Key = Key;
                int size = 2048;

                byte plaintextByte = 0xFE;
                Span<byte> plaintext = new byte[size + 1];
                plaintext.Fill(0xFE);
                Span<byte> destination = new byte[size];
                byte[] ciphertext = alg.EncryptCfb(plaintext, IV, paddingMode: paddingMode);

                bool success = alg.TryDecryptCfb(ciphertext, IV, destination, out int bytesWritten, paddingMode: paddingMode);
                Assert.False(success, nameof(alg.TryDecryptCfb));
                Assert.True(destination.IndexOf(plaintextByte) < 0, "does not contain plaintext data");
                Assert.Equal(0, bytesWritten);
            }
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

        private static void AssertCiphertexts(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual, PaddingMode padding, int paddingSizeBytes)
        {
            if (padding == PaddingMode.ISO10126)
            {
                // The padding is random, so we can't check the exact ciphertext.
                AssertExtensions.SequenceEqual(expected[..^paddingSizeBytes], actual[..^paddingSizeBytes]);
            }
            else
            {
                AssertExtensions.SequenceEqual(expected, actual);
            }
        }

        protected static byte[] ByteArrayFilledWith(byte contents, int length)
        {
            byte[] ret = new byte[length];
            ret.AsSpan().Fill(contents);
            return ret;
        }
    }
}
