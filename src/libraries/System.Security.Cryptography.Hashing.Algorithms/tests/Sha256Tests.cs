// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public class Sha256Tests : HashAlgorithmTest
    {
        protected override HashAlgorithm Create()
        {
            return SHA256.Create();
        }

        // These test cases are from http://csrc.nist.gov/publications/fips/fips180-2/fips180-2.pdf Appendix B
        [Fact]
        public void Sha256_Fips180_1()
        {
            Verify(
                "abc",
                "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        }

        [Fact]
        public void Sha256_Fips180_2()
        {
            Verify(
                "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq",
                "248d6a61d20638b8e5c026930c3e6039a33ce45964ff2167f6ecedd419db06c1");
        }

        [Fact]
        public void Sha256_Fips180_3()
        {
            VerifyRepeating(
                "a",
                1000000,
                "cdc76e5c9914fb9281a1c7e284d73e67f1809a48a497200e046d39ccc7112cd0");
        }
    }
}
