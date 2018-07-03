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
            VerifyByteImplicitCastToBigInteger(byte.MinValue);

            // Byte Implicit Cast to BigInteger: 0
            VerifyByteImplicitCastToBigInteger(0);

            // Byte Implicit Cast to BigInteger: 1
            VerifyByteImplicitCastToBigInteger(1);

            // Byte Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyByteImplicitCastToBigInteger((byte)s_random.Next(1, byte.MaxValue));
            }

            // Byte Implicit Cast to BigInteger: Byte.MaxValue
            VerifyByteImplicitCastToBigInteger(byte.MaxValue);
        }

        [Fact]
        public static void RunSByteImplicitCastToBigIntegerTests()
        {
            // SByte Implicit Cast to BigInteger: SByte.MinValue
            VerifySByteImplicitCastToBigInteger(sbyte.MinValue);

            // SByte Implicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySByteImplicitCastToBigInteger((sbyte)s_random.Next(sbyte.MinValue, 0));
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
                VerifySByteImplicitCastToBigInteger((sbyte)s_random.Next(1, sbyte.MaxValue));
            }

            // SByte Implicit Cast to BigInteger: SByte.MaxValue
            VerifySByteImplicitCastToBigInteger(sbyte.MaxValue);
        }

        [Fact]
        public static void RunUInt16ImplicitCastToBigIntegerTests()
        {
            // UInt16 Implicit Cast to BigInteger: UInt16.MinValue
            VerifyUInt16ImplicitCastToBigInteger(ushort.MinValue);

            // UInt16 Implicit Cast to BigInteger: 0
            VerifyUInt16ImplicitCastToBigInteger(0);

            // UInt16 Implicit Cast to BigInteger: 1
            VerifyUInt16ImplicitCastToBigInteger(1);

            // UInt16 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyUInt16ImplicitCastToBigInteger((ushort)s_random.Next(1, ushort.MaxValue));
            }

            // UInt16 Implicit Cast to BigInteger: UInt16.MaxValue
            VerifyUInt16ImplicitCastToBigInteger(ushort.MaxValue);
        }

        [Fact]
        public static void RunInt16ImplicitCastToBigIntegerTests()
        {
            // Int16 Implicit Cast to BigInteger: Int16.MinValue
            VerifyInt16ImplicitCastToBigInteger(short.MinValue);

            // Int16 Implicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt16ImplicitCastToBigInteger((short)s_random.Next(short.MinValue, 0));
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
                VerifyInt16ImplicitCastToBigInteger((short)s_random.Next(1, short.MaxValue));
            }

            // Int16 Implicit Cast to BigInteger: Int16.MaxValue
            VerifyInt16ImplicitCastToBigInteger(short.MaxValue);
        }

        [Fact]
        public static void RunUInt32ImplicitCastToBigIntegerTests()
        {
            // UInt32 Implicit Cast to BigInteger: UInt32.MinValue
            VerifyUInt32ImplicitCastToBigInteger(uint.MinValue);

            // UInt32 Implicit Cast to BigInteger: 0
            VerifyUInt32ImplicitCastToBigInteger(0);

            // UInt32 Implicit Cast to BigInteger: 1
            VerifyUInt32ImplicitCastToBigInteger(1);

            // UInt32 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyUInt32ImplicitCastToBigInteger((uint)(uint.MaxValue * s_random.NextDouble()));
            }

            // UInt32 Implicit Cast to BigInteger: UInt32.MaxValue
            VerifyUInt32ImplicitCastToBigInteger(uint.MaxValue);
        }

        [Fact]
        public static void RunInt32ImplicitCastToBigIntegerTests()
        {
            // Int32 Implicit Cast to BigInteger: Int32.MinValue
            VerifyInt32ImplicitCastToBigInteger(int.MinValue);

            // Int32 Implicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt32ImplicitCastToBigInteger((int)s_random.Next(int.MinValue, 0));
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
                VerifyInt32ImplicitCastToBigInteger((int)s_random.Next(1, int.MaxValue));
            }

            // Int32 Implicit Cast to BigInteger: Int32.MaxValue
            VerifyInt32ImplicitCastToBigInteger(int.MaxValue);
        }

        [Fact]
        public static void RunUInt64ImplicitCastToBigIntegerTests()
        {
            // UInt64 Implicit Cast to BigInteger: UInt64.MinValue
            VerifyUInt64ImplicitCastToBigInteger(ulong.MinValue);

            // UInt64 Implicit Cast to BigInteger: 0
            VerifyUInt64ImplicitCastToBigInteger(0);

            // UInt64 Implicit Cast to BigInteger: 1
            VerifyUInt64ImplicitCastToBigInteger(1);

            // UInt64 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyUInt64ImplicitCastToBigInteger((ulong)(ulong.MaxValue * s_random.NextDouble()));
            }

            // UInt64 Implicit Cast to BigInteger: UInt64.MaxValue
            VerifyUInt64ImplicitCastToBigInteger(ulong.MaxValue);
        }

        [Fact]
        public static void RunInt64ImplicitCastToBigIntegerTests()
        {
            // Int64 Implicit Cast to BigInteger: Int64.MinValue
            VerifyInt64ImplicitCastToBigInteger(long.MinValue);

            // Int64 Implicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt64ImplicitCastToBigInteger(((long)(long.MaxValue * s_random.NextDouble())) - long.MaxValue);
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
                VerifyInt64ImplicitCastToBigInteger((long)(long.MaxValue * s_random.NextDouble()));
            }

            // Int64 Implicit Cast to BigInteger: Int64.MaxValue
            VerifyInt64ImplicitCastToBigInteger(long.MaxValue);
        }

        [Fact]
        public static void RunSingleExplicitCastToBigIntegerTests()
        {
            float value;

            // Single Explicit Cast to BigInteger: Single.NegativeInfinity
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)float.NegativeInfinity; });

            // Single Explicit Cast to BigInteger: Single.MinValue
            VerifySingleExplicitCastToBigInteger(float.MinValue);

            // Single Explicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastToBigInteger(((float)(float.MaxValue * s_random.NextDouble())) - float.MaxValue);
            }

            // Single Explicit Cast to BigInteger: Random Non-Integral Negative > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastToBigInteger((float)(-100 * s_random.NextDouble()));
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
                VerifySingleExplicitCastToBigInteger((float)(100 * s_random.NextDouble()));
            }

            // Single Explicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastToBigInteger((float)(float.MaxValue * s_random.NextDouble()));
            }

            // Single Explicit Cast to BigInteger: Single.MaxValue
            VerifySingleExplicitCastToBigInteger(float.MaxValue);

            // Single Explicit Cast to BigInteger: Single.PositiveInfinity
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)float.PositiveInfinity; });

            // double.IsInfinity(float.MaxValue * 2.0f) == false, but we don't want this odd behavior here
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)(float.MaxValue * 2.0f); });

            // Single Explicit Cast to BigInteger: Single.Epsilon
            VerifySingleExplicitCastToBigInteger(float.Epsilon);

            // Single Explicit Cast to BigInteger: Single.NaN
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)float.NaN; });

            //There are multiple ways to represent a NaN just try another one
            // Single Explicit Cast to BigInteger: Single.NaN 2
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)ConvertInt32ToSingle(0x7FC00000); });

            // Single Explicit Cast to BigInteger: Smallest Exponent
            VerifySingleExplicitCastToBigInteger((float)Math.Pow(2, -126));

            // Single Explicit Cast to BigInteger: Largest Exponent
            VerifySingleExplicitCastToBigInteger((float)Math.Pow(2, 127));

            // Single Explicit Cast to BigInteger: Largest number less then 1
            value = 0;
            for (int i = 1; i <= 24; ++i)
            {
                value += (float)(Math.Pow(2, -i));
            }
            VerifySingleExplicitCastToBigInteger(value);

            // Single Explicit Cast to BigInteger: Smallest number greater then 1
            value = (float)(1 + Math.Pow(2, -23));
            VerifySingleExplicitCastToBigInteger(value);

            // Single Explicit Cast to BigInteger: Largest number less then 2
            value = 0;
            for (int i = 1; i <= 23; ++i)
            {
                value += (float)(Math.Pow(2, -i));
            }
            value += 1;
            VerifySingleExplicitCastToBigInteger(value);
        }

        [Fact]
        public static void RunDoubleExplicitCastToBigIntegerTests()
        {
            double value;

            // Double Explicit Cast to BigInteger: Double.NegativeInfinity
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)double.NegativeInfinity; });

            // Double Explicit Cast to BigInteger: Double.MinValue
            VerifyDoubleExplicitCastToBigInteger(double.MinValue);

            // Double Explicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastToBigInteger(((double)(double.MaxValue * s_random.NextDouble())) - double.MaxValue);
            }

            // Double Explicit Cast to BigInteger: Random Non-Integral Negative > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastToBigInteger((double)(-100 * s_random.NextDouble()));
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
                VerifyDoubleExplicitCastToBigInteger((double)(100 * s_random.NextDouble()));
            }

            // Double Explicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastToBigInteger((double)(double.MaxValue * s_random.NextDouble()));
            }

            // Double Explicit Cast to BigInteger: Double.MaxValue
            VerifyDoubleExplicitCastToBigInteger(double.MaxValue);

            // Double Explicit Cast to BigInteger: Double.PositiveInfinity
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)double.PositiveInfinity; });

            // Double Explicit Cast to BigInteger: Double.Epsilon
            VerifyDoubleExplicitCastToBigInteger(double.Epsilon);

            // Double Explicit Cast to BigInteger: Double.NaN
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)double.NaN; });

            //There are multiple ways to represent a NaN just try another one
            // Double Explicit Cast to BigInteger: Double.NaN 2
            Assert.Throws<OverflowException>(() => { BigInteger temp = (BigInteger)ConvertInt64ToDouble(0x7FF8000000000000); });

            // Double Explicit Cast to BigInteger: Smallest Exponent
            VerifyDoubleExplicitCastToBigInteger((double)Math.Pow(2, -1022));

            // Double Explicit Cast to BigInteger: Largest Exponent
            VerifyDoubleExplicitCastToBigInteger((double)Math.Pow(2, 1023));

            // Double Explicit Cast to BigInteger: Largest number less then 1
            value = 0;
            for (int i = 1; i <= 53; ++i)
            {
                value += (double)(Math.Pow(2, -i));
            }
            VerifyDoubleExplicitCastToBigInteger(value);

            // Double Explicit Cast to BigInteger: Smallest number greater then 1
            value = (double)(1 + Math.Pow(2, -52));
            VerifyDoubleExplicitCastToBigInteger(value);

            // Double Explicit Cast to BigInteger: Largest number less then 2
            value = 0;
            for (int i = 1; i <= 52; ++i)
            {
                value += (double)(Math.Pow(2, -i));
            }
            value += 1;
            VerifyDoubleExplicitCastToBigInteger(value);
        }

        [Fact]
        public static void RunDecimalExplicitCastToBigIntegerTests()
        {
            decimal value;

            // Decimal Explicit Cast to BigInteger: Decimal.MinValue
            VerifyDecimalExplicitCastToBigInteger(decimal.MinValue);

            // Decimal Explicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                value = new decimal(
                    s_random.Next(int.MinValue, int.MaxValue),
                    s_random.Next(int.MinValue, int.MaxValue),
                    s_random.Next(int.MinValue, int.MaxValue),
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
                value = new decimal(
                    s_random.Next(int.MinValue, int.MaxValue),
                    s_random.Next(int.MinValue, int.MaxValue),
                    s_random.Next(int.MinValue, int.MaxValue),
                    false,
                    (byte)s_random.Next(0, 29));
                VerifyDecimalExplicitCastToBigInteger(value);
            }

            // Decimal Explicit Cast to BigInteger: Decimal.MaxValue
            VerifyDecimalExplicitCastToBigInteger(decimal.MaxValue);

            // Decimal Explicit Cast to BigInteger: Smallest Exponent
            unchecked
            {
                value = new decimal(1, 0, 0, false, 0);
            }
            VerifyDecimalExplicitCastToBigInteger(value);

            // Decimal Explicit Cast to BigInteger: Largest Exponent and zero integer
            unchecked
            {
                value = new decimal(0, 0, 0, false, 28);
            }
            VerifyDecimalExplicitCastToBigInteger(value);

            // Decimal Explicit Cast to BigInteger: Largest Exponent and non zero integer
            unchecked
            {
                value = new decimal(1, 0, 0, false, 28);
            }
            VerifyDecimalExplicitCastToBigInteger(value);

            // Decimal Explicit Cast to BigInteger: Largest number less then 1
            value = 1 - new decimal(1, 0, 0, false, 28);
            VerifyDecimalExplicitCastToBigInteger(value);

            // Decimal Explicit Cast to BigInteger: Smallest number greater then 1
            value = 1 + new decimal(1, 0, 0, false, 28);
            VerifyDecimalExplicitCastToBigInteger(value);

            // Decimal Explicit Cast to BigInteger: Largest number less then 2
            value = 2 - new decimal(1, 0, 0, false, 28);
            VerifyDecimalExplicitCastToBigInteger(value);
        }

        private static float ConvertInt32ToSingle(int value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }

        private static double ConvertInt64ToDouble(long value)
        {
            return BitConverter.ToDouble(BitConverter.GetBytes(value), 0);
        }

        private static void VerifyByteImplicitCastToBigInteger(byte value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (byte)bigInteger);

            if (value != byte.MaxValue)
            {
                Assert.Equal((byte)(value + 1), (byte)(bigInteger + 1));
            }

            if (value != byte.MinValue)
            {
                Assert.Equal((byte)(value - 1), (byte)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifySByteImplicitCastToBigInteger(sbyte value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (sbyte)bigInteger);

            if (value != sbyte.MaxValue)
            {
                Assert.Equal((sbyte)(value + 1), (sbyte)(bigInteger + 1));
            }

            if (value != sbyte.MinValue)
            {
                Assert.Equal((sbyte)(value - 1), (sbyte)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifyUInt16ImplicitCastToBigInteger(ushort value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (ushort)bigInteger);

            if (value != ushort.MaxValue)
            {
                Assert.Equal((ushort)(value + 1), (ushort)(bigInteger + 1));
            }

            if (value != ushort.MinValue)
            {
                Assert.Equal((ushort)(value - 1), (ushort)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifyInt16ImplicitCastToBigInteger(short value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (short)bigInteger);

            if (value != short.MaxValue)
            {
                Assert.Equal((short)(value + 1), (short)(bigInteger + 1));
            }

            if (value != short.MinValue)
            {
                Assert.Equal((short)(value - 1), (short)(bigInteger - 1));
            }
    
            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifyUInt32ImplicitCastToBigInteger(uint value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (uint)bigInteger);

            if (value != uint.MaxValue)
            {
                Assert.Equal((uint)(value + 1), (uint)(bigInteger + 1));
            }

            if (value != uint.MinValue)
            {
                Assert.Equal((uint)(value - 1), (uint)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifyInt32ImplicitCastToBigInteger(int value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (int)bigInteger);

            if (value != int.MaxValue)
            {
                Assert.Equal((int)(value + 1), (int)(bigInteger + 1));
            }

            if (value != int.MinValue)
            {
                Assert.Equal((int)(value - 1), (int)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifyUInt64ImplicitCastToBigInteger(ulong value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (ulong)bigInteger);

            if (value != ulong.MaxValue)
            {
                Assert.Equal((ulong)(value + 1), (ulong)(bigInteger + 1));
            }

            if (value != ulong.MinValue)
            {
                Assert.Equal((ulong)(value - 1), (ulong)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifyInt64ImplicitCastToBigInteger(long value)
        {
            BigInteger bigInteger = value;

            Assert.Equal(value, bigInteger);
            Assert.Equal(value.ToString(), bigInteger.ToString());
            Assert.Equal(value, (long)bigInteger);

            if (value != long.MaxValue)
            {
                Assert.Equal((long)(value + 1), (long)(bigInteger + 1));
            }

            if (value != long.MinValue)
            {
                Assert.Equal((long)(value - 1), (long)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        private static void VerifySingleExplicitCastToBigInteger(float value)
        {
            float expectedValue;
            BigInteger bigInteger;

            if (value < 0)
            {
                expectedValue = (float)Math.Ceiling(value);
            }
            else
            {
                expectedValue = (float)Math.Floor(value);
            }

            bigInteger = (BigInteger)value;

            Assert.Equal(expectedValue, (float)bigInteger);

            // Single can only accurately represent integers between -16777216 and 16777216 exclusive.
            // ToString starts to become inaccurate at this point.
            if (expectedValue < 16777216 && -16777216 < expectedValue)
            {
                Assert.Equal(expectedValue.ToString("G9"), bigInteger.ToString());
            }

            if (expectedValue != Math.Floor(float.MaxValue))
            {
                Assert.Equal((float)(expectedValue + 1), (float)(bigInteger + 1));
            }

            if (expectedValue != Math.Ceiling(float.MinValue))
            {
                Assert.Equal((float)(expectedValue - 1), (float)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == expectedValue);
        }

        private static void VerifyDoubleExplicitCastToBigInteger(double value)
        {
            double expectedValue;
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

            Assert.Equal(expectedValue, (double)bigInteger);

            // Double can only accurately represent integers between -9007199254740992 and 9007199254740992 exclusive.
            // ToString starts to become inaccurate at this point.
            if (expectedValue < 9007199254740992 && -9007199254740992 < expectedValue)
            {
                Assert.Equal(expectedValue.ToString(), bigInteger.ToString());
            }

            if (!float.IsInfinity((float)expectedValue))
            {
                Assert.Equal((double)(expectedValue + 1), (double)(bigInteger + 1));
                Assert.Equal((double)(expectedValue - 1), (double)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == expectedValue);
        }

        private static void VerifyDecimalExplicitCastToBigInteger(decimal value)
        {
            decimal expectedValue;
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
            Assert.Equal(expectedValue, (decimal)bigInteger);

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == expectedValue);

            if (expectedValue != Math.Floor(decimal.MaxValue))
            {
                Assert.Equal((decimal)(expectedValue + 1), (decimal)(bigInteger + 1));
            }

            if (expectedValue != Math.Ceiling(decimal.MinValue))
            {
                Assert.Equal((decimal)(expectedValue - 1), (decimal)(bigInteger - 1));
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
