// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    public partial class AesCipherTests
    {
        private static readonly Encoding s_asciiEncoding = new ASCIIEncoding();
        private static readonly byte[] s_helloBytes = s_asciiEncoding.GetBytes("Hello");

        // This is the expected output of many decryptions. Changing this value requires re-generating test input.
        private static readonly byte[] s_multiBlockBytes =
            s_asciiEncoding.GetBytes("This is a sentence that is longer than a block, it ensures that multi-block functions work.");

        // A randomly generated 256-bit key.
        private static readonly byte[] s_aes256Key = new byte[]
        {
            0x3E, 0x8A, 0xB2, 0x5B, 0x41, 0xF2, 0x5D, 0xEF,
            0x48, 0x4E, 0x0C, 0x50, 0xBB, 0xCF, 0x89, 0xA1,
            0x1B, 0x6A, 0x26, 0x86, 0x60, 0x36, 0x7C, 0xFD,
            0x04, 0x3D, 0xE3, 0x97, 0x6D, 0xB0, 0x86, 0x60,
        };

        // A randomly generated IV, for use in the AES-256CBC tests (and other cases' negative tests)
        private static readonly byte[] s_aes256CbcIv = new byte[]
        {
            0x43, 0x20, 0xC3, 0xE1, 0xCA, 0x80, 0x0C, 0xD1,
            0xDB, 0x74, 0xF7, 0x30, 0x6D, 0xED, 0x40, 0xF7,
        };

        // A randomly generated 192-bit key.
        private static readonly byte[] s_aes192Key = new byte[]
        {
            0xA6, 0x1E, 0xC7, 0x54, 0x37, 0x4D, 0x8C, 0xA5,
            0xA4, 0xBB, 0x99, 0x50, 0x35, 0x4B, 0x30, 0x4D,
            0x6C, 0xFE, 0x3B, 0x59, 0x65, 0xCB, 0x93, 0xE3,
        };

        // A randomly generated 128-bit key.
        private static readonly byte[] s_aes128Key = new byte[]
        {
            0x8B, 0x74, 0xCF, 0x71, 0x34, 0x99, 0x97, 0x68,
            0x22, 0x86, 0xE7, 0x52, 0xED, 0xFC, 0x56, 0x7E,
        };
    }
}
