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
            byte value = 0;
            BigInteger bigInteger;

            // Byte Explicit Cast from BigInteger: Random value < Byte.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(byte.MinValue, s_random);
            value = bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifyByteExplicitCastFromBigInteger(value, bigInteger));

            // Byte Explicit Cast from BigInteger: Byte.MinValue - 1
            bigInteger = new BigInteger(byte.MinValue);
            bigInteger -= BigInteger.One;
            value = bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifyByteExplicitCastFromBigInteger(value, bigInteger));

            // Byte Explicit Cast from BigInteger: Byte.MinValue
            VerifyByteExplicitCastFromBigInteger(byte.MinValue);

            // Byte Explicit Cast from BigInteger: 0
            VerifyByteExplicitCastFromBigInteger(0);

            // Byte Explicit Cast from BigInteger: 1
            VerifyByteExplicitCastFromBigInteger(1);

            // Byte Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyByteExplicitCastFromBigInteger((byte)s_random.Next(1, byte.MaxValue));
            }

            // Byte Explicit Cast from BigInteger: Byte.MaxValue + 1
            bigInteger = new BigInteger(byte.MaxValue);
            bigInteger += BigInteger.One;
            value = bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifyByteExplicitCastFromBigInteger(value, bigInteger));

            // Byte Explicit Cast from BigInteger: Random value > Byte.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(byte.MaxValue, s_random);
            value = bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifyByteExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunSByteExplicitCastFromBigIntegerTests()
        {
            sbyte value = 0;
            BigInteger bigInteger;

            // SByte Explicit Cast from BigInteger: Random value < SByte.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(sbyte.MinValue, s_random);
            value = unchecked((sbyte)bigInteger.ToByteArray()[0]);
            Assert.Throws<OverflowException>(() => VerifySByteExplicitCastFromBigInteger(value, bigInteger));

            // SByte Explicit Cast from BigInteger: SByte.MinValue - 1
            bigInteger = new BigInteger(sbyte.MinValue);
            bigInteger -= BigInteger.One;
            value = (sbyte)bigInteger.ToByteArray()[0];
            Assert.Throws<OverflowException>(() => VerifySByteExplicitCastFromBigInteger(value, bigInteger));

            // SByte Explicit Cast from BigInteger: SByte.MinValue
            VerifySByteExplicitCastFromBigInteger(sbyte.MinValue);

            // SByte Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySByteExplicitCastFromBigInteger((sbyte)s_random.Next(sbyte.MinValue, 0));
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
                VerifySByteExplicitCastFromBigInteger((sbyte)s_random.Next(1, sbyte.MaxValue));
            }

            // SByte Explicit Cast from BigInteger: SByte.MaxValue
            VerifySByteExplicitCastFromBigInteger(sbyte.MaxValue);

            // SByte Explicit Cast from BigInteger: SByte.MaxValue + 1
            bigInteger = new BigInteger(sbyte.MaxValue);
            bigInteger += BigInteger.One;
            value = unchecked((sbyte)bigInteger.ToByteArray()[0]);
            Assert.Throws<OverflowException>(() => VerifySByteExplicitCastFromBigInteger(value, bigInteger));

            // SByte Explicit Cast from BigInteger: Random value > SByte.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan((ulong)sbyte.MaxValue, s_random);
            value = unchecked((sbyte)bigInteger.ToByteArray()[0]);
            Assert.Throws<OverflowException>(() => VerifySByteExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunUInt16ExplicitCastFromBigIntegerTests()
        {
            ushort value;
            BigInteger bigInteger;

            // UInt16 Explicit Cast from BigInteger: Random value < UInt16.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(ushort.MinValue, s_random);
            value = BitConverter.ToUInt16(ByteArrayMakeMinSize(bigInteger.ToByteArray(), 2), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger));

            // UInt16 Explicit Cast from BigInteger: UInt16.MinValue - 1
            bigInteger = new BigInteger(ushort.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToUInt16(new byte[] { 0xff, 0xff }, 0);
            Assert.Throws<OverflowException>(() => VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger));

            // UInt16 Explicit Cast from BigInteger: UInt16.MinValue
            VerifyUInt16ExplicitCastFromBigInteger(ushort.MinValue);

            // UInt16 Explicit Cast from BigInteger: 0
            VerifyUInt16ExplicitCastFromBigInteger(0);

            // UInt16 Explicit Cast from BigInteger: 1
            VerifyUInt16ExplicitCastFromBigInteger(1);

            // UInt16 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyUInt16ExplicitCastFromBigInteger((ushort)s_random.Next(1, ushort.MaxValue));
            }

            // UInt16 Explicit Cast from BigInteger: UInt16.MaxValue
            VerifyUInt16ExplicitCastFromBigInteger(ushort.MaxValue);

            // UInt16 Explicit Cast from BigInteger: UInt16.MaxValue + 1
            bigInteger = new BigInteger(ushort.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToUInt16(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger));

            // UInt16 Explicit Cast from BigInteger: Random value > UInt16.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(ushort.MaxValue, s_random);
            value = BitConverter.ToUInt16(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunInt16ExplicitCastFromBigIntegerTests()
        {
            short value;
            BigInteger bigInteger;

            // Int16 Explicit Cast from BigInteger: Random value < Int16.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(short.MinValue, s_random);
            value = BitConverter.ToInt16(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt16ExplicitCastFromBigInteger(value, bigInteger));

            // Int16 Explicit Cast from BigInteger: Int16.MinValue - 1
            bigInteger = new BigInteger(short.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToInt16(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt16ExplicitCastFromBigInteger(value, bigInteger));

            // Int16 Explicit Cast from BigInteger: Int16.MinValue
            VerifyInt16ExplicitCastFromBigInteger(short.MinValue);

            // Int16 Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt16ExplicitCastFromBigInteger((short)s_random.Next(short.MinValue, 0));
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
                VerifyInt16ExplicitCastFromBigInteger((short)s_random.Next(1, short.MaxValue));
            }

            // Int16 Explicit Cast from BigInteger: Int16.MaxValue
            VerifyInt16ExplicitCastFromBigInteger(short.MaxValue);

            // Int16 Explicit Cast from BigInteger: Int16.MaxValue + 1
            bigInteger = new BigInteger(short.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToInt16(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt16ExplicitCastFromBigInteger(value, bigInteger));

            // Int16 Explicit Cast from BigInteger: Random value > Int16.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan((ulong)short.MaxValue, s_random);
            value = BitConverter.ToInt16(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt16ExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunUInt32ExplicitCastFromBigIntegerTests()
        {
            uint value;
            BigInteger bigInteger;

            // UInt32 Explicit Cast from BigInteger: Random value < UInt32.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(uint.MinValue, s_random);
            value = BitConverter.ToUInt32(ByteArrayMakeMinSize(bigInteger.ToByteArray(), 4), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger));

            // UInt32 Explicit Cast from BigInteger: UInt32.MinValue - 1
            bigInteger = new BigInteger(uint.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToUInt32(new byte[] { 0xff, 0xff, 0xff, 0xff }, 0);
            Assert.Throws<OverflowException>(() => VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger));

            // UInt32 Explicit Cast from BigInteger: UInt32.MinValue
            VerifyUInt32ExplicitCastFromBigInteger(uint.MinValue);

            // UInt32 Explicit Cast from BigInteger: 0
            VerifyUInt32ExplicitCastFromBigInteger(0);

            // UInt32 Explicit Cast from BigInteger: 1
            VerifyUInt32ExplicitCastFromBigInteger(1);

            // UInt32 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyUInt32ExplicitCastFromBigInteger((uint)(uint.MaxValue * s_random.NextDouble()));
            }

            // UInt32 Explicit Cast from BigInteger: UInt32.MaxValue
            VerifyUInt32ExplicitCastFromBigInteger(uint.MaxValue);

            // UInt32 Explicit Cast from BigInteger: UInt32.MaxValue + 1
            bigInteger = new BigInteger(uint.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToUInt32(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger));

            // UInt32 Explicit Cast from BigInteger: Random value > UInt32.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(uint.MaxValue, s_random);
            value = BitConverter.ToUInt32(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunInt32ExplicitCastFromBigIntegerTests()
        {
            int value;
            BigInteger bigInteger;

            // Int32 Explicit Cast from BigInteger: Random value < Int32.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(int.MinValue, s_random);
            value = BitConverter.ToInt32(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt32ExplicitCastFromBigInteger(value, bigInteger));

            // Int32 Explicit Cast from BigInteger: Int32.MinValue - 1
            bigInteger = new BigInteger(int.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToInt32(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt32ExplicitCastFromBigInteger(value, bigInteger));

            // Int32 Explicit Cast from BigInteger: Int32.MinValue
            VerifyInt32ExplicitCastFromBigInteger(int.MinValue);

            // Int32 Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt32ExplicitCastFromBigInteger((int)s_random.Next(int.MinValue, 0));
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
                VerifyInt32ExplicitCastFromBigInteger((int)s_random.Next(1, int.MaxValue));
            }

            // Int32 Explicit Cast from BigInteger: Int32.MaxValue
            VerifyInt32ExplicitCastFromBigInteger(int.MaxValue);

            // Int32 Explicit Cast from BigInteger: Int32.MaxValue + 1
            bigInteger = new BigInteger(int.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToInt32(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt32ExplicitCastFromBigInteger(value, bigInteger));

            // Int32 Explicit Cast from BigInteger: Random value > Int32.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(int.MaxValue, s_random);
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
            bigInteger = new BigInteger(ulong.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToUInt64(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, 0);
            Assert.Throws<OverflowException>(() => VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger));

            // UInt64 Explicit Cast from BigInteger: UInt64.MinValue
            VerifyUInt64ExplicitCastFromBigInteger(ulong.MinValue);

            // UInt64 Explicit Cast from BigInteger: 0
            VerifyUInt64ExplicitCastFromBigInteger(0);

            // UInt64 Explicit Cast from BigInteger: 1
            VerifyUInt64ExplicitCastFromBigInteger(1);

            // UInt64 Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyUInt64ExplicitCastFromBigInteger((ulong)(ulong.MaxValue * s_random.NextDouble()));
            }

            // UInt64 Explicit Cast from BigInteger: UInt64.MaxValue
            VerifyUInt64ExplicitCastFromBigInteger(ulong.MaxValue);

            // UInt64 Explicit Cast from BigInteger: UInt64.MaxValue + 1
            bigInteger = new BigInteger(ulong.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToUInt64(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger));

            // UInt64 Explicit Cast from BigInteger: Random value > UInt64.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(ulong.MaxValue, s_random);
            value = BitConverter.ToUInt64(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunInt64ExplicitCastFromBigIntegerTests()
        {
            long value;
            BigInteger bigInteger;

            // Int64 Explicit Cast from BigInteger: Random value < Int64.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(long.MinValue, s_random);
            value = BitConverter.ToInt64(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt64ExplicitCastFromBigInteger(value, bigInteger));

            // Int64 Explicit Cast from BigInteger: Int64.MinValue - 1
            bigInteger = new BigInteger(long.MinValue);
            bigInteger -= BigInteger.One;
            value = BitConverter.ToInt64(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt64ExplicitCastFromBigInteger(value, bigInteger));

            // Int64 Explicit Cast from BigInteger: Int64.MinValue
            VerifyInt64ExplicitCastFromBigInteger(long.MinValue);

            // Int64 Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyInt64ExplicitCastFromBigInteger(((long)(long.MaxValue * s_random.NextDouble())) - long.MaxValue);
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
                VerifyInt64ExplicitCastFromBigInteger((long)(long.MaxValue * s_random.NextDouble()));
            }

            // Int64 Explicit Cast from BigInteger: Int64.MaxValue
            VerifyInt64ExplicitCastFromBigInteger(long.MaxValue);

            // Int64 Explicit Cast from BigInteger: Int64.MaxValue + 1
            bigInteger = new BigInteger(long.MaxValue);
            bigInteger += BigInteger.One;
            value = BitConverter.ToInt64(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt64ExplicitCastFromBigInteger(value, bigInteger));

            // Int64 Explicit Cast from BigInteger: Random value > Int64.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(long.MaxValue, s_random);
            value = BitConverter.ToInt64(bigInteger.ToByteArray(), 0);
            Assert.Throws<OverflowException>(() => VerifyInt64ExplicitCastFromBigInteger(value, bigInteger));
        }

        [Fact]
        public static void RunSingleExplicitCastFromBigIntegerTests()
        {
            BigInteger bigInteger;

            // Single Explicit Cast from BigInteger: Random value < Single.MinValue
            bigInteger = GenerateRandomBigIntegerLessThan(float.MinValue * 2.0, s_random);
            VerifySingleExplicitCastFromBigInteger(float.NegativeInfinity, bigInteger);

            // Single Explicit Cast from BigInteger: Single.MinValue - 1
            bigInteger = new BigInteger(float.MinValue);
            bigInteger -= BigInteger.One;
            VerifySingleExplicitCastFromBigInteger(float.MinValue, bigInteger);

            // Single Explicit Cast from BigInteger: Single.MinValue
            VerifySingleExplicitCastFromBigInteger(float.MinValue);

            // Single Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastFromBigInteger(((float)(float.MaxValue * s_random.NextDouble())) - float.MaxValue);
            }

            // Single Explicit Cast from BigInteger: Random Negative Non-integral > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastFromBigInteger(((float)(100 * s_random.NextDouble())) - 100);
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
                VerifySingleExplicitCastFromBigInteger((float)(100 * s_random.NextDouble()));
            }

            // Single Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifySingleExplicitCastFromBigInteger((float)(float.MaxValue * s_random.NextDouble()));
            }

            // Single Explicit Cast from BigInteger: Single.MaxValue + 1
            bigInteger = new BigInteger(float.MaxValue);
            bigInteger += BigInteger.One;
            VerifySingleExplicitCastFromBigInteger(float.MaxValue, bigInteger);

            // Single Explicit Cast from BigInteger: Random value > Single.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan((double)float.MaxValue * 2, s_random);
            VerifySingleExplicitCastFromBigInteger(float.PositiveInfinity, bigInteger);

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
            bigInteger = GenerateRandomBigIntegerLessThan(double.MinValue, s_random);
            bigInteger *= 2;
            VerifyDoubleExplicitCastFromBigInteger(double.NegativeInfinity, bigInteger);

            // Double Explicit Cast from BigInteger: Double.MinValue - 1
            bigInteger = new BigInteger(double.MinValue);
            bigInteger -= BigInteger.One;
            VerifyDoubleExplicitCastFromBigInteger(double.MinValue, bigInteger);

            // Double Explicit Cast from BigInteger: Double.MinValue
            VerifyDoubleExplicitCastFromBigInteger(double.MinValue);

            // Double Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastFromBigInteger(((double)(double.MaxValue * s_random.NextDouble())) - double.MaxValue);
            }

            // Double Explicit Cast from BigInteger: Random Negative Non-integral > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastFromBigInteger(((double)(100 * s_random.NextDouble())) - 100);
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
                VerifyDoubleExplicitCastFromBigInteger((double)(100 * s_random.NextDouble()));
            }

            // Double Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDoubleExplicitCastFromBigInteger((double)(double.MaxValue * s_random.NextDouble()));
            }

            // Double Explicit Cast from BigInteger: Double.MaxValue
            VerifyDoubleExplicitCastFromBigInteger(double.MaxValue);

            // Double Explicit Cast from BigInteger: Double.MaxValue + 1
            bigInteger = new BigInteger(double.MaxValue);
            bigInteger += BigInteger.One;
            VerifyDoubleExplicitCastFromBigInteger(double.MaxValue, bigInteger);

            // Double Explicit Cast from BigInteger: Double.MinValue - 1
            bigInteger = new BigInteger(double.MinValue);
            bigInteger -= BigInteger.One;
            VerifyDoubleExplicitCastFromBigInteger(double.MinValue, bigInteger);

            // Double Explicit Cast from BigInteger: Random value > Double.MaxValue
            bigInteger = GenerateRandomBigIntegerGreaterThan(double.MaxValue, s_random);
            bigInteger *= 2;
            VerifyDoubleExplicitCastFromBigInteger(double.PositiveInfinity, bigInteger);

            // Double Explicit Cast from BigInteger: Random value < -Double.MaxValue
            VerifyDoubleExplicitCastFromBigInteger(double.NegativeInfinity, -bigInteger);

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
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
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
            decimal value;
            BigInteger bigInteger;

            // Decimal Explicit Cast from BigInteger: Random value < Decimal.MinValue
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                bigInteger = GenerateRandomBigIntegerLessThan(decimal.MinValue, s_random);
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
                    bits[j] = unchecked((int)temp2);
                }
                value = new decimal(bits[0], bits[1], bits[2], true, 0);
                Assert.Throws<OverflowException>(() => VerifyDecimalExplicitCastFromBigInteger(value, bigInteger));
            }

            // Decimal Explicit Cast from BigInteger: Decimal.MinValue - 1
            bigInteger = new BigInteger(decimal.MinValue);
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
                    temp2 = unchecked(temp2 + 1);
                    if (temp2 == 0)
                    {
                        carry = true;
                    }
                }
                bits[j] = (int)temp2;
            }
            value = new decimal(bits[0], bits[1], bits[2], true, 0);
            Assert.Throws<OverflowException>(() => VerifyDecimalExplicitCastFromBigInteger(value, bigInteger));

            // Decimal Explicit Cast from BigInteger: Decimal.MinValue
            VerifyDecimalExplicitCastFromBigInteger(decimal.MinValue);

            // Decimal Explicit Cast from BigInteger: Random Negative
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDecimalExplicitCastFromBigInteger(((decimal)((double)decimal.MaxValue * s_random.NextDouble())) - decimal.MaxValue);
            }

            // Decimal Explicit Cast from BigInteger: Random Negative Non-Integral > -100
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                value = (decimal)(100 * s_random.NextDouble() - 100);
                VerifyDecimalExplicitCastFromBigInteger(decimal.Truncate(value), new BigInteger(value));
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
                value = (decimal)(100 * s_random.NextDouble());
                VerifyDecimalExplicitCastFromBigInteger(decimal.Truncate(value), new BigInteger(value));
            }

            // Decimal Explicit Cast from BigInteger: Random Positive
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                VerifyDecimalExplicitCastFromBigInteger((decimal)((double)decimal.MaxValue * s_random.NextDouble()));
            }

            // Decimal Explicit Cast from BigInteger: Decimal.MaxValue
            VerifyDecimalExplicitCastFromBigInteger(decimal.MaxValue);

            // Decimal Explicit Cast from BigInteger: Decimal.MaxValue + 1
            bigInteger = new BigInteger(decimal.MaxValue);
            bigInteger += BigInteger.One;
            temp = bigInteger.ToByteArray();
            for (int j = 0; j < 3; j++)
            {
                bits[j] = BitConverter.ToInt32(temp, 4 * j);
            }
            value = new decimal(bits[0], bits[1], bits[2], false, 0);
            Assert.Throws<OverflowException>(() => VerifyDecimalExplicitCastFromBigInteger(value, bigInteger));

            // Decimal Explicit Cast from BigInteger: Random value > Decimal.MaxValue
            for (int i = 0; i < NumberOfRandomIterations; ++i)
            {
                bigInteger = GenerateRandomBigIntegerGreaterThan(decimal.MaxValue, s_random);
                temp = bigInteger.ToByteArray();
                for (int j = 0; j < 3; j++)
                {
                    bits[j] = BitConverter.ToInt32(temp, 4 * j);
                }
                value = new decimal(bits[0], bits[1], bits[2], false, 0);
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
                    temp = temp << (int.MaxValue / 10);
                    VerifyDoubleExplicitCastFromBigInteger(double.PositiveInfinity, temp);
                    VerifyDoubleExplicitCastFromBigInteger(double.NegativeInfinity, -temp);
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

        private static BigInteger GenerateRandomBigIntegerLessThan(long value, Random random)
        {
            return (GenerateRandomNegativeBigInteger(random) + value) - 1;
        }

        private static BigInteger GenerateRandomBigIntegerLessThan(double value, Random random)
        {
            return (GenerateRandomNegativeBigInteger(random) + (BigInteger)value) - 1;
        }

        private static BigInteger GenerateRandomBigIntegerLessThan(decimal value, Random random)
        {
            return (GenerateRandomNegativeBigInteger(random) + (BigInteger)value) - 1;
        }

        private static BigInteger GenerateRandomBigIntegerGreaterThan(ulong value, Random random)
        {
            return (GenerateRandomPositiveBigInteger(random) + value) + 1;
        }

        private static BigInteger GenerateRandomBigIntegerGreaterThan(double value, Random random)
        {
            return (GenerateRandomPositiveBigInteger(random) + (BigInteger)value) + 1;
        }

        private static BigInteger GenerateRandomBigIntegerGreaterThan(decimal value, Random random)
        {
            return (GenerateRandomPositiveBigInteger(random) + (BigInteger)value) + 1;
        }

        private static void VerifyByteExplicitCastFromBigInteger(byte value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyByteExplicitCastFromBigInteger(value, bigInteger);
        }

        private static void VerifyByteExplicitCastFromBigInteger(byte value, BigInteger bigInteger)
        {
            Assert.Equal(value, (byte)bigInteger);
        }

        private static void VerifySByteExplicitCastFromBigInteger(sbyte value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifySByteExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifySByteExplicitCastFromBigInteger(sbyte value, BigInteger bigInteger)
        {
            Assert.Equal(value, (sbyte)bigInteger);
        }

        private static void VerifyUInt16ExplicitCastFromBigInteger(ushort value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyUInt16ExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyUInt16ExplicitCastFromBigInteger(ushort value, BigInteger bigInteger)
        {
            Assert.Equal(value, (ushort)bigInteger);
        }

        private static void VerifyInt16ExplicitCastFromBigInteger(short value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyInt16ExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyInt16ExplicitCastFromBigInteger(short value, BigInteger bigInteger)
        {
            Assert.Equal(value, (short)bigInteger);
        }

        private static void VerifyUInt32ExplicitCastFromBigInteger(uint value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyUInt32ExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyUInt32ExplicitCastFromBigInteger(uint value, BigInteger bigInteger)
        {
            Assert.Equal(value, (uint)bigInteger);
        }

        private static void VerifyInt32ExplicitCastFromBigInteger(int value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyInt32ExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyInt32ExplicitCastFromBigInteger(int value, BigInteger bigInteger)
        {
            Assert.Equal(value, (int)bigInteger);
        }

        private static void VerifyUInt64ExplicitCastFromBigInteger(ulong value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyUInt64ExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyUInt64ExplicitCastFromBigInteger(ulong value, BigInteger bigInteger)
        {
            Assert.Equal(value, (ulong)bigInteger);
        }

        private static void VerifyInt64ExplicitCastFromBigInteger(long value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyInt64ExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyInt64ExplicitCastFromBigInteger(long value, BigInteger bigInteger)
        {
            Assert.Equal(value, (long)bigInteger);
        }

        private static void VerifySingleExplicitCastFromBigInteger(float value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifySingleExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifySingleExplicitCastFromBigInteger(float value, BigInteger bigInteger)
        {
            Assert.Equal((float)Math.Truncate(value), (float)bigInteger);
        }

        private static void VerifyDoubleExplicitCastFromBigInteger(double value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyDoubleExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyDoubleExplicitCastFromBigInteger(double value, BigInteger bigInteger)
        {
            Assert.Equal(Math.Truncate(value), (double)bigInteger);
        }

        private static void VerifyDecimalExplicitCastFromBigInteger(decimal value)
        {
            BigInteger bigInteger = new BigInteger(value);
            VerifyDecimalExplicitCastFromBigInteger(value, bigInteger);
        }
        private static void VerifyDecimalExplicitCastFromBigInteger(decimal value, BigInteger bigInteger)
        {
            Assert.Equal(value, (decimal)bigInteger);
        }

        public static byte[] ByteArrayMakeMinSize(byte[] input, int minSize)
        {
            if (input.Length >= minSize)
            {
                return input;
            }

            byte[] output = new byte[minSize];
            byte filler = 0;

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
