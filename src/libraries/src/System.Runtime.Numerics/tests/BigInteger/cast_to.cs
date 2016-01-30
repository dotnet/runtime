// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class cast_toTest
    {
        public delegate void ExceptionGenerator();

        private const int NumberOfRandomIterations = 10;
        private static Random s_random = new Random(100);
        
        [Fact]
        public static void RunByteImplicitCastToBigIntegerTests()
        {
            // Byte Implicit Cast to BigInteger: Byte.MinValue
            VerifyByteImplicitCastToBigInteger(Byte.MinValue);

            // Byte Implicit Cast to BigInteger: 0
            VerifyByteImplicitCastToBigInteger(0);

            // Byte Implicit Cast to BigInteger: 1
            VerifyByteImplicitCastToBigInteger(1);

            // Byte Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyByteImplicitCastToBigInteger((Byte)s_random.Next(1, Byte.MaxValue));
            }

            // Byte Implicit Cast to BigInteger: Byte.MaxValue
            VerifyByteImplicitCastToBigInteger(Byte.MaxValue);
        }

        [Fact]
        public static void RunSByteImplicitCastToBigIntegerTests()
        {
            // SByte Implicit Cast to BigInteger: SByte.MinValue
            VerifySByteImplicitCastToBigInteger(SByte.MinValue);

            // SByte Implicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySByteImplicitCastToBigInteger((SByte)s_random.Next(SByte.MinValue, 0));
            }

            // SByte Implicit Cast to BigInteger: -1
            VerifySByteImplicitCastToBigInteger(-1);

            // SByte Implicit Cast to BigInteger: 0
            VerifySByteImplicitCastToBigInteger(0);

            // SByte Implicit Cast to BigInteger: 1
            VerifySByteImplicitCastToBigInteger(1);

            // SByte Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySByteImplicitCastToBigInteger((SByte)s_random.Next(1, SByte.MaxValue));
            }

            // SByte Implicit Cast to BigInteger: SByte.MaxValue
            VerifySByteImplicitCastToBigInteger(SByte.MaxValue);
        }

        [Fact]
        public static void RunUInt16ImplicitCastToBigIntegerTests()
        {
            // UInt16 Implicit Cast to BigInteger: UInt16.MinValue
            VerifyUInt16ImplicitCastToBigInteger(UInt16.MinValue);

            // UInt16 Implicit Cast to BigInteger: 0
            VerifyUInt16ImplicitCastToBigInteger(0);

            // UInt16 Implicit Cast to BigInteger: 1
            VerifyUInt16ImplicitCastToBigInteger(1);

            // UInt16 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyUInt16ImplicitCastToBigInteger((UInt16)s_random.Next(1, UInt16.MaxValue));
            }

            // UInt16 Implicit Cast to BigInteger: UInt16.MaxValue
            VerifyUInt16ImplicitCastToBigInteger(UInt16.MaxValue);
        }

        [Fact]
        public static void RunInt16ImplicitCastToBigIntegerTests()
        {
            // Int16 Implicit Cast to BigInteger: Int16.MinValue
            VerifyInt16ImplicitCastToBigInteger(Int16.MinValue);

            // Int16 Implicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt16ImplicitCastToBigInteger((Int16)s_random.Next(Int16.MinValue, 0));
            }

            // Int16 Implicit Cast to BigInteger: -1
            VerifyInt16ImplicitCastToBigInteger(-1);

            // Int16 Implicit Cast to BigInteger: 0
            VerifyInt16ImplicitCastToBigInteger(0);

            // Int16 Implicit Cast to BigInteger: 1
            VerifyInt16ImplicitCastToBigInteger(1);

            // Int16 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt16ImplicitCastToBigInteger((Int16)s_random.Next(1, Int16.MaxValue));
            }

            // Int16 Implicit Cast to BigInteger: Int16.MaxValue
            VerifyInt16ImplicitCastToBigInteger(Int16.MaxValue);
        }

        [Fact]
        public static void RunUInt32ImplicitCastToBigIntegerTests()
        {
            // UInt32 Implicit Cast to BigInteger: UInt32.MinValue
            VerifyUInt32ImplicitCastToBigInteger(UInt32.MinValue);

            // UInt32 Implicit Cast to BigInteger: 0
            VerifyUInt32ImplicitCastToBigInteger(0);

            // UInt32 Implicit Cast to BigInteger: 1
            VerifyUInt32ImplicitCastToBigInteger(1);

            // UInt32 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyUInt32ImplicitCastToBigInteger((UInt32)(UInt32.MaxValue * s_random.NextDouble()));
            }

            // UInt32 Implicit Cast to BigInteger: UInt32.MaxValue
            VerifyUInt32ImplicitCastToBigInteger(UInt32.MaxValue);
        }

        [Fact]
        public static void RunInt32ImplicitCastToBigIntegerTests()
        {
            // Int32 Implicit Cast to BigInteger: Int32.MinValue
            VerifyInt32ImplicitCastToBigInteger(Int32.MinValue);

            // Int32 Implicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt32ImplicitCastToBigInteger((Int32)s_random.Next(Int32.MinValue, 0));
            }

            // Int32 Implicit Cast to BigInteger: -1
            VerifyInt32ImplicitCastToBigInteger(-1);

            // Int32 Implicit Cast to BigInteger: 0
            VerifyInt32ImplicitCastToBigInteger(0);

            // Int32 Implicit Cast to BigInteger: 1
            VerifyInt32ImplicitCastToBigInteger(1);

            // Int32 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt32ImplicitCastToBigInteger((Int32)s_random.Next(1, Int32.MaxValue));
            }

            // Int32 Implicit Cast to BigInteger: Int32.MaxValue
            VerifyInt32ImplicitCastToBigInteger(Int32.MaxValue);
        }

        [Fact]
        public static void RunUInt64ImplicitCastToBigIntegerTests()
        {
            // UInt64 Implicit Cast to BigInteger: UInt64.MinValue
            VerifyUInt64ImplicitCastToBigInteger(UInt64.MinValue);

            // UInt64 Implicit Cast to BigInteger: 0
            VerifyUInt64ImplicitCastToBigInteger(0);

            // UInt64 Implicit Cast to BigInteger: 1
            VerifyUInt64ImplicitCastToBigInteger(1);

            // UInt64 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyUInt64ImplicitCastToBigInteger((UInt64)(UInt64.MaxValue * s_random.NextDouble()));
            }

            // UInt64 Implicit Cast to BigInteger: UInt64.MaxValue
            VerifyUInt64ImplicitCastToBigInteger(UInt64.MaxValue);
        }

        [Fact]
        public static void RunInt64ImplicitCastToBigIntegerTests()
        {
            // Int64 Implicit Cast to BigInteger: Int64.MinValue
            VerifyInt64ImplicitCastToBigInteger(Int64.MinValue);

            // Int64 Implicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt64ImplicitCastToBigInteger(((Int64)(Int64.MaxValue * s_random.NextDouble())) - Int64.MaxValue);
            }

            // Int64 Implicit Cast to BigInteger: -1
            VerifyInt64ImplicitCastToBigInteger(-1);

            // Int64 Implicit Cast to BigInteger: 0
            VerifyInt64ImplicitCastToBigInteger(0);

            // Int64 Implicit Cast to BigInteger: 1
            VerifyInt64ImplicitCastToBigInteger(1);

            // Int64 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt64ImplicitCastToBigInteger((Int64)(Int64.MaxValue * s_random.NextDouble()));
            }

            // Int64 Implicit Cast to BigInteger: Int64.MaxValue
            VerifyInt64ImplicitCastToBigInteger(Int64.MaxValue);
        }

        [Fact]
        public static void RunSingleExplicitCastToBigIntegerTests()
        {
            Single value;

            // Single Explicit Cast to BigInteger: Single.NegativeInfinity
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)Single.NegativeInfinity; });

            // Single Explicit Cast to BigInteger: Single.MinValue
            VerifySingleExplicitCastToBigInteger(Single.MinValue);

            // Single Explicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastToBigInteger(((Single)(Single.MaxValue * s_random.NextDouble())) - Single.MaxValue);
            }

            // Single Explicit Cast to BigInteger: Random Non-Integral Negative > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastToBigInteger((Single)(-100 * s_random.NextDouble()));
            }

            // Single Explicit Cast to BigInteger: -1
            VerifySingleExplicitCastToBigInteger(-1);

            // Single Explicit Cast to BigInteger: 0
            VerifySingleExplicitCastToBigInteger(0);

            // Single Explicit Cast to BigInteger: 1
            VerifySingleExplicitCastToBigInteger(1);

            // Single Explicit Cast to BigInteger: Random Non-Integral Positive < 100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastToBigInteger((Single)(100 * s_random.NextDouble()));
            }

            // Single Explicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastToBigInteger((Single)(Single.MaxValue * s_random.NextDouble()));
            }

            // Single Explicit Cast to BigInteger: Single.MaxValue
            VerifySingleExplicitCastToBigInteger(Single.MaxValue);

            // Single Explicit Cast to BigInteger: Single.PositiveInfinity
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)Single.PositiveInfinity; });

            // double.IsInfinity(float.MaxValue * 2.0f) == false, but we don't want this odd behavior here
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)(float.MaxValue * 2.0f); });

            // Single Explicit Cast to BigInteger: Single.Epsilon
            VerifySingleExplicitCastToBigInteger(Single.Epsilon);

            // Single Explicit Cast to BigInteger: Single.NaN
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)Single.NaN; });

            //There are multiple ways to represent a NaN just try another one
            // Single Explicit Cast to BigInteger: Single.NaN 2
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)ConvertInt32ToSingle(0x7FC00000); });

            // Single Explicit Cast to BigInteger: Smallest Exponent
            VerifySingleExplicitCastToBigInteger((Single)Math.Pow(2, -126));

            // Single Explicit Cast to BigInteger: Largest Exponent
            VerifySingleExplicitCastToBigInteger((Single)Math.Pow(2, 127));

            // Single Explicit Cast to BigInteger: Largest number less then 1
            value = 0;
            for (int i = 1; i <= 24; ++i)
            {
                value += (Single)(Math.Pow(2, -i));
            }
            VerifySingleExplicitCastToBigInteger(value);

            // Single Explicit Cast to BigInteger: Smallest number greater then 1
            value = (Single)(1 + Math.Pow(2, -23));
            VerifySingleExplicitCastToBigInteger(value);

            // Single Explicit Cast to BigInteger: Largest number less then 2
            value = 0;
            for (int i = 1; i <= 23; ++i)
            {
                value += (Single)(Math.Pow(2, -i));
            }
            value += 1;
            VerifySingleExplicitCastToBigInteger(value);
        }

        [Fact]
        public static void RunDoubleExplicitCastToBigIntegerTests()
        {
            Double value;

            // Double Explicit Cast to BigInteger: Double.NegativeInfinity
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)Double.NegativeInfinity; });

            // Double Explicit Cast to BigInteger: Double.MinValue
            VerifyDoubleExplicitCastToBigInteger(Double.MinValue);

            // Double Explicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastToBigInteger(((Double)(Double.MaxValue * s_random.NextDouble())) - Double.MaxValue);
            }

            // Double Explicit Cast to BigInteger: Random Non-Integral Negative > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastToBigInteger((Double)(-100 * s_random.NextDouble()));
            }

            // Double Explicit Cast to BigInteger: -1
            VerifyDoubleExplicitCastToBigInteger(-1);

            // Double Explicit Cast to BigInteger: 0
            VerifyDoubleExplicitCastToBigInteger(0);

            // Double Explicit Cast to BigInteger: 1
            VerifyDoubleExplicitCastToBigInteger(1);

            // Double Explicit Cast to BigInteger: Random Non-Integral Positive < 100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastToBigInteger((Double)(100 * s_random.NextDouble()));
            }

            // Double Explicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastToBigInteger((Double)(Double.MaxValue * s_random.NextDouble()));
            }

            // Double Explicit Cast to BigInteger: Double.MaxValue
            VerifyDoubleExplicitCastToBigInteger(Double.MaxValue);

            // Double Explicit Cast to BigInteger: Double.PositiveInfinity
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)Double.PositiveInfinity; });

            // Double Explicit Cast to BigInteger: Double.Epsilon
            VerifyDoubleExplicitCastToBigInteger(Double.Epsilon);

            // Double Explicit Cast to BigInteger: Double.NaN
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)Double.NaN; });

            //There are multiple ways to represent a NaN just try another one
            // Double Explicit Cast to BigInteger: Double.NaN 2
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)ConvertInt64ToDouble(0x7FF8000000000000); });

            // Double Explicit Cast to BigInteger: Smallest Exponent
            VerifyDoubleExplicitCastToBigInteger((Double)Math.Pow(2, -1022));

            // Double Explicit Cast to BigInteger: Largest Exponent
            VerifyDoubleExplicitCastToBigInteger((Double)Math.Pow(2, 1023));

            // Double Explicit Cast to BigInteger: Largest number less then 1
            value = 0;
            for (int i = 1; i <= 53; ++i)
            {
                value += (Double)(Math.Pow(2, -i));
            }
            VerifyDoubleExplicitCastToBigInteger(value);

            // Double Explicit Cast to BigInteger: Smallest number greater then 1
            value = (Double)(1 + Math.Pow(2, -52));
            VerifyDoubleExplicitCastToBigInteger(value);

            // Double Explicit Cast to BigInteger: Largest number less then 2
            value = 0;
            for (int i = 1; i <= 52; ++i)
            {
                value += (Double)(Math.Pow(2, -i));
            }
            value += 1;
            VerifyDoubleExplicitCastToBigInteger(value);
        }

        [Fact]
        public static void RunDecimalExplicitCastToBigIntegerTests()
        {
            Decimal value;

            // Decimal Explicit Cast to BigInteger: Decimal.MinValue
            VerifyDecimalExplicitCastToBigInteger(Decimal.MinValue);

            // Decimal Explicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                value = new Decimal(
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    true,
                    (byte)s_random.Next(0, 29));
                VerifyDecimalExplicitCastToBigInteger(value);
            }

            // Decimal Explicit Cast to BigInteger: -1
            VerifyDecimalExplicitCastToBigInteger(-1);

            // Decimal Explicit Cast to BigInteger: 0
            VerifyDecimalExplicitCastToBigInteger(0);

            // Decimal Explicit Cast to BigInteger: 1
            VerifyDecimalExplicitCastToBigInteger(1);

            // Decimal Explicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                value = new Decimal(
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    false,
                    (byte)s_random.Next(0, 29));
                VerifyDecimalExplicitCastToBigInteger(value);
            }

            // Decimal Explicit Cast to BigInteger: Decimal.MaxValue
            VerifyDecimalExplicitCastToBigInteger(Decimal.MaxValue);

            // Decimal Explicit Cast to BigInteger: Smallest Exponent
            unchecked
            {
                value = new Decimal(1, 0, 0, false, 0);
            }
            VerifyDecimalExplicitCastToBigInteger(value);

            // Decimal Explicit Cast to BigInteger: Largest Exponent and zero integer
            unchecked
            {
                value = new Decimal(0, 0, 0, false, 28);
            }
            VerifyDecimalExplicitCastToBigInteger(value);

            // Decimal Explicit Cast to BigInteger: Largest Exponent and non zero integer
            unchecked
            {
                value = new Decimal(1, 0, 0, false, 28);
            }
            VerifyDecimalExplicitCastToBigInteger(value);

            // Decimal Explicit Cast to BigInteger: Largest number less then 1
            value = 1 - new Decimal(1, 0, 0, false, 28);
            VerifyDecimalExplicitCastToBigInteger(value);

            // Decimal Explicit Cast to BigInteger: Smallest number greater then 1
            value = 1 + new Decimal(1, 0, 0, false, 28);
            VerifyDecimalExplicitCastToBigInteger(value);

            // Decimal Explicit Cast to BigInteger: Largest number less then 2
            value = 2 - new Decimal(1, 0, 0, false, 28);
            VerifyDecimalExplicitCastToBigInteger(value);
        }

        private static Single ConvertInt32ToSingle(Int32 value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }

        private static Double ConvertInt64ToDouble(Int64 value)
        {
            return BitConverter.ToDouble(BitConverter.GetBytes(value), 0);
        }

        private static void VerifyByteImplicitCastToBigInteger(Byte value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (Byte)bigInteger);

            if (value != Byte.MaxValue)
            {
                Assert.Equal((Byte)(value + 1), (Byte)(bigInteger + 1));
            }

            if (value != Byte.MinValue)
            {
                Assert.Equal((Byte)(value - 1), (Byte)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifySByteImplicitCastToBigInteger(SByte value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (SByte)bigInteger);

            if (value != SByte.MaxValue)
            {
                Assert.Equal((SByte)(value + 1), (SByte)(bigInteger + 1));
            }

            if (value != SByte.MinValue)
            {
                Assert.Equal((SByte)(value - 1), (SByte)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifyUInt16ImplicitCastToBigInteger(UInt16 value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (UInt16)bigInteger);

            if (value != UInt16.MaxValue)
            {
                Assert.Equal((UInt16)(value + 1), (UInt16)(bigInteger + 1));
            }

            if (value != UInt16.MinValue)
            {
                Assert.Equal((UInt16)(value - 1), (UInt16)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifyInt16ImplicitCastToBigInteger(Int16 value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (Int16)bigInteger);

            if (value != Int16.MaxValue)
            {
                Assert.Equal((Int16)(value + 1), (Int16)(bigInteger + 1));
            }

            if (value != Int16.MinValue)
            {
                Assert.Equal((Int16)(value - 1), (Int16)(bigInteger - 1));
            }
    
            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifyUInt32ImplicitCastToBigInteger(UInt32 value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (UInt32)bigInteger);

            if (value != UInt32.MaxValue)
            {
                Assert.Equal((UInt32)(value + 1), (UInt32)(bigInteger + 1));
            }

            if (value != UInt32.MinValue)
            {
                Assert.Equal((UInt32)(value - 1), (UInt32)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifyInt32ImplicitCastToBigInteger(Int32 value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (Int32)bigInteger);

            if (value != Int32.MaxValue)
            {
                Assert.Equal((Int32)(value + 1), (Int32)(bigInteger + 1));
            }

            if (value != Int32.MinValue)
            {
                Assert.Equal((Int32)(value - 1), (Int32)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifyUInt64ImplicitCastToBigInteger(UInt64 value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (UInt64)bigInteger);

            if (value != UInt64.MaxValue)
            {
                Assert.Equal((UInt64)(value + 1), (UInt64)(bigInteger + 1));
            }

            if (value != UInt64.MinValue)
            {
                Assert.Equal((UInt64)(value - 1), (UInt64)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifyInt64ImplicitCastToBigInteger(Int64 value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (Int64)bigInteger);

            if (value != Int64.MaxValue)
            {
                Assert.Equal((Int64)(value + 1), (Int64)(bigInteger + 1));
            }

            if (value != Int64.MinValue)
            {
                Assert.Equal((Int64)(value - 1), (Int64)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifySingleExplicitCastToBigInteger(Single value)
        {
            Single expectedValue;
            BigInteger bigInteger;

            if (value < 0)
            {
                expectedValue = (Single)Math.Ceiling(value);
            }
            else
            {
                expectedValue = (Single)Math.Floor(value);
            }

            bigInteger = (BigInteger)value;

            Assert.Equal(expectedValue, (Single)bigInteger);

            // Single can only accurately represent integers between -16777216 and 16777216 exclusive.
            // ToString starts to become inaccurate at this point.
            if (expectedValue < 16777216 && -16777216 < expectedValue)
            {
                Assert.Equal(expectedValue.ToString("G9"), bigInteger.ToString());
            }

            if (expectedValue != Math.Floor(Single.MaxValue))
            {
                Assert.Equal((Single)(expectedValue + 1), (Single)(bigInteger + 1));
            }

            if (expectedValue != Math.Ceiling(Single.MinValue))
            {
                Assert.Equal((Single)(expectedValue - 1), (Single)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == expectedValue);
        }

        private static void VerifyDoubleExplicitCastToBigInteger(Double value)
        {
            Double expectedValue;
            BigInteger bigInteger;

            if (value < 0)
            {
                expectedValue = Math.Ceiling(value);
            }
            else
            {
                expectedValue = Math.Floor(value);
            }

            bigInteger = (BigInteger)value;

            Assert.Equal(expectedValue, (Double)bigInteger);

            // Double can only accurately represent integers between -9007199254740992 and 9007199254740992 exclusive.
            // ToString starts to become inaccurate at this point.
            if (expectedValue < 9007199254740992 && -9007199254740992 < expectedValue)
            {
                Assert.Equal(expectedValue.ToString(), bigInteger.ToString());
            }

            if (!Single.IsInfinity((Single)expectedValue))
            {
                Assert.Equal((Double)(expectedValue + 1), (Double)(bigInteger + 1));
                Assert.Equal((Double)(expectedValue - 1), (Double)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == expectedValue);
        }

        private static void VerifyDecimalExplicitCastToBigInteger(Decimal value)
        {
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

            bigInteger = (BigInteger)value;

            Assert.Equal(expectedValue.ToString(), bigInteger.ToString());
            Assert.Equal(expectedValue, (Decimal)bigInteger);

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == expectedValue);

            if (expectedValue != Math.Floor(Decimal.MaxValue))
            {
                Assert.Equal((Decimal)(expectedValue + 1), (Decimal)(bigInteger + 1));
            }

            if (expectedValue != Math.Ceiling(Decimal.MinValue))
            {
                Assert.Equal((Decimal)(expectedValue - 1), (Decimal)(bigInteger - 1));
            }
        }

        private static void VerifyBigIntegerUsingIdentities(BigInteger bigInteger, bool isZero)
        {
            BigInteger tempBigInteger = new BigInteger(bigInteger.ToByteArray());

            Assert.Equal(bigInteger, tempBigInteger);

            if (isZero)
            {
               Assert.Equal(BigInteger.Zero, bigInteger);
            }
            else
            {
               Assert.NotEqual(BigInteger.Zero, bigInteger);

                // x/x = 1
               Assert.Equal(BigInteger.One, bigInteger / bigInteger);
            }

            // (x + 1) - 1 = x
            Assert.Equal(bigInteger, (bigInteger + BigInteger.One) - BigInteger.One);

            // (x + 1) - x = 1
            Assert.Equal(BigInteger.One, (bigInteger + BigInteger.One) - bigInteger);

            // x - x = 0
            Assert.Equal(BigInteger.Zero, bigInteger - bigInteger);

            // x + x = 2x
            Assert.Equal(2 * bigInteger, bigInteger + bigInteger);

            // x/1 = x
            Assert.Equal(bigInteger, bigInteger / BigInteger.One);

            // 1 * x = x
            Assert.Equal(bigInteger, BigInteger.One * bigInteger);
        }
    }
}
