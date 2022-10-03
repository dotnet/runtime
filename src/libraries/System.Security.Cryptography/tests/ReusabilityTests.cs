// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class ReusabilityTests
    {
        [Theory]
        [MemberData(nameof(ReusabilityHashAlgorithms))]
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
            if (!PlatformDetection.IsBrowser)
            {
                yield return new object[] { MD5.Create(), };
            }

            yield return new object[] { SHA1.Create(), };
            yield return new object[] { SHA256.Create(), };
            yield return new object[] { SHA384.Create(), };
            yield return new object[] { SHA512.Create(), };
            yield return new object[] { new HMACSHA1(), };
            yield return new object[] { new HMACSHA256(), };
            yield return new object[] { new HMACSHA384(), };
            yield return new object[] { new HMACSHA512(), };
        }
    }
}
