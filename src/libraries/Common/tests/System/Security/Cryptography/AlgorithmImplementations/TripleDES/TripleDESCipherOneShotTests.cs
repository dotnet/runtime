// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography;

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class TripleDESCipherOneShotTests : SymmetricOneShotBase
    {
        protected override byte[] Key => new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x05, 0x06, 0x07, 0x08,
                0x0A, 0x0B, 0x0C, 0x0D, 0x0F, 0x10, 0x11, 0x12,
                0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0xA0,
            };

        protected override byte[] IV => new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            };

        protected override SymmetricAlgorithm CreateAlgorithm() => TripleDESFactory.Create();

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
                    alg.TryEncryptCfb(ReadOnlySpan<byte>.Empty, IV, Span<byte>.Empty, out _, feedbackSizeInBits: 48));
            }
        }

        [Fact]
        public void DecryptOneShot_CfbFeedbackSizeNotSupported()
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                Assert.ThrowsAny<CryptographicException>(() =>
                    alg.TryDecryptCfb(ReadOnlySpan<byte>.Empty, IV, Span<byte>.Empty, out _, feedbackSizeInBits: 48));
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
                        0xDD, 0x97, 0xD0, 0xD3, 0x8D, 0xC6, 0xFD, 0x26,
                        0x5A, 0x76, 0x3B, 0x0E, 0x0D, 0x91, 0x12, 0x98,
                        0x55, 0xDF, 0x36, 0xB0, 0xB3, 0x91, 0x72, 0x30,
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
                        0xDD, 0x97, 0xD0, 0xD3, 0x8D, 0xC6, 0xFD, 0x26,
                        0x5A, 0x76, 0x3B, 0x0E, 0x0D, 0x91, 0x12, 0x98,
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
                        0xDD, 0x97, 0xD0, 0xD3, 0x8D, 0xC6, 0xFD, 0x26,
                        0x5A, 0x76, 0x3B, 0x0E, 0x0D, 0x91, 0x12, 0x98,
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
                        0xDD, 0x97, 0xD0, 0xD3, 0x8D, 0xC6, 0xFD, 0x26,
                        0x5A, 0x76, 0x3B, 0x0E, 0x0D, 0x91, 0x12, 0x98,
                        0x62, 0x8C, 0x88, 0x03, 0xD2, 0xD7, 0xBF, 0xB3,
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
                        0xDD, 0x97, 0xD0, 0xD3, 0x8D, 0xC6, 0xFD, 0x26,
                        0x5A, 0x76, 0x3B, 0x0E, 0x0D, 0x91, 0x12, 0x98,
                        0x64, 0x32, 0xD7, 0xEF, 0x2D, 0xAC, 0x16, 0xF9,
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
                        0xFC, 0x1C, 0xB2, 0x4C, 0x7E, 0x4A, 0x29, 0x08,
                        0xD9, 0x76, 0x41, 0xC4, 0x1A, 0x06, 0xD3, 0x9F,
                        0x1F, 0xA3, 0xA9, 0x74, 0x8A, 0x21, 0x15, 0x90,
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
                        0xFC, 0x1C, 0xB2, 0x4C, 0x7E, 0x4A, 0x29, 0x08,
                        0xD9, 0x76, 0x41, 0xC4, 0x1A, 0x06, 0xD3, 0x9F,
                        0x38, 0xBC, 0x88, 0x44, 0x52, 0xBD, 0x23, 0xEA,
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
                        0xFC, 0x1C, 0xB2, 0x4C, 0x7E, 0x4A, 0x29, 0x08,
                        0xD9, 0x76, 0x41, 0xC4, 0x1A, 0x06, 0xD3, 0x9F,
                        0xE0, 0x46, 0x72, 0xBA, 0x0B, 0x55, 0xF4, 0x29,
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
                        0xFC, 0x1C, 0xB2, 0x4C, 0x7E, 0x4A, 0x29, 0x08,
                        0xD9, 0x76, 0x41, 0xC4, 0x1A, 0x06, 0xD3, 0x9F,
                        0x94, 0x03, 0x51, 0x1C, 0xA8, 0xAC, 0x1E, 0x68,
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
                        0x79, 0x6B, 0x9D, 0x8B, 0xFD, 0xD4, 0x23, 0xCE,
                    },

                    PaddingMode.PKCS7,
                    CipherMode.CBC,
                };

                // plaintext requires no padding
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
                        0x2C, 0xE7, 0xF1, 0x5C, 0x7B, 0xA8, 0x40, 0x0C,
                        0x1A, 0x09, 0xDC, 0x63, 0x43, 0xC9, 0x1A, 0x63,
                        0x65, 0xE4, 0x9C, 0xD3, 0xE6, 0xBE, 0xB8, 0x40,
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
                        0x2C, 0xE7, 0xF1, 0x5C, 0x7B, 0xA8, 0x40, 0x0C,
                        0x1A, 0x09, 0xDC, 0x63, 0x43, 0xC9, 0x1A, 0x63,
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
                        0x2C, 0xE7, 0xF1, 0x5C, 0x7B, 0xA8, 0x40, 0x0C,
                        0x1A, 0x09, 0xDC, 0x63, 0x43, 0xC9, 0x1A, 0x63,
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
                        0x2C, 0xE7, 0xF1, 0x5C, 0x7B, 0xA8, 0x40, 0x0C,
                        0x1A, 0x09, 0xDC, 0x63, 0x43, 0xC9, 0x1A, 0x63,
                        0x34, 0xE6, 0x86, 0x6D, 0x94, 0x2E, 0x98, 0x0F,
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
                        0x2C, 0xE7, 0xF1, 0x5C, 0x7B, 0xA8, 0x40, 0x0C,
                        0x1A, 0x09, 0xDC, 0x63, 0x43, 0xC9, 0x1A, 0x63,
                        0x5E, 0xEE, 0x73, 0xBB, 0x94, 0xED, 0x29, 0x7A,
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
                        0xD5, 0xD5, 0x80, 0x3F, 0xC3, 0x7E, 0x4A, 0xE4,
                        0xF2, 0x93, 0x9B, 0xC3, 0xDC, 0x4F, 0xA0, 0x23,
                        0xB1, 0x3D, 0x05, 0x93, 0x98, 0xE6, 0x2C, 0xDF,
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
                        0xD5, 0xD5, 0x80, 0x3F, 0xC3, 0x7E, 0x4A, 0xE4,
                        0xF2, 0x93, 0x9B, 0xC3, 0xDC, 0x4F, 0xA0, 0x23,
                        0xC9, 0x52, 0x8F, 0xC1, 0x30, 0xC0, 0x7C, 0x63,
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
                        0xD5, 0xD5, 0x80, 0x3F, 0xC3, 0x7E, 0x4A, 0xE4,
                        0xF2, 0x93, 0x9B, 0xC3, 0xDC, 0x4F, 0xA0, 0x23,
                        0x6A, 0x97, 0x38, 0x85, 0x3B, 0x48, 0x81, 0x5E,
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
                        0xD5, 0xD5, 0x80, 0x3F, 0xC3, 0x7E, 0x4A, 0xE4,
                        0xF2, 0x93, 0x9B, 0xC3, 0xDC, 0x4F, 0xA0, 0x23,
                        0x33, 0x58, 0x09, 0x2C, 0xD8, 0xB5, 0x36, 0xAD,
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
                        0x65, 0xE4, 0x9C, 0xD3, 0xE6, 0xBE, 0xB8, 0x40,
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
                        0x46, 0x85, 0x45, 0x43, 0x3E, 0xD9, 0x40, 0xAF,
                        0x16, 0xBE, 0xC5, 0xEF, 0xD9, 0x12, 0xFE, 0x07,
                        0x66,
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
                        0x46, 0x85, 0x45, 0x43, 0x3E, 0xD9, 0x40, 0xAF,
                        0x16, 0xBE, 0xC5, 0xEF, 0xD9, 0x12, 0xFE, 0x07,
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
                        0x46, 0x85, 0x45, 0x43, 0x3E, 0xD9, 0x40, 0xAF,
                        0x16, 0xBE, 0xC5, 0xEF, 0xD9, 0x12, 0xFE, 0x07,
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
                        0x46, 0x85, 0x45, 0x43, 0x3E, 0xD9, 0x40, 0xAF,
                        0x16, 0xBE, 0xC5, 0xEF, 0xD9, 0x12, 0xFE, 0x07,
                        0x66,
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
                        0x46, 0x85, 0x45, 0x43, 0x3E, 0xD9, 0x40, 0xAF,
                        0x16, 0xBE, 0xC5, 0xEF, 0xD9, 0x12, 0xFE, 0x07,
                        0x66,
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
                        0x8F, 0xD6, 0x1C, 0xF3, 0xAF, 0x0D, 0x3C, 0x98,
                        0xCF, 0x29, 0x20, 0xB3, 0xF3, 0xFC, 0x34, 0xF0,
                        0x38, 0xA0,
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
                        0x8F, 0xD6, 0x1C, 0xF3, 0xAF, 0x0D, 0x3C, 0x98,
                        0xCF, 0x29, 0x20, 0xB3, 0xF3, 0xFC, 0x34, 0xF0,
                        0x38,
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
                        0x8F, 0xD6, 0x1C, 0xF3, 0xAF, 0x0D, 0x3C, 0x98,
                        0xCF, 0x29, 0x20, 0xB3, 0xF3, 0xFC, 0x34, 0xF0,
                        0x38,
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
                        0x8F, 0xD6, 0x1C, 0xF3, 0xAF, 0x0D, 0x3C, 0x98,
                        0xCF, 0x29, 0x20, 0xB3, 0xF3, 0xFC, 0x34, 0xF0,
                        0x38, 0xA0,
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
                        0x8F, 0xD6, 0x1C, 0xF3, 0xAF, 0x0D, 0x3C, 0x98,
                        0xCF, 0x29, 0x20, 0xB3, 0xF3, 0xFC, 0x34, 0xF0,
                        0x38, 0xA0,
                    },

                    PaddingMode.ISO10126,
                    CipherMode.CFB,
                    8,
                };

                yield return new object[]
                {
                    // plaintext
                    Array.Empty<byte>(),

                    // ciphertext
                    new byte[]
                    {
                        0x17,
                    },

                    PaddingMode.PKCS7,
                    CipherMode.CFB,
                    8,
                };

                yield return new object[]
                {
                    // plaintext
                    Array.Empty<byte>(),

                    // ciphertext
                    Array.Empty<byte>(),

                    PaddingMode.Zeros,
                    CipherMode.CFB,
                    8,
                };

                yield return new object[]
                {
                    // plaintext
                    Array.Empty<byte>(),

                    // ciphertext
                    Array.Empty<byte>(),

                    PaddingMode.None,
                    CipherMode.CFB,
                    8,
                };

                // 3DES CFB64 is not supported on Windows 7.
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
                            0x46, 0x82, 0xB5, 0xFC, 0x6A, 0xD3, 0x92, 0xF9,
                            0xB1, 0xF6, 0xD9, 0xF3, 0xBB, 0x8D, 0x57, 0xE5,
                            0xFF, 0x66, 0x88, 0x3C, 0x53, 0xC4, 0x5A, 0xC6,
                        },

                        PaddingMode.PKCS7,
                        CipherMode.CFB,
                        64,
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
                            0x46, 0x82, 0xB5, 0xFC, 0x6A, 0xD3, 0x92, 0xF9,
                            0xB1, 0xF6, 0xD9, 0xF3, 0xBB, 0x8D, 0x57, 0xE5,
                        },

                        PaddingMode.None,
                        CipherMode.CFB,
                        64,
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
                            0x46, 0x82, 0xB5, 0xFC, 0x6A, 0xD3, 0x92, 0xF9,
                            0xB1, 0xF6, 0xD9, 0xF3, 0xBB, 0x8D, 0x57, 0xE5,
                        },

                        PaddingMode.Zeros,
                        CipherMode.CFB,
                        64,
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
                            0x46, 0x82, 0xB5, 0xFC, 0x6A, 0xD3, 0x92, 0xF9,
                            0xB1, 0xF6, 0xD9, 0xF3, 0xBB, 0x8D, 0x57, 0xE5,
                            0xF7, 0x6E, 0x80, 0x34, 0x5B, 0xCC, 0x52, 0xC6,
                        },

                        PaddingMode.ANSIX923,
                        CipherMode.CFB,
                        64,
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
                            0x46, 0x82, 0xB5, 0xFC, 0x6A, 0xD3, 0x92, 0xF9,
                            0xB1, 0xF6, 0xD9, 0xF3, 0xBB, 0x8D, 0x57, 0xE5,
                            0x47, 0x0F, 0x9A, 0x12, 0x6F, 0x92, 0xB4, 0xC6,
                        },

                        PaddingMode.ISO10126,
                        CipherMode.CFB,
                        64,
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
                            0x8F, 0xBA, 0xCF, 0x4A, 0x91, 0x84, 0x52, 0xB8,
                            0xFF, 0x69, 0xE0, 0xAD, 0x2C, 0xEC, 0xE4, 0x8F,
                            0xE0, 0x50, 0x64, 0xD5, 0xA3, 0x32, 0x38, 0xA9,
                        },

                        PaddingMode.PKCS7,
                        CipherMode.CFB,
                        64,
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
                            0x8F, 0xBA, 0xCF, 0x4A, 0x91, 0x84, 0x52, 0xB8,
                            0xFF, 0x69, 0xE0, 0xAD, 0x2C, 0xEC, 0xE4, 0x8F,
                            0xE0, 0x57, 0x63, 0xD2, 0xA4, 0x35, 0x3F, 0xAE,
                        },

                        PaddingMode.Zeros,
                        CipherMode.CFB,
                        64,
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
                            0x8F, 0xBA, 0xCF, 0x4A, 0x91, 0x84, 0x52, 0xB8,
                            0xFF, 0x69, 0xE0, 0xAD, 0x2C, 0xEC, 0xE4, 0x8F,
                            0xE0, 0x57, 0x63, 0xD2, 0xA4, 0x35, 0x3F, 0xA9,
                        },

                        PaddingMode.ANSIX923,
                        CipherMode.CFB,
                        64,
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
                            0x8F, 0xBA, 0xCF, 0x4A, 0x91, 0x84, 0x52, 0xB8,
                            0xFF, 0x69, 0xE0, 0xAD, 0x2C, 0xEC, 0xE4, 0x8F,
                            0xE0, 0xE7, 0xF6, 0x44, 0xBE, 0xDD, 0x3D, 0xA9,
                        },

                        PaddingMode.ISO10126,
                        CipherMode.CFB,
                        64,
                    };

                    yield return new object[]
                    {
                        // plaintext
                        Array.Empty<byte>(),

                        // ciphertext
                        new byte[]
                        {
                            0x1E, 0xE2, 0xAF, 0x50, 0x3D, 0xD3, 0x52, 0x78,
                        },

                        PaddingMode.PKCS7,
                        CipherMode.CFB,
                        64,
                    };

                    yield return new object[]
                    {
                        // plaintext
                        Array.Empty<byte>(),

                        // ciphertext
                        Array.Empty<byte>(),

                        PaddingMode.None,
                        CipherMode.CFB,
                        64,
                    };

                    yield return new object[]
                    {
                        // plaintext
                        Array.Empty<byte>(),

                        // ciphertext
                        Array.Empty<byte>(),

                        PaddingMode.Zeros,
                        CipherMode.CFB,
                        64,
                    };
                }
            }
        }
    }
}
