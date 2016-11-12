// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.MD5.Tests
{
    /// <summary>
    /// Since MD5CryptoServiceProvider wraps IncrementalHash from Algorithms assembly, we only test minimally here.
    /// </summary>
    public class MD5CryptoServiceProviderTests
    {
        [Fact]
        public void MD5_Rfc1321_1()
        {
            Verify(string.Empty, "d41d8cd98f00b204e9800998ecf8427e");
        }

        [Fact]
        public void MD5_Rfc1321_7()
        {
            Verify("12345678901234567890123456789012345678901234567890123456789012345678901234567890", "57edf4a22be3c955ac49da2e2107b67a");
        }

        private void Verify(string rawText, string expected)
        {
            byte[] inputBytes = ByteUtils.AsciiBytes(rawText);
            byte[] expectedBytes = ByteUtils.HexToByteArray(expected);

            using (var hash = new MD5CryptoServiceProvider())
            {
                Assert.True(hash.HashSize > 0);
                byte[] actual = hash.ComputeHash(inputBytes, 0, inputBytes.Length);

                Assert.Equal(expectedBytes, actual);

                actual = hash.Hash;
                Assert.Equal(expectedBytes, actual);
            }
        }
    }
}
