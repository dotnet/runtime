// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public class Sha1Tests : HashAlgorithmTest
    {
        protected override HashAlgorithm Create()
        {
            return SHA1.Create();
        }

        [Fact]
        public void Sha1_Empty()
        {
            Verify(Array.Empty<byte>(), "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709");
        }

        // SHA1 tests are defined somewhat obliquely within RFC 3174, section 7.3
        // The same tests appear in http://csrc.nist.gov/publications/fips/fips180-2/fips180-2.pdf Appendix A
        [Fact]
        public void Sha1_Rfc3174_1()
        {
            Verify("abc", "A9993E364706816ABA3E25717850C26C9CD0D89D");
        }

        [Fact]
        public void Sha1_Rfc3174_MultiBlock()
        {
            VerifyMultiBlock("ab", "c", "A9993E364706816ABA3E25717850C26C9CD0D89D", "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709");
        }

        [Fact]
        public void Sha1_Rfc3174_2()
        {
            Verify("abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq", "84983E441C3BD26EBAAE4AA1F95129E5E54670F1");
        }

        [Fact]
        public void Sha1_Rfc3174_3()
        {
            VerifyRepeating("a", 1000000, "34AA973CD4C4DAA4F61EEB2BDBAD27316534016F");
        }

        [Fact]
        public void Sha1_Rfc3174_4()
        {
            VerifyRepeating("0123456701234567012345670123456701234567012345670123456701234567", 10, "DEA356A2CDDD90C7A7ECEDC5EBB563934F460452");
        }
    }
}
