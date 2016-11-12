// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class cast_fromTest
    {
        public delegate void ExceptionGenerator();

        private const int NumberOfRandomIterations = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunByteExplicitCastFromBigIntegerTests()
        {
            Byte value = 0;
            BigInteger bigInteger;

            // Byte Explicit Cast from BigInteger: Random value < Byte.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(Byte.MinValue, s_random);
            value = bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifyByteExplicitCastFromBigInteger(value, bigInteger));

            // Byte Explicit Cast from BigInteger: Byte.MinValue - 1
            bigInteger = new BigInteger(Byte.MinValue);
            bigInteger -= BigInteger.One;
            value = bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifyByteExplicitCastFromBigInteger(value, bigInteger));

            // Byte Explicit Cast from BigInteger: Byte.MinValue
            VerifyByteExplicitCastFromBigInteger(Byte.MinValue);

            // Byte Explicit Cast from BigInteger: 0
            VerifyByteExplicitCastFromBigInteger(0);

            // Byte Explicit Cast from BigInteger: 1
            VerifyByteExplicitCastFromBigInteger(1);

            // Byte Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyByteExplicitCastFromBigInteger((Byte)s_random.Next(1, Byte.MaxValue));
            }

            // Byte Explicit Cast from BigInteger: Byte.MaxValue + 1
            bigInteger = new BigInteger(Byte.MaxValue);
            bigInteger += BigInteger.One;
            value = bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifyByteExplicitCastFromBigInteger(value, bigInteger));

            // Byte Explicit Cast from BigInteger: Random value > Byte.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(Byte.MaxValue, s_random);
            value = bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifyByteExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunSByteExplicitCastFromBigIntegerTests()
        {
            SByte value = 0;
            BigInteger bigInteger;

            // SByte Explicit Cast from BigInteger: Random value < SByte.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(SByte.MinValue, s_random);
            value = (SByte)bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifySByteExplicitCastFromBigInteger(value, bigInteger));

            // SByte Explicit Cast from BigInteger: SByte.MinValue - 1
            bigInteger = new BigInteger(SByte.MinValue);
            bigInteger -= BigInteger.One;
            value = (SByte)bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifySByteExplicitCastFromBigInteger(value, bigInteger));

            // SByte Explicit Cast from BigInteger: SByte.MinValue
            VerifySByteExplicitCastFromBigInteger(SByte.MinValue);

            // SByte Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySByteExplicitCastFromBigInteger((SByte)s_random.Next(SByte.MinValue, 0));
            }

            // SByte Explicit Cast from BigInteger: -1
            VerifySByteExplicitCastFromBigInteger(-1);

            // SByte Explicit Cast from BigInteger: 0
            VerifySByteExplicitCastFromBigInteger(0);

            // SByte Explicit Cast from BigInteger: 1
            VerifySByteExplicitCastFromBigInteger(1);

            // SByte Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySByteExplicitCastFromBigInteger((SByte)s_random.Next(1, SByte.MaxValue));
            }

            // SByte Explicit Cast from BigInteger: SByte.MaxValue
            VerifySByteExplicitCastFromBigInteger(SByte.MaxValue);

            // SByte Explicit Cast from BigInteger: SByte.MaxValue + 1
            bigInteger = new BigInteger(SByte.MaxValue);
            bigInteger += BigInteger.One;
            value = (SByte)bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifySByteExplicitCastFromBigInteger(value, bigInteger));

            // SByte Explicit Cast from BigInteger: Random value > SByte.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan((UInt64)SByte.MaxValue, s_random);
            value = (SByte)bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifySByteExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunUInt16ExplicitCastFromBigIntegerTests()
        {
            ushort value;
            BigInteger bigInteger;

            // UInt16 Explicit Cast from BigInteger: Random value < UInt16.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(UInt16.MinValue, s_random);
            value = BitConverter.ToUInt16(ByteArrayMakeMinSize(bigInteger.ToByteArray(), 2), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger));

            // UInt16 Explicit Cast from BigInteger: UInt16.MinValue - 1
            bigInteger = new BigInteger(UInt16.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToUInt16(new byte[] { 0xff, 0xff }, 0);
            Assert.Throws<OverflowException>(() => VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger));

            // UInt16 Explicit Cast from BigInteger: UInt16.MinValue
            VerifyUInt16ExplicitCastFromBigInteger(UInt16.MinValue);

            // UInt16 Explicit Cast from BigInteger: 0
            VerifyUInt16ExplicitCastFromBigInteger(0);

            // UInt16 Explicit Cast from BigInteger: 1
            VerifyUInt16ExplicitCastFromBigInteger(1);

            // UInt16 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyUInt16ExplicitCastFromBigInteger((UInt16)s_random.Next(1, UInt16.MaxValue));
            }

            // UInt16 Explicit Cast from BigInteger: UInt16.MaxValue
            VerifyUInt16ExplicitCastFromBigInteger(UInt16.MaxValue);

            // UInt16 Explicit Cast from BigInteger: UInt16.MaxValue + 1
            bigInteger = new BigInteger(UInt16.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToUInt16(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger));

            // UInt16 Explicit Cast from BigInteger: Random value > UInt16.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(UInt16.MaxValue, s_random);
            value = BitConverter.ToUInt16(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunInt16ExplicitCastFromBigIntegerTests()
        {
            short value;
            BigInteger bigInteger;

            // Int16 Explicit Cast from BigInteger: Random value < Int16.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(Int16.MinValue, s_random);
            value = BitConverter.ToInt16(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt16ExplicitCastFromBigInteger(value, bigInteger));

            // Int16 Explicit Cast from BigInteger: Int16.MinValue - 1
            bigInteger = new BigInteger(Int16.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToInt16(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt16ExplicitCastFromBigInteger(value, bigInteger));

            // Int16 Explicit Cast from BigInteger: Int16.MinValue
            VerifyInt16ExplicitCastFromBigInteger(Int16.MinValue);

            // Int16 Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt16ExplicitCastFromBigInteger((Int16)s_random.Next(Int16.MinValue, 0));
            }

            // Int16 Explicit Cast from BigInteger: -1
            VerifyInt16ExplicitCastFromBigInteger(-1);

            // Int16 Explicit Cast from BigInteger: 0
            VerifyInt16ExplicitCastFromBigInteger(0);

            // Int16 Explicit Cast from BigInteger: 1
            VerifyInt16ExplicitCastFromBigInteger(1);

            // Int16 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt16ExplicitCastFromBigInteger((Int16)s_random.Next(1, Int16.MaxValue));
            }

            // Int16 Explicit Cast from BigInteger: Int16.MaxValue
            VerifyInt16ExplicitCastFromBigInteger(Int16.MaxValue);

            // Int16 Explicit Cast from BigInteger: Int16.MaxValue + 1
            bigInteger = new BigInteger(Int16.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToInt16(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt16ExplicitCastFromBigInteger(value, bigInteger));

            // Int16 Explicit Cast from BigInteger: Random value > Int16.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan((UInt64)Int16.MaxValue, s_random);
            value = BitConverter.ToInt16(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt16ExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunUInt32ExplicitCastFromBigIntegerTests()
        {
            uint value;
            BigInteger bigInteger;

            // UInt32 Explicit Cast from BigInteger: Random value < UInt32.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(UInt32.MinValue, s_random);
            value = BitConverter.ToUInt32(ByteArrayMakeMinSize(bigInteger.ToByteArray(), 4), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger));

            // UInt32 Explicit Cast from BigInteger: UInt32.MinValue - 1
            bigInteger = new BigInteger(UInt32.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToUInt32(new byte[] { 0xff, 0xff, 0xff, 0xff }, 0);
            Assert.Throws<OverflowException>(() => VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger));

            // UInt32 Explicit Cast from BigInteger: UInt32.MinValue
            VerifyUInt32ExplicitCastFromBigInteger(UInt32.MinValue);

            // UInt32 Explicit Cast from BigInteger: 0
            VerifyUInt32ExplicitCastFromBigInteger(0);

            // UInt32 Explicit Cast from BigInteger: 1
            VerifyUInt32ExplicitCastFromBigInteger(1);

            // UInt32 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyUInt32ExplicitCastFromBigInteger((UInt32)(UInt32.MaxValue * s_random.NextDouble()));
            }

            // UInt32 Explicit Cast from BigInteger: UInt32.MaxValue
            VerifyUInt32ExplicitCastFromBigInteger(UInt32.MaxValue);

            // UInt32 Explicit Cast from BigInteger: UInt32.MaxValue + 1
            bigInteger = new BigInteger(UInt32.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToUInt32(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger));

            // UInt32 Explicit Cast from BigInteger: Random value > UInt32.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(UInt32.MaxValue, s_random);
            value = BitConverter.ToUInt32(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunInt32ExplicitCastFromBigIntegerTests()
        {
            int value;
            BigInteger bigInteger;

            // Int32 Explicit Cast from BigInteger: Random value < Int32.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(Int32.MinValue, s_random);
            value = BitConverter.ToInt32(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt32ExplicitCastFromBigInteger(value, bigInteger));

            // Int32 Explicit Cast from BigInteger: Int32.MinValue - 1
            bigInteger = new BigInteger(Int32.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToInt32(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt32ExplicitCastFromBigInteger(value, bigInteger));

            // Int32 Explicit Cast from BigInteger: Int32.MinValue
            VerifyInt32ExplicitCastFromBigInteger(Int32.MinValue);

            // Int32 Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt32ExplicitCastFromBigInteger((Int32)s_random.Next(Int32.MinValue, 0));
            }

            // Int32 Explicit Cast from BigInteger: -1
            VerifyInt32ExplicitCastFromBigInteger(-1);

            // Int32 Explicit Cast from BigInteger: 0
            VerifyInt32ExplicitCastFromBigInteger(0);

            // Int32 Explicit Cast from BigInteger: 1
            VerifyInt32ExplicitCastFromBigInteger(1);

            // Int32 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt32ExplicitCastFromBigInteger((Int32)s_random.Next(1, Int32.MaxValue));
            }

            // Int32 Explicit Cast from BigInteger: Int32.MaxValue
            VerifyInt32ExplicitCastFromBigInteger(Int32.MaxValue);

            // Int32 Explicit Cast from BigInteger: Int32.MaxValue + 1
            bigInteger = new BigInteger(Int32.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToInt32(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt32ExplicitCastFromBigInteger(value, bigInteger));

            // Int32 Explicit Cast from BigInteger: Random value > Int32.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(Int32.MaxValue, s_random);
            value = BitConverter.ToInt32(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt32ExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunUInt64ExplicitCastFromBigIntegerTests()
        {
            ulong value;
            BigInteger bigInteger;

            // UInt64 Explicit Cast from BigInteger: Random value < UInt64.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(0, s_random);
            value = BitConverter.ToUInt64(ByteArrayMakeMinSize(bigInteger.ToByteArray(), 8), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger));

            // UInt64 Explicit Cast from BigInteger: UInt64.MinValue - 1
            bigInteger = new BigInteger(UInt64.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToUInt64(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, 0);
            Assert.Throws<OverflowException>(() => VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger));

            // UInt64 Explicit Cast from BigInteger: UInt64.MinValue
            VerifyUInt64ExplicitCastFromBigInteger(UInt64.MinValue);

            // UInt64 Explicit Cast from BigInteger: 0
            VerifyUInt64ExplicitCastFromBigInteger(0);

            // UInt64 Explicit Cast from BigInteger: 1
            VerifyUInt64ExplicitCastFromBigInteger(1);

            // UInt64 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyUInt64ExplicitCastFromBigInteger((UInt64)(UInt64.MaxValue * s_random.NextDouble()));
            }

            // UInt64 Explicit Cast from BigInteger: UInt64.MaxValue
            VerifyUInt64ExplicitCastFromBigInteger(UInt64.MaxValue);

            // UInt64 Explicit Cast from BigInteger: UInt64.MaxValue + 1
            bigInteger = new BigInteger(UInt64.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToUInt64(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger));

            // UInt64 Explicit Cast from BigInteger: Random value > UInt64.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(UInt64.MaxValue, s_random);
            value = BitConverter.ToUInt64(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunInt64ExplicitCastFromBigIntegerTests()
        {
            long value;
            BigInteger bigInteger;

            // Int64 Explicit Cast from BigInteger: Random value < Int64.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(Int64.MinValue, s_random);
            value = BitConverter.ToInt64(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt64ExplicitCastFromBigInteger(value, bigInteger));

            // Int64 Explicit Cast from BigInteger: Int64.MinValue - 1
            bigInteger = new BigInteger(Int64.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToInt64(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt64ExplicitCastFromBigInteger(value, bigInteger));

            // Int64 Explicit Cast from BigInteger: Int64.MinValue
            VerifyInt64ExplicitCastFromBigInteger(Int64.MinValue);

            // Int64 Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt64ExplicitCastFromBigInteger(((Int64)(Int64.MaxValue * s_random.NextDouble())) - Int64.MaxValue);
            }

            // Int64 Explicit Cast from BigInteger: -1
            VerifyInt64ExplicitCastFromBigInteger(-1);

            // Int64 Explicit Cast from BigInteger: 0
            VerifyInt64ExplicitCastFromBigInteger(0);

            // Int64 Explicit Cast from BigInteger: 1
            VerifyInt64ExplicitCastFromBigInteger(1);

            // Int64 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt64ExplicitCastFromBigInteger((Int64)(Int64.MaxValue * s_random.NextDouble()));
            }

            // Int64 Explicit Cast from BigInteger: Int64.MaxValue
            VerifyInt64ExplicitCastFromBigInteger(Int64.MaxValue);

            // Int64 Explicit Cast from BigInteger: Int64.MaxValue + 1
            bigInteger = new BigInteger(Int64.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToInt64(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt64ExplicitCastFromBigInteger(value, bigInteger));

            // Int64 Explicit Cast from BigInteger: Random value > Int64.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(Int64.MaxValue, s_random);
            value = BitConverter.ToInt64(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt64ExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunSingleExplicitCastFromBigIntegerTests()
        {
            BigInteger bigInteger;

            // Single Explicit Cast from BigInteger: Random value < Single.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(Single.MinValue * 2.0, s_random);
            VerifySingleExplicitCastFromBigInteger(Single.NegativeInfinity, bigInteger);

            // Single Explicit Cast from BigInteger: Single.MinValue - 1
            bigInteger = new BigInteger(Single.MinValue);
            bigInteger -= BigInteger.One;
            VerifySingleExplicitCastFromBigInteger(Single.MinValue, bigInteger);

            // Single Explicit Cast from BigInteger: Single.MinValue
            VerifySingleExplicitCastFromBigInteger(Single.MinValue);

            // Single Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastFromBigInteger(((Single)(Single.MaxValue * s_random.NextDouble())) - Single.MaxValue);
            }

            // Single Explicit Cast from BigInteger: Random Negative Non-integral > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastFromBigInteger(((Single)(100 * s_random.NextDouble())) - 100);
            }

            // Single Explicit Cast from BigInteger: -1
            VerifySingleExplicitCastFromBigInteger(-1);

            // Single Explicit Cast from BigInteger: 0
            VerifySingleExplicitCastFromBigInteger(0);

            // Single Explicit Cast from BigInteger: 1
            VerifySingleExplicitCastFromBigInteger(1);

            // Single Explicit Cast from BigInteger: Random Positive Non-integral < 100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastFromBigInteger((Single)(100 * s_random.NextDouble()));
            }

            // Single Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastFromBigInteger((Single)(Single.MaxValue * s_random.NextDouble()));
            }

            // Single Explicit Cast from BigInteger: Single.MaxValue + 1
            bigInteger = new BigInteger(Single.MaxValue);
            bigInteger += BigInteger.One;
            VerifySingleExplicitCastFromBigInteger(Single.MaxValue, bigInteger);

            // Single Explicit Cast from BigInteger: Random value > Single.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan((double)Single.MaxValue * 2, s_random);
            VerifySingleExplicitCastFromBigInteger(Single.PositiveInfinity, bigInteger);

            // Single Explicit Cast from BigInteger: value < Single.MaxValue but can not be accurately represented in a Single
            bigInteger = new BigInteger(16777217);
            VerifySingleExplicitCastFromBigInteger(16777216f, bigInteger);

            // Single Explicit Cast from BigInteger: Single.MinValue < value but can not be accurately represented in a Single
            bigInteger = new BigInteger(-16777217);
            VerifySingleExplicitCastFromBigInteger(-16777216f, bigInteger);
        }

        [Fact]
        public static void RunDoubleExplicitCastFromBigIntegerTests()
        {
            BigInteger bigInteger;

            // Double Explicit Cast from BigInteger: Random value < Double.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(Double.MinValue, s_random);
            bigInteger *= 2;
            VerifyDoubleExplicitCastFromBigInteger(Double.NegativeInfinity, bigInteger);

            // Double Explicit Cast from BigInteger: Double.MinValue - 1
            bigInteger = new BigInteger(Double.MinValue);
            bigInteger -= BigInteger.One;
            VerifyDoubleExplicitCastFromBigInteger(Double.MinValue, bigInteger);

            // Double Explicit Cast from BigInteger: Double.MinValue
            VerifyDoubleExplicitCastFromBigInteger(Double.MinValue);

            // Double Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastFromBigInteger(((Double)(Double.MaxValue * s_random.NextDouble())) - Double.MaxValue);
            }

            // Double Explicit Cast from BigInteger: Random Negative Non-integral > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastFromBigInteger(((Double)(100 * s_random.NextDouble())) - 100);
            }

            // Double Explicit Cast from BigInteger: -1
            VerifyDoubleExplicitCastFromBigInteger(-1);

            // Double Explicit Cast from BigInteger: 0
            VerifyDoubleExplicitCastFromBigInteger(0);

            // Double Explicit Cast from BigInteger: 1
            VerifyDoubleExplicitCastFromBigInteger(1);

            // Double Explicit Cast from BigInteger: Random Positive Non-integral < 100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastFromBigInteger((Double)(100 * s_random.NextDouble()));
            }

            // Double Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastFromBigInteger((Double)(Double.MaxValue * s_random.NextDouble()));
            }

            // Double Explicit Cast from BigInteger: Double.MaxValue
            VerifyDoubleExplicitCastFromBigInteger(Double.MaxValue);

            // Double Explicit Cast from BigInteger: Double.MaxValue + 1
            bigInteger = new BigInteger(Double.MaxValue);
            bigInteger += BigInteger.One;
            VerifyDoubleExplicitCastFromBigInteger(Double.MaxValue, bigInteger);

            // Double Explicit Cast from BigInteger: Double.MinValue - 1
            bigInteger = new BigInteger(Double.MinValue);
            bigInteger -= BigInteger.One;
            VerifyDoubleExplicitCastFromBigInteger(Double.MinValue, bigInteger);

            // Double Explicit Cast from BigInteger: Random value > Double.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(Double.MaxValue, s_random);
            bigInteger *= 2;
            VerifyDoubleExplicitCastFromBigInteger(Double.PositiveInfinity, bigInteger);

            // Double Explicit Cast from BigInteger: Random value < -Double.MaxValue
            VerifyDoubleExplicitCastFromBigInteger(Double.NegativeInfinity, -bigInteger);

            // Double Explicit Cast from BigInteger: very large values (more than Int32.MaxValue bits) should be infinity
            DoubleExplicitCastFromLargeBigIntegerTests(128, 1);

            // Double Explicit Cast from BigInteger: value < Double.MaxValue but can not be accurately represented in a Double
            bigInteger = new BigInteger(9007199254740993);
            VerifyDoubleExplicitCastFromBigInteger(9007199254740992, bigInteger);

            // Double Explicit Cast from BigInteger: Double.MinValue < value but can not be accurately represented in a Double
            bigInteger = new BigInteger(-9007199254740993);
            VerifyDoubleExplicitCastFromBigInteger(-9007199254740992, bigInteger);
        }

        [Fact]
        [OuterLoop]
        public static void RunDoubleExplicitCastFromLargeBigIntegerTests()
        {
            DoubleExplicitCastFromLargeBigIntegerTests(0, 4, 32, 3);
        }

        [Fact]
        public static void RunDecimalExplicitCastFromBigIntegerTests()
        {
            int[] bits = new int[3];
            uint temp2;
            bool carry;
            byte[] temp;
            Decimal value;
            BigInteger bigInteger;

            // Decimal Explicit Cast from BigInteger: Random value < Decimal.MinValue
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                bigInteger = GenerateRandomBigIntegerLessThan(Decimal.MinValue, s_random);
                temp = bigInteger.ToByteArray();
                carry = true;
                for (int j = 0; j < 3; j++)
                {
                    temp2 = BitConverter.ToUInt32(temp, 4 * j);
                    temp2 = ~temp2;
                    if (carry)
                    {
                        carry = false;
                        temp2 += 1;
                        if (temp2 == 0)
                        {
                            carry = true;
                        }
                    }
                    bits[j] = (int)temp2;
                }
                value = new Decimal(bits[0], bits[1], bits[2], true, 0);
                Assert.Throws<OverflowException>(() => VerifyDecimalExplicitCastFromBigInteger(value, bigInteger));
            }

            // Decimal Explicit Cast from BigInteger: Decimal.MinValue - 1
            bigInteger = new BigInteger(Decimal.MinValue);
            bigInteger -= BigInteger.One;
            temp = bigInteger.ToByteArray();
            carry = true;
            for (int j = 0; j < 3; j++)
            {
                temp2 = BitConverter.ToUInt32(temp, 4 * j);
                temp2 = ~temp2;
                if (carry)
                {
                    carry = false;
                    temp2 += 1;
                    if (temp2 == 0)
                    {
                        carry = true;
                    }
                }
                bits[j] = (int)temp2;
            }
            value = new Decimal(bits[0], bits[1], bits[2], true, 0);
            Assert.Throws<OverflowException>(() => VerifyDecimalExplicitCastFromBigInteger(value, bigInteger));

            // Decimal Explicit Cast from BigInteger: Decimal.MinValue
            VerifyDecimalExplicitCastFromBigInteger(Decimal.MinValue);

            // Decimal Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDecimalExplicitCastFromBigInteger(((Decimal)((Double)Decimal.MaxValue * s_random.NextDouble())) - Decimal.MaxValue);
            }

            // Decimal Explicit Cast from BigInteger: Random Negative Non-Integral > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                value = (Decimal)(100 * s_random.NextDouble() - 100);
                VerifyDecimalExplicitCastFromBigInteger(Decimal.Truncate(value), new BigInteger(value));
            }

            // Decimal Explicit Cast from BigInteger: -1
            VerifyDecimalExplicitCastFromBigInteger(-1);

            // Decimal Explicit Cast from BigInteger: 0
            VerifyDecimalExplicitCastFromBigInteger(0);

            // Decimal Explicit Cast from BigInteger: 1
            VerifyDecimalExplicitCastFromBigInteger(1);

            // Decimal Explicit Cast from BigInteger: Random Positive Non-Integral < 100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                value = (Decimal)(100 * s_random.NextDouble());
                VerifyDecimalExplicitCastFromBigInteger(Decimal.Truncate(value), new BigInteger(value));
            }

            // Decimal Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDecimalExplicitCastFromBigInteger((Decimal)((Double)Decimal.MaxValue * s_random.NextDouble()));
            }

            // Decimal Explicit Cast from BigInteger: Decimal.MaxValue
            VerifyDecimalExplicitCastFromBigInteger(Decimal.MaxValue);

            // Decimal Explicit Cast from BigInteger: Decimal.MaxValue + 1
            bigInteger = new BigInteger(Decimal.MaxValue);
            bigInteger += BigInteger.One;
            temp = bigInteger.ToByteArray();
            for (int j = 0; j < 3; j++)
            {
                bits[j] = BitConverter.ToInt32(temp, 4 * j);
            }
            value = new Decimal(bits[0], bits[1], bits[2], false, 0);
            Assert.Throws<OverflowException>(() => VerifyDecimalExplicitCastFromBigInteger(value, bigInteger));

            // Decimal Explicit Cast from BigInteger: Random value > Decimal.MaxValue
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                bigInteger = GenerateRandomBigIntegerGreaterThan(Decimal.MaxValue, s_random);
                temp = bigInteger.ToByteArray();
                for (int j = 0; j < 3; j++)
                {
                    bits[j] = BitConverter.ToInt32(temp, 4 * j);
                }
                value = new Decimal(bits[0], bits[1], bits[2], false, 0);
                Assert.Throws<OverflowException>(() => VerifyDecimalExplicitCastFromBigInteger(value, bigInteger));
            }
        }

        /// <summary>
        /// Test cast to Double on Very Large BigInteger more than (1 &lt;&lt; Int.MaxValue)
        /// Tested BigInteger are: +/-pow(2, startShift + smallLoopShift * [1..smallLoopLimit] + Int32.MaxValue * [1..bigLoopLimit])
        /// Expected double is positive and negative infinity
        /// Note: 
        /// ToString() can not operate such large values
        /// </summary>
        private static void DoubleExplicitCastFromLargeBigIntegerTests(int startShift, int bigShiftLoopLimit, int smallShift = 0, int smallShiftLoopLimit = 1)
        {
            BigInteger init = BigInteger.One << startShift;

            for (int i = 0; i < smallShiftLoopLimit; i++)
            {
                BigInteger temp = init << ((i + 1) * smallShift);

                for (int j = 0; j < bigShiftLoopLimit; j++)
                {
                    temp = temp << (int.MaxValue / 2);
                    VerifyDoubleExplicitCastFromBigInteger(Double.PositiveInfinity, temp);
                    VerifyDoubleExplicitCastFromBigInteger(Double.NegativeInfinity, -temp);
                }

            }
        }

        private static BigInteger GenerateRandomNegativeBigInteger(Random random)
        {
            BigInteger bigInteger;
            int arraySize = random.Next(1, 8) * 4;
            byte[] byteArray = new byte[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                byteArray[i] = (byte)random.Next(0, 256);
            }
            byteArray[arraySize - 1] |= 0x80;

            bigInteger = new BigInteger(byteArray);

            return bigInteger;
        }

        private static BigInteger GenerateRandomPositiveBigInteger(Random random)
        {
            BigInteger bigInteger;
            int arraySize = random.Next(1, 8) * 4;
            byte[] byteArray = new byte[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                byteArray[i] = (byte)random.Next(0, 256);
            }
            byteArray[arraySize - 1] &= 0x7f;

            bigInteger = new BigInteger(byteArray);

            return bigInteger;
        }

        private static BigInteger GenerateRandomBigIntegerLessThan(Int64 value, Random random)
        {
            return (GenerateRandomNegativeBigInteger(random) + value) - 1;
        }

        private static BigInteger GenerateRandomBigIntegerLessThan(Double value, Random random)
        {
            return (GenerateRandomNegativeBigInteger(random) + (BigInteger)value) - 1;
        }

        private static BigInteger GenerateRandomBigIntegerLessThan(Decimal value, Random random)
        {
            return (GenerateRandomNegativeBigInteger(random) + (BigInteger)value) - 1;
        }

        private static BigInteger GenerateRandomBigIntegerGreaterThan(UInt64 value, Random random)
        {
            return (GenerateRandomPositiveBigInteger(random) + value) + 1;
        }

        private static BigInteger GenerateRandomBigIntegerGreaterThan(Double value, Random random)
        {
            return (GenerateRandomPositiveBigInteger(random) + (BigInteger)value) + 1;
        }

        private static BigInteger GenerateRandomBigIntegerGreaterThan(Decimal value, Random random)
        {
            return (GenerateRandomPositiveBigInteger(random) + (BigInteger)value) + 1;
        }

        private static void VerifyByteExplicitCastFromBigInteger(Byte value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyByteExplicitCastFromBigInteger(value, bigInteger);
        }

        private static void VerifyByteExplicitCastFromBigInteger(Byte value, BigInteger bigInteger)
        {
            Assert.Equal(value, (Byte)bigInteger);
        }

        private static void VerifySByteExplicitCastFromBigInteger(SByte value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifySByteExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifySByteExplicitCastFromBigInteger(SByte value, BigInteger bigInteger)
        {
            Assert.Equal(value, (SByte)bigInteger);
        }

        private static void VerifyUInt16ExplicitCastFromBigInteger(UInt16 value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyUInt16ExplicitCastFromBigInteger(UInt16 value, BigInteger bigInteger)
        {
            Assert.Equal(value, (UInt16)bigInteger);
        }

        private static void VerifyInt16ExplicitCastFromBigInteger(Int16 value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyInt16ExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyInt16ExplicitCastFromBigInteger(Int16 value, BigInteger bigInteger)
        {
            Assert.Equal(value, (Int16)bigInteger);
        }

        private static void VerifyUInt32ExplicitCastFromBigInteger(UInt32 value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyUInt32ExplicitCastFromBigInteger(UInt32 value, BigInteger bigInteger)
        {
            Assert.Equal(value, (UInt32)bigInteger);
        }

        private static void VerifyInt32ExplicitCastFromBigInteger(Int32 value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyInt32ExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyInt32ExplicitCastFromBigInteger(Int32 value, BigInteger bigInteger)
        {
            Assert.Equal(value, (Int32)bigInteger);
        }

        private static void VerifyUInt64ExplicitCastFromBigInteger(UInt64 value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyUInt64ExplicitCastFromBigInteger(UInt64 value, BigInteger bigInteger)
        {
            Assert.Equal(value, (UInt64)bigInteger);
        }

        private static void VerifyInt64ExplicitCastFromBigInteger(Int64 value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyInt64ExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyInt64ExplicitCastFromBigInteger(Int64 value, BigInteger bigInteger)
        {
            Assert.Equal(value, (Int64)bigInteger);
        }

        private static void VerifySingleExplicitCastFromBigInteger(Single value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifySingleExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifySingleExplicitCastFromBigInteger(Single value, BigInteger bigInteger)
        {
            Assert.Equal((Single)Math.Truncate(value), (Single)bigInteger);
        }

        private static void VerifyDoubleExplicitCastFromBigInteger(Double value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyDoubleExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyDoubleExplicitCastFromBigInteger(Double value, BigInteger bigInteger)
        {
            Assert.Equal(Math.Truncate(value), (Double)bigInteger);
        }

        private static void VerifyDecimalExplicitCastFromBigInteger(Decimal value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyDecimalExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyDecimalExplicitCastFromBigInteger(Decimal value, BigInteger bigInteger)
        {
            Assert.Equal(value, (Decimal)bigInteger);
        }

        public static byte[] ByteArrayMakeMinSize(Byte[] input, int minSize)
        {
            if (input.Length >= minSize)
            {
                return input;
            }

            Byte[] output = new byte[minSize];
            Byte filler = 0;

            if ((input[input.Length - 1] & 0x80) != 0)
            {
                filler = 0xff;
            }

            for (int i = 0; i < output.Length; i++)
            {
                output[i] = (i < input.Length) ? input[i] : filler;
            }

            return output;
        }
    }
}
