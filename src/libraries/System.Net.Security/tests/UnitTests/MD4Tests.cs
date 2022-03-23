// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    public class MD4Tests
    {
        // MD4("") = 31d6cfe0d16ae931b73c59d7e0c089c0
        [Fact]
        public void TryEncrypt_Empty()
        {
            ReadOnlySpan<byte> input = new byte[0];
            ReadOnlySpan<byte> expected = new byte[] { 0x31, 0xd6, 0xcf, 0xe0, 0xd1, 0x6a, 0xe9, 0x31, 0xb7, 0x3c, 0x59, 0xd7, 0xe0, 0xc0, 0x89, 0xc0 };
            Verify(input, expected);
        }

        // // MD4("a") = bde52cb31de33e46245e05fbdbd6fb24
        [Fact]
        public void TryEncrypt_SingleLetter()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("a"));
            ReadOnlySpan<byte> expected = new byte[] { 0xbd, 0xe5, 0x2c, 0xb3, 0x1d, 0xe3, 0x3e, 0x46, 0x24, 0x5e, 0x05, 0xfb, 0xdb, 0xd6, 0xfb, 0x24 };
            Verify(input, expected);
        }

        // MD4("abc") = a448017aaf21d8525fc10ae87aa6729d
        [Fact]
        public void TryEncrypt_ThreeLetters()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("abc"));
            ReadOnlySpan<byte> expected = new byte[] { 0xa4, 0x48, 0x01, 0x7a, 0xaf, 0x21, 0xd8, 0x52, 0x5f, 0xc1, 0x0a, 0xe8, 0x7a, 0xa6, 0x72, 0x9d };
            Verify(input, expected);
        }

        // MD4("message digest") = d9130a8164549fe818874806e1c7014b
        [Fact]
        public void TryEncrypt_Phrase()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("message digest"));
            ReadOnlySpan<byte> expected = new byte[] { 0xd9, 0x13, 0x0a, 0x81, 0x64, 0x54, 0x9f, 0xe8, 0x18, 0x87, 0x48, 0x06, 0xe1, 0xc7, 0x01, 0x4b };
            Verify(input, expected);
        }

        // MD4("abcdefghijklmnopqrstuvwxyz") = d79e1c308aa5bbcdeea8ed63df412da9
        [Fact]
        public void TryEncrypt_AlphabetInLowercase()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("abcdefghijklmnopqrstuvwxyz"));
            ReadOnlySpan<byte> expected = new byte[] { 0xd7, 0x9e, 0x1c, 0x30, 0x8a, 0xa5, 0xbb, 0xcd, 0xee, 0xa8, 0xed, 0x63, 0xdf, 0x41, 0x2d, 0xa9 };
            Verify(input, expected);
        }

        // MD4("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789") =	043f8582f241db351ce627e153e7f0e4
        [Fact]
        public void TryEncrypt_AlphabetInUpperLowerCasesAndNumbers()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>((Encoding.Default.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")));
            ReadOnlySpan<byte> expected = new byte[] { 0x04, 0x3f, 0x85, 0x82, 0xf2, 0x41, 0xdb, 0x35, 0x1c, 0xe6, 0x27, 0xe1, 0x53, 0xe7, 0xf0, 0xe4 };
            Verify(input, expected);
        }

        // MD4("12345678901234567890123456789012345678901234567890123456789012345678901234567890") = e33b4ddc9c38f2199c3e7b164fcc0536
        [Fact]
        public void TryEncrypt_RepeatedSequenceOfNumbers()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("12345678901234567890123456789012345678901234567890123456789012345678901234567890"));
            ReadOnlySpan<byte> expected = new byte[] { 0xe3, 0x3b, 0x4d, 0xdc, 0x9c, 0x38, 0xf2, 0x19, 0x9c, 0x3e, 0x7b, 0x16, 0x4f, 0xcc, 0x05, 0x36 };
            Verify(input, expected);
        }

        // MD4("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012") = 14fdf2056bf88b3491c385d8ac4f48e6
        // 55 bytes (padLen == 56 - 55 => 1)
        [Fact]
        public void TryEncrypt_55bytes_HitsEdgeCaseForPaddingLength()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012"));
            ReadOnlySpan<byte> expected = new byte[] { 0x14, 0xfd, 0xf2, 0x05, 0x6b, 0xf8, 0x8b, 0x34, 0x91, 0xc3, 0x85, 0xd8, 0xac, 0x4f, 0x48, 0xe6 };
            Verify(input, expected);
        }

        // MD4("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123") = db837dbb6098a50a2d3974bc1cc76133
        // 56 bytes (padLen == 120 - 56 => 64)
        [Fact]
        public void TryEncrypt_56bytes_HitsEdgeCaseForPaddingLength()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123"));
            ReadOnlySpan<byte> expected = new byte[] { 0xdb, 0x83, 0x7d, 0xbb, 0x60, 0x98, 0xa5, 0x0a, 0x2d, 0x39, 0x74, 0xbc, 0x1c, 0xc7, 0x61, 0x33 };
            Verify(input, expected);
        }

        // MD4("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz01234567890") = ce64c40ecfbe896462f3c1a925884624
        [Fact]
        public void TryEncrypt_63bytes_HitsEdgeCase()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz01234567890"));
            ReadOnlySpan<byte> expected = new byte[] { 0xce, 0x64, 0xc4, 0x0e, 0xcf, 0xbe, 0x89, 0x64, 0x62, 0xf3, 0xc1, 0xa9, 0x25, 0x88, 0x46, 0x24 };
            Verify(input, expected);
        }

        // MD4("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012345678901") = 4b0e77758d2ede1eb21d267d492ae70b
        [Fact]
        public void TryEncrypt_64bytes_HitsEdgeCase()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012345678901"));
            ReadOnlySpan<byte> expected = new byte[] { 0x4b, 0x0e, 0x77, 0x75, 0x8d, 0x2e, 0xde, 0x1e, 0xb2, 0x1d, 0x26, 0x7d, 0x49, 0x2a, 0xe7, 0x0b };
            Verify(input, expected);
        }

        // MD4("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789012") = 3b46ad159b3fd800d254e3c4cc71fe36
        [Fact]
        public void TryEncrypt_65bytes_HitsEdgeCase()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789012"));
            ReadOnlySpan<byte> expected = new byte[] { 0x3b, 0x46, 0xad, 0x15, 0x9b, 0x3f, 0xd8, 0x00, 0xd2, 0x54, 0xe3, 0xc4, 0xcc, 0x71, 0xfe, 0x36 };
            Verify(input, expected);
        }

        // MD4("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012345678901ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz01234567890") = a3e23048e4ade47a0f00fa8aed2a0248
        [Fact]
        public void TryEncrypt_127bytes_HitsEdgeCase()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012345678901ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz01234567890"));
            ReadOnlySpan<byte> expected = new byte[] { 0xa3, 0xe2, 0x30, 0x48, 0xe4, 0xad, 0xe4, 0x7a, 0x0f, 0x00, 0xfa, 0x8a, 0xed, 0x2a, 0x02, 0x48 };
            Verify(input, expected);
        }

        // MD4("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012345678901ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012345678901") = de49da96c105be37b242f2bee86c4759
        [Fact]
        public void TryEncrypt_128bytes_HitsEdgeCase()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012345678901ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012345678901"));
            ReadOnlySpan<byte> expected = new byte[] { 0xde, 0x49, 0xda, 0x96, 0xc1, 0x05, 0xbe, 0x37, 0xb2, 0x42, 0xf2, 0xbe, 0xe8, 0x6c, 0x47, 0x59 };
            Verify(input, expected);
        }

        // MD4("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012345678901ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789012") = a6c10c320f8827d08248a2d8b124b040
        [Fact]
        public void TryEncrypt_129bytes_HitsEdgeCase()
        {
            ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(Encoding.Default.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012345678901ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789012"));
            ReadOnlySpan<byte> expected = new byte[] { 0xa6, 0xc1, 0x0c, 0x32, 0x0f, 0x88, 0x27, 0xd0, 0x82, 0x48, 0xa2, 0xd8, 0xb1, 0x24, 0xb0, 0x40 };
            Verify(input, expected);
        }

        private void Verify(ReadOnlySpan<byte> input, ReadOnlySpan<byte> expected)
        {
            Span<byte> output = stackalloc byte[expected.Length];
            MD4.HashData(input, output);
            Assert.Equal(expected.ToArray(), output.ToArray());
        }
    }
}
