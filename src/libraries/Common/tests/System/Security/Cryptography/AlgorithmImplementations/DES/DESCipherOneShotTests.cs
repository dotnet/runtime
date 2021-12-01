// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography;

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class DesCipherOneShotTests : SymmetricOneShotBase
    {
        protected override byte[] Key => new byte[]
            {
                0x74, 0x4B, 0x93, 0x3A, 0x96, 0x33, 0x61, 0xD6
            };

        protected override byte[] IV => new byte[]
            {
                0x01, 0x01, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

        protected override SymmetricAlgorithm CreateAlgorithm() => DESFactory.Create();

        [Theory]
        [MemberData(nameof(TestCases))]
        public void OneShotRoundtrip(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            OneShotRoundtripTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryDecryptOneShot_DestinationTooSmall(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            TryDecryptOneShot_DestinationTooSmallTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryEncryptOneShot_DestinationTooSmall(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            TryEncryptOneShot_DestinationTooSmallTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryDecryptOneShot_DestinationJustRight(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            TryDecryptOneShot_DestinationJustRightTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryEncryptOneShot_DestinationJustRight(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            TryEncryptOneShot_DestinationJustRightTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryDecryptOneShot_DestinationLarger(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            TryDecryptOneShot_DestinationLargerTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryEncryptOneShot_DestinationLarger(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            TryEncryptOneShot_DestinationLargerTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryDecryptOneShot_Overlaps(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            TryDecryptOneShot_OverlapsTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryEncryptOneShot_Overlaps(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            TryEncryptOneShot_OverlapsTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void DecryptOneShot_Span(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            DecryptOneShot_SpanTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void EncryptOneShot_Span(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            EncryptOneShot_SpanTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void DecryptOneShot_Array(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            DecryptOneShot_ArrayTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void EncryptOneShot_Array(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode, int feedbackSize = 0) =>
            EncryptOneShot_ArrayTest(plaintext, ciphertext, padding, mode, feedbackSize);

        [Fact]
        public void EncryptOneShot_CfbFeedbackSizeNotSupported()
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                Assert.ThrowsAny<CryptographicException>(() =>
                    alg.TryEncryptCfb(ReadOnlySpan<byte>.Empty, IV, Span<byte>.Empty, out _, feedbackSizeInBits: 56));
            }
        }

        [Fact]
        public void DecryptOneShot_CfbFeedbackSizeNotSupported()
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                Assert.ThrowsAny<CryptographicException>(() =>
                    alg.TryDecryptCfb(ReadOnlySpan<byte>.Empty, IV, Span<byte>.Empty, out _, feedbackSizeInBits: 56));
            }
        }

        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                        0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0x29, 0x30, 0x13, 0x97, 0xCD, 0x5E, 0x30, 0x2C,
                        0xED, 0x11, 0x65, 0xA8, 0xF3, 0xA0, 0x11, 0x42,
                        0xD0, 0x53, 0x1B, 0xB2, 0x55, 0xC0, 0x65, 0x8D,
                    },

                    PaddingMode.PKCS7,
                    CipherMode.CBC,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                        0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0x29, 0x30, 0x13, 0x97, 0xCD, 0x5E, 0x30, 0x2C,
                        0xED, 0x11, 0x65, 0xA8, 0xF3, 0xA0, 0x11, 0x42,
                    },

                    PaddingMode.None,
                    CipherMode.CBC,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                        0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0x29, 0x30, 0x13, 0x97, 0xCD, 0x5E, 0x30, 0x2C,
                        0xED, 0x11, 0x65, 0xA8, 0xF3, 0xA0, 0x11, 0x42,
                    },

                    PaddingMode.Zeros,
                    CipherMode.CBC,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                        0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0x29, 0x30, 0x13, 0x97, 0xCD, 0x5E, 0x30, 0x2C,
                        0xED, 0x11, 0x65, 0xA8, 0xF3, 0xA0, 0x11, 0x42,
                        0x26, 0xA4, 0x0F, 0x7F, 0x69, 0x53, 0xEE, 0xF1,
                    },

                    PaddingMode.ANSIX923,
                    CipherMode.CBC,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                        0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0x29, 0x30, 0x13, 0x97, 0xCD, 0x5E, 0x30, 0x2C,
                        0xED, 0x11, 0x65, 0xA8, 0xF3, 0xA0, 0x11, 0x42,
                        0x4F, 0xE1, 0xDF, 0x40, 0xE8, 0x30, 0x80, 0xB3,
                    },

                    PaddingMode.ISO10126,
                    CipherMode.CBC,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                        0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                        0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0x57, 0xB9, 0x91, 0xCF, 0x46, 0x03, 0x2B, 0x45,
                        0x2A, 0x59, 0x19, 0xB3, 0x97, 0x7A, 0xC7, 0x73,
                        0x69, 0xD7, 0xBD, 0x06, 0x3A, 0x3B, 0x40, 0x87,
                    },

                    PaddingMode.PKCS7,
                    CipherMode.CBC,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                        0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                        0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0x57, 0xB9, 0x91, 0xCF, 0x46, 0x03, 0x2B, 0x45,
                        0x2A, 0x59, 0x19, 0xB3, 0x97, 0x7A, 0xC7, 0x73,
                        0x71, 0xB4, 0x8D, 0x6C, 0xDD, 0x98, 0x83, 0x55,
                    },

                    PaddingMode.Zeros,
                    CipherMode.CBC,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                        0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                        0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0x57, 0xB9, 0x91, 0xCF, 0x46, 0x03, 0x2B, 0x45,
                        0x2A, 0x59, 0x19, 0xB3, 0x97, 0x7A, 0xC7, 0x73,
                        0xAE, 0xF8, 0x98, 0x2C, 0xB3, 0x71, 0x9F, 0xDF,
                    },

                    PaddingMode.ANSIX923,
                    CipherMode.CBC,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                        0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                        0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0x57, 0xB9, 0x91, 0xCF, 0x46, 0x03, 0x2B, 0x45,
                        0x2A, 0x59, 0x19, 0xB3, 0x97, 0x7A, 0xC7, 0x73,
                        0x5B, 0xFE, 0xD3, 0x3F, 0xDA, 0xBC, 0x15, 0x29,
                    },

                    PaddingMode.ISO10126,
                    CipherMode.CBC,
                };

                yield return new object[]
                {
                    // plaintext
                    Array.Empty<byte>(),

                    // ciphertext
                    Array.Empty<byte>(),

                    PaddingMode.Zeros,
                    CipherMode.CBC,
                };

                yield return new object[]
                {
                    // plaintext
                    Array.Empty<byte>(),

                    // ciphertext
                    Array.Empty<byte>(),

                    PaddingMode.None,
                    CipherMode.CBC,
                };

                yield return new object[]
                {
                    // plaintext
                    Array.Empty<byte>(),

                    // ciphertext
                    new byte[]
                    {
                        0x49, 0xD5, 0xE1, 0x5E, 0x17, 0x12, 0x23, 0xCF
                    },

                    PaddingMode.PKCS7,
                    CipherMode.CBC,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                        0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0xEE, 0x8B, 0xA7, 0xEE, 0x11, 0x84, 0x1D, 0xA2,
                        0xC4, 0x16, 0xB4, 0x05, 0x83, 0xA0, 0x60, 0x37,
                        0xED, 0xD9, 0xE3, 0xFC, 0xC6, 0x55, 0xDC, 0x32,
                    },

                    PaddingMode.PKCS7,
                    CipherMode.ECB,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                        0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0xEE, 0x8B, 0xA7, 0xEE, 0x11, 0x84, 0x1D, 0xA2,
                        0xC4, 0x16, 0xB4, 0x05, 0x83, 0xA0, 0x60, 0x37,
                    },

                    PaddingMode.None,
                    CipherMode.ECB,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                        0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0xEE, 0x8B, 0xA7, 0xEE, 0x11, 0x84, 0x1D, 0xA2,
                        0xC4, 0x16, 0xB4, 0x05, 0x83, 0xA0, 0x60, 0x37,
                    },

                    PaddingMode.Zeros,
                    CipherMode.ECB,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                        0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0xEE, 0x8B, 0xA7, 0xEE, 0x11, 0x84, 0x1D, 0xA2,
                        0xC4, 0x16, 0xB4, 0x05, 0x83, 0xA0, 0x60, 0x37,
                        0xEC, 0x52, 0xA1, 0x7E, 0x52, 0x54, 0x6E, 0x9E,
                    },

                    PaddingMode.ANSIX923,
                    CipherMode.ECB,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                        0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0xEE, 0x8B, 0xA7, 0xEE, 0x11, 0x84, 0x1D, 0xA2,
                        0xC4, 0x16, 0xB4, 0x05, 0x83, 0xA0, 0x60, 0x37,
                        0x44, 0x4C, 0xA5, 0xC2, 0xCC, 0x54, 0xAC, 0xF9,
                    },

                    PaddingMode.ISO10126,
                    CipherMode.ECB,
                };

                // plaintext requires padding
                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                        0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                        0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0xEA, 0x91, 0x68, 0xFE, 0x02, 0xFE, 0x57, 0x6F,
                        0x60, 0x17, 0x05, 0xD5, 0x94, 0xA2, 0xF8, 0xE2,
                        0x60, 0x8E, 0xC3, 0xB8, 0x09, 0x84, 0xCF, 0x3B,
                    },

                    PaddingMode.PKCS7,
                    CipherMode.ECB,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                        0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                        0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0xEA, 0x91, 0x68, 0xFE, 0x02, 0xFE, 0x57, 0x6F,
                        0x60, 0x17, 0x05, 0xD5, 0x94, 0xA2, 0xF8, 0xE2,
                        0xE7, 0xA4, 0x10, 0xF1, 0x7B, 0xFF, 0x32, 0x4A,
                    },

                    PaddingMode.Zeros,
                    CipherMode.ECB,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                        0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                        0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0xEA, 0x91, 0x68, 0xFE, 0x02, 0xFE, 0x57, 0x6F,
                        0x60, 0x17, 0x05, 0xD5, 0x94, 0xA2, 0xF8, 0xE2,
                        0x92, 0x9A, 0x36, 0xFE, 0xA4, 0xB3, 0xEC, 0xA0,
                    },

                    PaddingMode.ANSIX923,
                    CipherMode.ECB,
                };

                yield return new object[]
                {
                    // plaintext
                    new byte[]
                    {
                        0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                        0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                        0x59,
                    },

                    // ciphertext
                    new byte[]
                    {
                        0xEA, 0x91, 0x68, 0xFE, 0x02, 0xFE, 0x57, 0x6F,
                        0x60, 0x17, 0x05, 0xD5, 0x94, 0xA2, 0xF8, 0xE2,
                        0xDB, 0x86, 0xA4, 0xAB, 0xDE, 0x05, 0xE4, 0xE7,
                    },

                    PaddingMode.ISO10126,
                    CipherMode.ECB,
                };

                yield return new object[]
                {
                    // plaintext
                    Array.Empty<byte>(),

                    // ciphertext
                    Array.Empty<byte>(),

                    PaddingMode.Zeros,
                    CipherMode.ECB,
                };

                yield return new object[]
                {
                    // plaintext
                    Array.Empty<byte>(),

                    // ciphertext
                    Array.Empty<byte>(),

                    PaddingMode.None,
                    CipherMode.ECB,
                };

                yield return new object[]
                {
                    // plaintext
                    Array.Empty<byte>(),

                    // ciphertext
                    new byte[]
                    {
                        0xED, 0xD9, 0xE3, 0xFC, 0xC6, 0x55, 0xDC, 0x32,
                    },

                    PaddingMode.PKCS7,
                    CipherMode.ECB,
                };

                // Windows 7 does not support CFB8
                if (PlatformDetection.IsNotWindows7)
                {
                    yield return new object[]
                    {
                        // plaintext
                        new byte[]
                        {
                            0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                            0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                        },

                        // ciphertext
                        new byte[]
                        {
                            0x6F, 0xF8, 0x24, 0x52, 0x68, 0x8E, 0x53, 0x97,
                            0x2A, 0x6B, 0x8A, 0x5E, 0xBE, 0x98, 0x84, 0x28,
                            0x39,
                        },

                        PaddingMode.PKCS7,
                        CipherMode.CFB,
                        8,
                    };

                    yield return new object[]
                    {
                        // plaintext
                        new byte[]
                        {
                            0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                            0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                        },

                        // ciphertext
                        new byte[]
                        {
                            0x6F, 0xF8, 0x24, 0x52, 0x68, 0x8E, 0x53, 0x97,
                            0x2A, 0x6B, 0x8A, 0x5E, 0xBE, 0x98, 0x84, 0x28,
                        },

                        PaddingMode.None,
                        CipherMode.CFB,
                        8,
                    };

                    yield return new object[]
                    {
                        // plaintext
                        new byte[]
                        {
                            0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                            0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                        },

                        // ciphertext
                        new byte[]
                        {
                            0x6F, 0xF8, 0x24, 0x52, 0x68, 0x8E, 0x53, 0x97,
                            0x2A, 0x6B, 0x8A, 0x5E, 0xBE, 0x98, 0x84, 0x28,
                        },

                        PaddingMode.Zeros,
                        CipherMode.CFB,
                        8,
                    };

                    yield return new object[]
                    {
                        // plaintext
                        new byte[]
                        {
                            0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                            0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                        },

                        // ciphertext
                        new byte[]
                        {
                            0x6F, 0xF8, 0x24, 0x52, 0x68, 0x8E, 0x53, 0x97,
                            0x2A, 0x6B, 0x8A, 0x5E, 0xBE, 0x98, 0x84, 0x28,
                            0x39,
                        },

                        PaddingMode.ANSIX923,
                        CipherMode.CFB,
                        8,
                    };

                    yield return new object[]
                    {
                        // plaintext
                        new byte[]
                        {
                            0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89,
                            0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59,
                        },

                        // ciphertext
                        new byte[]
                        {
                            0x6F, 0xF8, 0x24, 0x52, 0x68, 0x8E, 0x53, 0x97,
                            0x2A, 0x6B, 0x8A, 0x5E, 0xBE, 0x98, 0x84, 0x28,
                            0x39,
                        },

                        PaddingMode.ISO10126,
                        CipherMode.CFB,
                        8,
                    };

                    yield return new object[]
                    {
                        // plaintext
                        new byte[]
                        {
                            0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                            0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                            0x59,
                        },

                        // ciphertext
                        new byte[]
                        {
                            0xA6, 0x51, 0xB4, 0xF2, 0x2B, 0xFA, 0x22, 0xF5,
                            0x15, 0x1E, 0x5E, 0x65, 0x39, 0xFD, 0x84, 0x4F,
                            0xE1, 0x7E,
                        },

                        PaddingMode.PKCS7,
                        CipherMode.CFB,
                        8,
                    };

                    yield return new object[]
                    {
                        // plaintext
                        new byte[]
                        {
                            0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                            0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                            0x59,
                        },

                        // ciphertext
                        new byte[]
                        {
                            0xA6, 0x51, 0xB4, 0xF2, 0x2B, 0xFA, 0x22, 0xF5,
                            0x15, 0x1E, 0x5E, 0x65, 0x39, 0xFD, 0x84, 0x4F,
                            0xE1,
                        },

                        PaddingMode.None,
                        CipherMode.CFB,
                        8,
                    };

                    yield return new object[]
                    {
                        // plaintext
                        new byte[]
                        {
                            0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                            0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                            0x59,
                        },

                        // ciphertext
                        new byte[]
                        {
                            0xA6, 0x51, 0xB4, 0xF2, 0x2B, 0xFA, 0x22, 0xF5,
                            0x15, 0x1E, 0x5E, 0x65, 0x39, 0xFD, 0x84, 0x4F,
                            0xE1,
                        },

                        PaddingMode.Zeros,
                        CipherMode.CFB,
                        8,
                    };

                    yield return new object[]
                    {
                        // plaintext
                        new byte[]
                        {
                            0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                            0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                            0x59,
                        },

                        // ciphertext
                        new byte[]
                        {
                            0xA6, 0x51, 0xB4, 0xF2, 0x2B, 0xFA, 0x22, 0xF5,
                            0x15, 0x1E, 0x5E, 0x65, 0x39, 0xFD, 0x84, 0x4F,
                            0xE1, 0x7E,
                        },

                        PaddingMode.ANSIX923,
                        CipherMode.CFB,
                        8,
                    };

                    yield return new object[]
                    {
                        // plaintext
                        new byte[]
                        {
                            0x99, 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8,
                            0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83,
                            0x59,
                        },

                        // ciphertext
                        new byte[]
                        {
                            0xA6, 0x51, 0xB4, 0xF2, 0x2B, 0xFA, 0x22, 0xF5,
                            0x15, 0x1E, 0x5E, 0x65, 0x39, 0xFD, 0x84, 0x4F,
                            0xE1, 0x7E,
                        },

                        PaddingMode.ISO10126,
                        CipherMode.CFB,
                        8,
                    };
                }
            }
        }
    }
}
