// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public class MD5Tests : HashAlgorithmTest
    {
        protected override HashAlgorithm Create()
        {
            return MD5.Create();
        }

        // Test cases are defined in RFC 1321, section A.5

        [Fact]
        public void MD5_Rfc1321_1()
        {
            Verify("", "d41d8cd98f00b204e9800998ecf8427e");
        }

        [Fact]
        public void MD5_Rfc1321_2()
        {
            Verify("a", "0cc175b9c0f1b6a831c399e269772661");
        }

        [Fact]
        public void MD5_Rfc1321_3()
        {
            Verify("abc", "900150983cd24fb0d6963f7d28e17f72");
        }

        [Fact]
        public void MD5_Rfc1321_MultiBlock()
        {
            VerifyMultiBlock(
                "a",
                "bc",
                "900150983cd24fb0d6963f7d28e17f72",
                "d41d8cd98f00b204e9800998ecf8427e");
        }

        [Fact]
        public void MD5_Rfc1321_4()
        {
            Verify("message digest", "f96b697d7cb7938d525a2f31aaf161d0");
        }

        [Fact]
        public void MD5_Rfc1321_5()
        {
            Verify("abcdefghijklmnopqrstuvwxyz", "c3fcd3d76192e4007dfb496cca67e13b");
        }

        [Fact]
        public void MD5_Rfc1321_6()
        {
            Verify("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", "d174ab98d277d9f5a5611c2c9f419d9f");
        }

        [Fact]
        public void MD5_Rfc1321_7()
        {
            Verify("12345678901234567890123456789012345678901234567890123456789012345678901234567890", "57edf4a22be3c955ac49da2e2107b67a");
        }

        [Fact]
        public void MD5_Rfc1321_7_AsStream()
        {
            VerifyRepeating("1234567890", 8, "57edf4a22be3c955ac49da2e2107b67a");
        }
    }
}
