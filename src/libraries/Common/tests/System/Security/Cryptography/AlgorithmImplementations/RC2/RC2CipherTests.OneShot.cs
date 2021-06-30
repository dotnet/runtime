// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    using RC2 = System.Security.Cryptography.RC2;

    public partial class RC2CipherTests
    {
        private static byte[] s_rc2OneShotKey = new byte[]
            {
                0x83, 0x2F, 0x81, 0x1B, 0x61, 0x02, 0xCC, 0x8F,
                0x2F, 0x78, 0x10, 0x68, 0x06, 0xA6, 0x35, 0x50,
            };

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void EcbRoundtrip(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.Key = s_rc2OneShotKey;

                byte[] encrypted = rc2.EncryptEcb(plaintext, padding);
                byte[] decrypted = rc2.DecryptEcb(encrypted, padding);

                if (padding == PaddingMode.Zeros)
                {
                    Assert.Equal(plaintext, decrypted[..plaintext.Length]);
                    AssertFilledWith(0, plaintext.AsSpan(plaintext.Length));
                }
                else
                {
                    Assert.Equal(plaintext, decrypted);
                }

                decrypted = rc2.DecryptEcb(ciphertext, padding);
                encrypted = rc2.EncryptEcb(decrypted, padding);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = rc2.BlockSize / 8;
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

            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.Key = s_rc2OneShotKey;

                Span<byte> destinationBuffer = new byte[plaintext.Length - 1];

                bool result = rc2.TryDecryptEcb(ciphertext, destinationBuffer, padding, out int bytesWritten);
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

            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.Key = s_rc2OneShotKey;

                Span<byte> destinationBuffer = new byte[ciphertext.Length - 1];

                bool result = rc2.TryEncryptEcb(plaintext, destinationBuffer, padding, out int bytesWritten);
                Assert.False(result, "TryDecryptEcb");
                Assert.Equal(0, bytesWritten);
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void TryDecryptEcb_DestinationJustRight(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.Key = s_rc2OneShotKey;

                int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;
                Span<byte> destinationBuffer = new byte[expectedPlaintextSize];

                bool result = rc2.TryDecryptEcb(ciphertext, destinationBuffer, padding, out int bytesWritten);
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
            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.Key = s_rc2OneShotKey;

                int expectedCiphertextSize = rc2.GetCiphertextLengthEcb(plaintext.Length, padding);
                Span<byte> destinationBuffer = new byte[expectedCiphertextSize];

                bool result = rc2.TryEncryptEcb(plaintext, destinationBuffer, padding, out int bytesWritten);
                Assert.True(result, "TryEncryptEcb");
                Assert.Equal(expectedCiphertextSize, bytesWritten);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = rc2.BlockSize / 8;
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
            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.Key = s_rc2OneShotKey;
                int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;

                Span<byte> largeBuffer = new byte[expectedPlaintextSize + 10];
                Span<byte> destinationBuffer = largeBuffer.Slice(0, expectedPlaintextSize);
                largeBuffer.Fill(0xCC);

                bool result = rc2.TryDecryptEcb(
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
            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.Key = s_rc2OneShotKey;

                Span<byte> largeBuffer = new byte[ciphertext.Length + 10];
                Span<byte> destinationBuffer = largeBuffer.Slice(0, ciphertext.Length);
                largeBuffer.Fill(0xCC);

                bool result = rc2.TryEncryptEcb(
                    plaintext,
                    destinationBuffer,
                    padding,
                    out int bytesWritten);

                Assert.True(result, "TryEncryptEcb");
                Assert.Equal(destinationBuffer.Length, bytesWritten);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = rc2.BlockSize / 8;
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
                using (RC2 rc2 = RC2Factory.Create())
                {
                    rc2.Key = s_rc2OneShotKey;

                    int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;
                    int destinationSize = Math.Max(expectedPlaintextSize, ciphertext.Length) + Math.Max(plaintextOffset, ciphertextOffset);
                    Span<byte> buffer = new byte[destinationSize];
                    Span<byte> destinationBuffer = buffer.Slice(plaintextOffset, expectedPlaintextSize);
                    Span<byte> ciphertextBuffer = buffer.Slice(ciphertextOffset, ciphertext.Length);
                    ciphertext.AsSpan().CopyTo(ciphertextBuffer);

                    bool result = rc2.TryDecryptEcb(ciphertextBuffer, destinationBuffer, padding, out int bytesWritten);
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
                using (RC2 rc2 = RC2Factory.Create())
                {
                    rc2.Key = s_rc2OneShotKey;

                    int destinationSize = ciphertext.Length + Math.Max(plaintextOffset, ciphertextOffset);
                    Span<byte> buffer = new byte[destinationSize];
                    Span<byte> destinationBuffer = buffer.Slice(ciphertextOffset, ciphertext.Length);
                    Span<byte> plaintextBuffer = buffer.Slice(plaintextOffset, plaintext.Length);
                    plaintext.AsSpan().CopyTo(plaintextBuffer);

                    bool result = rc2.TryEncryptEcb(plaintextBuffer, destinationBuffer, padding, out int bytesWritten);
                    Assert.True(result, "TryEncryptEcb");
                    Assert.Equal(destinationBuffer.Length, bytesWritten);

                    if (padding == PaddingMode.ISO10126)
                    {
                        int blockSizeBytes = rc2.BlockSize / 8;
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
            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.Key = s_rc2OneShotKey;
                byte[] decrypted = rc2.DecryptEcb(ciphertext.AsSpan(), padding);

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
            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.Key = s_rc2OneShotKey;
                byte[] encrypted = rc2.EncryptEcb(plaintext.AsSpan(), padding);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = rc2.BlockSize / 8;
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
            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.Key = s_rc2OneShotKey;
                byte[] decrypted = rc2.DecryptEcb(ciphertext, padding);

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
            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.Key = s_rc2OneShotKey;
                byte[] encrypted = rc2.EncryptEcb(plaintext, padding);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = rc2.BlockSize / 8;
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
                        0x9D, 0x70, 0x70, 0x58, 0x47, 0x5A, 0xD0, 0xC8,
                    },

                    PaddingMode.PKCS7,
                };
            }
        }
    }
}
