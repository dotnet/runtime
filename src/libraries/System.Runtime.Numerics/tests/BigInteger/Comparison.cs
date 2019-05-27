// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class ComparisonTest
    {
        private const int NumberOfRandomIterations = 1;

        [Fact]
        public static void ComparisonTests()
        {
            int seed = 100;
            RunTests(seed);
        }

        public static void RunTests(int seed)
        {
            Random random = new Random(seed);

            RunPositiveTests(random);
            RunNegativeTests(random);
        }

        private static void RunPositiveTests(Random random)
        {
            BigInteger bigInteger1, bigInteger2;
            int expectedResult;
            byte[] byteArray;
            bool isNegative;

            //1 Inputs from BigInteger Properties
            // BigInteger.MinusOne, BigInteger.MinusOne
            VerifyComparison(BigInteger.MinusOne, BigInteger.MinusOne, 0);

            // BigInteger.MinusOne, BigInteger.Zero
            VerifyComparison(BigInteger.MinusOne, BigInteger.Zero, -1);

            // BigInteger.MinusOne, BigInteger.One
            VerifyComparison(BigInteger.MinusOne, BigInteger.One, -1);

            // BigInteger.MinusOne, Large Negative
            VerifyComparison(BigInteger.MinusOne, -1L * ((BigInteger)long.MaxValue), 1);

            // BigInteger.MinusOne, Small Negative
            VerifyComparison(BigInteger.MinusOne, -1L * ((BigInteger)short.MaxValue), 1);

            // BigInteger.MinusOne, Large Number
            VerifyComparison(BigInteger.MinusOne, (BigInteger)int.MaxValue + 1, -1);

            // BigInteger.MinusOne, Small Number
            VerifyComparison(BigInteger.MinusOne, (BigInteger)int.MaxValue - 1, -1);

            // BigInteger.MinusOne, One Less
            VerifyComparison(BigInteger.MinusOne, BigInteger.MinusOne - 1, 1);


            // BigInteger.Zero, BigInteger.Zero
            VerifyComparison(BigInteger.Zero, BigInteger.Zero, 0);

            // BigInteger.Zero, Large Negative
            VerifyComparison(BigInteger.Zero, -1L * ((BigInteger)int.MaxValue + 1), 1);

            // BigInteger.Zero, Small Negative
            VerifyComparison(BigInteger.Zero, -1L * ((BigInteger)int.MaxValue - 1), 1);

            // BigInteger.Zero, Large Number
            VerifyComparison(BigInteger.Zero, (BigInteger)int.MaxValue + 1, -1);

            // BigInteger.Zero, Small Number
            VerifyComparison(BigInteger.Zero, (BigInteger)int.MaxValue - 1, -1);


            // BigInteger.One, BigInteger.One
            VerifyComparison(BigInteger.One, BigInteger.One, 0);

            // BigInteger.One, BigInteger.MinusOne
            VerifyComparison(BigInteger.One, BigInteger.MinusOne, 1);

            // BigInteger.One, BigInteger.Zero
            VerifyComparison(BigInteger.One, BigInteger.Zero, 1);

            // BigInteger.One, Large Negative
            VerifyComparison(BigInteger.One, -1 * ((BigInteger)int.MaxValue + 1), 1);

            // BigInteger.One, Small Negative
            VerifyComparison(BigInteger.One, -1 * ((BigInteger)int.MaxValue - 1), 1);

            // BigInteger.One, Large Number
            VerifyComparison(BigInteger.One, (BigInteger)int.MaxValue + 1, -1);

            // BigInteger.One, Small Number
            VerifyComparison(BigInteger.One, (BigInteger)int.MaxValue - 1, -1);

            //Basic Checks
            // BigInteger.MinusOne, (Int32) -1
            VerifyComparison(BigInteger.MinusOne, (int)(-1), 0);

            // BigInteger.Zero, (Int32) 0
            VerifyComparison(BigInteger.Zero, (int)(0), 0);

            // BigInteger.One, 1
            VerifyComparison(BigInteger.One, (int)(1), 0);


            //1 Inputs Around the boundary of UInt32
            // -1 * UInt32.MaxValue, -1 * UInt32.MaxValue
            VerifyComparison(-1L * (BigInteger)uint.MaxValue - 1, -1L * (BigInteger)uint.MaxValue - 1, 0);

            // -1 * UInt32.MaxValue, -1 * UInt32.MaxValue -1
            VerifyComparison(-1L * (BigInteger)uint.MaxValue, (-1L * (BigInteger)uint.MaxValue) - 1L, 1);

            // UInt32.MaxValue, -1 * UInt32.MaxValue
            VerifyComparison((BigInteger)uint.MaxValue, -1L * (BigInteger)uint.MaxValue, 1);

            // UInt32.MaxValue, UInt32.MaxValue
            VerifyComparison((BigInteger)uint.MaxValue, (BigInteger)uint.MaxValue, 0);

            // UInt32.MaxValue, UInt32.MaxValue + 1
            VerifyComparison((BigInteger)uint.MaxValue, (BigInteger)uint.MaxValue + 1, -1);

            // UInt64.MaxValue, UInt64.MaxValue
            VerifyComparison((BigInteger)ulong.MaxValue, (BigInteger)ulong.MaxValue, 0);

            // UInt64.MaxValue + 1, UInt64.MaxValue
            VerifyComparison((BigInteger)ulong.MaxValue + 1, ulong.MaxValue, 1);

            //Other cases
            // -1 * Large Bigint, -1 * Large BigInt
            VerifyComparison(-1L * ((BigInteger)int.MaxValue + 1), -1L * ((BigInteger)int.MaxValue + 1), 0);

            // Large Bigint, Large Negative BigInt
            VerifyComparison((BigInteger)int.MaxValue + 1, -1L * ((BigInteger)int.MaxValue + 1), 1);

            // Large Bigint, UInt32.MaxValue
            VerifyComparison((BigInteger)long.MaxValue + 1, (BigInteger)uint.MaxValue, 1);

            // Large Bigint, One More
            VerifyComparison((BigInteger)int.MaxValue + 1, ((BigInteger)int.MaxValue) + 2, -1);

            // -1 * Small Bigint, -1 * Small BigInt
            VerifyComparison(-1L * ((BigInteger)int.MaxValue - 1), -1L * ((BigInteger)int.MaxValue - 1), 0);

            // Small Bigint, Small Negative BigInt
            VerifyComparison((BigInteger)int.MaxValue - 1, -1L * ((BigInteger)int.MaxValue - 1), 1);

            // Small Bigint, UInt32.MaxValue
            VerifyComparison((BigInteger)int.MaxValue - 1, (BigInteger)uint.MaxValue - 1, -1);

            // Small Bigint, One More
            VerifyComparison((BigInteger)int.MaxValue - 2, ((BigInteger)int.MaxValue) - 1, -1);

            //BigInteger vs. Int32

            // One Larger (BigInteger), Int32.MaxValue
            VerifyComparison((BigInteger)int.MaxValue + 1, int.MaxValue, 1);

            // Larger BigInteger, Int32.MaxValue
            VerifyComparison((BigInteger)ulong.MaxValue + 1, int.MaxValue, 1);

            // Smaller BigInteger, Int32.MaxValue
            VerifyComparison((BigInteger)short.MinValue - 1, int.MaxValue, -1);

            // One Smaller (BigInteger), Int32.MaxValue
            VerifyComparison((BigInteger)int.MaxValue - 1, int.MaxValue, -1);

            // (BigInteger) Int32.MaxValue, Int32.MaxValue
            VerifyComparison((BigInteger)int.MaxValue, int.MaxValue, 0);

            //BigInteger vs. UInt32
            // One Larger (BigInteger), UInt32.MaxValue
            VerifyComparison((BigInteger)uint.MaxValue + 1, uint.MaxValue, 1);

            // Larger BigInteger, UInt32.MaxValue
            VerifyComparison((BigInteger)long.MaxValue + 1, uint.MaxValue, 1);

            // Smaller BigInteger, UInt32.MaxValue
            VerifyComparison((BigInteger)short.MinValue - 1, uint.MaxValue, -1);

            // One Smaller (BigInteger), UInt32.MaxValue
            VerifyComparison((BigInteger)uint.MaxValue - 1, uint.MaxValue, -1);

            // (BigInteger UInt32.MaxValue, UInt32.MaxValue
            VerifyComparison((BigInteger)uint.MaxValue, uint.MaxValue, 0);

            //BigInteger vs. UInt64
            // One Larger (BigInteger), UInt64.MaxValue
            VerifyComparison((BigInteger)ulong.MaxValue + 1, ulong.MaxValue, 1);

            // Larger BigInteger, UInt64.MaxValue
            VerifyComparison((BigInteger)ulong.MaxValue + 100, ulong.MaxValue, 1);

            // Smaller BigInteger, UInt64.MaxValue
            VerifyComparison((BigInteger)short.MinValue - 1, ulong.MaxValue, -1);
            VerifyComparison((BigInteger)short.MaxValue - 1, ulong.MaxValue, -1);
            VerifyComparison((BigInteger)int.MaxValue + 1, ulong.MaxValue, -1);

            // One Smaller (BigInteger), UInt64.MaxValue
            VerifyComparison((BigInteger)ulong.MaxValue - 1, ulong.MaxValue, -1);

            // (BigInteger UInt64.MaxValue, UInt64.MaxValue
            VerifyComparison((BigInteger)ulong.MaxValue, ulong.MaxValue, 0);

            //BigInteger vs. Int64
            // One Smaller (BigInteger), Int64.MaxValue
            VerifyComparison((BigInteger)long.MaxValue - 1, long.MaxValue, -1);

            // Larger BigInteger, Int64.MaxValue
            VerifyComparison((BigInteger)ulong.MaxValue + 100, long.MaxValue, 1);

            // Smaller BigInteger, Int32.MaxValue
            VerifyComparison((BigInteger)short.MinValue - 1, long.MaxValue, -1);

            // (BigInteger Int64.MaxValue, Int64.MaxValue
            VerifyComparison((BigInteger)long.MaxValue, long.MaxValue, 0);

            // One Larger (BigInteger), Int64.MaxValue
            VerifyComparison((BigInteger)long.MaxValue + 1, long.MaxValue, 1);
            

            //1 Random Inputs
            // Random BigInteger only differs by sign

            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                do
                {
                    byteArray = GetRandomByteArray(random);
                } 
                while (MyBigIntImp.IsZero(byteArray));

                BigInteger b2 = new BigInteger(byteArray);
                if (b2 > (BigInteger)0)
                {
                    VerifyComparison(b2, -1L * b2, 1);
                }
                else
                {
                    VerifyComparison(b2, -1L * b2, -1);
                }
            }

            // Random BigInteger, Random BigInteger
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                expectedResult = GetRandomInputForComparison(random, out bigInteger1, out bigInteger2);
                VerifyComparison(bigInteger1, bigInteger2, expectedResult);
            }

            // Random BigInteger
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                byteArray = GetRandomByteArray(random);
                isNegative = 0 == random.Next(0, 2);
                VerifyComparison(new BigInteger(byteArray), isNegative, new BigInteger(byteArray), isNegative, 0);
            }

            //1 Identical values constructed multiple ways
            // BigInteger.Zero, BigInteger constructed with a byte[] isNegative=true
            VerifyComparison(BigInteger.Zero, false, new BigInteger(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), true, 0);

            // BigInteger.Zero, BigInteger constructed with a byte[] isNegative=false
            VerifyComparison(BigInteger.Zero, false, new BigInteger(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), false, 0);

            // BigInteger.Zero, BigInteger constructed from an Int64
            VerifyComparison(BigInteger.Zero, 0L, 0);

            // BigInteger.Zero, BigInteger constructed from a Double
            VerifyComparison(BigInteger.Zero, (BigInteger)0d, 0);

            // BigInteger.Zero, BigInteger constructed from a Decimal
            VerifyComparison(BigInteger.Zero, (BigInteger)0, 0);

            // BigInteger.Zero, BigInteger constructed with Addition
            byteArray = GetRandomByteArray(random);
            isNegative = 0 == random.Next(0, 2);
            VerifyComparison(BigInteger.Zero, false, new BigInteger(byteArray) + (-1 * new BigInteger(byteArray)), isNegative, 0);

            // BigInteger.Zero, BigInteger constructed with Subtraction
            byteArray = GetRandomByteArray(random);
            isNegative = 0 == random.Next(0, 2);
            VerifyComparison(BigInteger.Zero, false, new BigInteger(byteArray) - new BigInteger(byteArray), isNegative, 0);

            // BigInteger.Zero, BigInteger constructed with Multiplication
            byteArray = GetRandomByteArray(random);
            isNegative = 0 == random.Next(0, 2);
            VerifyComparison(BigInteger.Zero, false, 0 * new BigInteger(byteArray), isNegative, 0);

            // BigInteger.Zero, BigInteger constructed with Division
            do
            {
                byteArray = GetRandomByteArray(random);
            } 
            while (MyBigIntImp.IsZero(byteArray));

            isNegative = 0 == random.Next(0, 2);
            VerifyComparison(BigInteger.Zero, false, 0 / new BigInteger(byteArray), isNegative, 0);

            // BigInteger.One, BigInteger constructed with a byte[]
            VerifyComparison(BigInteger.One, false, new BigInteger(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 }), false, 0);

            // BigInteger.One, BigInteger constructed from an Int64
            VerifyComparison(BigInteger.One, 1L, 0);

            // BigInteger.One, BigInteger constructed from a Double
            VerifyComparison(BigInteger.One, (BigInteger)1d, 0);

            // BigInteger.One, BigInteger constructed from a Decimal
            VerifyComparison(BigInteger.One, (BigInteger)1, 0);

            // BigInteger.One, BigInteger constructed with Addition
            byteArray = GetRandomByteArray(random);
            isNegative = 0 == random.Next(0, 2);
            VerifyComparison(BigInteger.One, false, (((BigInteger)(-1)) * new BigInteger(byteArray)) + (new BigInteger(byteArray)) + 1, false, 0);

            // BigInteger.One, BigInteger constructed with Subtraction
            byteArray = GetRandomByteArray(random);
            isNegative = 0 == random.Next(0, 2);
            BigInteger b = new BigInteger(byteArray);
            if (b > (BigInteger)0)
            {
                VerifyComparison(BigInteger.One, false, (b + 1) - (b), false, 0);
            }
            else
            {
                b = -1L * b;
                VerifyComparison(BigInteger.One, false, (b + 1) - (b), false, 0);
                b = -1L * b;
            }

            // BigInteger.One, BigInteger constructed with Multiplication
            byteArray = GetRandomByteArray(random);
            isNegative = 0 == random.Next(0, 2);
            VerifyComparison(BigInteger.One, (BigInteger)1 * (BigInteger)1, 0);

            // BigInteger.One, BigInteger constructed with Division
            do
            {
                byteArray = GetRandomByteArray(random);
            } 
            while (MyBigIntImp.IsZero(byteArray));

            BigInteger b1 = new BigInteger(byteArray);
            VerifyComparison(BigInteger.One, false, b1 / b1, false, 0);
        }

        private static void RunNegativeTests(Random random)
        {
            // BigInteger.Zero, 0
            Assert.Equal(false, BigInteger.Zero.Equals((object)0));

            // BigInteger.Zero, null
            Assert.Equal(false, BigInteger.Zero.Equals((object)null));

            // BigInteger.Zero, string
            Assert.Equal(false, BigInteger.Zero.Equals((object)"0"));
        }

        public static void IComparable_Invalid(string paramName)
        {
            IComparable comparable = new BigInteger();
            Assert.Equal(1, comparable.CompareTo(null));
            AssertExtensions.Throws<ArgumentException>(paramName, () => comparable.CompareTo(0)); // Obj is not a BigInteger
        }

        [Fact]
        public static void IComparable_Invalid_netcore()
        {
            IComparable_Invalid("obj");
        }

        private static void VerifyComparison(BigInteger x, BigInteger y, int expectedResult)
        {
            bool expectedEquals = 0 == expectedResult;
            bool expectedLessThan = expectedResult < 0;
            bool expectedGreaterThan = expectedResult > 0;

            Assert.Equal(expectedEquals, x == y);
            Assert.Equal(expectedEquals, y == x);

            Assert.Equal(!expectedEquals, x != y);
            Assert.Equal(!expectedEquals, y != x);

            Assert.Equal(expectedEquals, x.Equals(y));
            Assert.Equal(expectedEquals, y.Equals(x));

            Assert.Equal(expectedEquals, x.Equals((object)y));
            Assert.Equal(expectedEquals, y.Equals((object)x));

            VerifyCompareResult(expectedResult, x.CompareTo(y), "x.CompareTo(y)");
            VerifyCompareResult(-expectedResult, y.CompareTo(x), "y.CompareTo(x)");

            IComparable comparableX = x;
            IComparable comparableY = y;
            VerifyCompareResult(expectedResult, comparableX.CompareTo(y), "comparableX.CompareTo(y)");
            VerifyCompareResult(-expectedResult, comparableY.CompareTo(x), "comparableY.CompareTo(x)");

            VerifyCompareResult(expectedResult, BigInteger.Compare(x, y), "Compare(x,y)");
            VerifyCompareResult(-expectedResult, BigInteger.Compare(y, x), "Compare(y,x)");

            if (expectedEquals)
            {
                Assert.Equal(x.GetHashCode(), y.GetHashCode());
                Assert.Equal(x.ToString(), y.ToString());
            }

            Assert.Equal(x.GetHashCode(), x.GetHashCode());
            Assert.Equal(y.GetHashCode(), y.GetHashCode());

            Assert.Equal(expectedLessThan, x < y);
            Assert.Equal(expectedGreaterThan, y < x);

            Assert.Equal(expectedGreaterThan, x > y);
            Assert.Equal(expectedLessThan, y > x);

            Assert.Equal(expectedLessThan || expectedEquals, x <= y);
            Assert.Equal(expectedGreaterThan || expectedEquals, y <= x);

            Assert.Equal(expectedGreaterThan || expectedEquals, x >= y);
            Assert.Equal(expectedLessThan || expectedEquals, y >= x);
        }

        private static void VerifyComparison(BigInteger x, int y, int expectedResult)
        {
            bool expectedEquals = 0 == expectedResult;
            bool expectedLessThan = expectedResult < 0;
            bool expectedGreaterThan = expectedResult > 0;

            Assert.Equal(expectedEquals, x == y);
            Assert.Equal(expectedEquals, y == x);

            Assert.Equal(!expectedEquals, x != y);
            Assert.Equal(!expectedEquals, y != x);

            Assert.Equal(expectedEquals, x.Equals(y));

            VerifyCompareResult(expectedResult, x.CompareTo(y), "x.CompareTo(y)");

            if (expectedEquals)
            {
                Assert.Equal(x.GetHashCode(), ((BigInteger)y).GetHashCode());
                Assert.Equal(x.ToString(), ((BigInteger)y).ToString());
            }
            
            Assert.Equal(x.GetHashCode(), x.GetHashCode());
            Assert.Equal(((BigInteger)y).GetHashCode(), ((BigInteger)y).GetHashCode());

            Assert.Equal(expectedLessThan, x < y);
            Assert.Equal(expectedGreaterThan, y < x);

            Assert.Equal(expectedGreaterThan, x > y);
            Assert.Equal(expectedLessThan, y > x);

            Assert.Equal(expectedLessThan || expectedEquals, x <= y);
            Assert.Equal(expectedGreaterThan || expectedEquals, y <= x);

            Assert.Equal(expectedGreaterThan || expectedEquals, x >= y);
            Assert.Equal(expectedLessThan || expectedEquals, y >= x);
        }

        private static void VerifyComparison(BigInteger x, uint y, int expectedResult)
        {
            bool expectedEquals = 0 == expectedResult;
            bool expectedLessThan = expectedResult < 0;
            bool expectedGreaterThan = expectedResult > 0;

            Assert.Equal(expectedEquals, x == y);
            Assert.Equal(expectedEquals, y == x);

            Assert.Equal(!expectedEquals, x != y);
            Assert.Equal(!expectedEquals, y != x);

            Assert.Equal(expectedEquals, x.Equals(y));

            VerifyCompareResult(expectedResult, x.CompareTo(y), "x.CompareTo(y)");

            if (expectedEquals)
            {
                Assert.Equal(x.GetHashCode(), ((BigInteger)y).GetHashCode());
                Assert.Equal(x.ToString(), ((BigInteger)y).ToString());
            }


            Assert.Equal(x.GetHashCode(), x.GetHashCode());
            Assert.Equal(((BigInteger)y).GetHashCode(), ((BigInteger)y).GetHashCode());

            Assert.Equal(expectedLessThan, x < y);
            Assert.Equal(expectedGreaterThan, y < x);

            Assert.Equal(expectedGreaterThan, x > y);
            Assert.Equal(expectedLessThan, y > x);

            Assert.Equal(expectedLessThan || expectedEquals, x <= y);
            Assert.Equal(expectedGreaterThan || expectedEquals, y <= x);

            Assert.Equal(expectedGreaterThan || expectedEquals, x >= y);
            Assert.Equal(expectedLessThan || expectedEquals, y >= x);
        }

        private static void VerifyComparison(BigInteger x, long y, int expectedResult)
        {
            bool expectedEquals = 0 == expectedResult;
            bool expectedLessThan = expectedResult < 0;
            bool expectedGreaterThan = expectedResult > 0;

            Assert.Equal(expectedEquals, x == y);
            Assert.Equal(expectedEquals, y == x);

            Assert.Equal(!expectedEquals, x != y);
            Assert.Equal(!expectedEquals, y != x);

            Assert.Equal(expectedEquals, x.Equals(y));

            VerifyCompareResult(expectedResult, x.CompareTo(y), "x.CompareTo(y)");

            if (expectedEquals)
            {
                Assert.Equal(x.GetHashCode(), ((BigInteger)y).GetHashCode());
                Assert.Equal(x.ToString(), ((BigInteger)y).ToString());
            }

            Assert.Equal(x.GetHashCode(), x.GetHashCode());
            Assert.Equal(((BigInteger)y).GetHashCode(), ((BigInteger)y).GetHashCode());

            Assert.Equal(expectedLessThan, x < y);
            Assert.Equal(expectedGreaterThan, y < x);

            Assert.Equal(expectedGreaterThan, x > y);
            Assert.Equal(expectedLessThan, y > x);

            Assert.Equal(expectedLessThan || expectedEquals, x <= y);
            Assert.Equal(expectedGreaterThan || expectedEquals, y <= x);

            Assert.Equal(expectedGreaterThan || expectedEquals, x >= y);
            Assert.Equal(expectedLessThan || expectedEquals, y >= x);
        }

        private static void VerifyComparison(BigInteger x, ulong y, int expectedResult)
        {
            bool expectedEquals = 0 == expectedResult;
            bool expectedLessThan = expectedResult < 0;
            bool expectedGreaterThan = expectedResult > 0;

            Assert.Equal(expectedEquals, x == y);
            Assert.Equal(expectedEquals, y == x);

            Assert.Equal(!expectedEquals, x != y);
            Assert.Equal(!expectedEquals, y != x);

            Assert.Equal(expectedEquals, x.Equals(y));

            VerifyCompareResult(expectedResult, x.CompareTo(y), "x.CompareTo(y)");

            if (expectedEquals)
            {
                Assert.Equal(x.GetHashCode(), ((BigInteger)y).GetHashCode());
                Assert.Equal(x.ToString(), ((BigInteger)y).ToString());
            }
            else
            {
                Assert.NotEqual(x.GetHashCode(), ((BigInteger)y).GetHashCode());
                Assert.NotEqual(x.ToString(), ((BigInteger)y).ToString());
            }

            Assert.Equal(x.GetHashCode(), x.GetHashCode());
            Assert.Equal(((BigInteger)y).GetHashCode(), ((BigInteger)y).GetHashCode());

            Assert.Equal(expectedLessThan, x < y);
            Assert.Equal(expectedGreaterThan, y < x);

            Assert.Equal(expectedGreaterThan, x > y);
            Assert.Equal(expectedLessThan, y > x);

            Assert.Equal(expectedLessThan || expectedEquals, x <= y);
            Assert.Equal(expectedGreaterThan || expectedEquals, y <= x);

            Assert.Equal(expectedGreaterThan || expectedEquals, x >= y);
            Assert.Equal(expectedLessThan || expectedEquals, y >= x);
        }

        private static void VerifyComparison(BigInteger x, bool IsXNegative, BigInteger y, bool IsYNegative, int expectedResult)
        {
            bool expectedEquals = 0 == expectedResult;
            bool expectedLessThan = expectedResult < 0;
            bool expectedGreaterThan = expectedResult > 0;

            if (IsXNegative == true)
            {
                x = x * -1;
            }
            if (IsYNegative == true)
            {
                y = y * -1;
            }

            Assert.Equal(expectedEquals, x == y);
            Assert.Equal(expectedEquals, y == x);

            Assert.Equal(!expectedEquals, x != y);
            Assert.Equal(!expectedEquals, y != x);

            Assert.Equal(expectedEquals, x.Equals(y));
            Assert.Equal(expectedEquals, y.Equals(x));

            Assert.Equal(expectedEquals, x.Equals((object)y));
            Assert.Equal(expectedEquals, y.Equals((object)x));

            VerifyCompareResult(expectedResult, x.CompareTo(y), "x.CompareTo(y)");
            VerifyCompareResult(-expectedResult, y.CompareTo(x), "y.CompareTo(x)");

            if (expectedEquals)
            {
                Assert.Equal(x.GetHashCode(), y.GetHashCode());
                Assert.Equal(x.ToString(), y.ToString());
            }
            else
            {
                Assert.NotEqual(x.GetHashCode(), y.GetHashCode());
                Assert.NotEqual(x.ToString(), y.ToString());
            }

            Assert.Equal(x.GetHashCode(), x.GetHashCode());
            Assert.Equal(y.GetHashCode(), y.GetHashCode());

            Assert.Equal(expectedLessThan, x < y);
            Assert.Equal(expectedGreaterThan, y < x);

            Assert.Equal(expectedGreaterThan, x > y);
            Assert.Equal(expectedLessThan, y > x);

            Assert.Equal(expectedLessThan || expectedEquals, x <= y);
            Assert.Equal(expectedGreaterThan || expectedEquals, y <= x);

            Assert.Equal(expectedGreaterThan || expectedEquals, x >= y);
            Assert.Equal(expectedLessThan || expectedEquals, y >= x);
        }
        
        private static void VerifyCompareResult(int expected, int actual, string message)
        {
            if (0 == expected)
            {
                Assert.Equal(expected, actual);
            }
            else if (0 > expected)
            {
                Assert.InRange(actual, int.MinValue, -1);
            }
            else if (0 < expected)
            {
                Assert.InRange(actual, 1, int.MaxValue);
            }
        }

        private static int GetRandomInputForComparison(Random random, out BigInteger bigInteger1, out BigInteger bigInteger2)
        {
            byte[] byteArray1, byteArray2;
            bool sameSize = 0 == random.Next(0, 2);

            if (sameSize)
            {
                int size = random.Next(0, 1024);
                byteArray1 = GetRandomByteArray(random, size);
                byteArray2 = GetRandomByteArray(random, size);
            }
            else
            {
                byteArray1 = GetRandomByteArray(random);
                byteArray2 = GetRandomByteArray(random);
            }

            bigInteger1 = new BigInteger(byteArray1);
            bigInteger2 = new BigInteger(byteArray2);

            if (bigInteger1 > 0 && bigInteger2 > 0)
            {
                if (byteArray1.Length < byteArray2.Length)
                {
                    for (int i = byteArray2.Length - 1; byteArray1.Length <= i; --i)
                    {
                        if (0 != byteArray2[i])
                        {
                            return -1;
                        }
                    }
                }
                else if (byteArray1.Length > byteArray2.Length)
                {
                    for (int i = byteArray1.Length - 1; byteArray2.Length <= i; --i)
                    {
                        if (0 != byteArray1[i])
                        {
                            return 1;
                        }
                    }
                }
            }
            else if ((bigInteger1 < 0 && bigInteger2 > 0) || (bigInteger1 == 0 && bigInteger2 > 0) || (bigInteger1 < 0 && bigInteger2 == 0))
            {
                return -1;
            }
            else if ((bigInteger1 > 0 && bigInteger2 < 0) || (bigInteger1 == 0 && bigInteger2 < 0) || (bigInteger1 > 0 && bigInteger2 == 0))
            {
                return 1;
            }
            else if (bigInteger1 != 0 && bigInteger2 != 0)
            {
                if (byteArray1.Length < byteArray2.Length)
                {
                    for (int i = byteArray2.Length - 1; byteArray1.Length <= i; --i)
                    {
                        if (0xFF != byteArray2[i])
                        {
                            return 1;
                        }
                    }
                }
                else if (byteArray1.Length > byteArray2.Length)
                {
                    for (int i = byteArray1.Length - 1; byteArray2.Length <= i; --i)
                    {
                        if (0xFF != byteArray1[i])
                        {
                            return -1;
                        }
                    }
                }
            }

            for (int i = Math.Min(byteArray1.Length, byteArray2.Length) - 1; 0 <= i; --i)
            {
                if (byteArray1[i] > byteArray2[i])
                {
                    return 1;
                }
                else if (byteArray1[i] < byteArray2[i])
                {
                    return -1;
                }
            }

            return 0;
        }

        private static byte[] GetRandomByteArray(Random random)
        {
            return GetRandomByteArray(random, random.Next(0, 1024));
        }

        private static byte[] GetRandomByteArray(Random random, int size)
        {
            return MyBigIntImp.GetRandomByteArray(random, size);
        }
    }
}
