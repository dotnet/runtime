// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Security.Cryptography.RNG.Tests
{
    /// <summary>
    /// Since RNGCryptoServiceProviderTests wraps RandomNumberGenerator from Algorithms assembly, we only test minimally here.
    /// </summary>
    public class RNGCryptoServiceProviderTests
    {
        [Fact]
        public static void DifferentSequential_10()
        {
            // Ensure that the RNG doesn't produce a stable set of data.
            byte[] first = new byte[10];
            byte[] second = new byte[10];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(first);
                rng.GetBytes(second);
            }

            // Random being random, there is a chance that it could produce the same sequence.
            // The smallest test case that we have is 10 bytes.
            // The probability that they are the same, given a Truly Random Number Generator is:
            // Pmatch(byte0) * Pmatch(byte1) * Pmatch(byte2) * ... * Pmatch(byte9)
            // = 1/256 * 1/256 * ... * 1/256
            // = 1/(256^10)
            // = 1/1,208,925,819,614,629,174,706,176
            Assert.NotEqual(first, second);
        }

        [Fact]
        public static void GetNonZeroBytes()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                Assert.Throws<ArgumentNullException>("data", () => rng.GetNonZeroBytes(null));

                // Array should not have any zeros
                byte[] rand = new byte[65536];
                rng.GetNonZeroBytes(rand);
                Assert.Equal(-1, Array.IndexOf<byte>(rand, 0));
            }
        }

        [Fact]
        public static void GetBytes_Offset()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] rand = new byte[400];

                // Set canary bytes
                rand[99] = 77;
                rand[399] = 77;

                rng.GetBytes(rand, 100, 200);

                // Array should not have been touched outside of 100-299
                Assert.Equal(99, Array.IndexOf<byte>(rand, 77, 0));
                Assert.Equal(399, Array.IndexOf<byte>(rand, 77, 300));

                // Ensure 100-300 has random bytes; not likely to ever fail here by chance (256^200)
                Assert.True(rand.Skip(100).Take(200).Sum(b => b) > 0);
            }
        }

        [Fact]
        public static void VerifyCtors()
        {
            using (new RNGCryptoServiceProvider()) { }
            using (new RNGCryptoServiceProvider(string.Empty)) { }
            using (new RNGCryptoServiceProvider((string)null)) { }
            using (new RNGCryptoServiceProvider((CspParameters)null)) { }

            Assert.Throws<PlatformNotSupportedException>(() =>
            { using (new RNGCryptoServiceProvider(new CspParameters())) { } });
        }
    }
}
