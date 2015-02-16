// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            Assert.True(VerifyByteImplicitCastToBigInteger(Byte.MinValue), " Verification Failed");

            // Byte Implicit Cast to BigInteger: 0
            Assert.True(VerifyByteImplicitCastToBigInteger(0), " Verification Failed");

            // Byte Implicit Cast to BigInteger: 1
            Assert.True(VerifyByteImplicitCastToBigInteger(1), " Verification Failed");

            // Byte Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyByteImplicitCastToBigInteger((Byte)s_random.Next(1, Byte.MaxValue)), " Verification Failed");
            }

            // Byte Implicit Cast to BigInteger: Byte.MaxValue
            Assert.True(VerifyByteImplicitCastToBigInteger(Byte.MaxValue), " Verification Failed");
        }

        [Fact]
        public static void RunSByteImplicitCastToBigIntegerTests()
        {
            // SByte Implicit Cast to BigInteger: SByte.MinValue
            Assert.True(VerifySByteImplicitCastToBigInteger(SByte.MinValue), " Verification Failed");

            // SByte Implicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifySByteImplicitCastToBigInteger((SByte)s_random.Next(SByte.MinValue, 0)), " Verification Failed");
            }

            // SByte Implicit Cast to BigInteger: -1
            Assert.True(VerifySByteImplicitCastToBigInteger(-1), " Verification Failed");

            // SByte Implicit Cast to BigInteger: 0
            Assert.True(VerifySByteImplicitCastToBigInteger(0), " Verification Failed");

            // SByte Implicit Cast to BigInteger: 1
            Assert.True(VerifySByteImplicitCastToBigInteger(1), " Verification Failed");

            // SByte Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifySByteImplicitCastToBigInteger((SByte)s_random.Next(1, SByte.MaxValue)), " Verification Failed");
            }

            // SByte Implicit Cast to BigInteger: SByte.MaxValue
            Assert.True(VerifySByteImplicitCastToBigInteger(SByte.MaxValue), " Verification Failed");
        }

        [Fact]
        public static void RunUInt16ImplicitCastToBigIntegerTests()
        {
            // UInt16 Implicit Cast to BigInteger: UInt16.MinValue
            Assert.True(VerifyUInt16ImplicitCastToBigInteger(UInt16.MinValue), " Verification Failed");

            // UInt16 Implicit Cast to BigInteger: 0
            Assert.True(VerifyUInt16ImplicitCastToBigInteger(0), " Verification Failed");

            // UInt16 Implicit Cast to BigInteger: 1
            Assert.True(VerifyUInt16ImplicitCastToBigInteger(1), " Verification Failed");

            // UInt16 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyUInt16ImplicitCastToBigInteger((UInt16)s_random.Next(1, UInt16.MaxValue)), " Verification Failed");
            }

            // UInt16 Implicit Cast to BigInteger: UInt16.MaxValue
            Assert.True(VerifyUInt16ImplicitCastToBigInteger(UInt16.MaxValue), " Verification Failed");
        }

        [Fact]
        public static void RunInt16ImplicitCastToBigIntegerTests()
        {
            // Int16 Implicit Cast to BigInteger: Int16.MinValue
            Assert.True(VerifyInt16ImplicitCastToBigInteger(Int16.MinValue), " Verification Failed");

            // Int16 Implicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyInt16ImplicitCastToBigInteger((Int16)s_random.Next(Int16.MinValue, 0)), " Verification Failed");
            }

            // Int16 Implicit Cast to BigInteger: -1
            Assert.True(VerifyInt16ImplicitCastToBigInteger(-1), " Verification Failed");

            // Int16 Implicit Cast to BigInteger: 0
            Assert.True(VerifyInt16ImplicitCastToBigInteger(0), " Verification Failed");

            // Int16 Implicit Cast to BigInteger: 1
            Assert.True(VerifyInt16ImplicitCastToBigInteger(1), " Verification Failed");

            // Int16 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyInt16ImplicitCastToBigInteger((Int16)s_random.Next(1, Int16.MaxValue)), " Verification Failed");
            }

            // Int16 Implicit Cast to BigInteger: Int16.MaxValue
            Assert.True(VerifyInt16ImplicitCastToBigInteger(Int16.MaxValue), " Verification Failed");
        }

        [Fact]
        public static void RunUInt32ImplicitCastToBigIntegerTests()
        {
            // UInt32 Implicit Cast to BigInteger: UInt32.MinValue
            Assert.True(VerifyUInt32ImplicitCastToBigInteger(UInt32.MinValue), " Verification Failed");

            // UInt32 Implicit Cast to BigInteger: 0
            Assert.True(VerifyUInt32ImplicitCastToBigInteger(0), " Verification Failed");

            // UInt32 Implicit Cast to BigInteger: 1
            Assert.True(VerifyUInt32ImplicitCastToBigInteger(1), " Verification Failed");

            // UInt32 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyUInt32ImplicitCastToBigInteger((UInt32)(UInt32.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // UInt32 Implicit Cast to BigInteger: UInt32.MaxValue
            Assert.True(VerifyUInt32ImplicitCastToBigInteger(UInt32.MaxValue), " Verification Failed");
        }

        [Fact]
        public static void RunInt32ImplicitCastToBigIntegerTests()
        {
            // Int32 Implicit Cast to BigInteger: Int32.MinValue
            Assert.True(VerifyInt32ImplicitCastToBigInteger(Int32.MinValue), " Verification Failed");

            // Int32 Implicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyInt32ImplicitCastToBigInteger((Int32)s_random.Next(Int32.MinValue, 0)), " Verification Failed");
            }

            // Int32 Implicit Cast to BigInteger: -1
            Assert.True(VerifyInt32ImplicitCastToBigInteger(-1), " Verification Failed");

            // Int32 Implicit Cast to BigInteger: 0
            Assert.True(VerifyInt32ImplicitCastToBigInteger(0), " Verification Failed");

            // Int32 Implicit Cast to BigInteger: 1
            Assert.True(VerifyInt32ImplicitCastToBigInteger(1), " Verification Failed");

            // Int32 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyInt32ImplicitCastToBigInteger((Int32)s_random.Next(1, Int32.MaxValue)), " Verification Failed");
            }

            // Int32 Implicit Cast to BigInteger: Int32.MaxValue
            Assert.True(VerifyInt32ImplicitCastToBigInteger(Int32.MaxValue), " Verification Failed");
        }

        [Fact]
        public static void RunUInt64ImplicitCastToBigIntegerTests()
        {
            // UInt64 Implicit Cast to BigInteger: UInt64.MinValue
            Assert.True(VerifyUInt64ImplicitCastToBigInteger(UInt64.MinValue), " Verification Failed");

            // UInt64 Implicit Cast to BigInteger: 0
            Assert.True(VerifyUInt64ImplicitCastToBigInteger(0), " Verification Failed");

            // UInt64 Implicit Cast to BigInteger: 1
            Assert.True(VerifyUInt64ImplicitCastToBigInteger(1), " Verification Failed");

            // UInt64 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyUInt64ImplicitCastToBigInteger((UInt64)(UInt64.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // UInt64 Implicit Cast to BigInteger: UInt64.MaxValue
            Assert.True(VerifyUInt64ImplicitCastToBigInteger(UInt64.MaxValue), " Verification Failed");
        }

        [Fact]
        public static void RunInt64ImplicitCastToBigIntegerTests()
        {
            // Int64 Implicit Cast to BigInteger: Int64.MinValue
            Assert.True(VerifyInt64ImplicitCastToBigInteger(Int64.MinValue), " Verification Failed");

            // Int64 Implicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyInt64ImplicitCastToBigInteger(((Int64)(Int64.MaxValue * s_random.NextDouble())) - Int64.MaxValue), " Verification Failed");
            }

            // Int64 Implicit Cast to BigInteger: -1
            Assert.True(VerifyInt64ImplicitCastToBigInteger(-1), " Verification Failed");

            // Int64 Implicit Cast to BigInteger: 0
            Assert.True(VerifyInt64ImplicitCastToBigInteger(0), " Verification Failed");

            // Int64 Implicit Cast to BigInteger: 1
            Assert.True(VerifyInt64ImplicitCastToBigInteger(1), " Verification Failed");

            // Int64 Implicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyInt64ImplicitCastToBigInteger((Int64)(Int64.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // Int64 Implicit Cast to BigInteger: Int64.MaxValue
            Assert.True(VerifyInt64ImplicitCastToBigInteger(Int64.MaxValue), " Verification Failed");
        }

        [Fact]
        public static void RunSingleExplicitCastToBigIntegerTests()
        {
            Single value;

            // Single Explicit Cast to BigInteger: Single.NegativeInfinity
            Assert.True(VerifyException<OverflowException>(delegate () { BigInteger temp = (BigInteger)Single.NegativeInfinity; }), " Verification Failed");

            // Single Explicit Cast to BigInteger: Single.MinValue
            Assert.True(VerifySingleExplicitCastToBigInteger(Single.MinValue), " Verification Failed");

            // Single Explicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifySingleExplicitCastToBigInteger(((Single)(Single.MaxValue * s_random.NextDouble())) - Single.MaxValue), " Verification Failed");
            }

            // Single Explicit Cast to BigInteger: Random Non-Integral Negative > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifySingleExplicitCastToBigInteger((Single)(-100 * s_random.NextDouble())), " Verification Failed");
            }

            // Single Explicit Cast to BigInteger: -1
            Assert.True(VerifySingleExplicitCastToBigInteger(-1), " Verification Failed");

            // Single Explicit Cast to BigInteger: 0
            Assert.True(VerifySingleExplicitCastToBigInteger(0), " Verification Failed");

            // Single Explicit Cast to BigInteger: 1
            Assert.True(VerifySingleExplicitCastToBigInteger(1), " Verification Failed");

            // Single Explicit Cast to BigInteger: Random Non-Integral Positive < 100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifySingleExplicitCastToBigInteger((Single)(100 * s_random.NextDouble())), " Verification Failed");
            }

            // Single Explicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifySingleExplicitCastToBigInteger((Single)(Single.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // Single Explicit Cast to BigInteger: Single.MaxValue
            Assert.True(VerifySingleExplicitCastToBigInteger(Single.MaxValue), " Verification Failed");

            // Single Explicit Cast to BigInteger: Single.PositiveInfinity
            Assert.True(VerifyException<OverflowException>(delegate () { BigInteger temp = (BigInteger)Single.PositiveInfinity; }), " Verification Failed");

            // Single Explicit Cast to BigInteger: Single.Epsilon
            Assert.True(VerifySingleExplicitCastToBigInteger(Single.Epsilon), " Verification Failed");

            // Single Explicit Cast to BigInteger: Single.NaN
            Assert.True(VerifyException<OverflowException>(delegate () { BigInteger temp = (BigInteger)Single.NaN; }), " Verification Failed");

            //There are multiple ways to represent a NaN just try another one
            // Single Explicit Cast to BigInteger: Single.NaN 2
            Assert.True(VerifyException<OverflowException>(delegate () { BigInteger temp = (BigInteger)ConvertInt32ToSingle(0x7FC00000); }), " Verification Failed");

            // Single Explicit Cast to BigInteger: Smallest Exponent
            Assert.True(VerifySingleExplicitCastToBigInteger((Single)Math.Pow(2, -126)), " Verification Failed");

            // Single Explicit Cast to BigInteger: Largest Exponent
            Assert.True(VerifySingleExplicitCastToBigInteger((Single)Math.Pow(2, 127)), " Verification Failed");

            // Single Explicit Cast to BigInteger: Largest number less then 1
            value = 0;
            for (int i = 1; i <= 24; ++i)
            {
                value += (Single)(Math.Pow(2, -i));
            }
            Assert.True(VerifySingleExplicitCastToBigInteger(value), " Verification Failed");

            // Single Explicit Cast to BigInteger: Smallest number greater then 1
            value = (Single)(1 + Math.Pow(2, -23));
            Assert.True(VerifySingleExplicitCastToBigInteger(value), " Verification Failed");

            // Single Explicit Cast to BigInteger: Largest number less then 2
            value = 0;
            for (int i = 1; i <= 23; ++i)
            {
                value += (Single)(Math.Pow(2, -i));
            }
            value += 1;
            Assert.True(VerifySingleExplicitCastToBigInteger(value), " Verification Failed");
        }

        [Fact]
        public static void RunDoubleExplicitCastToBigIntegerTests()
        {
            Double value;

            // Double Explicit Cast to BigInteger: Double.NegativeInfinity
            Assert.True(VerifyException<OverflowException>(delegate () { BigInteger temp = (BigInteger)Double.NegativeInfinity; }), " Verification Failed");

            // Double Explicit Cast to BigInteger: Double.MinValue
            Assert.True(VerifyDoubleExplicitCastToBigInteger(Double.MinValue), " Verification Failed");

            // Double Explicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyDoubleExplicitCastToBigInteger(((Double)(Double.MaxValue * s_random.NextDouble())) - Double.MaxValue), " Verification Failed");
            }

            // Double Explicit Cast to BigInteger: Random Non-Integral Negative > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyDoubleExplicitCastToBigInteger((Double)(-100 * s_random.NextDouble())), " Verification Failed");
            }

            // Double Explicit Cast to BigInteger: -1
            Assert.True(VerifyDoubleExplicitCastToBigInteger(-1), " Verification Failed");

            // Double Explicit Cast to BigInteger: 0
            Assert.True(VerifyDoubleExplicitCastToBigInteger(0), " Verification Failed");

            // Double Explicit Cast to BigInteger: 1
            Assert.True(VerifyDoubleExplicitCastToBigInteger(1), " Verification Failed");

            // Double Explicit Cast to BigInteger: Random Non-Integral Positive < 100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyDoubleExplicitCastToBigInteger((Double)(100 * s_random.NextDouble())), " Verification Failed");
            }

            // Double Explicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyDoubleExplicitCastToBigInteger((Double)(Double.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // Double Explicit Cast to BigInteger: Double.MaxValue
            Assert.True(VerifyDoubleExplicitCastToBigInteger(Double.MaxValue), " Verification Failed");

            // Double Explicit Cast to BigInteger: Double.PositiveInfinity
            Assert.True(VerifyException<OverflowException>(delegate () { BigInteger temp = (BigInteger)Double.PositiveInfinity; }), " Verification Failed");

            // Double Explicit Cast to BigInteger: Double.Epsilon
            Assert.True(VerifyDoubleExplicitCastToBigInteger(Double.Epsilon), " Verification Failed");

            // Double Explicit Cast to BigInteger: Double.NaN
            Assert.True(VerifyException<OverflowException>(delegate () { BigInteger temp = (BigInteger)Double.NaN; }), " Verification Failed");

            //There are multiple ways to represent a NaN just try another one
            // Double Explicit Cast to BigInteger: Double.NaN 2
            Assert.True(VerifyException<OverflowException>(delegate () { BigInteger temp = (BigInteger)ConvertInt64ToDouble(0x7FF8000000000000); }), " Verification Failed");

            // Double Explicit Cast to BigInteger: Smallest Exponent
            Assert.True(VerifyDoubleExplicitCastToBigInteger((Double)Math.Pow(2, -1022)), " Verification Failed");

            // Double Explicit Cast to BigInteger: Largest Exponent
            Assert.True(VerifyDoubleExplicitCastToBigInteger((Double)Math.Pow(2, 1023)), " Verification Failed");

            // Double Explicit Cast to BigInteger: Largest number less then 1
            value = 0;
            for (int i = 1; i <= 53; ++i)
            {
                value += (Double)(Math.Pow(2, -i));
            }
            Assert.True(VerifyDoubleExplicitCastToBigInteger(value), " Verification Failed");

            // Double Explicit Cast to BigInteger: Smallest number greater then 1
            value = (Double)(1 + Math.Pow(2, -52));
            Assert.True(VerifyDoubleExplicitCastToBigInteger(value), " Verification Failed");

            // Double Explicit Cast to BigInteger: Largest number less then 2
            value = 0;
            for (int i = 1; i <= 52; ++i)
            {
                value += (Double)(Math.Pow(2, -i));
            }
            value += 1;
            Assert.True(VerifyDoubleExplicitCastToBigInteger(value), " Verification Failed");
        }

        [Fact]
        public static void RunDecimalExplicitCastToBigIntegerTests()
        {
            Decimal value;

            // Decimal Explicit Cast to BigInteger: Decimal.MinValue
            Assert.True(VerifyDecimalExplicitCastToBigInteger(Decimal.MinValue), " Verification Failed");

            // Decimal Explicit Cast to BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                value = new Decimal(
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    true,
                    (byte)s_random.Next(0, 29));
                Assert.True(VerifyDecimalExplicitCastToBigInteger(value), " Verification Failed");
            }

            // Decimal Explicit Cast to BigInteger: -1
            Assert.True(VerifyDecimalExplicitCastToBigInteger(-1), " Verification Failed");

            // Decimal Explicit Cast to BigInteger: 0
            Assert.True(VerifyDecimalExplicitCastToBigInteger(0), " Verification Failed");

            // Decimal Explicit Cast to BigInteger: 1
            Assert.True(VerifyDecimalExplicitCastToBigInteger(1), " Verification Failed");

            // Decimal Explicit Cast to BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                value = new Decimal(
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    false,
                    (byte)s_random.Next(0, 29));
                Assert.True(VerifyDecimalExplicitCastToBigInteger(value), " Verification Failed");
            }

            // Decimal Explicit Cast to BigInteger: Decimal.MaxValue
            Assert.True(VerifyDecimalExplicitCastToBigInteger(Decimal.MaxValue), " Verification Failed");

            // Decimal Explicit Cast to BigInteger: Smallest Exponent
            unchecked
            {
                value = new Decimal(1, 0, 0, false, 0);
            }
            Assert.True(VerifyDecimalExplicitCastToBigInteger(value), " Verification Failed");

            // Decimal Explicit Cast to BigInteger: Largest Exponent and zero integer
            unchecked
            {
                value = new Decimal(0, 0, 0, false, 28);
            }
            Assert.True(VerifyDecimalExplicitCastToBigInteger(value), " Verification Failed");

            // Decimal Explicit Cast to BigInteger: Largest Exponent and non zero integer
            unchecked
            {
                value = new Decimal(1, 0, 0, false, 28);
            }
            Assert.True(VerifyDecimalExplicitCastToBigInteger(value), " Verification Failed");

            // Decimal Explicit Cast to BigInteger: Largest number less then 1
            value = 1 - new Decimal(1, 0, 0, false, 28);
            Assert.True(VerifyDecimalExplicitCastToBigInteger(value), " Verification Failed");

            // Decimal Explicit Cast to BigInteger: Smallest number greater then 1
            value = 1 + new Decimal(1, 0, 0, false, 28);
            Assert.True(VerifyDecimalExplicitCastToBigInteger(value), " Verification Failed");

            // Decimal Explicit Cast to BigInteger: Largest number less then 2
            value = 2 - new Decimal(1, 0, 0, false, 28);
            Assert.True(VerifyDecimalExplicitCastToBigInteger(value), " Verification Failed");
        }

        private static Single ConvertInt32ToSingle(Int32 value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }

        private static Double ConvertInt64ToDouble(Int64 value)
        {
            return BitConverter.ToDouble(BitConverter.GetBytes(value), 0);
        }

        private static bool VerifyByteImplicitCastToBigInteger(Byte value)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = value;

            ret &= Eval(bigInteger.Equals(value), String.Format("Expected BigInteger {0} to be equal to Byte {1}", bigInteger, value));
            ret &= Eval(value.ToString(), bigInteger.ToString(), "Byte.ToString() and BigInteger.ToString()");
            ret &= Eval(value, (Byte)bigInteger, "Round tripped Byte");

            if (value != Byte.MaxValue)
            {
                ret &= Eval((Byte)(value + 1), (Byte)(bigInteger + 1), "BigInteger added to 1");
            }

            if (value != Byte.MinValue)
            {
                ret &= Eval((Byte)(value - 1), (Byte)(bigInteger - 1), "BigInteger subtracted by 1");
            }

            ret &= VerifyBigintegerUsingIdentities(bigInteger, 0 == value);

            return ret;
        }

        private static bool VerifySByteImplicitCastToBigInteger(SByte value)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = value;

            ret &= Eval(bigInteger.Equals(value), String.Format("Expected BigInteger {0} to be equal to SByte {1}", bigInteger, value));
            ret &= Eval(value.ToString(), bigInteger.ToString(), "SByte.ToString() and BigInteger.ToString()");
            ret &= Eval(value, (SByte)bigInteger, "Round tripped SByte");

            if (value != SByte.MaxValue)
            {
                ret &= Eval((SByte)(value + 1), (SByte)(bigInteger + 1), "BigInteger added to 1");
            }

            if (value != SByte.MinValue)
            {
                ret &= Eval((SByte)(value - 1), (SByte)(bigInteger - 1), "BigInteger subtracted by 1");
            }

            ret &= VerifyBigintegerUsingIdentities(bigInteger, 0 == value);

            return ret;
        }

        private static bool VerifyUInt16ImplicitCastToBigInteger(UInt16 value)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = value;

            ret &= Eval(bigInteger.Equals(value), String.Format("Expected BigInteger {0} to be equal to UInt16 {1}", bigInteger, value));
            ret &= Eval(value.ToString(), bigInteger.ToString(), "UInt16.ToString() and BigInteger.ToString()");
            ret &= Eval(value, (UInt16)bigInteger, "Round tripped UInt16");

            if (value != UInt16.MaxValue)
            {
                ret &= Eval((UInt16)(value + 1), (UInt16)(bigInteger + 1), "BigInteger added to 1");
            }

            if (value != UInt16.MinValue)
            {
                ret &= Eval((UInt16)(value - 1), (UInt16)(bigInteger - 1), "BigInteger subtracted by 1");
            }

            ret &= VerifyBigintegerUsingIdentities(bigInteger, 0 == value);

            return ret;
        }

        private static bool VerifyInt16ImplicitCastToBigInteger(Int16 value)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = value;

            ret &= Eval(bigInteger.Equals(value), String.Format("Expected BigInteger {0} to be equal to Int16 {1}", bigInteger, value));
            ret &= Eval(value.ToString(), bigInteger.ToString(), "Int16.ToString() and BigInteger.ToString()");
            ret &= Eval(value, (Int16)bigInteger, "Round tripped Int16");

            if (value != Int16.MaxValue)
            {
                ret &= Eval((Int16)(value + 1), (Int16)(bigInteger + 1), "BigInteger added to 1");
            }

            if (value != Int16.MinValue)
            {
                ret &= Eval((Int16)(value - 1), (Int16)(bigInteger - 1), "BigInteger subtracted by 1");
            }

            ret &= VerifyBigintegerUsingIdentities(bigInteger, 0 == value);

            return ret;
        }

        private static bool VerifyUInt32ImplicitCastToBigInteger(UInt32 value)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = value;

            ret &= Eval(bigInteger.Equals(value), String.Format("Expected BigInteger {0} to be equal to UInt32 {1}", bigInteger, value));
            ret &= Eval(value.ToString(), bigInteger.ToString(), "UInt32.ToString() and BigInteger.ToString()");
            ret &= Eval(value, (UInt32)bigInteger, "Round tripped UInt32");

            if (value != UInt32.MaxValue)
            {
                ret &= Eval((UInt32)(value + 1), (UInt32)(bigInteger + 1), "BigInteger added to 1");
            }

            if (value != UInt32.MinValue)
            {
                ret &= Eval((UInt32)(value - 1), (UInt32)(bigInteger - 1), "BigInteger subtracted by 1");
            }

            ret &= VerifyBigintegerUsingIdentities(bigInteger, 0 == value);

            return ret;
        }

        private static bool VerifyInt32ImplicitCastToBigInteger(Int32 value)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = value;

            ret &= Eval(bigInteger.Equals(value), String.Format("Expected BigInteger {0} to be equal to Int32 {1}", bigInteger, value));
            ret &= Eval(value.ToString(), bigInteger.ToString(), "Int32.ToString() and BigInteger.ToString()");
            ret &= Eval(value, (Int32)bigInteger, "Round tripped Int32");

            if (value != Int32.MaxValue)
            {
                ret &= Eval((Int32)(value + 1), (Int32)(bigInteger + 1), "BigInteger added to 1");
            }

            if (value != Int32.MinValue)
            {
                ret &= Eval((Int32)(value - 1), (Int32)(bigInteger - 1), "BigInteger subtracted by 1");
            }

            ret &= VerifyBigintegerUsingIdentities(bigInteger, 0 == value);

            return ret;
        }

        private static bool VerifyUInt64ImplicitCastToBigInteger(UInt64 value)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = value;

            ret &= Eval(bigInteger.Equals(value), String.Format("Expected BigInteger {0} to be equal to UInt64 {1}", bigInteger, value));
            ret &= Eval(value.ToString(), bigInteger.ToString(), "UInt64.ToString() and BigInteger.ToString()");
            ret &= Eval(value, (UInt64)bigInteger, "Round tripped UInt64");

            if (value != UInt64.MaxValue)
            {
                ret &= Eval((UInt64)(value + 1), (UInt64)(bigInteger + 1), "BigInteger added to 1");
            }

            if (value != UInt64.MinValue)
            {
                ret &= Eval((UInt64)(value - 1), (UInt64)(bigInteger - 1), "BigInteger subtracted by 1");
            }

            ret &= VerifyBigintegerUsingIdentities(bigInteger, 0 == value);

            return ret;
        }

        private static bool VerifyInt64ImplicitCastToBigInteger(Int64 value)
        {
            bool ret = true;
            BigInteger bigInteger;

            bigInteger = value;

            ret &= Eval(bigInteger.Equals(value), String.Format("Expected BigInteger {0} to be equal to Int64 {1}", bigInteger, value));
            ret &= Eval(value.ToString(), bigInteger.ToString(), "Int64.ToString() and BigInteger.ToString()");
            ret &= Eval(value, (Int64)bigInteger, "Round tripped Int64");

            if (value != Int64.MaxValue)
            {
                ret &= Eval((Int64)(value + 1), (Int64)(bigInteger + 1), "BigInteger added to 1");
            }

            if (value != Int64.MinValue)
            {
                ret &= Eval((Int64)(value - 1), (Int64)(bigInteger - 1), "BigInteger subtracted by 1");
            }

            ret &= VerifyBigintegerUsingIdentities(bigInteger, 0 == value);

            return ret;
        }

        private static bool VerifySingleExplicitCastToBigInteger(Single value)
        {
            bool ret = true;
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

            ret &= Eval(expectedValue, (Single)bigInteger, "Round tripped Single");

            // Single can only accurately represent integers between -16777216 and 16777216 exclusive.
            // ToString starts to become inaccurate at this point.
            if (expectedValue < 16777216 && -16777216 < expectedValue)
            {
                ret &= Eval(expectedValue.ToString("G9"), bigInteger.ToString(), "Single.ToString() and BigInteger.ToString()");
            }

            if (expectedValue != Math.Floor(Single.MaxValue))
            {
                ret &= Eval((Single)(expectedValue + 1), (Single)(bigInteger + 1), "BigInteger added to 1");
            }

            if (expectedValue != Math.Ceiling(Single.MinValue))
            {
                ret &= Eval((Single)(expectedValue - 1), (Single)(bigInteger - 1), "BigInteger subtracted by 1");
            }

            ret &= VerifyBigintegerUsingIdentities(bigInteger, 0 == expectedValue);

            return ret;
        }

        private static bool VerifyDoubleExplicitCastToBigInteger(Double value)
        {
            bool ret = true;
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

            ret &= Eval(expectedValue, (Double)bigInteger, "Round tripped Double");

            // Double can only accurately represent integers between -9007199254740992 and 9007199254740992 exclusive.
            // ToString starts to become inaccurate at this point.
            if (expectedValue < 9007199254740992 && -9007199254740992 < expectedValue)
            {
                ret &= Eval(expectedValue.ToString(), bigInteger.ToString(), "Single.ToString() and BigInteger.ToString()");
            }

            if (!Single.IsInfinity((Single)expectedValue))
            {
                ret &= Eval((Double)(expectedValue + 1), (Double)(bigInteger + 1), "Adding 1");
                ret &= Eval((Double)(expectedValue - 1), (Double)(bigInteger - 1), "Subtracting 1");
            }

            ret &= VerifyBigintegerUsingIdentities(bigInteger, 0 == expectedValue);

            return ret;
        }

        private static bool VerifyDecimalExplicitCastToBigInteger(Decimal value)
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

            bigInteger = (BigInteger)value;

            ret &= Eval(expectedValue.ToString(), bigInteger.ToString(), "Decimal.ToString() and BigInteger.ToString()");
            ret &= Eval(expectedValue, (Decimal)bigInteger, "Round tripped Decimal");

            ret &= VerifyBigintegerUsingIdentities(bigInteger, 0 == expectedValue);

            if (expectedValue != Math.Floor(Decimal.MaxValue))
            {
                ret &= Eval((Decimal)(expectedValue + 1), (Decimal)(bigInteger + 1), "BigInteger added to 1");
            }

            if (expectedValue != Math.Ceiling(Decimal.MinValue))
            {
                ret &= Eval((Decimal)(expectedValue - 1), (Decimal)(bigInteger - 1), "BigInteger subtracted by 1");
            }

            return ret;
        }

        private static bool VerifyBigintegerUsingIdentities(BigInteger bigInteger, bool isZero)
        {
            bool ret = true;
            BigInteger tempBigInteger;

            tempBigInteger = new BigInteger(bigInteger.ToByteArray());

            ret &= Eval(bigInteger, tempBigInteger, "BigInteger coppied using ctor(byte[], bool)");

            if (isZero)
            {
                ret &= Eval(BigInteger.Zero, bigInteger, "Comparing constructed BigInteger with BigInteger.Zero");
            }
            else
            {
                ret &= Eval(BigInteger.Zero != bigInteger, String.Format("Expected BigInteger to not be equal to zero {0}", bigInteger));

                // x/x = 1
                ret &= Eval(BigInteger.One, bigInteger / bigInteger, "BigInteger divided by itself");
            }

            // (x + 1) - 1 = x
            ret &= Eval(bigInteger, (bigInteger + BigInteger.One) - BigInteger.One, "Add 1 to the BigInteger then subtract 1");

            // (x + 1) - x = 1
            ret &= Eval(BigInteger.One, (bigInteger + BigInteger.One) - bigInteger, "Add 1 to the BigInteger then subtract the BigInteger");

            // x - x = 0
            ret &= Eval(BigInteger.Zero, bigInteger - bigInteger, "Subtract the BigInteger form itself");

            // x + x = 2x
            ret &= Eval(2 * bigInteger, bigInteger + bigInteger, "Expected Adding the BigInteger to istself to be equal to 2 times the BigInteger");

            // x/1 = x
            ret &= Eval(bigInteger, bigInteger / BigInteger.One, "BigInteger divided by 1");

            // 1 * x = x
            ret &= Eval(bigInteger, BigInteger.One * bigInteger, "BigInteger multiplied by 1");

            return ret;
        }

        public static bool Eval<T>(T expected, T actual, String errorMsg)
        {
            bool retValue = expected == null ? actual == null : expected.Equals(actual);

            if (!retValue)
                return Eval(retValue, errorMsg +
                " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                " Actual:" + (null == actual ? "<null>" : actual.ToString()));

            return true;
        }
        public static bool Eval(bool expression, string message)
        {
            if (!expression)
            {
                Console.WriteLine(message);
            }

            return expression;
        }

        public static bool VerifyException<T>(ExceptionGenerator exceptionGenerator) where T : Exception
        {
            return VerifyException(typeof(T), exceptionGenerator);
        }
        public static bool VerifyException(Type expectedExceptionType, ExceptionGenerator exceptionGenerator)
        {
            return VerifyException(expectedExceptionType, exceptionGenerator, String.Empty);
        }
        public static bool VerifyException(Type expectedExceptionType, ExceptionGenerator exceptionGenerator, string message)
        {
            bool retValue = true;

            try
            {
                exceptionGenerator();
                retValue &= Eval(false, (String.IsNullOrEmpty(message) ? String.Empty : (message + Environment.NewLine)) +
                    String.Format("Err_05940iedz Expected exception of the type {0} to be thrown and nothing was thrown", expectedExceptionType));
            }
            catch (Exception exception)
            {
                retValue &= Eval<Type>(expectedExceptionType, exception.GetType(),
                    (String.IsNullOrEmpty(message) ? String.Empty : (message + Environment.NewLine)) +
                    String.Format("Err_38223oipwj Expected exception and actual exception differ.  Expected {0}, got \n{1}", expectedExceptionType, exception));
            }

            return retValue;
        }

        private static bool ApproxEqual(double value1, double value2)
        {
            //Special case values;
            if (Double.IsNaN(value1))
                return Double.IsNaN(value2);
            if (Double.IsNegativeInfinity(value1))
                return Double.IsNegativeInfinity(value2);
            if (Double.IsPositiveInfinity(value1))
                return Double.IsPositiveInfinity(value2);

            double result = Math.Abs((value1 / value2) - 1);

            if (result <= Double.Parse("1e-16"))
                Console.WriteLine("Values not approximately equal");

            return (result <= Double.Parse("1e-16"));
        }
    }
}
