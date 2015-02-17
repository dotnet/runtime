// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class BigIntegerConstructorTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);


        [Fact]
        public static void RunCtorInt32Tests()
        {
            // ctor(Int32): Int32.MinValue
            Assert.True(VerifyCtorInt32(Int32.MinValue), " Verification Failed");

            // ctor(Int32): -1
            Assert.True(VerifyCtorInt32((Int32)(-1)), " Verification Failed");

            // ctor(Int32): 0
            Assert.True(VerifyCtorInt32((Int32)0), " Verification Failed");

            // ctor(Int32): 1
            Assert.True(VerifyCtorInt32((Int32)1), " Verification Failed");

            // ctor(Int32): Int32.MaxValue
            Assert.True(VerifyCtorInt32(Int32.MaxValue), " Verification Failed");

            // ctor(Int32): Random Positive
            for (int i = 0; i < s_samples; ++i)
            {
                Assert.True(VerifyCtorInt32((Int32)s_random.Next(1, Int32.MaxValue)), " Verification Failed");
            }

            // ctor(Int32): Random Negative
            for (int i = 0; i < s_samples; ++i)
            {
                Assert.True(VerifyCtorInt32((Int32)s_random.Next(Int32.MinValue, 0)), " Verification Failed");
            }
        }
        private static bool VerifyCtorInt32(Int32 value)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = new BigInteger(value);

            if (!bigInteger.Equals(value))
            {
                Console.WriteLine("Expected BigInteger {0} to be equal to Int32 {1}", bigInteger, value);
                ret = false;
            }
            if (String.CompareOrdinal(value.ToString(), bigInteger.ToString()) != 0)
            {
                Console.WriteLine("Int32.ToString() and BigInteger.ToString() on {0} and {1} should be equal", value, bigInteger);
                ret = false;
            }
            if (value != (Int32)bigInteger)
            {
                Console.WriteLine("Expected BigInteger {0} to be equal to Int32 {1}", bigInteger, value);
                ret = false;
            }

            if (value != Int32.MaxValue)
            {
                if ((Int32)(value + 1) != (Int32)(bigInteger + 1))
                {
                    Console.WriteLine("Adding 1 to both {0} and {1} should remain equal", value, bigInteger);
                    ret = false;
                }
            }

            if (value != Int32.MinValue)
            {
                if ((Int32)(value - 1) != (Int32)(bigInteger - 1))
                {
                    Console.WriteLine("Subtracting 1 from both {0} and {1} should remain equal", value, bigInteger);
                    ret = false;
                }
            }

            Assert.True(VerifyBigintegerUsingIdentities(bigInteger, 0 == value), " Verification Failed");

            return ret;
        }

        [Fact]
        public static void RunCtorInt64Tests()
        {
            // ctor(Int64): Int64.MinValue
            Assert.True(VerifyCtorInt64(Int64.MinValue), " Verification Failed");

            // ctor(Int64): Int32.MinValue-1
            Assert.True(VerifyCtorInt64(((Int64)Int32.MinValue) - 1), " Verification Failed");

            // ctor(Int64): Int32.MinValue
            Assert.True(VerifyCtorInt64((Int64)Int32.MinValue), " Verification Failed");

            // ctor(Int64): -1
            Assert.True(VerifyCtorInt64((Int64)(-1)), " Verification Failed");

            // ctor(Int64): 0
            Assert.True(VerifyCtorInt64((Int64)0), " Verification Failed");

            // ctor(Int64): 1
            Assert.True(VerifyCtorInt64((Int64)1), " Verification Failed");

            // ctor(Int64): Int32.MaxValue
            Assert.True(VerifyCtorInt64((Int64)Int32.MaxValue), " Verification Failed");

            // ctor(Int64): Int32.MaxValue+1
            Assert.True(VerifyCtorInt64(((Int64)Int32.MaxValue) + 1), " Verification Failed");

            // ctor(Int64): Int64.MaxValue
            Assert.True(VerifyCtorInt64(Int64.MaxValue), " Verification Failed");

            // ctor(Int64): Random Positive
            for (int i = 0; i < s_samples; ++i)
            {
                Assert.True(VerifyCtorInt64((Int64)(Int64.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // ctor(Int64): Random Negative
            for (int i = 0; i < s_samples; ++i)
            {
                Assert.True(VerifyCtorInt64((Int64)(Int64.MaxValue * s_random.NextDouble()) - Int64.MaxValue), " Verification Failed");
            }
        }
        private static bool VerifyCtorInt64(Int64 value)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = new BigInteger(value);

            if (!bigInteger.Equals(value))
            {
                Console.WriteLine("Expected BigInteger {0} to be equal to Int64 {1}", bigInteger, value);
                ret = false;
            }
            if (String.CompareOrdinal(value.ToString(), bigInteger.ToString()) != 0)
            {
                Console.WriteLine("Int64.ToString() and BigInteger.ToString() on {0} and {1} should be equal", value, bigInteger);
                ret = false;
            }
            if (value != (Int64)bigInteger)
            {
                Console.WriteLine("Expected BigInteger {0} to be equal to Int64 {1}", bigInteger, value);
                ret = false;
            }

            if (value != Int64.MaxValue)
            {
                if ((Int64)(value + 1) != (Int64)(bigInteger + 1))
                {
                    Console.WriteLine("Adding 1 to both {0} and {1} should remain equal", value, bigInteger);
                    ret = false;
                }
            }

            if (value != Int64.MinValue)
            {
                if ((Int64)(value - 1) != (Int64)(bigInteger - 1))
                {
                    Console.WriteLine("Subtracting 1 from both {0} and {1} should remain equal", value, bigInteger);
                    ret = false;
                }
            }

            Assert.True(VerifyBigintegerUsingIdentities(bigInteger, 0 == value), " Verification Failed");
            return ret;
        }

        [Fact]
        public static void RunCtorUInt32Tests()
        {
            // ctor(UInt32): UInt32.MinValue
            Assert.True(VerifyCtorUInt32(UInt32.MinValue), " Verification Failed");

            // ctor(UInt32): 0
            Assert.True(VerifyCtorUInt32((UInt32)0), " Verification Failed");

            // ctor(UInt32): 1
            Assert.True(VerifyCtorUInt32((UInt32)1), " Verification Failed");

            // ctor(UInt32): Int32.MaxValue
            Assert.True(VerifyCtorUInt32((UInt32)Int32.MaxValue), " Verification Failed");

            // ctor(UInt32): Int32.MaxValue+1
            Assert.True(VerifyCtorUInt32(((UInt32)Int32.MaxValue) + 1), " Verification Failed");

            // ctor(UInt32): UInt32.MaxValue
            Assert.True(VerifyCtorUInt32(UInt32.MaxValue), " Verification Failed");

            // ctor(UInt32): Random Positive
            for (int i = 0; i < s_samples; ++i)
            {
                Assert.True(VerifyCtorUInt32((UInt32)(UInt32.MaxValue * s_random.NextDouble())), " Verification Failed");
            }
        }
        private static bool VerifyCtorUInt32(UInt32 value)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = new BigInteger(value);

            if (!bigInteger.Equals(value))
            {
                Console.WriteLine("Expected BigInteger {0} to be equal to UInt32 {1}", bigInteger, value);
                ret = false;
            }
            if (String.CompareOrdinal(value.ToString(), bigInteger.ToString()) != 0)
            {
                Console.WriteLine("UInt32.ToString() and BigInteger.ToString() on {0} and {1} should be equal", value, bigInteger);
                ret = false;
            }
            if (value != (UInt32)bigInteger)
            {
                Console.WriteLine("Expected BigInteger {0} to be equal to UInt32 {1}", bigInteger, value);
                ret = false;
            }

            if (value != UInt32.MaxValue)
            {
                if ((UInt32)(value + 1) != (UInt32)(bigInteger + 1))
                {
                    Console.WriteLine("Adding 1 to both {0} and {1} should remain equal", value, bigInteger);
                    ret = false;
                }
            }

            if (value != UInt32.MinValue)
            {
                if ((UInt32)(value - 1) != (UInt32)(bigInteger - 1))
                {
                    Console.WriteLine("Subtracting 1 from both {0} and {1} should remain equal", value, bigInteger);
                    ret = false;
                }
            }

            Assert.True(VerifyBigintegerUsingIdentities(bigInteger, 0 == value), " Verification Failed");
            return ret;
        }

        [Fact]
        public static void RunCtorUInt64Tests()
        {
            // ctor(UInt64): UInt64.MinValue
            Assert.True(VerifyCtorUInt64(UInt64.MinValue), " Verification Failed");

            // ctor(UInt64): 0
            Assert.True(VerifyCtorUInt64((UInt64)0), " Verification Failed");

            // ctor(UInt64): 1
            Assert.True(VerifyCtorUInt64((UInt64)1), " Verification Failed");

            // ctor(UInt64): Int32.MaxValue
            Assert.True(VerifyCtorUInt64((UInt64)Int32.MaxValue), " Verification Failed");

            // ctor(UInt64): Int32.MaxValue+1
            Assert.True(VerifyCtorUInt64(((UInt64)Int32.MaxValue) + 1), " Verification Failed");

            // ctor(UInt64): UInt64.MaxValue
            Assert.True(VerifyCtorUInt64(UInt64.MaxValue), " Verification Failed");

            // ctor(UInt64): Random Positive
            for (int i = 0; i < s_samples; ++i)
            {
                Assert.True(VerifyCtorUInt64((UInt64)(UInt64.MaxValue * s_random.NextDouble())), " Verification Failed");
            }
        }
        private static bool VerifyCtorUInt64(UInt64 value)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = new BigInteger(value);

            if (!bigInteger.Equals(value))
            {
                Console.WriteLine("Expected BigInteger {0} to be equal to UInt64 {1}", bigInteger, value);
                ret = false;
            }
            if (String.CompareOrdinal(value.ToString(), bigInteger.ToString()) != 0)
            {
                Console.WriteLine("UInt64.ToString() and BigInteger.ToString() on {0} and {1} should be equal", value, bigInteger);
                ret = false;
            }
            if (value != (UInt64)bigInteger)
            {
                Console.WriteLine("Expected BigInteger {0} to be equal to UInt64 {1}", bigInteger, value);
                ret = false;
            }

            if (value != UInt64.MaxValue)
            {
                if ((UInt64)(value + 1) != (UInt64)(bigInteger + 1))
                {
                    Console.WriteLine("Adding 1 to both {0} and {1} should remain equal", value, bigInteger);
                    ret = false;
                }
            }

            if (value != UInt64.MinValue)
            {
                if ((UInt64)(value - 1) != (UInt64)(bigInteger - 1))
                {
                    Console.WriteLine("Subtracting 1 from both {0} and {1} should remain equal", value, bigInteger);
                    ret = false;
                }
            }

            Assert.True(VerifyBigintegerUsingIdentities(bigInteger, 0 == value), " Verification Failed");
            return ret;
        }

        [Fact]
        public static void RunCtorSingleTests()
        {
            Single value;

            // ctor(Single): Single.Minvalue
            Assert.True(VerifyCtorSingle(Single.MinValue), " Verification Failed");

            // ctor(Single): Int32.MinValue-1
            Assert.True(VerifyCtorSingle(((Single)Int32.MinValue) - 1), " Verification Failed");

            // ctor(Single): Int32.MinValue
            Assert.True(VerifyCtorSingle(((Single)Int32.MinValue)), " Verification Failed");

            // ctor(Single): Int32.MinValue+1
            Assert.True(VerifyCtorSingle(((Single)Int32.MinValue) + 1), " Verification Failed");

            // ctor(Single): -1
            Assert.True(VerifyCtorSingle((Single)(-1)), " Verification Failed");

            // ctor(Single): 0
            Assert.True(VerifyCtorSingle((Single)0), " Verification Failed");

            // ctor(Single): 1
            Assert.True(VerifyCtorSingle((Single)1), " Verification Failed");

            // ctor(Single): Int32.MaxValue-1
            Assert.True(VerifyCtorSingle(((Single)Int32.MaxValue) - 1), " Verification Failed");

            // ctor(Single): Int32.MaxValue
            Assert.True(VerifyCtorSingle(((Single)Int32.MaxValue)), " Verification Failed");

            // ctor(Single): Int32.MaxValue+1
            Assert.True(VerifyCtorSingle(((Single)Int32.MaxValue) + 1), " Verification Failed");

            // ctor(Single): Single.MaxValue
            Assert.True(VerifyCtorSingle(Single.MaxValue), " Verification Failed");

            // ctor(Single): Random Positive
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorSingle((Single)(Single.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // ctor(Single): Random Negative
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorSingle(((Single)(Single.MaxValue * s_random.NextDouble())) - Single.MaxValue), " Verification Failed");
            }

            // ctor(Single): Small Random Positive with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorSingle((Single)(s_random.Next(0, 100) + s_random.NextDouble())), " Verification Failed");
            }

            // ctor(Single): Small Random Negative with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorSingle(((Single)(s_random.Next(-100, 0) - s_random.NextDouble()))), " Verification Failed");
            }

            // ctor(Single): Large Random Positive with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorSingle((Single)((Single.MaxValue * s_random.NextDouble()) + s_random.NextDouble())), " Verification Failed");
            }

            // ctor(Single): Large Random Negative with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorSingle(((Single)((-(Single.MaxValue - 1) * s_random.NextDouble()) - s_random.NextDouble()))), " Verification Failed");
            }

            // ctor(Single): Single.Epsilon
            Assert.True(VerifyCtorSingle(Single.Epsilon), " Verification Failed");

            // ctor(Single): Single.NegativeInfinity
            Assert.Throws<OverflowException>(() => new BigInteger(Single.NegativeInfinity));

            // ctor(Single): Single.PositiveInfinity
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(Single.PositiveInfinity);
            });

            // ctor(Single): Single.NaN
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(Single.NaN);
            });

            // ctor(Single): Single.NaN 2
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(ConvertInt32ToSingle(0x7FC00000));
            });

            // ctor(Single): Smallest Exponent
            Assert.True(VerifyCtorSingle((Single)Math.Pow(2, -126)), " Verification Failed");

            // ctor(Single): Largest Exponent
            Assert.True(VerifyCtorSingle((Single)Math.Pow(2, 127)), " Verification Failed");

            // ctor(Single): Largest number less then 1
            value = 0;
            for (int i = 1; i <= 24; ++i)
            {
                value += (Single)(Math.Pow(2, -i));
            }
            Assert.True(VerifyCtorSingle(value), " Verification Failed");

            // ctor(Single): Smallest number greater then 1
            value = (Single)(1 + Math.Pow(2, -23));
            Assert.True(VerifyCtorSingle(value), " Verification Failed");

            // ctor(Single): Largest number less then 2
            value = 0;
            for (int i = 1; i <= 23; ++i)
            {
                value += (Single)(Math.Pow(2, -i));
            }
            value += 1;
            Assert.True(VerifyCtorSingle(value), " Verification Failed");
        }
        private static bool VerifyCtorSingle(Single value)
        {
            bool ret = true;
            BigInteger bigInteger;
            Single expectedValue;

            if (value < 0)
            {
                expectedValue = (Single)Math.Ceiling(value);
            }
            else
            {
                expectedValue = (Single)Math.Floor(value);
            }

            bigInteger = new BigInteger(value);

            if (expectedValue != (Single)bigInteger)
            {
                Console.WriteLine("Expected BigInteger {0} to be equal to Single {1}", bigInteger, expectedValue);
                ret = false;
            }

            // Single can only accurately represent integers between -16777216 and 16777216 exclusive.
            // ToString starts to become inaccurate at this point.
            if (expectedValue < 16777216 && -16777216 < expectedValue)
            {
                if (!expectedValue.ToString("G9").Equals(bigInteger.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    ret = false;
                }
            }

            Assert.True(VerifyBigintegerUsingIdentities(bigInteger, 0 == expectedValue), " Verification Failed");
            return ret;
        }

        [Fact]
        public static void RunCtorDoubleTests()
        {
            Double value;

            // ctor(Double): Double.Minvalue
            Assert.True(VerifyCtorDouble(Double.MinValue), " Verification Failed");

            // ctor(Double): Single.Minvalue
            Assert.True(VerifyCtorDouble((Double)Single.MinValue), " Verification Failed");

            // ctor(Double): Int64.MinValue-1
            Assert.True(VerifyCtorDouble(((Double)Int64.MinValue) - 1), " Verification Failed");

            // ctor(Double): Int64.MinValue
            Assert.True(VerifyCtorDouble(((Double)Int64.MinValue)), " Verification Failed");

            // ctor(Double): Int64.MinValue+1
            Assert.True(VerifyCtorDouble(((Double)Int64.MinValue) + 1), " Verification Failed");

            // ctor(Double): Int32.MinValue-1
            Assert.True(VerifyCtorDouble(((Double)Int32.MinValue) - 1), " Verification Failed");

            // ctor(Double): Int32.MinValue
            Assert.True(VerifyCtorDouble(((Double)Int32.MinValue)), " Verification Failed");

            // ctor(Double): Int32.MinValue+1
            Assert.True(VerifyCtorDouble(((Double)Int32.MinValue) + 1), " Verification Failed");

            // ctor(Double): -1
            Assert.True(VerifyCtorDouble((Double)(-1)), " Verification Failed");

            // ctor(Double): 0
            Assert.True(VerifyCtorDouble((Double)0), " Verification Failed");

            // ctor(Double): 1
            Assert.True(VerifyCtorDouble((Double)1), " Verification Failed");

            // ctor(Double): Int32.MaxValue-1
            Assert.True(VerifyCtorDouble(((Double)Int32.MaxValue) - 1), " Verification Failed");

            // ctor(Double): Int32.MaxValue
            Assert.True(VerifyCtorDouble(((Double)Int32.MaxValue)), " Verification Failed");

            // ctor(Double): Int32.MaxValue+1
            Assert.True(VerifyCtorDouble(((Double)Int32.MaxValue) + 1), " Verification Failed");

            // ctor(Double): Int64.MaxValue-1
            Assert.True(VerifyCtorDouble(((Double)Int64.MaxValue) - 1), " Verification Failed");

            // ctor(Double): Int64.MaxValue
            Assert.True(VerifyCtorDouble(((Double)Int64.MaxValue)), " Verification Failed");

            // ctor(Double): Int64.MaxValue+1
            Assert.True(VerifyCtorDouble(((Double)Int64.MaxValue) + 1), " Verification Failed");

            // ctor(Double): Single.MaxValue
            Assert.True(VerifyCtorDouble((Double)Single.MaxValue), " Verification Failed");

            // ctor(Double): Double.MaxValue
            Assert.True(VerifyCtorDouble(Double.MaxValue), " Verification Failed");

            // ctor(Double): Random Positive
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorDouble((Double)(Double.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // ctor(Double): Random Negative
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorDouble((Double.MaxValue * s_random.NextDouble()) - Double.MaxValue), " Verification Failed");
            }

            // ctor(Double): Small Random Positive with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorDouble((Double)(s_random.Next(0, 100) + s_random.NextDouble())), " Verification Failed");
            }

            // ctor(Double): Small Random Negative with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorDouble(((Double)(s_random.Next(-100, 0) - s_random.NextDouble()))), " Verification Failed");
            }

            // ctor(Double): Large Random Positive with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorDouble((Double)((Int64.MaxValue / 100 * s_random.NextDouble()) + s_random.NextDouble())), " Verification Failed");
            }

            // ctor(Double): Large Random Negative with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorDouble(((Double)((-(Int64.MaxValue / 100) * s_random.NextDouble()) - s_random.NextDouble()))), " Verification Failed");
            }

            // ctor(Double): Double.Epsilon
            Assert.True(VerifyCtorDouble(Double.Epsilon), " Verification Failed");

            // ctor(Double): Double.NegativeInfinity
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(Double.NegativeInfinity);
            });

            // ctor(Double): Double.PositiveInfinity
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(Double.PositiveInfinity);
            });

            // ctor(Double): Double.NaN
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(Double.NaN);
            });

            // ctor(Double): Double.NaN 2
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(ConvertInt64ToDouble(0x7FF8000000000000));
            });

            // ctor(Double): Smallest Exponent
            Assert.True(VerifyCtorDouble((Double)Math.Pow(2, -1022)), " Verification Failed");

            // ctor(Double): Largest Exponent
            Assert.True(VerifyCtorDouble((Double)Math.Pow(2, 1023)), " Verification Failed");

            // ctor(Double): Largest number less then 1
            value = 0;
            for (int i = 1; i <= 53; ++i)
            {
                value += (Double)(Math.Pow(2, -i));
            }
            Assert.True(VerifyCtorDouble(value), " Verification Failed");

            // ctor(Double): Smallest number greater then 1
            value = (Double)(1 + Math.Pow(2, -52));
            Assert.True(VerifyCtorDouble(value), " Verification Failed");

            // ctor(Double): Largest number less then 2
            value = 0;
            for (int i = 1; i <= 52; ++i)
            {
                value += (Double)(Math.Pow(2, -i));
            }
            value += 2;
            Assert.True(VerifyCtorDouble(value), " Verification Failed");
        }
        private static bool VerifyCtorDouble(Double value)
        {
            bool ret = true;
            BigInteger bigInteger;
            Double expectedValue;

            if (value < 0)
            {
                expectedValue = (Double)Math.Ceiling(value);
            }
            else
            {
                expectedValue = (Double)Math.Floor(value);
            }

            bigInteger = new BigInteger(value);

            if (expectedValue != (Double)bigInteger)
            {
                Console.WriteLine("Expected BigInteger {0} to be equal to Double {1}", bigInteger, expectedValue);
                ret = false;
            }

            // Single can only accurately represent integers between -16777216 and 16777216 exclusive.
            // ToString starts to become inaccurate at this point.
            if (expectedValue < 9007199254740992 && -9007199254740992 < expectedValue)
            {
                if (!expectedValue.ToString("G17").Equals(bigInteger.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Single.ToString() and BigInteger.ToString() not equal");
                    ret = false;
                }
            }

            Assert.True(VerifyBigintegerUsingIdentities(bigInteger, 0 == expectedValue), " Verification Failed");
            return ret;
        }

        [Fact]
        public static void RunCtorDecimalTests()
        {
            Decimal value;

            // ctor(Decimal): Decimal.MinValue
            Assert.True(VerifyCtorDecimal(Decimal.MinValue), " Verification Failed");

            // ctor(Decimal): -1
            Assert.True(VerifyCtorDecimal(-1), " Verification Failed");

            // ctor(Decimal): 0
            Assert.True(VerifyCtorDecimal(0), " Verification Failed");

            // ctor(Decimal): 1
            Assert.True(VerifyCtorDecimal(1), " Verification Failed");

            // ctor(Decimal): Decimal.MaxValue
            Assert.True(VerifyCtorDecimal(Decimal.MaxValue), " Verification Failed");

            // ctor(Decimal): Random Positive
            for (int i = 0; i < s_samples; i++)
            {
                value = new Decimal(
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    false,
                    (byte)s_random.Next(0, 29));
                Assert.True(VerifyCtorDecimal(value), " Verification Failed");
            }

            // ctor(Decimal): Random Negative
            for (int i = 0; i < s_samples; i++)
            {
                value = new Decimal(
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    true,
                    (byte)s_random.Next(0, 29));
                Assert.True(VerifyCtorDecimal(value), " Verification Failed");
            }

            // ctor(Decimal): Smallest Exponent
            unchecked
            {
                value = new Decimal(1, 0, 0, false, 0);
            }
            Assert.True(VerifyCtorDecimal(value), " Verification Failed");

            // ctor(Decimal): Largest Exponent and zero integer
            unchecked
            {
                value = new Decimal(0, 0, 0, false, 28);
            }
            Assert.True(VerifyCtorDecimal(value), " Verification Failed");

            // ctor(Decimal): Largest Exponent and non zero integer
            unchecked
            {
                value = new Decimal(1, 0, 0, false, 28);
            }
            Assert.True(VerifyCtorDecimal(value), " Verification Failed");

            // ctor(Decimal): Largest number less then 1
            value = 1 - new Decimal(1, 0, 0, false, 28);
            Assert.True(VerifyCtorDecimal(value), " Verification Failed");

            // ctor(Decimal): Smallest number greater then 1
            value = 1 + new Decimal(1, 0, 0, false, 28);
            Assert.True(VerifyCtorDecimal(value), " Verification Failed");

            // ctor(Decimal): Largest number less then 2
            value = 2 - new Decimal(1, 0, 0, false, 28);
            Assert.True(VerifyCtorDecimal(value), " Verification Failed");
        }
        private static bool VerifyCtorDecimal(Decimal value)
        {
            bool ret = true;
            Decimal expectedValue;
            BigInteger bigInteger;

            if (value < 0)
            {
                expectedValue = Math.Ceiling(value);
            }
            else
            {
                expectedValue = Math.Floor(value);
            }

            bigInteger = new BigInteger(value);

            if (!expectedValue.ToString().Equals(bigInteger.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Decimal.ToString() and BigInteger.ToString()");
                ret = false;
            }
            if (expectedValue != (Decimal)bigInteger)
            {
                Console.WriteLine("Round tripped Decimal");
                ret = false;
            }

            if (expectedValue != Math.Floor(Decimal.MaxValue))
            {
                if ((Decimal)(expectedValue + 1) != (Decimal)(bigInteger + 1))
                {
                    Console.WriteLine("BigInteger added to 1");
                    ret = false;
                }
            }

            if (expectedValue != Math.Ceiling(Decimal.MinValue))
            {
                if ((Decimal)(expectedValue - 1) != (Decimal)(bigInteger - 1))
                {
                    Console.WriteLine("BigInteger subtracted by 1");
                    ret = false;
                }
            }

            Assert.True(VerifyBigintegerUsingIdentities(bigInteger, 0 == expectedValue), " Verification Failed");
            return ret;
        }

        [Fact]
        public static void RunCtorByteArrayTests()
        {
            UInt64 tempUInt64;
            byte[] tempByteArray;

            // ctor(byte[]): array is null
            Assert.Throws<ArgumentNullException>(() =>
            {
                BigInteger bigInteger = new BigInteger((byte[])null);
            });

            // ctor(byte[]): array is empty
            Assert.True(VerifyCtorByteArray(new byte[0], 0), " Verification Failed");

            // ctor(byte[]): array is 1 byte
            tempUInt64 = (UInt32)s_random.Next(0, 256);
            tempByteArray = BitConverter.GetBytes(tempUInt64);
            if (tempByteArray[0] > 127)
            {
                Assert.True(VerifyCtorByteArray(new byte[] { tempByteArray[0] }), " Verification Failed");
                Assert.True(VerifyCtorByteArray(new byte[] { tempByteArray[0], 0 }, tempUInt64), " Verification Failed");
            }
            else
            {
                Assert.True(VerifyCtorByteArray(new byte[] { tempByteArray[0] }, tempUInt64), " Verification Failed");
            }

            // ctor(byte[]): Small array with all zeros
            Assert.True(VerifyCtorByteArray(new byte[] { 0, 0, 0, 0 }), " Verification Failed");

            // ctor(byte[]): Large array with all zeros
            Assert.True(VerifyCtorByteArray(
                new byte[] {
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0}), "Test Failed");

            // ctor(byte[]): Small array with all ones
            Assert.True(VerifyCtorByteArray(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }), " Verification Failed");

            // ctor(byte[]): Large array with all ones
            Assert.True(VerifyCtorByteArray(
                new byte[] {
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF}), "Test Failed");

            // ctor(byte[]): array with a lot of leading zeros
            for (int i = 0; i < s_samples; i++)
            {
                tempUInt64 = (UInt32)s_random.Next(Int32.MinValue, Int32.MaxValue);
                tempByteArray = BitConverter.GetBytes(tempUInt64);

                Assert.True(VerifyCtorByteArray(
                    new byte[] {
                    tempByteArray[0],
                    tempByteArray[1],
                    tempByteArray[2],
                    tempByteArray[3],
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0},
                    tempUInt64), "Test Failed");
            }

            // ctor(byte[]): array 4 bytes
            for (int i = 0; i < s_samples; i++)
            {
                tempUInt64 = (UInt32)s_random.Next(Int32.MinValue, Int32.MaxValue);
                tempByteArray = BitConverter.GetBytes(tempUInt64);

                if (tempUInt64 > Int32.MaxValue)
                {
                    Assert.True(VerifyCtorByteArray(
                       new byte[] {
                    tempByteArray[0],
                    tempByteArray[1],
                    tempByteArray[2],
                    tempByteArray[3]}), "Test Failed");
                    Assert.True(VerifyCtorByteArray(
                       new byte[] {
                    tempByteArray[0],
                    tempByteArray[1],
                    tempByteArray[2],
                    tempByteArray[3],
                    0},
                        tempUInt64), "Test Failed");
                }
                else
                {
                    Assert.True(VerifyCtorByteArray(
                        new byte[] {
                    tempByteArray[0],
                    tempByteArray[1],
                    tempByteArray[2],
                    tempByteArray[3]},
                        tempUInt64), "Test Failed");
                }
            }

            // ctor(byte[]): array 5 bytes
            for (int i = 0; i < s_samples; i++)
            {
                tempUInt64 = (UInt32)s_random.Next(Int32.MinValue, Int32.MaxValue);
                tempUInt64 <<= 8;
                tempUInt64 += (UInt64)s_random.Next(0, 256);
                tempByteArray = BitConverter.GetBytes(tempUInt64);

                if (tempUInt64 >= (UInt64)0x00080000)
                {
                    Assert.True(VerifyCtorByteArray(
                        new byte[] {
                    tempByteArray[0],
                    tempByteArray[1],
                    tempByteArray[2],
                    tempByteArray[3],
                    tempByteArray[4]}), "Test Failed");
                    Assert.True(VerifyCtorByteArray(
                        new byte[] {
                    tempByteArray[0],
                    tempByteArray[1],
                    tempByteArray[2],
                    tempByteArray[3],
                    tempByteArray[4],
                    0},
                        tempUInt64), "Test Failed");
                }
                else
                {
                    Assert.True(VerifyCtorByteArray(
                        new byte[] {
                    tempByteArray[0],
                    tempByteArray[1],
                    tempByteArray[2],
                    tempByteArray[3],
                    tempByteArray[4]},
                        tempUInt64), "Test Failed");
                }
            }

            // ctor(byte[]): array 8 bytes
            for (int i = 0; i < s_samples; i++)
            {
                tempUInt64 = (UInt32)s_random.Next(Int32.MinValue, Int32.MaxValue);
                tempUInt64 <<= 32;
                tempUInt64 += (UInt32)s_random.Next(Int32.MinValue, Int32.MaxValue);
                tempByteArray = BitConverter.GetBytes(tempUInt64);

                if (tempUInt64 > Int64.MaxValue)
                {
                    Assert.True(VerifyCtorByteArray(
                        new byte[] {
                    tempByteArray[0],
                    tempByteArray[1],
                    tempByteArray[2],
                    tempByteArray[3],
                    tempByteArray[4],
                    tempByteArray[5],
                    tempByteArray[6],
                    tempByteArray[7]}), "Test Failed");
                    Assert.True(VerifyCtorByteArray(
                        new byte[] {
                    tempByteArray[0],
                    tempByteArray[1],
                    tempByteArray[2],
                    tempByteArray[3],
                    tempByteArray[4],
                    tempByteArray[5],
                    tempByteArray[6],
                    tempByteArray[7],
                    0},
                        tempUInt64), "Test Failed");
                }
                else
                {
                    Assert.True(VerifyCtorByteArray(
                        new byte[] {
                    tempByteArray[0],
                    tempByteArray[1],
                    tempByteArray[2],
                    tempByteArray[3],
                    tempByteArray[4],
                    tempByteArray[5],
                    tempByteArray[6],
                    tempByteArray[7]},
                        tempUInt64), "Test Failed");
                }
            }

            // ctor(byte[]): array 9 bytes
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyCtorByteArray(
                    new byte[] {
                    (byte)s_random.Next(0, 256),
                    (byte)s_random.Next(0, 256),
                    (byte)s_random.Next(0, 256),
                    (byte)s_random.Next(0, 256),
                    (byte)s_random.Next(0, 256),
                    (byte)s_random.Next(0, 256),
                    (byte)s_random.Next(0, 256),
                    (byte)s_random.Next(0, 256),
                    (byte)s_random.Next(0, 256)}), "Test Failed");
            }

            // ctor(byte[]): array is UInt32.MaxValue
            Assert.True(VerifyCtorByteArray(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0 }, UInt32.MaxValue), " Verification Failed");

            // ctor(byte[]): array is UInt32.MaxValue + 1
            Assert.True(VerifyCtorByteArray(new byte[] { 0, 0, 0, 0, 1 }, (UInt64)UInt32.MaxValue + 1), " Verification Failed");

            // ctor(byte[]): array is UInt64.MaxValue
            Assert.True(VerifyCtorByteArray(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0 }, UInt64.MaxValue), " Verification Failed");

            // ctor(byte[]): UInt64.MaxValue + 1
            Assert.True(VerifyCtorByteArray(
                new byte[] {
                0, 0, 0, 0,
                0, 0, 0, 0,
                1}), "Test Failed");

            // ctor(byte[]): UInt64.MaxValue + 2^64
            Assert.True(VerifyCtorByteArray(
                new byte[] {
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF,
                1}), "Test Failed");

            // ctor(byte[]): array is random > UInt64
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray = new byte[s_random.Next(0, 1024)];
                for (int arrayIndex = 0; arrayIndex < tempByteArray.Length; ++arrayIndex)
                {
                    tempByteArray[arrayIndex] = (byte)s_random.Next(0, 256);
                }
                Assert.True(VerifyCtorByteArray(tempByteArray), " Verification Failed");
            }

            // ctor(byte[]): array is large
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray = new byte[s_random.Next(16384, 2097152)];
                for (int arrayIndex = 0; arrayIndex < tempByteArray.Length; ++arrayIndex)
                {
                    tempByteArray[arrayIndex] = (byte)s_random.Next(0, 256);
                }
                Assert.True(VerifyCtorByteArray(tempByteArray), " Verification Failed");
            }
        }
        private static bool VerifyCtorByteArray(byte[] value, UInt64 expectedValue)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = new BigInteger(value);

            if (!bigInteger.Equals(expectedValue))
            {
                Console.WriteLine("Expected BigInteger {0} to be equal to UInt64 {1}", bigInteger, expectedValue);
                ret = false;
            }
            if (!expectedValue.ToString().Equals(bigInteger.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("UInt64.ToString() and BigInteger.ToString()");
                ret = false;
            }
            if (expectedValue != (UInt64)bigInteger)
            {
                Console.WriteLine("BigInteger casted to a UInt64");
                ret = false;
            }

            if (expectedValue != UInt64.MaxValue)
            {
                if ((UInt64)(expectedValue + 1) != (UInt64)(bigInteger + 1))
                {
                    Console.WriteLine("BigInteger added to 1");
                    ret = false;
                }
            }

            if (expectedValue != UInt64.MinValue)
            {
                if ((UInt64)(expectedValue - 1) != (UInt64)(bigInteger - 1))
                {
                    Console.WriteLine("BigInteger subtracted by 1");
                    ret = false;
                }
            }

            Assert.True(VerifyCtorByteArray(value), " Verification Failed");
            return ret;
        }


        private static bool VerifyCtorByteArray(byte[] value)
        {
            bool ret = true;
            BigInteger bigInteger;
            byte[] roundTrippedByteArray;
            bool isZero = IsZero(value);

            bigInteger = new BigInteger(value);

            roundTrippedByteArray = bigInteger.ToByteArray();

            for (int i = Math.Min(value.Length, roundTrippedByteArray.Length) - 1; 0 <= i; --i)
            {
                if (value[i] != roundTrippedByteArray[i])
                {
                    Console.WriteLine("Round Tripped ByteArray at {0}", i);
                    ret = false;
                }
            }
            if (value.Length < roundTrippedByteArray.Length)
            {
                for (int i = value.Length; i < roundTrippedByteArray.Length; ++i)
                {
                    if (0 != roundTrippedByteArray[i])
                    {
                        Console.WriteLine("Round Tripped ByteArray is larger than the original array and byte is non zero at {0}", i);
                        ret = false;
                    }
                }
            }
            else if (value.Length > roundTrippedByteArray.Length)
            {
                for (int i = roundTrippedByteArray.Length; i < value.Length; ++i)
                {
                    if (((0 != value[i]) && ((roundTrippedByteArray[roundTrippedByteArray.Length - 1] & 0x80) == 0)) ||
                        ((0xFF != value[i]) && ((roundTrippedByteArray[roundTrippedByteArray.Length - 1] & 0x80) != 0)))
                    {
                        Console.WriteLine("Round Tripped ByteArray is smaller than the original array and byte is non zero at {0}", i);
                        ret = false;
                    }
                }
            }

            if (value.Length < 8)
            {
                byte[] newvalue = new byte[8];

                for (int i = 0; i < 8; i++)
                {
                    if (bigInteger < 0)
                    {
                        newvalue[i] = 0xFF;
                    }
                    else
                    {
                        newvalue[i] = 0;
                    }
                }

                for (int i = 0; i < value.Length; i++)
                {
                    newvalue[i] = value[i];
                }

                value = newvalue;
            }
            else if (value.Length > 8)
            {
                int newlength = value.Length;

                for (; newlength > 8; newlength--)
                {
                    if (bigInteger < 0)
                    {
                        if ((value[newlength - 1] != 0xFF) | ((value[newlength - 2] & 0x80) == 0))
                        {
                            break;
                        }
                    }
                    else
                    {
                        if ((value[newlength - 1] != 0) | ((value[newlength - 2] & 0x80) != 0))
                        {
                            break;
                        }
                    }
                }

                byte[] newvalue = new byte[newlength];

                for (int i = 0; i < newlength; i++)
                {
                    newvalue[i] = value[i];
                }

                value = newvalue;
            }

            if (IsOutOfRangeInt64(value))
            {
                // Try subtracting a value from the BigInteger that will allow it to be represented as an Int64
                byte[] tempByteArray;
                BigInteger tempBigInteger;
                bool isNeg = ((value[value.Length - 1] & 0x80) != 0);

                tempByteArray = new byte[value.Length];
                Array.Copy(value, 8, tempByteArray, 8, value.Length - 8);

                tempBigInteger = bigInteger - (new BigInteger(tempByteArray));

                tempByteArray = new byte[8];
                Array.Copy(value, 0, tempByteArray, 0, 8);

                if (!(((tempByteArray[7] & 0x80) == 0) ^ isNeg))
                {
                    tempByteArray[7] ^= 0x80;
                    tempBigInteger = tempBigInteger + (new BigInteger(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0x80 }));
                }
                if (isNeg & (tempBigInteger > 0))
                {
                    tempBigInteger = tempBigInteger + (new BigInteger(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0xFF }));
                }

                if (BitConverter.ToInt64(tempByteArray, 0) != (Int64)tempBigInteger)
                {
                    Console.WriteLine("BigInteger after subtracting all bits greater then Int64");
                    ret = false;
                }
            }
            else
            {
                if (BitConverter.ToInt64(value, 0) != (Int64)bigInteger)
                {
                    Console.WriteLine("BigInteger not equal to Int64");
                    ret = false;
                }
            }

            if (IsOutOfRangeUInt64(value))
            {
                // Try subtracting a value from the BigInteger that will allow it to be represented as an UInt64
                byte[] tempByteArray;
                BigInteger tempBigInteger;
                bool isNeg = ((value[value.Length - 1] & 0x80) != 0);

                tempByteArray = new byte[value.Length];
                Array.Copy(value, 8, tempByteArray, 8, value.Length - 8);

                tempBigInteger = bigInteger - (new BigInteger(tempByteArray));

                tempByteArray = new byte[8];
                Array.Copy(value, 0, tempByteArray, 0, 8);

                if ((tempByteArray[7] & 0x80) != 0)
                {
                    tempByteArray[7] &= 0x7f;
                    if (tempBigInteger < 0)
                    {
                        tempBigInteger = tempBigInteger - (new BigInteger(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0x80 }));
                    }
                    else
                    {
                        tempBigInteger = tempBigInteger + (new BigInteger(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0x80 }));
                    }
                }

                if (BitConverter.ToUInt64(tempByteArray, 0) != (UInt64)tempBigInteger)
                {
                    Console.WriteLine("BigInteger after subtracting all bits greater then UInt64");
                    ret = false;
                }
            }
            else
            {
                if (BitConverter.ToUInt64(value, 0) != (UInt64)bigInteger)
                {
                    Console.WriteLine("BigInteger not equal to UInt64");
                    ret = false;
                }
            }

            Assert.True(VerifyBigintegerUsingIdentities(bigInteger, isZero), " Verification Failed");

            return ret;
        }

        private static Single ConvertInt32ToSingle(Int32 value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }
        private static Double ConvertInt64ToDouble(Int64 value)
        {
            return BitConverter.ToDouble(BitConverter.GetBytes(value), 0);
        }
        private static bool VerifyBigintegerUsingIdentities(BigInteger bigInteger, bool isZero)
        {
            bool ret = true;
            BigInteger tempBigInteger;

            tempBigInteger = new BigInteger(bigInteger.ToByteArray());

            if (bigInteger != tempBigInteger)
            {
                Console.WriteLine("BigInteger {0} copied using ctor(byte[]) gave different value {1}", bigInteger, tempBigInteger);
                ret = false;
            }

            if (isZero)
            {
                if (BigInteger.Zero != bigInteger)
                {
                    Console.WriteLine("Comparing constructed BigInteger with BigInteger.Zero fails");
                    ret = false;
                }
            }
            else
            {
                if (BigInteger.Zero == bigInteger)
                {
                    Console.WriteLine("Expected BigInteger to not be equal to zero {0}", bigInteger);
                    ret = false;
                }

                if (BigInteger.One != bigInteger / bigInteger)
                {
                    Console.WriteLine("BigInteger divided by itself should be One");
                    ret = false;
                }
            }

            // (x + 1) - 1 = x
            if (bigInteger != (bigInteger + BigInteger.One) - BigInteger.One)
            {
                Console.WriteLine("Add 1 to the BigInteger then subtract 1");
                ret = false;
            }

            // (x + 1) - x = 1
            if (BigInteger.One != (bigInteger + BigInteger.One) - bigInteger)
            {
                Console.WriteLine("Add 1 to the BigInteger then subtract the BigInteger");
                ret = false;
            }

            // x - x = 0
            if (BigInteger.Zero != bigInteger - bigInteger)
            {
                Console.WriteLine("Subtract the BigInteger form itself");
                ret = false;
            }

            // x + x = 2x
            if (2 * bigInteger != bigInteger + bigInteger)
            {
                Console.WriteLine("Expected Adding the BigInteger to istself to be equal to 2 times the BigInteger");
                ret = false;
            }

            // x/1 = x
            if (bigInteger != bigInteger / BigInteger.One)
            {
                Console.WriteLine("BigInteger divided by 1");
                ret = false;
            }

            // 1 * x = x
            if (bigInteger != BigInteger.One * bigInteger)
            {
                Console.WriteLine("BigInteger multiplied by 1");
                ret = false;
            }

            return ret;
        }
        private static bool IsZero(byte[] value)
        {
            for (int i = 0; i < value.Length; ++i)
            {
                if (0 != value[i])
                {
                    return false;
                }
            }

            return true;
        }
        private static bool IsOutOfRangeUInt64(byte[] value)
        {
            if (value.Length == 0)
                return false;
            if ((0x80 & value[value.Length - 1]) != 0)
                return true;

            byte zeroValue = 0;

            if (value.Length <= 8)
            {
                return false;
            }

            for (int i = 8; i < value.Length; ++i)
            {
                if (zeroValue != value[i])
                {
                    return true;
                }
            }

            return false;
        }
        private static bool IsOutOfRangeInt64(byte[] value)
        {
            if (value.Length == 0)
                return false;

            bool isNeg = ((0x80 & value[value.Length - 1]) != 0);
            byte zeroValue = 0;

            if (isNeg)
            {
                zeroValue = 0xFF;
            }

            if (value.Length < 8)
            {
                return false;
            }

            for (int i = 8; i < value.Length; i++)
            {
                if (zeroValue != value[i])
                {
                    return true;
                }
            }

            return (!((0 == (0x80 & value[7])) ^ isNeg));
        }
        private static String Print(byte[] bytes)
        {
            String ret = String.Empty;

            for (int i = 0; i < bytes.Length; i++)
            {
                ret += bytes[i] + " ";
            }

            return ret;
        }
    }
}
