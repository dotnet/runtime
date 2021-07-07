// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    public partial class TripleDESCipherTests
    {
        private static byte[] s_tdes192OneShotKey = new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x05, 0x06, 0x07, 0x08,
                0x0A, 0x0B, 0x0C, 0x0D, 0x0F, 0x10, 0x11, 0x12,
                0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0xA0,
            };

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void EcbRoundtrip(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Key = s_tdes192OneShotKey;

                // Even though we have set the instance to use CFB, the Ecb one shots should
                // always be done in ECB.
                tdes.FeedbackSize = 8;
                tdes.Mode = CipherMode.CFB;
                tdes.Padding = padding == PaddingMode.None ? PaddingMode.PKCS7 : PaddingMode.None;

                byte[] encrypted = tdes.EncryptEcb(plaintext, padding);
                byte[] decrypted = tdes.DecryptEcb(encrypted, padding);

                if (padding == PaddingMode.Zeros)
                {
                    Assert.Equal(plaintext, decrypted[..plaintext.Length]);
                    AssertFilledWith(0, plaintext.AsSpan(plaintext.Length));
                }
                else
                {
                    Assert.Equal(plaintext, decrypted);
                }

                decrypted = tdes.DecryptEcb(ciphertext, padding);
                encrypted = tdes.EncryptEcb(decrypted, padding);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = tdes.BlockSize / 8;
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

            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Key = s_tdes192OneShotKey;

                Span<byte> destinationBuffer = new byte[plaintext.Length - 1];

                bool result = tdes.TryDecryptEcb(ciphertext, destinationBuffer, padding, out int bytesWritten);
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

            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Key = s_tdes192OneShotKey;

                Span<byte> destinationBuffer = new byte[ciphertext.Length - 1];

                bool result = tdes.TryEncryptEcb(plaintext, destinationBuffer, padding, out int bytesWritten);
                Assert.False(result, "TryDecryptEcb");
                Assert.Equal(0, bytesWritten);
            }
        }

        [Theory]
        [MemberData(nameof(EcbTestCases))]
        public static void TryDecryptEcb_DestinationJustRight(byte[] plaintext, byte[] ciphertext, PaddingMode padding)
        {
            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Key = s_tdes192OneShotKey;

                int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;
                Span<byte> destinationBuffer = new byte[expectedPlaintextSize];

                bool result = tdes.TryDecryptEcb(ciphertext, destinationBuffer, padding, out int bytesWritten);
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
            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Key = s_tdes192OneShotKey;

                int expectedCiphertextSize = tdes.GetCiphertextLengthEcb(plaintext.Length, padding);
                Span<byte> destinationBuffer = new byte[expectedCiphertextSize];

                bool result = tdes.TryEncryptEcb(plaintext, destinationBuffer, padding, out int bytesWritten);
                Assert.True(result, "TryEncryptEcb");
                Assert.Equal(expectedCiphertextSize, bytesWritten);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = tdes.BlockSize / 8;
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
            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Key = s_tdes192OneShotKey;
                int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;

                Span<byte> largeBuffer = new byte[expectedPlaintextSize + 10];
                Span<byte> destinationBuffer = largeBuffer.Slice(0, expectedPlaintextSize);
                largeBuffer.Fill(0xCC);

                bool result = tdes.TryDecryptEcb(
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
            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Key = s_tdes192OneShotKey;

                Span<byte> largeBuffer = new byte[ciphertext.Length + 10];
                Span<byte> destinationBuffer = largeBuffer.Slice(0, ciphertext.Length);
                largeBuffer.Fill(0xCC);

                bool result = tdes.TryEncryptEcb(
                    plaintext,
                    destinationBuffer,
                    padding,
                    out int bytesWritten);

                Assert.True(result, "TryEncryptEcb");
                Assert.Equal(destinationBuffer.Length, bytesWritten);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = tdes.BlockSize / 8;
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
                using (TripleDES tdes = TripleDESFactory.Create())
                {
                    tdes.Key = s_tdes192OneShotKey;

                    int expectedPlaintextSize = padding == PaddingMode.Zeros ? ciphertext.Length : plaintext.Length;
                    int destinationSize = Math.Max(expectedPlaintextSize, ciphertext.Length) + Math.Max(plaintextOffset, ciphertextOffset);
                    Span<byte> buffer = new byte[destinationSize];
                    Span<byte> destinationBuffer = buffer.Slice(plaintextOffset, expectedPlaintextSize);
                    Span<byte> ciphertextBuffer = buffer.Slice(ciphertextOffset, ciphertext.Length);
                    ciphertext.AsSpan().CopyTo(ciphertextBuffer);

                    bool result = tdes.TryDecryptEcb(ciphertextBuffer, destinationBuffer, padding, out int bytesWritten);
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
                using (TripleDES tdes = TripleDESFactory.Create())
                {
                    tdes.Key = s_tdes192OneShotKey;

                    int destinationSize = ciphertext.Length + Math.Max(plaintextOffset, ciphertextOffset);
                    Span<byte> buffer = new byte[destinationSize];
                    Span<byte> destinationBuffer = buffer.Slice(ciphertextOffset, ciphertext.Length);
                    Span<byte> plaintextBuffer = buffer.Slice(plaintextOffset, plaintext.Length);
                    plaintext.AsSpan().CopyTo(plaintextBuffer);

                    bool result = tdes.TryEncryptEcb(plaintextBuffer, destinationBuffer, padding, out int bytesWritten);
                    Assert.True(result, "TryEncryptEcb");
                    Assert.Equal(destinationBuffer.Length, bytesWritten);

                    if (padding == PaddingMode.ISO10126)
                    {
                        int blockSizeBytes = tdes.BlockSize / 8;
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
            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Key = s_tdes192OneShotKey;
                byte[] decrypted = tdes.DecryptEcb(ciphertext.AsSpan(), padding);

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
            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Key = s_tdes192OneShotKey;
                byte[] encrypted = tdes.EncryptEcb(plaintext.AsSpan(), padding);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = tdes.BlockSize / 8;
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
            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Key = s_tdes192OneShotKey;
                byte[] decrypted = tdes.DecryptEcb(ciphertext, padding);

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
            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Key = s_tdes192OneShotKey;
                byte[] encrypted = tdes.EncryptEcb(plaintext, padding);

                if (padding == PaddingMode.ISO10126)
                {
                    int blockSizeBytes = tdes.BlockSize / 8;
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
                        0x2C, 0xE7, 0xF1, 0x5C, 0x7B, 0xA8, 0x40, 0x0C,
                        0x1A, 0x09, 0xDC, 0x63, 0x43, 0xC9, 0x1A, 0x63,
                        0x65, 0xE4, 0x9C, 0xD3, 0xE6, 0xBE, 0xB8, 0x40,
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
                        0x2C, 0xE7, 0xF1, 0x5C, 0x7B, 0xA8, 0x40, 0x0C,
                        0x1A, 0x09, 0xDC, 0x63, 0x43, 0xC9, 0x1A, 0x63,
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
                        0x2C, 0xE7, 0xF1, 0x5C, 0x7B, 0xA8, 0x40, 0x0C,
                        0x1A, 0x09, 0xDC, 0x63, 0x43, 0xC9, 0x1A, 0x63,
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
                        0x2C, 0xE7, 0xF1, 0x5C, 0x7B, 0xA8, 0x40, 0x0C,
                        0x1A, 0x09, 0xDC, 0x63, 0x43, 0xC9, 0x1A, 0x63,
                        0x34, 0xE6, 0x86, 0x6D, 0x94, 0x2E, 0x98, 0x0F,
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
                        0x2C, 0xE7, 0xF1, 0x5C, 0x7B, 0xA8, 0x40, 0x0C,
                        0x1A, 0x09, 0xDC, 0x63, 0x43, 0xC9, 0x1A, 0x63,
                        0x5E, 0xEE, 0x73, 0xBB, 0x94, 0xED, 0x29, 0x7A,
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
                        0xD5, 0xD5, 0x80, 0x3F, 0xC3, 0x7E, 0x4A, 0xE4,
                        0xF2, 0x93, 0x9B, 0xC3, 0xDC, 0x4F, 0xA0, 0x23,
                        0xB1, 0x3D, 0x05, 0x93, 0x98, 0xE6, 0x2C, 0xDF,
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
                        0xD5, 0xD5, 0x80, 0x3F, 0xC3, 0x7E, 0x4A, 0xE4,
                        0xF2, 0x93, 0x9B, 0xC3, 0xDC, 0x4F, 0xA0, 0x23,
                        0xC9, 0x52, 0x8F, 0xC1, 0x30, 0xC0, 0x7C, 0x63,
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
                        0xD5, 0xD5, 0x80, 0x3F, 0xC3, 0x7E, 0x4A, 0xE4,
                        0xF2, 0x93, 0x9B, 0xC3, 0xDC, 0x4F, 0xA0, 0x23,
                        0x6A, 0x97, 0x38, 0x85, 0x3B, 0x48, 0x81, 0x5E,
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
                        0xD5, 0xD5, 0x80, 0x3F, 0xC3, 0x7E, 0x4A, 0xE4,
                        0xF2, 0x93, 0x9B, 0xC3, 0xDC, 0x4F, 0xA0, 0x23,
                        0x33, 0x58, 0x09, 0x2C, 0xD8, 0xB5, 0x36, 0xAD,
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
                        0x65, 0xE4, 0x9C, 0xD3, 0xE6, 0xBE, 0xB8, 0x40,
                    },

                    PaddingMode.PKCS7,
                };
            }
        }
    }
}
