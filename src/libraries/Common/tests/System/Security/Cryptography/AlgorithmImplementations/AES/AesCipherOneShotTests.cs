// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography;

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class AesCipherOneShotTests : SymmetricOneShotBase
    {
        protected override byte[] Key =>
            new byte[] { 0x00, 0x01, 0x02, 0x03, 0x05, 0x06, 0x07, 0x08, 0x0A, 0x0B, 0x0C, 0x0D, 0x0F, 0x10, 0x11, 0x12 };

        protected override byte[] IV =>
            new byte[] { 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22 };

        protected override SymmetricAlgorithm CreateAlgorithm() => AesFactory.Create();

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
                        0x2E, 0x35, 0xFF, 0xFB, 0x08, 0x56, 0xE9, 0x21,
                        0x83, 0xF8, 0x8A, 0xF1, 0x79, 0xC0, 0x2A, 0xD4,
                        0x07, 0xBC, 0x83, 0x1E, 0x5B, 0x12, 0x48, 0x1F,
                        0x9E, 0x91, 0xD5, 0x44, 0xA0, 0x85, 0x0B, 0x19,
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
                        0x2E, 0x35, 0xFF, 0xFB, 0x08, 0x56, 0xE9, 0x21,
                        0x83, 0xF8, 0x8A, 0xF1, 0x79, 0xC0, 0x2A, 0xD4,
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
                        0x2E, 0x35, 0xFF, 0xFB, 0x08, 0x56, 0xE9, 0x21,
                        0x83, 0xF8, 0x8A, 0xF1, 0x79, 0xC0, 0x2A, 0xD4,
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
                        0x2E, 0x35, 0xFF, 0xFB, 0x08, 0x56, 0xE9, 0x21,
                        0x83, 0xF8, 0x8A, 0xF1, 0x79, 0xC0, 0x2A, 0xD4,
                        0x63, 0xB1, 0x3D, 0x28, 0xD7, 0xD1, 0x1E, 0x2E,
                        0x09, 0x40, 0xA1, 0xF0, 0xFD, 0xE3, 0xF1, 0x03,
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
                        0x2E, 0x35, 0xFF, 0xFB, 0x08, 0x56, 0xE9, 0x21,
                        0x83, 0xF8, 0x8A, 0xF1, 0x79, 0xC0, 0x2A, 0xD4,
                        0xAC, 0xBB, 0xEC, 0xDF, 0x53, 0x6B, 0x9A, 0x34,
                        0x0F, 0x03, 0x58, 0x00, 0x2D, 0x86, 0x13, 0xC9,
                    },

                    PaddingMode.ISO10126,
                    CipherMode.CBC,
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
                        0x1B, 0x98, 0x27, 0x53, 0xC2, 0xCA, 0x0B, 0xF9,
                        0x60, 0x32, 0xD8, 0x07, 0x16, 0x28, 0xCB, 0xEB,
                        0x12, 0x9A, 0xC3, 0xC8, 0x9C, 0x14, 0xCD, 0x37,
                        0xA2, 0x43, 0x65, 0x14, 0xC7, 0xDC, 0x17, 0xFD,
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
                        0x1B, 0x98, 0x27, 0x53, 0xC2, 0xCA, 0x0B, 0xF9,
                        0x60, 0x32, 0xD8, 0x07, 0x16, 0x28, 0xCB, 0xEB,
                        0x31, 0xD7, 0xED, 0x14, 0x2B, 0x5D, 0x76, 0x3A,
                        0x35, 0xFD, 0x3C, 0x56, 0xD0, 0xF1, 0x16, 0xB3,
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
                        0x1B, 0x98, 0x27, 0x53, 0xC2, 0xCA, 0x0B, 0xF9,
                        0x60, 0x32, 0xD8, 0x07, 0x16, 0x28, 0xCB, 0xEB,
                        0x13, 0xCC, 0xD3, 0x3E, 0xE4, 0xE0, 0x3A, 0x04,
                        0x6F, 0xA3, 0xD6, 0xCB, 0x98, 0xBD, 0x47, 0x59,
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
                        0x1B, 0x98, 0x27, 0x53, 0xC2, 0xCA, 0x0B, 0xF9,
                        0x60, 0x32, 0xD8, 0x07, 0x16, 0x28, 0xCB, 0xEB,
                        0xA8, 0xB6, 0xE2, 0x45, 0x01, 0xF2, 0xA0, 0x3A,
                        0xA6, 0x57, 0x69, 0x4F, 0xF9, 0x8B, 0xB9, 0x40,
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
                        0xF0, 0x32, 0xE1, 0x6F, 0x32, 0x7C, 0x4C, 0xB4,
                        0xC9, 0x99, 0xF2, 0x10, 0x5C, 0xE9, 0xED, 0x70,
                    },

                    PaddingMode.PKCS7,
                    CipherMode.CBC,
                };

                // ECB test cases
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
                        0xD8, 0xF5, 0x32, 0x53, 0x82, 0x89, 0xEF, 0x7D,
                        0x06, 0xB5, 0x06, 0xA4, 0xFD, 0x5B, 0xE9, 0xC9,
                        0x6D, 0xE5, 0xF6, 0x07, 0xAB, 0x7E, 0xB8, 0x20,
                        0x2F, 0x39, 0x57, 0x70, 0x3B, 0x04, 0xE8, 0xB5,
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
                        0xD8, 0xF5, 0x32, 0x53, 0x82, 0x89, 0xEF, 0x7D,
                        0x06, 0xB5, 0x06, 0xA4, 0xFD, 0x5B, 0xE9, 0xC9,
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
                        0xD8, 0xF5, 0x32, 0x53, 0x82, 0x89, 0xEF, 0x7D,
                        0x06, 0xB5, 0x06, 0xA4, 0xFD, 0x5B, 0xE9, 0xC9,
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
                        0xD8, 0xF5, 0x32, 0x53, 0x82, 0x89, 0xEF, 0x7D,
                        0x06, 0xB5, 0x06, 0xA4, 0xFD, 0x5B, 0xE9, 0xC9,
                        0xC1, 0xCA, 0x44, 0xE8, 0x05, 0xFF, 0xCB, 0x6F,
                        0x4D, 0x7F, 0xE9, 0x17, 0x12, 0xFE, 0xBB, 0xAC,
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
                        0xD8, 0xF5, 0x32, 0x53, 0x82, 0x89, 0xEF, 0x7D,
                        0x06, 0xB5, 0x06, 0xA4, 0xFD, 0x5B, 0xE9, 0xC9,
                        0xD3, 0xAA, 0x33, 0x5B, 0x93, 0xC2, 0x3D, 0x96,
                        0xFD, 0x89, 0xB1, 0x8C, 0x47, 0x75, 0x65, 0xA8,
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
                        0xC3, 0x03, 0x87, 0xCD, 0x79, 0x19, 0xB1, 0xC3,
                        0x50, 0x2C, 0x9D, 0x7B, 0x1F, 0x8A, 0xBE, 0x0F,
                        0x82, 0x8D, 0x60, 0xDC, 0x44, 0x26, 0xCF, 0xDE,
                        0xC9, 0x54, 0x33, 0x47, 0xE2, 0x9E, 0xF0, 0x8C,
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
                        0xC3, 0x03, 0x87, 0xCD, 0x79, 0x19, 0xB1, 0xC3,
                        0x50, 0x2C, 0x9D, 0x7B, 0x1F, 0x8A, 0xBE, 0x0F,
                        0x49, 0x39, 0x1B, 0x69, 0xA1, 0xF3, 0x66, 0xE4,
                        0x3E, 0x40, 0x51, 0xB8, 0x05, 0x60, 0xDC, 0xFD,
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
                        0xC3, 0x03, 0x87, 0xCD, 0x79, 0x19, 0xB1, 0xC3,
                        0x50, 0x2C, 0x9D, 0x7B, 0x1F, 0x8A, 0xBE, 0x0F,
                        0xCD, 0x0D, 0xCD, 0xEA, 0xA2, 0x1F, 0xC1, 0xC3,
                        0x81, 0xEE, 0x8A, 0x63, 0x94, 0x5F, 0x85, 0x43,
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
                        0xC3, 0x03, 0x87, 0xCD, 0x79, 0x19, 0xB1, 0xC3,
                        0x50, 0x2C, 0x9D, 0x7B, 0x1F, 0x8A, 0xBE, 0x0F,
                        0x9C, 0xE4, 0x0D, 0x2F, 0xCD, 0x82, 0x25, 0x0E,
                        0x13, 0xAB, 0x4B, 0x6B, 0xC0, 0x9A, 0x21, 0x2E,
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
                        0x6D, 0xE5, 0xF6, 0x07, 0xAB, 0x7E, 0xB8, 0x20,
                        0x2F, 0x39, 0x57, 0x70, 0x3B, 0x04, 0xE8, 0xB5,
                    },

                    PaddingMode.PKCS7,
                    CipherMode.ECB,
                };
            }
        }
    }
}
