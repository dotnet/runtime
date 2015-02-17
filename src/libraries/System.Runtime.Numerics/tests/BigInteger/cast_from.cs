// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            Assert.True(VerifyException<OverflowException>(() => VerifyByteExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Byte Explicit Cast from BigInteger: Byte.MinValue - 1
            bigInteger = new BigInteger(Byte.MinValue);
            bigInteger -= BigInteger.One;
            value = bigInteger.ToByteArray()[0];
            Assert.True(VerifyException<OverflowException>(() => VerifyByteExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Byte Explicit Cast from BigInteger: Byte.MinValue
            Assert.True(VerifyByteExplicitCastFromBigInteger(Byte.MinValue), " Verification Failed");

            // Byte Explicit Cast from BigInteger: 0
            Assert.True(VerifyByteExplicitCastFromBigInteger(0), " Verification Failed");

            // Byte Explicit Cast from BigInteger: 1
            Assert.True(VerifyByteExplicitCastFromBigInteger(1), " Verification Failed");

            // Byte Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyByteExplicitCastFromBigInteger((Byte)s_random.Next(1, Byte.MaxValue)), " Verification Failed");
            }

            // Byte Explicit Cast from BigInteger: Byte.MaxValue + 1
            bigInteger = new BigInteger(Byte.MaxValue);
            bigInteger += BigInteger.One;
            value = bigInteger.ToByteArray()[0];
            Assert.True(VerifyException<OverflowException>(() => VerifyByteExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Byte Explicit Cast from BigInteger: Random value > Byte.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(Byte.MaxValue, s_random);
            value = bigInteger.ToByteArray()[0];
            Assert.True(VerifyException<OverflowException>(() => VerifyByteExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");
        }

        [Fact]
        public static void RunSByteExplicitCastFromBigIntegerTests()
        {
            SByte value = 0;
            BigInteger bigInteger;

            // SByte Explicit Cast from BigInteger: Random value < SByte.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(SByte.MinValue, s_random);
            value = (SByte)bigInteger.ToByteArray()[0];
            Assert.True(VerifyException<OverflowException>(() => VerifySByteExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // SByte Explicit Cast from BigInteger: SByte.MinValue - 1
            bigInteger = new BigInteger(SByte.MinValue);
            bigInteger -= BigInteger.One;
            value = (SByte)bigInteger.ToByteArray()[0];
            Assert.True(VerifyException<OverflowException>(() => VerifySByteExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // SByte Explicit Cast from BigInteger: SByte.MinValue
            VerifySByteExplicitCastFromBigInteger(SByte.MinValue);

            // SByte Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifySByteExplicitCastFromBigInteger((SByte)s_random.Next(SByte.MinValue, 0)), " Verification Failed");
            }

            // SByte Explicit Cast from BigInteger: -1
            Assert.True(VerifySByteExplicitCastFromBigInteger(-1), " Verification Failed");

            // SByte Explicit Cast from BigInteger: 0
            Assert.True(VerifySByteExplicitCastFromBigInteger(0), " Verification Failed");

            // SByte Explicit Cast from BigInteger: 1
            Assert.True(VerifySByteExplicitCastFromBigInteger(1), " Verification Failed");

            // SByte Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifySByteExplicitCastFromBigInteger((SByte)s_random.Next(1, SByte.MaxValue)), " Verification Failed");
            }

            // SByte Explicit Cast from BigInteger: SByte.MaxValue
            VerifySByteExplicitCastFromBigInteger(SByte.MaxValue);

            // SByte Explicit Cast from BigInteger: SByte.MaxValue + 1
            bigInteger = new BigInteger(SByte.MaxValue);
            bigInteger += BigInteger.One;
            value = (SByte)bigInteger.ToByteArray()[0];
            Assert.True(VerifyException<OverflowException>(() => VerifySByteExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // SByte Explicit Cast from BigInteger: Random value > SByte.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan((UInt64)SByte.MaxValue, s_random);
            value = (SByte)bigInteger.ToByteArray()[0];
            Assert.True(VerifyException<OverflowException>(() => VerifySByteExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");
        }

        [Fact]
        public static void RunUInt16ExplicitCastFromBigIntegerTests()
        {
            ushort value;
            BigInteger bigInteger;

            // UInt16 Explicit Cast from BigInteger: Random value < UInt16.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(UInt16.MinValue, s_random);
            value = BitConverter.ToUInt16(ByteArrayMakeMinSize(bigInteger.ToByteArray(), 2), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // UInt16 Explicit Cast from BigInteger: UInt16.MinValue - 1
            bigInteger = new BigInteger(UInt16.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToUInt16(new byte[] { 0xff, 0xff }, 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // UInt16 Explicit Cast from BigInteger: UInt16.MinValue
            Assert.True(VerifyUInt16ExplicitCastFromBigInteger(UInt16.MinValue), " Verification Failed");

            // UInt16 Explicit Cast from BigInteger: 0
            Assert.True(VerifyUInt16ExplicitCastFromBigInteger(0), " Verification Failed");

            // UInt16 Explicit Cast from BigInteger: 1
            Assert.True(VerifyUInt16ExplicitCastFromBigInteger(1), " Verification Failed");

            // UInt16 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyUInt16ExplicitCastFromBigInteger((UInt16)s_random.Next(1, UInt16.MaxValue)), " Verification Failed");
            }

            // UInt16 Explicit Cast from BigInteger: UInt16.MaxValue
            Assert.True(VerifyUInt16ExplicitCastFromBigInteger(UInt16.MaxValue), " Verification Failed");

            // UInt16 Explicit Cast from BigInteger: UInt16.MaxValue + 1
            bigInteger = new BigInteger(UInt16.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToUInt16(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // UInt16 Explicit Cast from BigInteger: Random value > UInt16.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(UInt16.MaxValue, s_random);
            value = BitConverter.ToUInt16(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");
        }

        [Fact]
        public static void RunInt16ExplicitCastFromBigIntegerTests()
        {
            short value;
            BigInteger bigInteger;

            // Int16 Explicit Cast from BigInteger: Random value < Int16.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(Int16.MinValue, s_random);
            value = BitConverter.ToInt16(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyInt16ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Int16 Explicit Cast from BigInteger: Int16.MinValue - 1
            bigInteger = new BigInteger(Int16.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToInt16(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyInt16ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Int16 Explicit Cast from BigInteger: Int16.MinValue
            Assert.True(VerifyInt16ExplicitCastFromBigInteger(Int16.MinValue), " Verification Failed");

            // Int16 Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyInt16ExplicitCastFromBigInteger((Int16)s_random.Next(Int16.MinValue, 0)), " Verification Failed");
            }

            // Int16 Explicit Cast from BigInteger: -1
            Assert.True(VerifyInt16ExplicitCastFromBigInteger(-1), " Verification Failed");

            // Int16 Explicit Cast from BigInteger: 0
            Assert.True(VerifyInt16ExplicitCastFromBigInteger(0), " Verification Failed");

            // Int16 Explicit Cast from BigInteger: 1
            Assert.True(VerifyInt16ExplicitCastFromBigInteger(1), " Verification Failed");

            // Int16 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyInt16ExplicitCastFromBigInteger((Int16)s_random.Next(1, Int16.MaxValue)), " Verification Failed");
            }

            // Int16 Explicit Cast from BigInteger: Int16.MaxValue
            Assert.True(VerifyInt16ExplicitCastFromBigInteger(Int16.MaxValue), " Verification Failed");

            // Int16 Explicit Cast from BigInteger: Int16.MaxValue + 1
            bigInteger = new BigInteger(Int16.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToInt16(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyInt16ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Int16 Explicit Cast from BigInteger: Random value > Int16.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan((UInt64)Int16.MaxValue, s_random);
            value = BitConverter.ToInt16(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyInt16ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");
        }

        [Fact]
        public static void RunUInt32ExplicitCastFromBigIntegerTests()
        {
            uint value;
            BigInteger bigInteger;

            // UInt32 Explicit Cast from BigInteger: Random value < UInt32.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(UInt32.MinValue, s_random);
            value = BitConverter.ToUInt32(ByteArrayMakeMinSize(bigInteger.ToByteArray(), 4), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // UInt32 Explicit Cast from BigInteger: UInt32.MinValue - 1
            bigInteger = new BigInteger(UInt32.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToUInt32(new byte[] { 0xff, 0xff, 0xff, 0xff }, 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // UInt32 Explicit Cast from BigInteger: UInt32.MinValue
            Assert.True(VerifyUInt32ExplicitCastFromBigInteger(UInt32.MinValue), " Verification Failed");

            // UInt32 Explicit Cast from BigInteger: 0
            Assert.True(VerifyUInt32ExplicitCastFromBigInteger(0), " Verification Failed");

            // UInt32 Explicit Cast from BigInteger: 1
            Assert.True(VerifyUInt32ExplicitCastFromBigInteger(1), " Verification Failed");

            // UInt32 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyUInt32ExplicitCastFromBigInteger((UInt32)(UInt32.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // UInt32 Explicit Cast from BigInteger: UInt32.MaxValue
            Assert.True(VerifyUInt32ExplicitCastFromBigInteger(UInt32.MaxValue), " Verification Failed");

            // UInt32 Explicit Cast from BigInteger: UInt32.MaxValue + 1
            bigInteger = new BigInteger(UInt32.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToUInt32(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // UInt32 Explicit Cast from BigInteger: Random value > UInt32.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(UInt32.MaxValue, s_random);
            value = BitConverter.ToUInt32(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");
        }

        [Fact]
        public static void RunInt32ExplicitCastFromBigIntegerTests()
        {
            int value;
            BigInteger bigInteger;

            // Int32 Explicit Cast from BigInteger: Random value < Int32.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(Int32.MinValue, s_random);
            value = BitConverter.ToInt32(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyInt32ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Int32 Explicit Cast from BigInteger: Int32.MinValue - 1
            bigInteger = new BigInteger(Int32.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToInt32(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyInt32ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Int32 Explicit Cast from BigInteger: Int32.MinValue
            Assert.True(VerifyInt32ExplicitCastFromBigInteger(Int32.MinValue), " Verification Failed");

            // Int32 Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyInt32ExplicitCastFromBigInteger((Int32)s_random.Next(Int32.MinValue, 0)), " Verification Failed");
            }

            // Int32 Explicit Cast from BigInteger: -1
            Assert.True(VerifyInt32ExplicitCastFromBigInteger(-1), " Verification Failed");

            // Int32 Explicit Cast from BigInteger: 0
            Assert.True(VerifyInt32ExplicitCastFromBigInteger(0), " Verification Failed");

            // Int32 Explicit Cast from BigInteger: 1
            Assert.True(VerifyInt32ExplicitCastFromBigInteger(1), " Verification Failed");

            // Int32 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyInt32ExplicitCastFromBigInteger((Int32)s_random.Next(1, Int32.MaxValue)), " Verification Failed");
            }

            // Int32 Explicit Cast from BigInteger: Int32.MaxValue
            Assert.True(VerifyInt32ExplicitCastFromBigInteger(Int32.MaxValue), " Verification Failed");

            // Int32 Explicit Cast from BigInteger: Int32.MaxValue + 1
            bigInteger = new BigInteger(Int32.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToInt32(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyInt32ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Int32 Explicit Cast from BigInteger: Random value > Int32.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(Int32.MaxValue, s_random);
            value = BitConverter.ToInt32(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyInt32ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");
        }

        [Fact]
        public static void RunUInt64ExplicitCastFromBigIntegerTests()
        {
            ulong value;
            BigInteger bigInteger;

            // UInt64 Explicit Cast from BigInteger: Random value < UInt64.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(0, s_random);
            value = BitConverter.ToUInt64(ByteArrayMakeMinSize(bigInteger.ToByteArray(), 8), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // UInt64 Explicit Cast from BigInteger: UInt64.MinValue - 1
            bigInteger = new BigInteger(UInt64.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToUInt64(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // UInt64 Explicit Cast from BigInteger: UInt64.MinValue
            Assert.True(VerifyUInt64ExplicitCastFromBigInteger(UInt64.MinValue), " Verification Failed");

            // UInt64 Explicit Cast from BigInteger: 0
            Assert.True(VerifyUInt64ExplicitCastFromBigInteger(0), " Verification Failed");

            // UInt64 Explicit Cast from BigInteger: 1
            Assert.True(VerifyUInt64ExplicitCastFromBigInteger(1), " Verification Failed");

            // UInt64 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyUInt64ExplicitCastFromBigInteger((UInt64)(UInt64.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // UInt64 Explicit Cast from BigInteger: UInt64.MaxValue
            Assert.True(VerifyUInt64ExplicitCastFromBigInteger(UInt64.MaxValue), " Verification Failed");

            // UInt64 Explicit Cast from BigInteger: UInt64.MaxValue + 1
            bigInteger = new BigInteger(UInt64.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToUInt64(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // UInt64 Explicit Cast from BigInteger: Random value > UInt64.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(UInt64.MaxValue, s_random);
            value = BitConverter.ToUInt64(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");
        }

        [Fact]
        public static void RunInt64ExplicitCastFromBigIntegerTests()
        {
            long value;
            BigInteger bigInteger;

            // Int64 Explicit Cast from BigInteger: Random value < Int64.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(Int64.MinValue, s_random);
            value = BitConverter.ToInt64(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyInt64ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Int64 Explicit Cast from BigInteger: Int64.MinValue - 1
            bigInteger = new BigInteger(Int64.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToInt64(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyInt64ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Int64 Explicit Cast from BigInteger: Int64.MinValue
            Assert.True(VerifyInt64ExplicitCastFromBigInteger(Int64.MinValue), " Verification Failed");

            // Int64 Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyInt64ExplicitCastFromBigInteger(((Int64)(Int64.MaxValue * s_random.NextDouble())) - Int64.MaxValue), " Verification Failed");
            }

            // Int64 Explicit Cast from BigInteger: -1
            Assert.True(VerifyInt64ExplicitCastFromBigInteger(-1), " Verification Failed");

            // Int64 Explicit Cast from BigInteger: 0
            Assert.True(VerifyInt64ExplicitCastFromBigInteger(0), " Verification Failed");

            // Int64 Explicit Cast from BigInteger: 1
            Assert.True(VerifyInt64ExplicitCastFromBigInteger(1), " Verification Failed");

            // Int64 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyInt64ExplicitCastFromBigInteger((Int64)(Int64.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // Int64 Explicit Cast from BigInteger: Int64.MaxValue
            Assert.True(VerifyInt64ExplicitCastFromBigInteger(Int64.MaxValue), " Verification Failed");

            // Int64 Explicit Cast from BigInteger: Int64.MaxValue + 1
            bigInteger = new BigInteger(Int64.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToInt64(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyInt64ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Int64 Explicit Cast from BigInteger: Random value > Int64.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(Int64.MaxValue, s_random);
            value = BitConverter.ToInt64(bigInteger.ToByteArray(), 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyInt64ExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");
        }

        [Fact]
        public static void RunSingleExplicitCastFromBigIntegerTests()
        {
            BigInteger bigInteger;

            // Single Explicit Cast from BigInteger: Random value < Single.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(Single.MinValue * 2.0, s_random);
            Assert.True(VerifySingleExplicitCastFromBigInteger(Single.NegativeInfinity, bigInteger), " Verification Failed");

            // Single Explicit Cast from BigInteger: Single.MinValue - 1
            bigInteger = new BigInteger(Single.MinValue);
            bigInteger -= BigInteger.One;
            Assert.True(VerifySingleExplicitCastFromBigInteger(Single.MinValue, bigInteger), " Verification Failed");

            // Single Explicit Cast from BigInteger: Single.MinValue
            Assert.True(VerifySingleExplicitCastFromBigInteger(Single.MinValue), " Verification Failed");

            // Single Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifySingleExplicitCastFromBigInteger(((Single)(Single.MaxValue * s_random.NextDouble())) - Single.MaxValue), " Verification Failed");
            }

            // Single Explicit Cast from BigInteger: Random Negative Non-integral > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifySingleExplicitCastFromBigInteger(((Single)(100 * s_random.NextDouble())) - 100), " Verification Failed");
            }

            // Single Explicit Cast from BigInteger: -1
            Assert.True(VerifySingleExplicitCastFromBigInteger(-1), " Verification Failed");

            // Single Explicit Cast from BigInteger: 0
            Assert.True(VerifySingleExplicitCastFromBigInteger(0), " Verification Failed");

            // Single Explicit Cast from BigInteger: 1
            Assert.True(VerifySingleExplicitCastFromBigInteger(1), " Verification Failed");

            // Single Explicit Cast from BigInteger: Random Positive Non-integral < 100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifySingleExplicitCastFromBigInteger((Single)(100 * s_random.NextDouble())), " Verification Failed");
            }

            // Single Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifySingleExplicitCastFromBigInteger((Single)(Single.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // Single Explicit Cast from BigInteger: Single.MaxValue + 1
            bigInteger = new BigInteger(Single.MaxValue);
            bigInteger += BigInteger.One;
            Assert.True(VerifySingleExplicitCastFromBigInteger(Single.MaxValue, bigInteger), " Verification Failed");

            // Single Explicit Cast from BigInteger: Random value > Single.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(Single.MaxValue * 2, s_random);
            Assert.True(VerifySingleExplicitCastFromBigInteger(Single.PositiveInfinity, bigInteger), " Verification Failed");

            // Single Explicit Cast from BigInteger: value < Single.MaxValue but can not be accurately represented in a Single
            bigInteger = new BigInteger(16777217);
            Assert.True(VerifySingleExplicitCastFromBigInteger(16777216f, bigInteger), " Verification Failed");

            // Single Explicit Cast from BigInteger: Single.MinValue < value but can not be accurately represented in a Single
            bigInteger = new BigInteger(-16777217);
            Assert.True(VerifySingleExplicitCastFromBigInteger(-16777216f, bigInteger), " Verification Failed");
        }

        [Fact]
        public static void RunDoubleExplicitCastFromBigIntegerTests()
        {
            BigInteger bigInteger;

            // Double Explicit Cast from BigInteger: Random value < Double.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(Double.MinValue, s_random);
            bigInteger *= 2;
            Assert.True(VerifyDoubleExplicitCastFromBigInteger(Double.NegativeInfinity, bigInteger), " Verification Failed");

            // Double Explicit Cast from BigInteger: Double.MinValue - 1
            bigInteger = new BigInteger(Double.MinValue);
            bigInteger -= BigInteger.One;
            Assert.True(VerifyDoubleExplicitCastFromBigInteger(Double.MinValue, bigInteger), " Verification Failed");

            // Double Explicit Cast from BigInteger: Double.MinValue
            Assert.True(VerifyDoubleExplicitCastFromBigInteger(Double.MinValue), " Verification Failed");

            // Double Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyDoubleExplicitCastFromBigInteger(((Double)(Double.MaxValue * s_random.NextDouble())) - Double.MaxValue), " Verification Failed");
            }

            // Double Explicit Cast from BigInteger: Random Negative Non-integral > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyDoubleExplicitCastFromBigInteger(((Double)(100 * s_random.NextDouble())) - 100), " Verification Failed");
            }

            // Double Explicit Cast from BigInteger: -1
            Assert.True(VerifyDoubleExplicitCastFromBigInteger(-1), " Verification Failed");

            // Double Explicit Cast from BigInteger: 0
            Assert.True(VerifyDoubleExplicitCastFromBigInteger(0), " Verification Failed");

            // Double Explicit Cast from BigInteger: 1
            Assert.True(VerifyDoubleExplicitCastFromBigInteger(1), " Verification Failed");

            // Double Explicit Cast from BigInteger: Random Positive Non-integral < 100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyDoubleExplicitCastFromBigInteger((Double)(100 * s_random.NextDouble())), " Verification Failed");
            }

            // Double Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyDoubleExplicitCastFromBigInteger((Double)(Double.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // Double Explicit Cast from BigInteger: Double.MaxValue
            Assert.True(VerifyDoubleExplicitCastFromBigInteger(Double.MaxValue), " Verification Failed");

            // Double Explicit Cast from BigInteger: Double.MaxValue + 1
            bigInteger = new BigInteger(Double.MaxValue);
            bigInteger += BigInteger.One;
            Assert.True(VerifyDoubleExplicitCastFromBigInteger(Double.MaxValue, bigInteger), " Verification Failed");

            // Double Explicit Cast from BigInteger: Random value > Double.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(Double.MaxValue, s_random);
            bigInteger *= 2;
            Assert.True(VerifyDoubleExplicitCastFromBigInteger(Double.PositiveInfinity, bigInteger), " Verification Failed");

            // Double Explicit Cast from BigInteger: value < Double.MaxValue but can not be accurately represented in a Double
            bigInteger = new BigInteger(9007199254740993);
            Assert.True(VerifyDoubleExplicitCastFromBigInteger(9007199254740992, bigInteger), " Verification Failed");

            // Double Explicit Cast from BigInteger: Double.MinValue < value but can not be accurately represented in a Double
            bigInteger = new BigInteger(-9007199254740993);
            Assert.True(VerifyDoubleExplicitCastFromBigInteger(-9007199254740992, bigInteger), " Verification Failed");
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
                Assert.True(VerifyException<OverflowException>(() => VerifyDecimalExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");
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
            Assert.True(VerifyException<OverflowException>(() => VerifyDecimalExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

            // Decimal Explicit Cast from BigInteger: Decimal.MinValue
            Assert.True(VerifyDecimalExplicitCastFromBigInteger(Decimal.MinValue), " Verification Failed");

            // Decimal Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyDecimalExplicitCastFromBigInteger(((Decimal)((Double)Decimal.MaxValue * s_random.NextDouble())) - Decimal.MaxValue), " Verification Failed");
            }

            // Decimal Explicit Cast from BigInteger: Random Negative Non-Integral > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                value = (Decimal)(100 * s_random.NextDouble() - 100);
                Assert.True(VerifyDecimalExplicitCastFromBigInteger(Decimal.Truncate(value), new BigInteger(value)), " Verification Failed");
            }

            // Decimal Explicit Cast from BigInteger: -1
            Assert.True(VerifyDecimalExplicitCastFromBigInteger(-1), " Verification Failed");

            // Decimal Explicit Cast from BigInteger: 0
            Assert.True(VerifyDecimalExplicitCastFromBigInteger(0), " Verification Failed");

            // Decimal Explicit Cast from BigInteger: 1
            Assert.True(VerifyDecimalExplicitCastFromBigInteger(1), " Verification Failed");

            // Decimal Explicit Cast from BigInteger: Random Positive Non-Integral < 100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                value = (Decimal)(100 * s_random.NextDouble());
                Assert.True(VerifyDecimalExplicitCastFromBigInteger(Decimal.Truncate(value), new BigInteger(value)), " Verification Failed");
            }

            // Decimal Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                Assert.True(VerifyDecimalExplicitCastFromBigInteger((Decimal)((Double)Decimal.MaxValue * s_random.NextDouble())), " Verification Failed");
            }

            // Decimal Explicit Cast from BigInteger: Decimal.MaxValue
            Assert.True(VerifyDecimalExplicitCastFromBigInteger(Decimal.MaxValue), " Verification Failed");

            // Decimal Explicit Cast from BigInteger: Decimal.MaxValue + 1
            bigInteger = new BigInteger(Decimal.MaxValue);
            bigInteger += BigInteger.One;
            temp = bigInteger.ToByteArray();
            for (int j = 0; j < 3; j++)
            {
                bits[j] = BitConverter.ToInt32(temp, 4 * j);
            }
            value = new Decimal(bits[0], bits[1], bits[2], false, 0);
            Assert.True(VerifyException<OverflowException>(() => VerifyDecimalExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");

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
                Assert.True(VerifyException<OverflowException>(() => VerifyDecimalExplicitCastFromBigInteger(value, bigInteger)), " Verification Failed");
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

        private static bool VerifyByteExplicitCastFromBigInteger(Byte value)
        {
            BigInteger bigInteger = new BigInteger(value);

            return Eval(value, (Byte)bigInteger, "Value after cast");
        }
        private static bool VerifyByteExplicitCastFromBigInteger(Byte value, BigInteger bigInteger)
        {
            return Eval(value, (Byte)bigInteger, "Value after cast");
        }

        private static bool VerifySByteExplicitCastFromBigInteger(SByte value)
        {
            BigInteger bigInteger = new BigInteger(value);

            return Eval(value, (SByte)bigInteger, "Value after cast");
        }
        private static bool VerifySByteExplicitCastFromBigInteger(SByte value, BigInteger bigInteger)
        {
            return Eval(value, (SByte)bigInteger, "Value after cast");
        }

        private static bool VerifyUInt16ExplicitCastFromBigInteger(UInt16 value)
        {
            BigInteger bigInteger = new BigInteger(value);

            return Eval(value, (UInt16)bigInteger, "Value after cast");
        }
        private static bool VerifyUInt16ExplicitCastFromBigInteger(UInt16 value, BigInteger bigInteger)
        {
            return Eval(value, (UInt16)bigInteger, "Value after cast");
        }

        private static bool VerifyInt16ExplicitCastFromBigInteger(Int16 value)
        {
            BigInteger bigInteger = new BigInteger(value);

            return Eval(value, (Int16)bigInteger, "Value after cast");
        }
        private static bool VerifyInt16ExplicitCastFromBigInteger(Int16 value, BigInteger bigInteger)
        {
            return Eval(value, (Int16)bigInteger, "Value after cast");
        }

        private static bool VerifyUInt32ExplicitCastFromBigInteger(UInt32 value)
        {
            BigInteger bigInteger = new BigInteger(value);

            return Eval(value, (UInt32)bigInteger, "Value after cast");
        }
        private static bool VerifyUInt32ExplicitCastFromBigInteger(UInt32 value, BigInteger bigInteger)
        {
            return Eval(value, (UInt32)bigInteger, "Value after cast");
        }

        private static bool VerifyInt32ExplicitCastFromBigInteger(Int32 value)
        {
            BigInteger bigInteger = new BigInteger(value);

            return Eval(value, (Int32)bigInteger, "Value after cast");
        }
        private static bool VerifyInt32ExplicitCastFromBigInteger(Int32 value, BigInteger bigInteger)
        {
            return Eval(value, (Int32)bigInteger, "Value after cast");
        }

        private static bool VerifyUInt64ExplicitCastFromBigInteger(UInt64 value)
        {
            BigInteger bigInteger = new BigInteger(value);

            return Eval(value, (UInt64)bigInteger, "Value after cast");
        }
        private static bool VerifyUInt64ExplicitCastFromBigInteger(UInt64 value, BigInteger bigInteger)
        {
            return Eval(value, (UInt64)bigInteger, "Value after cast");
        }

        private static bool VerifyInt64ExplicitCastFromBigInteger(Int64 value)
        {
            BigInteger bigInteger = new BigInteger(value);

            return Eval(value, (Int64)bigInteger, "Value after cast");
        }
        private static bool VerifyInt64ExplicitCastFromBigInteger(Int64 value, BigInteger bigInteger)
        {
            return Eval(value, (Int64)bigInteger, "Value after cast");
        }

        private static bool VerifySingleExplicitCastFromBigInteger(Single value)
        {
            BigInteger bigInteger = new BigInteger(value);

            return VerifySingleExplicitCastFromBigInteger(value, bigInteger);
        }
        private static bool VerifySingleExplicitCastFromBigInteger(Single value, BigInteger bigInteger)
        {
            return Eval((Single)Math.Truncate(value), (Single)bigInteger, "Value after cast");
        }

        private static bool VerifyDoubleExplicitCastFromBigInteger(Double value)
        {
            BigInteger bigInteger = new BigInteger(value);

            return VerifyDoubleExplicitCastFromBigInteger(value, bigInteger);
        }
        private static bool VerifyDoubleExplicitCastFromBigInteger(Double value, BigInteger bigInteger)
        {
            return Eval(Math.Truncate(value), (Double)bigInteger, "Value after cast");
        }

        private static bool VerifyDecimalExplicitCastFromBigInteger(Decimal value)
        {
            BigInteger bigInteger = new BigInteger(value);

            return VerifyDecimalExplicitCastFromBigInteger(value, bigInteger);
        }
        private static bool VerifyDecimalExplicitCastFromBigInteger(Decimal value, BigInteger bigInteger)
        {
            return Eval(value, (Decimal)bigInteger, "Value after cast");
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

        public static byte[] ByteArrayMakeMinSize(Byte[] input, int minSize)
        {
            if (input.Length >= minSize)
                return input;

            Byte[] output = new byte[minSize];
            Byte filler = 0;

            if ((input[input.Length - 1] & 0x80) != 0)
            {
                filler = 0xff;
            }

            for (int i = 0; i < output.Length; i++)
            {
                if (i < input.Length)
                    output[i] = input[i];
                if (i >= input.Length)
                    output[i] = filler;
            }

            return output;
        }
    }
}
