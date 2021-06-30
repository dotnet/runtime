// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    public partial class DesCipherTests
    {
        private static byte[] s_desOneShotKey = new byte[]
            {
                0x74, 0x4B, 0x93, 0x3A, 0x96, 0x33, 0x61, 0xD6
            };

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void EcbRoundtrip(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (DES des = DESFactory.Create())
            {
                des.Key = s_desOneShotKey;

                byte[] encrypted = des.EncryptEcb(plaintext, padding);
                byte[] decrypted = des.DecryptEcb(encrypted, padding);

                if (padding == PaddingMode.Zeros)
                {
                    Assert.Equal(plaintext, decrypted[..plaintext.Length]);
                    AssertFilledWith(0, plaintext.AsSpan(plaintext.Length));
                }
                else
                {
                    Assert.Equal(plaintext, decrypted);
                }

                decrypted = des.DecryptEcb(ciphertext, padding);
                encrypted = des.EncryptEcb(decrypted, padding);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = des.BlockSize / 8;
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

            using (DES des = DESFactory.Create())
            {
                des.Key = s_desOneShotKey;

                Span<byte> destinationBuffer = new byte[plaintext.Length - 1];

                bool result = des.TryDecryptEcb(ciphertext, destinationBuffer, padding, out int bytesWritten);
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

            using (DES des = DESFactory.Create())
            {
                des.Key = s_desOneShotKey;

                Span<byte> destinationBuffer = new byte[ciphertext.Length - 1];

                bool result = des.TryEncryptEcb(plaintext, destinationBuffer, padding, out int bytesWritten);
                Assert.False(result, "TryDecryptEcb");
                Assert.Equal(0, bytesWritten);
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void TryDecryptEcb_DestinationJustRight(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (DES des = DESFactory.Create())
            {
                des.Key = s_desOneShotKey;

                int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;
                Span<byte> destinationBuffer = new byte[expectedPlaintextSize];

                bool result = des.TryDecryptEcb(ciphertext, destinationBuffer, padding, out int bytesWritten);
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
            using (DES des = DESFactory.Create())
            {
                des.Key = s_desOneShotKey;

                int expectedCiphertextSize = des.GetCiphertextLengthEcb(plaintext.Length, padding);
                Span<byte> destinationBuffer = new byte[expectedCiphertextSize];

                bool result = des.TryEncryptEcb(plaintext, destinationBuffer, padding, out int bytesWritten);
                Assert.True(result, "TryEncryptEcb");
                Assert.Equal(expectedCiphertextSize, bytesWritten);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = des.BlockSize / 8;
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
            using (DES des = DESFactory.Create())
            {
                des.Key = s_desOneShotKey;
                int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;

                Span<byte> largeBuffer = new byte[expectedPlaintextSize + 10];
                Span<byte> destinationBuffer = largeBuffer.Slice(0, expectedPlaintextSize);
                largeBuffer.Fill(0xCC);

                bool result = des.TryDecryptEcb(
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
            using (DES des = DESFactory.Create())
            {
                des.Key = s_desOneShotKey;

                Span<byte> largeBuffer = new byte[ciphertext.Length + 10];
                Span<byte> destinationBuffer = largeBuffer.Slice(0, ciphertext.Length);
                largeBuffer.Fill(0xCC);

                bool result = des.TryEncryptEcb(
                    plaintext,
                    destinationBuffer,
                    padding,
                    out int bytesWritten);

                Assert.True(result, "TryEncryptEcb");
                Assert.Equal(destinationBuffer.Length, bytesWritten);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = des.BlockSize / 8;
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
                using (DES des = DESFactory.Create())
                {
                    des.Key = s_desOneShotKey;

                    int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;
                    int destinationSize = Math.Max(expectedPlaintextSize, ciphertext.Length) + Math.Max(plaintextOffset, ciphertextOffset);
                    Span<byte> buffer = new byte[destinationSize];
                    Span<byte> destinationBuffer = buffer.Slice(plaintextOffset, expectedPlaintextSize);
                    Span<byte> ciphertextBuffer = buffer.Slice(ciphertextOffset, ciphertext.Length);
                    ciphertext.AsSpan().CopyTo(ciphertextBuffer);

                    bool result = des.TryDecryptEcb(ciphertextBuffer, destinationBuffer, padding, out int bytesWritten);
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
                using (DES des = DESFactory.Create())
                {
                    des.Key = s_desOneShotKey;

                    int destinationSize = ciphertext.Length + Math.Max(plaintextOffset, ciphertextOffset);
                    Span<byte> buffer = new byte[destinationSize];
                    Span<byte> destinationBuffer = buffer.Slice(ciphertextOffset, ciphertext.Length);
                    Span<byte> plaintextBuffer = buffer.Slice(plaintextOffset, plaintext.Length);
                    plaintext.AsSpan().CopyTo(plaintextBuffer);

                    bool result = des.TryEncryptEcb(plaintextBuffer, destinationBuffer, padding, out int bytesWritten);
                    Assert.True(result, "TryEncryptEcb");
                    Assert.Equal(destinationBuffer.Length, bytesWritten);

                    if (padding == PaddingMode.ISO10126)
                    {
                        int blockSizeBytes = des.BlockSize / 8;
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
            using (DES des = DESFactory.Create())
            {
                des.Key = s_desOneShotKey;
                byte[] decrypted = des.DecryptEcb(ciphertext.AsSpan(), padding);

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
            using (DES des = DESFactory.Create())
            {
                des.Key = s_desOneShotKey;
                byte[] encrypted = des.EncryptEcb(plaintext.AsSpan(), padding);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = des.BlockSize / 8;
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
            using (DES des = DESFactory.Create())
            {
                des.Key = s_desOneShotKey;
                byte[] decrypted = des.DecryptEcb(ciphertext, padding);

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
            using (DES des = DESFactory.Create())
            {
                des.Key = s_desOneShotKey;
                byte[] encrypted = des.EncryptEcb(plaintext, padding);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = des.BlockSize / 8;
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
                        0xEE, 0x8B, 0xA7, 0xEE, 0x11, 0x84, 0x1D, 0xA2,
                        0xC4, 0x16, 0xB4, 0x05, 0x83, 0xA0, 0x60, 0x37,
                        0xED, 0xD9, 0xE3, 0xFC, 0xC6, 0x55, 0xDC, 0x32,
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
                        0xEE, 0x8B, 0xA7, 0xEE, 0x11, 0x84, 0x1D, 0xA2,
                        0xC4, 0x16, 0xB4, 0x05, 0x83, 0xA0, 0x60, 0x37,
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
                        0xEE, 0x8B, 0xA7, 0xEE, 0x11, 0x84, 0x1D, 0xA2,
                        0xC4, 0x16, 0xB4, 0x05, 0x83, 0xA0, 0x60, 0x37,
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
                        0xEE, 0x8B, 0xA7, 0xEE, 0x11, 0x84, 0x1D, 0xA2,
                        0xC4, 0x16, 0xB4, 0x05, 0x83, 0xA0, 0x60, 0x37,
                        0xEC, 0x52, 0xA1, 0x7E, 0x52, 0x54, 0x6E, 0x9E,
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
                        0xEE, 0x8B, 0xA7, 0xEE, 0x11, 0x84, 0x1D, 0xA2,
                        0xC4, 0x16, 0xB4, 0x05, 0x83, 0xA0, 0x60, 0x37,
                        0x44, 0x4C, 0xA5, 0xC2, 0xCC, 0x54, 0xAC, 0xF9,
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
                        0xEA, 0x91, 0x68, 0xFE, 0x02, 0xFE, 0x57, 0x6F,
                        0x60, 0x17, 0x05, 0xD5, 0x94, 0xA2, 0xF8, 0xE2,
                        0x60, 0x8E, 0xC3, 0xB8, 0x09, 0x84, 0xCF, 0x3B,
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
                        0xEA, 0x91, 0x68, 0xFE, 0x02, 0xFE, 0x57, 0x6F,
                        0x60, 0x17, 0x05, 0xD5, 0x94, 0xA2, 0xF8, 0xE2,
                        0xE7, 0xA4, 0x10, 0xF1, 0x7B, 0xFF, 0x32, 0x4A,
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
                        0xEA, 0x91, 0x68, 0xFE, 0x02, 0xFE, 0x57, 0x6F,
                        0x60, 0x17, 0x05, 0xD5, 0x94, 0xA2, 0xF8, 0xE2,
                        0x92, 0x9A, 0x36, 0xFE, 0xA4, 0xB3, 0xEC, 0xA0,
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
                        0xEA, 0x91, 0x68, 0xFE, 0x02, 0xFE, 0x57, 0x6F,
                        0x60, 0x17, 0x05, 0xD5, 0x94, 0xA2, 0xF8, 0xE2,
                        0xDB, 0x86, 0xA4, 0xAB, 0xDE, 0x05, 0xE4, 0xE7,
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
                        0xED, 0xD9, 0xE3, 0xFC, 0xC6, 0x55, 0xDC, 0x32,
                    },

                    PaddingMode.PKCS7,
                };
            }
        }
    }
}
