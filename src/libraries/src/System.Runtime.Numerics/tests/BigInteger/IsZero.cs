// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class IsZeroTest
    {
        private static int s_seed = 0;
        
        [Fact]
        public static void RunIsZeroTests()
        {
            Random random = new Random(s_seed);

            //Just basic tests
            // Zero
            VerifyIsZero(BigInteger.Zero, true);

            // Negative One
            VerifyIsZero(BigInteger.MinusOne, false);

            // One
            VerifyIsZero(BigInteger.One, false);

            // -Int32.MaxValue
            VerifyIsZero((BigInteger)Int32.MaxValue * -1, false);

            // Int32.MaxValue
            VerifyIsZero((BigInteger)Int32.MaxValue, false);

            // int32.MaxValue + 1
            VerifyIsZero((BigInteger)Int32.MaxValue + 1, false);

            // UInt32.MaxValue
            VerifyIsZero((BigInteger)UInt32.MaxValue, false);

            // Uint32.MaxValue + 1
            VerifyIsZero((BigInteger)UInt32.MaxValue + 1, false);
        }

        private static void VerifyIsZero(BigInteger bigInt, bool expectedAnswer)
        {
            Assert.Equal(expectedAnswer, bigInt.IsZero);
        }
    }
}
