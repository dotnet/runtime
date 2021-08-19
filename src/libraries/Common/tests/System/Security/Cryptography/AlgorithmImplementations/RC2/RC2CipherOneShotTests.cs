// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography;

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class RC2CipherOneShotTests : SymmetricOneShotBase
    {
        protected override byte[] Key => new byte[]
            {
                0x83, 0x2F, 0x81, 0x1B, 0x61, 0x02, 0xCC, 0x8F,
                0x2F, 0x78, 0x10, 0x68, 0x06, 0xA6, 0x35, 0x50,
            };

        protected override byte[] IV => new byte[]
            {
                0x01, 0x01, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

        protected override SymmetricAlgorithm CreateAlgorithm() => RC2Factory.Create();

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

        [Fact]
        public void EncryptOneShot_CfbNotSupported()
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                Assert.ThrowsAny<CryptographicException>(() =>
                    alg.TryEncryptCfb(ReadOnlySpan<byte>.Empty, IV, Span<byte>.Empty, out _));
            }
        }

        [Fact]
        public void DecryptOneShot_CfbNotSupported()
        {
            using (SymmetricAlgorithm alg = CreateAlgorithm())
            {
                Assert.ThrowsAny<CryptographicException>(() =>
                    alg.TryDecryptCfb(ReadOnlySpan<byte>.Empty, IV, Span<byte>.Empty, out _));
            }
        }

        public static IEnumerable<object[]> TestCases
        {
            get
            {
                // plaintext that is block aligned
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
                        0x9E, 0x33, 0x06, 0xDE, 0x21, 0xA7, 0xC6, 0x6C,
                        0x59, 0x21, 0xEE, 0x34, 0x9F, 0x28, 0x1D, 0x0F,
                        0xED, 0xFC, 0x8E, 0xD8, 0x85, 0x94, 0xC9, 0x20,
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
                        0x9E, 0x33, 0x06, 0xDE, 0x21, 0xA7, 0xC6, 0x6C,
                        0x59, 0x21, 0xEE, 0x34, 0x9F, 0x28, 0x1D, 0x0F,
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
                        0x9E, 0x33, 0x06, 0xDE, 0x21, 0xA7, 0xC6, 0x6C,
                        0x59, 0x21, 0xEE, 0x34, 0x9F, 0x28, 0x1D, 0x0F,
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
                        0x9E, 0x33, 0x06, 0xDE, 0x21, 0xA7, 0xC6, 0x6C,
                        0x59, 0x21, 0xEE, 0x34, 0x9F, 0x28, 0x1D, 0x0F,
                        0xF7, 0x6D, 0x66, 0xD5, 0x32, 0xA3, 0x1F, 0xB1,
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
                        0x9E, 0x33, 0x06, 0xDE, 0x21, 0xA7, 0xC6, 0x6C,
                        0x59, 0x21, 0xEE, 0x34, 0x9F, 0x28, 0x1D, 0x0F,
                        0x0E, 0x76, 0x86, 0x35, 0x65, 0x52, 0x48, 0x07,
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
                        0x1E, 0xEC, 0x97, 0x05, 0xAC, 0xAB, 0x92, 0x13,
                        0x69, 0x53, 0x97, 0x00, 0x00, 0xD0, 0x1E, 0x3D,
                        0xA9, 0x9E, 0xA2, 0x1D, 0x16, 0x8E, 0xCF, 0x1A,
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
                        0x1E, 0xEC, 0x97, 0x05, 0xAC, 0xAB, 0x92, 0x13,
                        0x69, 0x53, 0x97, 0x00, 0x00, 0xD0, 0x1E, 0x3D,
                        0x27, 0xF6, 0x57, 0x70, 0x65, 0x1D, 0x58, 0x87,
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
                        0x1E, 0xEC, 0x97, 0x05, 0xAC, 0xAB, 0x92, 0x13,
                        0x69, 0x53, 0x97, 0x00, 0x00, 0xD0, 0x1E, 0x3D,
                        0x2F, 0xDD, 0xAC, 0xE4, 0xB0, 0x01, 0x4D, 0x75,
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
                        0x1E, 0xEC, 0x97, 0x05, 0xAC, 0xAB, 0x92, 0x13,
                        0x69, 0x53, 0x97, 0x00, 0x00, 0xD0, 0x1E, 0x3D,
                        0x01, 0x52, 0x7A, 0x20, 0x22, 0x94, 0xD4, 0x3D,
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
                        0x60, 0x4C, 0x99, 0xAF, 0xA4, 0xE1, 0xB0, 0x76,
                    },

                    PaddingMode.PKCS7,
                    CipherMode.CBC,
                };

                // plaintext that is block aligned
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
                        0x8A, 0x65, 0xEB, 0x8C, 0x62, 0x14, 0xDD, 0x83,
                        0x71, 0x0F, 0x1B, 0x21, 0xAD, 0x5F, 0xCD, 0xC1,
                        0x9D, 0x70, 0x70, 0x58, 0x47, 0x5A, 0xD0, 0xC8,
                    },

                    PaddingMode.PKCS7,
                    CipherMode.ECB,
                };

                // plaintext that is block aligned
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
                        0x8A, 0x65, 0xEB, 0x8C, 0x62, 0x14, 0xDD, 0x83,
                        0x71, 0x0F, 0x1B, 0x21, 0xAD, 0x5F, 0xCD, 0xC1,
                        0x9D, 0x70, 0x70, 0x58, 0x47, 0x5A, 0xD0, 0xC8,
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
                        0x8A, 0x65, 0xEB, 0x8C, 0x62, 0x14, 0xDD, 0x83,
                        0x71, 0x0F, 0x1B, 0x21, 0xAD, 0x5F, 0xCD, 0xC1,
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
                        0x8A, 0x65, 0xEB, 0x8C, 0x62, 0x14, 0xDD, 0x83,
                        0x71, 0x0F, 0x1B, 0x21, 0xAD, 0x5F, 0xCD, 0xC1,
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
                        0x8A, 0x65, 0xEB, 0x8C, 0x62, 0x14, 0xDD, 0x83,
                        0x71, 0x0F, 0x1B, 0x21, 0xAD, 0x5F, 0xCD, 0xC1,
                        0x72, 0x8A, 0x57, 0x94, 0x2D, 0x79, 0xBD, 0xAA,
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
                        0x8A, 0x65, 0xEB, 0x8C, 0x62, 0x14, 0xDD, 0x83,
                        0x71, 0x0F, 0x1B, 0x21, 0xAD, 0x5F, 0xCD, 0xC1,
                        0xEB, 0x5E, 0x2E, 0xB9, 0x1A, 0x1E, 0x1B, 0xE4,
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
                        0x14, 0x76, 0x59, 0x63, 0xBD, 0x9E, 0xF3, 0x2E,
                        0xF6, 0xA1, 0x05, 0x03, 0x44, 0x59, 0xF5, 0x88,
                        0xF2, 0x94, 0x11, 0xA3, 0xE8, 0xAD, 0xA7, 0xE6,
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
                        0x14, 0x76, 0x59, 0x63, 0xBD, 0x9E, 0xF3, 0x2E,
                        0xF6, 0xA1, 0x05, 0x03, 0x44, 0x59, 0xF5, 0x88,
                        0xE3, 0xB2, 0x3D, 0xAA, 0x91, 0x6A, 0xD0, 0x06,
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
                        0x14, 0x76, 0x59, 0x63, 0xBD, 0x9E, 0xF3, 0x2E,
                        0xF6, 0xA1, 0x05, 0x03, 0x44, 0x59, 0xF5, 0x88,
                        0x17, 0x97, 0x3A, 0x77, 0x69, 0x5E, 0x79, 0xE9,
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
                        0x14, 0x76, 0x59, 0x63, 0xBD, 0x9E, 0xF3, 0x2E,
                        0xF6, 0xA1, 0x05, 0x03, 0x44, 0x59, 0xF5, 0x88,
                        0x22, 0xC0, 0x50, 0x52, 0x56, 0x5A, 0x15, 0xFD,
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
                        0x9D, 0x70, 0x70, 0x58, 0x47, 0x5A, 0xD0, 0xC8,
                    },

                    PaddingMode.PKCS7,
                    CipherMode.ECB,
                };
            }
        }
    }
}
