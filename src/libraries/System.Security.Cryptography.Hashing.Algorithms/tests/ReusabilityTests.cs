// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public class ReusabilityTests
    {
        [Theory]
        [MemberData("ReusabilityHashAlgorithms")]
        public void TestReusability(HashAlgorithm hashAlgorithm)
        {
            using (hashAlgorithm)
            {
                byte[] input = { 8, 6, 7, 5, 3, 0, 9, };
                byte[] hash1 = hashAlgorithm.ComputeHash(input);
                byte[] hash2 = hashAlgorithm.ComputeHash(input);

                Assert.Equal(hash1, hash2);
            }
        }

        public static IEnumerable<object[]> ReusabilityHashAlgorithms()
        {
            return new[]
            {
                new object[] { MD5.Create(), },
                new object[] { SHA1.Create(), },
                new object[] { SHA256.Create(), },
                new object[] { SHA384.Create(), },
                new object[] { SHA512.Create(), },
                new object[] { new HMACSHA1(), },
                new object[] { new HMACSHA256(), },
                new object[] { new HMACSHA384(), },
                new object[] { new HMACSHA512(), },
            };
        }
    }
}
