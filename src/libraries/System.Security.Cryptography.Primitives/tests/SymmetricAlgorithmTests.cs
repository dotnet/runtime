// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Primitives.Tests
{
    public static class SymmetricAlgorithmTests
    {
        [Theory]
        [MemberData(nameof(CiphertextLengthTheories))]
        public static void GetCiphertextLengthBlock_ValidInputs(
            PaddingMode mode,
            int plaintextSize,
            int expectedCiphertextSize,
            int alignmentSizeInBits)
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm { BlockSize = alignmentSizeInBits };
            int ciphertextSizeCbc = alg.GetCiphertextLengthCbc(plaintextSize, mode);
            int ciphertextSizeEcb = alg.GetCiphertextLengthEcb(plaintextSize, mode);
            Assert.Equal(expectedCiphertextSize, ciphertextSizeCbc);
            Assert.Equal(expectedCiphertextSize, ciphertextSizeEcb);
        }

        [Theory]
        [MemberData(nameof(CiphertextLengthTheories))]
        public static void GetCiphertextLengthCfb_ValidInputs(
            PaddingMode mode,
            int plaintextSize,
            int expectedCiphertextSize,
            int alignmentSizeInBits)
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm();
            int ciphertextSizeCfb = alg.GetCiphertextLengthCfb(plaintextSize, mode, alignmentSizeInBits);
            Assert.Equal(expectedCiphertextSize, ciphertextSizeCfb);
        }

        [Theory]
        [MemberData(nameof(AllPaddingModes))]
        public static void GetCiphertextLength_ThrowsForNegativeInput(PaddingMode mode)
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm { BlockSize = 128 };
            AssertExtensions.Throws<ArgumentOutOfRangeException>("plaintextLength", () => alg.GetCiphertextLengthCbc(-1, mode));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("plaintextLength", () => alg.GetCiphertextLengthEcb(-1, mode));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("plaintextLength", () => alg.GetCiphertextLengthCfb(-1, mode));
        }

        [Theory]
        [InlineData(PaddingMode.ANSIX923)]
        [InlineData(PaddingMode.ISO10126)]
        [InlineData(PaddingMode.PKCS7)]
        [InlineData(PaddingMode.Zeros)]
        public static void GetCiphertextLengthBlock_ThrowsForOverflow(PaddingMode mode)
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm { BlockSize = 128 };
            AssertExtensions.Throws<ArgumentOutOfRangeException>("plaintextLength", () => alg.GetCiphertextLengthCbc(0x7FFFFFF1, mode));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("plaintextLength", () => alg.GetCiphertextLengthEcb(0x7FFFFFF1, mode));
        }

        [Theory]
        [InlineData(PaddingMode.ANSIX923)]
        [InlineData(PaddingMode.ISO10126)]
        [InlineData(PaddingMode.PKCS7)]
        [InlineData(PaddingMode.Zeros)]
        public static void GetCiphertextLengthCfb_ThrowsForOverflow(PaddingMode mode)
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("plaintextLength", () =>
                alg.GetCiphertextLengthCfb(0x7FFFFFFF, mode, feedbackSizeInBits: 128));
        }

        [Theory]
        [MemberData(nameof(AllPaddingModes))]
        public static void GetCiphertextLengthBlock_ThrowsForNonByteBlockSize(PaddingMode mode)
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm { BlockSize = 5 };
            Assert.Throws<InvalidOperationException>(() => alg.GetCiphertextLengthCbc(16, mode));
            Assert.Throws<InvalidOperationException>(() => alg.GetCiphertextLengthEcb(16, mode));
        }

        [Theory]
        [MemberData(nameof(AllPaddingModes))]
        public static void GetCiphertextLengthCfb_ThrowsForNonByteFeedbackSize(PaddingMode mode)
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm();
            AssertExtensions.Throws<ArgumentException>("feedbackSizeInBits", () =>
                alg.GetCiphertextLengthCfb(16, mode, 7));
        }

        [Theory]
        [MemberData(nameof(AllPaddingModes))]
        public static void GetCiphertextLengthBlock_ThrowsForZeroBlockSize(PaddingMode mode)
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm { BlockSize = 0 };
            Assert.Throws<InvalidOperationException>(() => alg.GetCiphertextLengthCbc(16, mode));
            Assert.Throws<InvalidOperationException>(() => alg.GetCiphertextLengthEcb(16, mode));
        }

        [Theory]
        [MemberData(nameof(AllPaddingModes))]
        public static void GetCiphertextLengthCfb_ThrowsForZeroFeedbackSize(PaddingMode mode)
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("feedbackSizeInBits", () =>
                alg.GetCiphertextLengthCfb(16, mode, 0));
        }

        [Fact]
        public static void GetCiphertextLength_ThrowsForInvalidPaddingMode()
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm { BlockSize = 128 };
            PaddingMode mode = (PaddingMode)(-1);
            Assert.Throws<ArgumentOutOfRangeException>("paddingMode", () => alg.GetCiphertextLengthCbc(16, mode));
            Assert.Throws<ArgumentOutOfRangeException>("paddingMode", () => alg.GetCiphertextLengthEcb(16, mode));
            Assert.Throws<ArgumentOutOfRangeException>("paddingMode", () => alg.GetCiphertextLengthCfb(16, mode));
        }

        [Fact]
        public static void GetCiphertextLengthBlock_NoPaddingAndPlaintextSizeNotBlockAligned()
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm { BlockSize = 128 };
            Assert.Throws<ArgumentException>("plaintextLength", () => alg.GetCiphertextLengthCbc(17, PaddingMode.None));
            Assert.Throws<ArgumentException>("plaintextLength", () => alg.GetCiphertextLengthEcb(17, PaddingMode.None));
        }

        [Fact]
        public static void GetCiphertextLengthCfb_NoPaddingAndPlaintextSizeNotFeedbackAligned()
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm();
            Assert.Throws<ArgumentException>("plaintextLength", () =>
                alg.GetCiphertextLengthCfb(17, PaddingMode.None, feedbackSizeInBits: 128));
        }

        [Fact]
        public static void EncryptEcb_NotSupportedInDerived()
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm { BlockSize = 128 };

            Assert.Throws<NotSupportedException>(() =>
                alg.EncryptEcb(Array.Empty<byte>(), PaddingMode.None));
        }

        [Fact]
        public static void DecryptEcb_NotSupportedInDerived()
        {
            AnySizeAlgorithm alg = new AnySizeAlgorithm { BlockSize = 128 };

            Assert.Throws<NotSupportedException>(() =>
                alg.DecryptEcb(Array.Empty<byte>(), PaddingMode.None));
        }

        [Fact]
        public static void EncryptEcb_EncryptProducesIncorrectlyPaddedValue()
        {
            static bool EncryptImpl(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
            {
                bytesWritten = destination.Length + 1;
                return true;
            }

            EcbSymmetricAlgorithm alg = new EcbSymmetricAlgorithm
            {
                BlockSize = 128,
                TryEncryptEcbCoreImpl = EncryptImpl,
            };

            Assert.Throws<CryptographicException>(() =>
                alg.EncryptEcb(Array.Empty<byte>(), PaddingMode.None));
        }

        [Fact]
        public static void DecryptEcb_DecryptBytesWrittenLies()
        {
            static bool DecryptImpl(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
            {
                bytesWritten = destination.Length + 1;
                return true;
            }

            EcbSymmetricAlgorithm alg = new EcbSymmetricAlgorithm
            {
                BlockSize = 128,
                TryDecryptEcbCoreImpl = DecryptImpl,
            };

            Assert.Throws<CryptographicException>(() =>
                alg.DecryptEcb(new byte[128 / 8], PaddingMode.None));
        }

        [Fact]
        public static void EncryptEcb_EncryptCoreFails()
        {
            static bool EncryptImpl(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            EcbSymmetricAlgorithm alg = new EcbSymmetricAlgorithm
            {
                BlockSize = 128,
                TryEncryptEcbCoreImpl = EncryptImpl,
            };

            Assert.Throws<CryptographicException>(() =>
                alg.EncryptEcb(Array.Empty<byte>(), PaddingMode.None));
        }

        [Fact]
        public static void EncryptEcb_EncryptCoreOverflowWritten()
        {
            static bool EncryptImpl(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
            {
                bytesWritten = -1;
                return true;
            }

            EcbSymmetricAlgorithm alg = new EcbSymmetricAlgorithm
            {
                BlockSize = 128,
                TryEncryptEcbCoreImpl = EncryptImpl,
            };

            Assert.Throws<CryptographicException>(() =>
                alg.EncryptEcb(Array.Empty<byte>(), PaddingMode.None));
        }

        [Fact]
        public static void DecryptEcb_DecryptCoreFails()
        {
            static bool DecryptImpl(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            EcbSymmetricAlgorithm alg = new EcbSymmetricAlgorithm
            {
                BlockSize = 128,
                TryDecryptEcbCoreImpl = DecryptImpl,
            };

            Assert.Throws<CryptographicException>(() =>
                alg.DecryptEcb(Array.Empty<byte>(), PaddingMode.None));
        }

        [Fact]
        public static void DecryptEcb_DecryptCoreOverflowWritten()
        {
            static bool DecryptImpl(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
            {
                bytesWritten = -1;
                return true;
            }

            EcbSymmetricAlgorithm alg = new EcbSymmetricAlgorithm
            {
                BlockSize = 128,
                TryDecryptEcbCoreImpl = DecryptImpl,
            };

            Assert.Throws<CryptographicException>(() =>
                alg.DecryptEcb(Array.Empty<byte>(), PaddingMode.None));
        }

        public static IEnumerable<object[]> CiphertextLengthTheories
        {
            get
            {
                // new object[] { PaddingMode mode, int plaintextSize, int expectedCiphertextSize, int alignmentSizeInBits }

                PaddingMode[] fullPaddings = new[] {
                    PaddingMode.ANSIX923,
                    PaddingMode.ISO10126,
                    PaddingMode.PKCS7,
                };

                foreach (PaddingMode mode in fullPaddings)
                {
                    // 128-bit aligned value
                    yield return new object[] { mode, 0, 16, 128 };
                    yield return new object[] { mode, 15, 16, 128 };
                    yield return new object[] { mode, 16, 32, 128 };
                    yield return new object[] { mode, 17, 32, 128 };
                    yield return new object[] { mode, 1023, 1024, 128 };
                    yield return new object[] { mode, 0x7FFFFFEF, 0x7FFFFFF0, 128 };

                    // 64-bit aligned value
                    yield return new object[] { mode, 0, 8, 64 };
                    yield return new object[] { mode, 15, 16, 64 };
                    yield return new object[] { mode, 16, 24, 64 };
                    yield return new object[] { mode, 17, 24, 64 };
                    yield return new object[] { mode, 1023, 1024, 64 };
                    yield return new object[] { mode, 0x7FFFFFF7, 0x7FFFFFF8, 64 };

                    // 8-bit aligned value
                    yield return new object[] { mode, 0, 1, 8 };
                    yield return new object[] { mode, 7, 8, 8 };
                    yield return new object[] { mode, 16, 17, 8 };
                    yield return new object[] { mode, 17, 18, 8 };
                    yield return new object[] { mode, 1023, 1024, 8 };
                    yield return new object[] { mode, 0x7FFFFFFE, 0x7FFFFFFF, 8 };

                    // 176-bit (22 byte) aligned value
                    yield return new object[] { mode, 0, 22, 176 };
                    yield return new object[] { mode, 21, 22, 176 };
                    yield return new object[] { mode, 22, 44, 176 };
                    yield return new object[] { mode, 43, 44, 176 };
                    yield return new object[] { mode, 1011, 1012, 176 };
                    yield return new object[] { mode, 0x7FFFFFFD, 0x7FFFFFFE, 176 };
                }

                PaddingMode[] noPadOnAlignSize = new[] {
                    PaddingMode.Zeros,
                    PaddingMode.None,
                };

                foreach(PaddingMode mode in noPadOnAlignSize)
                {
                    // 128-bit aligned
                    yield return new object[] { mode, 16, 16, 128 };
                    yield return new object[] { mode, 00, 00, 128 };
                    yield return new object[] { mode, 1024, 1024, 128 };
                    yield return new object[] { mode, 0x7FFFFFF0, 0x7FFFFFF0, 128 };

                    // 8-bit aligned
                    yield return new object[] { mode, 0x7FFFFFFF, 0x7FFFFFFF, 8 };
                }

                // Pad only when length is not aligned
                yield return new object[] { PaddingMode.Zeros, 15, 16, 128 };
                yield return new object[] { PaddingMode.Zeros, 17, 32, 128 };
                yield return new object[] { PaddingMode.Zeros, 0x7FFFFFEF, 0x7FFFFFF0, 128 };
            }
        }

        public static IEnumerable<object[]> AllPaddingModes
        {
            get
            {
                yield return new object[] { PaddingMode.ANSIX923 };
                yield return new object[] { PaddingMode.ISO10126 };
                yield return new object[] { PaddingMode.PKCS7 };
                yield return new object[] { PaddingMode.Zeros };
                yield return new object[] { PaddingMode.None };
            }
        }

        private class AnySizeAlgorithm : SymmetricAlgorithm
        {
            public override int BlockSize
            {
                get => BlockSizeValue;
                set => BlockSizeValue = value;
            }

            public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV) =>
                throw new NotImplementedException();
            public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV) =>
                throw new NotImplementedException();
            public override void GenerateIV() => throw new NotImplementedException();
            public override void GenerateKey() => throw new NotImplementedException();
        }

        private class EcbSymmetricAlgorithm : AnySizeAlgorithm
        {
            public delegate bool TryEncryptEcbCoreFunc(
                ReadOnlySpan<byte> plaintext,
                Span<byte> destination,
                PaddingMode paddingMode,
                out int bytesWritten);

            public delegate bool TryDecryptEcbCoreFunc(
                ReadOnlySpan<byte> ciphertext,
                Span<byte> destination,
                PaddingMode paddingMode,
                out int bytesWritten);

            public TryEncryptEcbCoreFunc TryEncryptEcbCoreImpl { get; set; }
            public TryDecryptEcbCoreFunc TryDecryptEcbCoreImpl { get; set; }

            protected override bool TryEncryptEcbCore(
                ReadOnlySpan<byte> plaintext,
                Span<byte> destination,
                PaddingMode paddingMode,
                out int bytesWritten) => TryEncryptEcbCoreImpl(plaintext, destination, paddingMode, out bytesWritten);

            protected override bool TryDecryptEcbCore(
                ReadOnlySpan<byte> ciphertext,
                Span<byte> destination,
                PaddingMode paddingMode,
                out int bytesWritten) => TryDecryptEcbCoreImpl(ciphertext, destination, paddingMode, out bytesWritten);
        }
    }
}
