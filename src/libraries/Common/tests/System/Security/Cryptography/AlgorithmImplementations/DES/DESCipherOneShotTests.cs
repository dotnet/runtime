// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography;

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    public class DesCipherOneShotTests : SymmetricOneShotBase
    {
        protected override byte[] Key => new byte[]
            {
                0x74, 0x4B, 0x93, 0x3A, 0x96, 0x33, 0x61, 0xD6
            };

        protected override byte[] Iv => new byte[]
            {
                0x01, 0x01, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

        protected override SymmetricAlgorithm CreateAlgorithm() => DESFactory.Create();

        [Theory]
        [MemberData(nameof(TestCases))]
        public void OneShotRoundtrip(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            OneShotRoundtripTest(plaintext, ciphertext, padding, mode);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryDecryptOneShot_DestinationTooSmall(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            TryDecryptOneShot_DestinationTooSmallTest(plaintext, ciphertext, padding, mode);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryEncryptOneShot_DestinationTooSmall(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            TryEncryptOneShot_DestinationTooSmallTest(plaintext, ciphertext, padding, mode);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryDecryptOneShot_DestinationJustRight(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            TryDecryptOneShot_DestinationJustRightTest(plaintext, ciphertext, padding, mode);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryEncryptOneShot_DestinationJustRight(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            TryEncryptOneShot_DestinationJustRightTest(plaintext, ciphertext, padding, mode);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryDecryptOneShot_DestinationLarger(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            TryDecryptOneShot_DestinationLargerTest(plaintext, ciphertext, padding, mode);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryEncryptOneShot_DestinationLarger(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            TryEncryptOneShot_DestinationLargerTest(plaintext, ciphertext, padding, mode);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryDecryptOneShot_Overlaps(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            TryDecryptOneShot_OverlapsTest(plaintext, ciphertext, padding, mode);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TryEncryptOneShot_Overlaps(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            TryEncryptOneShot_OverlapsTest(plaintext, ciphertext, padding, mode);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void DecryptOneShot_Span(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            DecryptOneShot_SpanTest(plaintext, ciphertext, padding, mode);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void EncryptOneShot_Span(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            EncryptOneShot_SpanTest(plaintext, ciphertext, padding, mode);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void DecryptOneShot_Array(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            DecryptOneShot_ArrayTest(plaintext, ciphertext, padding, mode);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void EncryptOneShot_Array(byte[] plaintext, byte[] ciphertext, PaddingMode padding, CipherMode mode) =>
            EncryptOneShot_ArrayTest(plaintext, ciphertext, padding, mode);

        public static IEnumerable<object[]> TestCases
        {
            get
            {
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
            }
        }
    }
}
