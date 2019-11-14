// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class IsOneTest
    {
        private static int s_seed = 0;

        [Fact]
        public static void RunIsOneTests()
        {
            Random random = new Random(s_seed);

            //Just basic tests
            // Zero
            VerifyIsOne(BigInteger.Zero, false);

            // Negative One
            VerifyIsOne(BigInteger.MinusOne, false);

            // One
            VerifyIsOne(BigInteger.One, true);

            // -Int32.MaxValue
            VerifyIsOne((BigInteger)int.MaxValue * -1, false);

            // Int32.MaxValue
            VerifyIsOne((BigInteger)int.MaxValue, false);

            // int32.MaxValue + 1
            VerifyIsOne((BigInteger)int.MaxValue + 1, false);

            // UInt32.MaxValue
            VerifyIsOne((BigInteger)uint.MaxValue, false);

            // Uint32.MaxValue + 1
            VerifyIsOne((BigInteger)uint.MaxValue + 1, false);
        }

        private static void VerifyIsOne(BigInteger bigInt, bool expectedAnswer)
        {
            Assert.Equal(expectedAnswer, bigInt.IsOne);
        }
    }
}
