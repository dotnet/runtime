// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public partial class AesCipherTests
    {
        private static byte[] s_aes128OneShotKey =
            new byte[] { 0x00, 0x01, 0x02, 0x03, 0x05, 0x06, 0x07, 0x08, 0x0A, 0x0B, 0x0C, 0x0D, 0x0F, 0x10, 0x11, 0x12 };

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void EcbRoundtrip(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.Key = s_aes128OneShotKey;

                // Even though we have set the instance to use CFB, the Ecb one shots should
                // always be done in ECB.
                aes.FeedbackSize = 8;
                aes.Mode = CipherMode.CFB;
                aes.Padding = padding == PaddingMode.None ? PaddingMode.PKCS7 : PaddingMode.None;

                byte[] encrypted = aes.EncryptEcb(plaintext, padding);
                byte[] decrypted = aes.DecryptEcb(encrypted, padding);

                if (padding == PaddingMode.Zeros)
                {
                    Assert.Equal(plaintext, decrypted[..plaintext.Length]);
                    AssertFilledWith(0, plaintext.AsSpan(plaintext.Length));
                }
                else
                {
                    Assert.Equal(plaintext, decrypted);
                }

                decrypted = aes.DecryptEcb(ciphertext, padding);
                encrypted = aes.EncryptEcb(decrypted, padding);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = aes.BlockSize / 8;
                    Assert.Equal(ciphertext[..^blockSizeBytes], encrypted[..^blockSizeBytes]);
                }
                else
                {
                    Assert.Equal(ciphertext, encrypted);
                }
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void TryDecryptEcb_DestinationTooSmall(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            if (plaintext.Length == 0)
            {
                // Can't have a ciphertext length shorter than zero.
                return;
            }

            using (Aes aes = AesFactory.Create())
            {
                aes.Key = s_aes128OneShotKey;

                Span<byte> destinationBuffer = new byte[plaintext.Length - 1];

                bool result = aes.TryDecryptEcb(ciphertext, destinationBuffer, padding, out int bytesWritten);
                Assert.False(result, "TryDecryptEcb");
                Assert.Equal(0, bytesWritten);
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void TryEncryptEcb_DestinationTooSmall(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            if (ciphertext.Length == 0)
            {
                // Can't have a too small buffer for zero.
                return;
            }

            using (Aes aes = AesFactory.Create())
            {
                aes.Key = s_aes128OneShotKey;

                Span<byte> destinationBuffer = new byte[ciphertext.Length - 1];

                bool result = aes.TryEncryptEcb(plaintext, destinationBuffer, padding, out int bytesWritten);
                Assert.False(result, "TryDecryptEcb");
                Assert.Equal(0, bytesWritten);
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void TryDecryptEcb_DestinationJustRight(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.Key = s_aes128OneShotKey;

                int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;
                Span<byte> destinationBuffer = new byte[expectedPlaintextSize];

                bool result = aes.TryDecryptEcb(ciphertext, destinationBuffer, padding, out int bytesWritten);
                Assert.True(result, "TryDecryptEcb");
                Assert.Equal(destinationBuffer.Length, bytesWritten);

                if (padding == PaddingMode.Zeros)
                {
                    Assert.Equal(plaintext, destinationBuffer.Slice(0, plaintext.Length).ToArray());
                    AssertFilledWith(0, destinationBuffer.Slice(plaintext.Length));
                }
                else
                {
                    Assert.Equal(plaintext, destinationBuffer.ToArray());
                }
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void TryEncryptEcb_DestinationJustRight(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.Key = s_aes128OneShotKey;

                int expectedCiphertextSize = aes.GetCiphertextLengthEcb(plaintext.Length, padding);
                Span<byte> destinationBuffer = new byte[expectedCiphertextSize];

                bool result = aes.TryEncryptEcb(plaintext, destinationBuffer, padding, out int bytesWritten);
                Assert.True(result, "TryEncryptEcb");
                Assert.Equal(expectedCiphertextSize, bytesWritten);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = aes.BlockSize / 8;
                    // Padding is random so we can't validate the last block.
                    Assert.Equal(ciphertext[..^blockSizeBytes], destinationBuffer[..^blockSizeBytes].ToArray());
                }
                else
                {
                    Assert.Equal(ciphertext, destinationBuffer.ToArray());
                }
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void TryDecryptEcb_DestinationLarger(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.Key = s_aes128OneShotKey;
                int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;

                Span<byte> largeBuffer = new byte[expectedPlaintextSize + 10];
                Span<byte> destinationBuffer = largeBuffer.Slice(0, expectedPlaintextSize);
                largeBuffer.Fill(0xCC);

                bool result = aes.TryDecryptEcb(
                    ciphertext,
                    destinationBuffer,
                    padding,
                    out int bytesWritten);

                Assert.True(result, "TryDecryptEcb");
                Assert.Equal(destinationBuffer.Length, bytesWritten);

                if (padding == PaddingMode.Zeros)
                {
                    Assert.Equal(plaintext, destinationBuffer.Slice(0, plaintext.Length).ToArray());
                    AssertFilledWith(0, destinationBuffer.Slice(plaintext.Length));
                }
                else
                {
                    Assert.Equal(plaintext, destinationBuffer.ToArray());
                }

                Span<byte> excess = largeBuffer.Slice(destinationBuffer.Length);
                AssertFilledWith(0xCC, excess);
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void TryEncryptEcb_DestinationLarger(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.Key = s_aes128OneShotKey;

                Span<byte> largeBuffer = new byte[ciphertext.Length + 10];
                Span<byte> destinationBuffer = largeBuffer.Slice(0, ciphertext.Length);
                largeBuffer.Fill(0xCC);

                bool result = aes.TryEncryptEcb(
                    plaintext,
                    destinationBuffer,
                    padding,
                    out int bytesWritten);

                Assert.True(result, "TryEncryptEcb");
                Assert.Equal(destinationBuffer.Length, bytesWritten);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = aes.BlockSize / 8;
                    Assert.Equal(ciphertext[..^blockSizeBytes], destinationBuffer[..^blockSizeBytes].ToArray());
                }
                else
                {
                    Assert.Equal(ciphertext, destinationBuffer.ToArray());
                }

                AssertFilledWith(0xCC, largeBuffer.Slice(ciphertext.Length));
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void TryDecryptEcb_Overlaps(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            (int plaintextOffset, int ciphertextOffset)[] offsets =
            {
                (0, 0), (8, 0), (0, 8), (8, 8),
            };

            foreach ((int plaintextOffset, int ciphertextOffset) in offsets)
            {
                using (Aes aes = AesFactory.Create())
                {
                    aes.Key = s_aes128OneShotKey;

                    int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;
                    int destinationSize = Math.Max(expectedPlaintextSize, ciphertext.Length) + Math.Max(plaintextOffset, ciphertextOffset);
                    Span<byte> buffer = new byte[destinationSize];
                    Span<byte> destinationBuffer = buffer.Slice(plaintextOffset, expectedPlaintextSize);
                    Span<byte> ciphertextBuffer = buffer.Slice(ciphertextOffset, ciphertext.Length);
                    ciphertext.AsSpan().CopyTo(ciphertextBuffer);

                    bool result = aes.TryDecryptEcb(ciphertextBuffer, destinationBuffer, padding, out int bytesWritten);
                    Assert.True(result, "TryDecryptEcb");
                    Assert.Equal(destinationBuffer.Length, bytesWritten);

                    if (padding == PaddingMode.Zeros)
                    {
                        Assert.Equal(plaintext, destinationBuffer.Slice(0, plaintext.Length).ToArray());
                        AssertFilledWith(0, destinationBuffer.Slice(plaintext.Length));
                    }
                    else
                    {
                        Assert.Equal(plaintext, destinationBuffer.ToArray());
                        Assert.True(destinationBuffer.Overlaps(ciphertextBuffer) || plaintext.Length == 0 || ciphertext.Length == 0);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void TryEncryptEcb_Overlaps(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            (int plaintextOffset, int ciphertextOffset)[] offsets =
            {
                (0, 0), (8, 0), (0, 8), (8, 8),
            };

            foreach ((int plaintextOffset, int ciphertextOffset) in offsets)
            {
                using (Aes aes = AesFactory.Create())
                {
                    aes.Key = s_aes128OneShotKey;

                    int destinationSize = ciphertext.Length + Math.Max(plaintextOffset, ciphertextOffset);
                    Span<byte> buffer = new byte[destinationSize];
                    Span<byte> destinationBuffer = buffer.Slice(ciphertextOffset, ciphertext.Length);
                    Span<byte> plaintextBuffer = buffer.Slice(plaintextOffset, plaintext.Length);
                    plaintext.AsSpan().CopyTo(plaintextBuffer);

                    bool result = aes.TryEncryptEcb(plaintextBuffer, destinationBuffer, padding, out int bytesWritten);
                    Assert.True(result, "TryEncryptEcb");
                    Assert.Equal(destinationBuffer.Length, bytesWritten);

                    if (padding == PaddingMode.ISO10126)
                    {
                        int blockSizeBytes = aes.BlockSize / 8;
                        Assert.Equal(ciphertext[..^blockSizeBytes], destinationBuffer[..^blockSizeBytes].ToArray());
                    }
                    else
                    {
                        Assert.Equal(ciphertext, destinationBuffer.ToArray());
                        Assert.True(destinationBuffer.Overlaps(plaintextBuffer) || plaintext.Length == 0 || ciphertext.Length == 0);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void DecryptEcb_Span(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.Key = s_aes128OneShotKey;
                byte[] decrypted = aes.DecryptEcb(ciphertext.AsSpan(), padding);

                if (padding == PaddingMode.Zeros)
                {
                    Assert.Equal(plaintext, decrypted.AsSpan(0, plaintext.Length).ToArray());
                    AssertFilledWith(0, decrypted.AsSpan(plaintext.Length));
                }
                else
                {
                    Assert.Equal(plaintext, decrypted);
                }
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void EncryptEcb_Span(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.Key = s_aes128OneShotKey;
                byte[] encrypted = aes.EncryptEcb(plaintext.AsSpan(), padding);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = aes.BlockSize / 8;
                    Assert.Equal(ciphertext[..^blockSizeBytes], encrypted[..^blockSizeBytes]);
                }
                else
                {
                    Assert.Equal(ciphertext, encrypted);
                }
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void DecryptEcb_Array(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.Key = s_aes128OneShotKey;
                byte[] decrypted = aes.DecryptEcb(ciphertext, padding);

                if (padding == PaddingMode.Zeros)
                {
                    Assert.Equal(plaintext, decrypted.AsSpan(0, plaintext.Length).ToArray());
                    AssertFilledWith(0, decrypted.AsSpan(plaintext.Length));
                }
                else
                {
                    Assert.Equal(plaintext, decrypted);
                }
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void EncryptEcb_Array(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.Key = s_aes128OneShotKey;
                byte[] encrypted = aes.EncryptEcb(plaintext, padding);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = aes.BlockSize / 8;
                    Assert.Equal(ciphertext[..^blockSizeBytes], encrypted[..^blockSizeBytes]);
                }
                else
                {
                    Assert.Equal(ciphertext, encrypted);
                }
            }
        }

        private static void AssertFilledWith(byte value, ReadOnlySpan<byte> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                Assert.Equal(value, span[i]);
            }
        }

        public static IEnumerable<object[]> EcbTestCases
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
                        0xD8, 0xF5, 0x32, 0x53, 0x82, 0x89, 0xEF, 0x7D,
                        0x06, 0xB5, 0x06, 0xA4, 0xFD, 0x5B, 0xE9, 0xC9,
                        0x6D, 0xE5, 0xF6, 0x07, 0xAB, 0x7E, 0xB8, 0x20,
                        0x2F, 0x39, 0x57, 0x70, 0x3B, 0x04, 0xE8, 0xB5,
                    },

                    PaddingMode.PKCS7,
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
                };

                yield return new object[]
                {
                    // plaintext
                    Array.Empty<byte>(),

                    // ciphertext
                    Array.Empty<byte>(),

                    PaddingMode.Zeros,
                };

                yield return new object[]
                {
                    // plaintext
                    Array.Empty<byte>(),

                    // ciphertext
                    Array.Empty<byte>(),

                    PaddingMode.None,
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
                };
            }
        }
    }
}
