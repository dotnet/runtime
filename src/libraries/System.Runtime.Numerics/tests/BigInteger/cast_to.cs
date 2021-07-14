// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Numerics.Tests
{
    public class cast_toTest
    {
        public delegate void ExceptionGenerator();

        private const int NumberOfRandomIterations = 10;
        private static Random s_random = new Random(100);

        public static IEnumerable<object[]> RunByteImplicitCastToBigIntegerTestSources
        {
            get
            {
                yield return new object[] { byte.MinValue, new BigInteger(byte.MinValue) };
                yield return new object[] { byte.MaxValue, new BigInteger(byte.MaxValue) };
                yield return new object[] { 0, new BigInteger(0) };
                yield return new object[] { 1, new BigInteger(1) };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    byte currentValue = (byte)s_random.Next(byte.MinValue, byte.MaxValue);
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunByteImplicitCastToBigIntegerTestSources))]
        public void RunByteImplicitCastToBigIntegerTest(byte testValue, BigInteger expectedValue)
        {
            BigInteger actualValue;

            actualValue = testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunSByteImplicitCastToBigIntegerTestSources
        {
            get
            {
                yield return new object[] { sbyte.MinValue, new BigInteger(sbyte.MinValue) };
                yield return new object[] { sbyte.MaxValue, new BigInteger(sbyte.MaxValue) };
                yield return new object[] { -1, new BigInteger(-1) };
                yield return new object[] { 0, new BigInteger(0) };
                yield return new object[] { 1, new BigInteger(1) };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    sbyte currentValue = (sbyte)s_random.Next(sbyte.MinValue, 0);
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    sbyte currentValue = (sbyte)s_random.Next(0, sbyte.MaxValue);
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunSByteImplicitCastToBigIntegerTestSources))]
        public void RunSByteImplicitCastToBigIntegerTest(sbyte testValue, BigInteger expectedValue)
        {
            BigInteger actualValue;

            actualValue = testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunUInt16ImplicitCastToBigIntegerTestSources
        {
            get
            {
                yield return new object[] { ushort.MinValue, new BigInteger(ushort.MinValue) };
                yield return new object[] { ushort.MaxValue, new BigInteger(ushort.MaxValue) };
                yield return new object[] { 0, new BigInteger(0) };
                yield return new object[] { 1, new BigInteger(1) };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    ushort currentValue = (ushort)s_random.Next(ushort.MinValue, ushort.MaxValue);
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunUInt16ImplicitCastToBigIntegerTestSources))]
        public void RunUInt16ImplicitCastToBigIntegerTest(ushort testValue, BigInteger expectedValue)
        {
            BigInteger actualValue;

            actualValue = testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunInt16ImplicitCastToBigIntegerTestSources
        {
            get
            {
                yield return new object[] { short.MinValue, new BigInteger(short.MinValue) };
                yield return new object[] { short.MaxValue, new BigInteger(short.MaxValue) };
                yield return new object[] { -1, new BigInteger(-1) };
                yield return new object[] { 0, new BigInteger(0) };
                yield return new object[] { 1, new BigInteger(1) };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    short currentValue = (short)s_random.Next(short.MinValue, 0);
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    short currentValue = (short)s_random.Next(0, short.MaxValue);
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunInt16ImplicitCastToBigIntegerTestSources))]
        public void RunInt16ImplicitCastToBigIntegerTest(short testValue, BigInteger expectedValue)
        {
            BigInteger actualValue;

            actualValue = testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunUInt32ImplicitCastToBigIntegerTestSources
        {
            get
            {
                yield return new object[] { uint.MinValue, new BigInteger(uint.MinValue) };
                yield return new object[] { uint.MaxValue, new BigInteger(uint.MaxValue) };
                yield return new object[] { 0, new BigInteger(0) };
                yield return new object[] { 1, new BigInteger(1) };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    uint currentValue = (uint)(uint.MaxValue * s_random.NextDouble());
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunUInt32ImplicitCastToBigIntegerTestSources))]
        public void RunUInt32ImplicitCastToBigIntegerTest(uint testValue, BigInteger expectedValue)
        {
            BigInteger actualValue;

            actualValue = testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunInt32ImplicitCastToBigIntegerTestSources
        {
            get
            {
                yield return new object[] { int.MinValue, new BigInteger(int.MinValue) };
                yield return new object[] { int.MaxValue, new BigInteger(int.MaxValue) };
                yield return new object[] { -1, new BigInteger(-1) };
                yield return new object[] { 0, new BigInteger(0) };
                yield return new object[] { 1, new BigInteger(1) };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    int currentValue = s_random.Next(int.MinValue, 0);
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    int currentValue = s_random.Next(0, int.MaxValue);
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunInt32ImplicitCastToBigIntegerTestSources))]
        public void RunInt32ImplicitCastToBigIntegerTest(int testValue, BigInteger expectedValue)
        {
            BigInteger actualValue;

            actualValue = testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunUInt64ImplicitCastToBigIntegerTestSources
        {
            get
            {
                yield return new object[] { ulong.MinValue, new BigInteger(ulong.MinValue) };
                yield return new object[] { ulong.MaxValue, new BigInteger(ulong.MaxValue) };
                yield return new object[] { 0, new BigInteger(0) };
                yield return new object[] { 1, new BigInteger(1) };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    ulong currentValue = (ulong)(ulong.MaxValue * s_random.NextDouble());
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunUInt64ImplicitCastToBigIntegerTestSources))]
        public void RunUInt64ImplicitCastToBigIntegerTest(ulong testValue, BigInteger expectedValue)
        {
            BigInteger actualValue;

            actualValue = testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunInt64ImplicitCastToBigIntegerTestSources
        {
            get
            {
                yield return new object[] { long.MinValue, new BigInteger(long.MinValue) };
                yield return new object[] { long.MaxValue, new BigInteger(long.MaxValue) };
                yield return new object[] { -1, new BigInteger(-1) };
                yield return new object[] { 0, new BigInteger(0) };
                yield return new object[] { 1, new BigInteger(1) };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    long currentValue = (long)(long.MinValue * s_random.NextDouble());
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    long currentValue = (long)(long.MaxValue * s_random.NextDouble());
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunInt64ImplicitCastToBigIntegerTestSources))]
        public void RunInt64ImplicitCastToBigIntegerTest(long testValue, BigInteger expectedValue)
        {
            BigInteger actualValue;

            actualValue = testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunSingleExplicitCastToBigIntegerThrowsOverflowExceptionTestSources
        {
            get
            {
                yield return new object[] { float.NaN };
                yield return new object[] { float.NegativeInfinity };
                yield return new object[] { float.PositiveInfinity };
                yield return new object[] { float.MaxValue * 2.0f };
            }
        }

        [Theory]
        [MemberData(nameof(RunSingleExplicitCastToBigIntegerThrowsOverflowExceptionTestSources))]
        public void RunSingleExplicitCastToBigIntegerThrowsOverflowExceptionTest(float testValue)
        {
            Assert.Throws<OverflowException>(() => (BigInteger)testValue);
        }

        public static IEnumerable<object[]> RunSingleImplicitCastToBigIntegerTestSources
        {
            get
            {
                yield return new object[] { float.MinValue, new BigInteger(float.MinValue) };
                yield return new object[] { float.Epsilon, new BigInteger(float.Epsilon) };
                yield return new object[] { float.MaxValue, new BigInteger(float.MaxValue) };
                yield return new object[] { -1, new BigInteger(-1) };
                yield return new object[] { 0, new BigInteger(0) };
                yield return new object[] { Math.Pow(2, -126), new BigInteger(Math.Pow(2, -126)) };
                yield return new object[] { 1 + Math.Pow(2, -23), new BigInteger(1 + Math.Pow(2, -23)) };
                yield return new object[] { 1, new BigInteger(1) };
                yield return new object[] { Math.Pow(2, 127), new BigInteger(Math.Pow(2, 127)) };
                float value = 0;
                for (int i = 1; i <= 24; ++i)
                {
                    value += (float)(Math.Pow(2, -i));
                }
                yield return new object[] { value, new BigInteger(value) };
                value = 0;
                for (int i = 1; i <= 23; ++i)
                {
                    value += (float)(Math.Pow(2, -i));
                }
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    float currentValue = (float)(float.MinValue * s_random.NextDouble());
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    float currentValue = (float)(float.MaxValue * s_random.NextDouble());
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunSingleImplicitCastToBigIntegerTestSources))]
        public static void RunSingleExplicitCastToBigIntegerTest(float testValue, BigInteger expectedValue)
        {
            BigInteger actualValue;

            actualValue = (BigInteger)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunDoubleExplicitCastToBigIntegerThrowsOverflowExceptionTestSources
        {
            get
            {
                yield return new object[] { double.NaN };
                yield return new object[] { double.NegativeInfinity };
                yield return new object[] { double.PositiveInfinity };
            }
        }

        [Theory]
        [MemberData(nameof(RunDoubleExplicitCastToBigIntegerThrowsOverflowExceptionTestSources))]
        public void RunDoubleExplicitCastToBigIntegerThrowsOverflowExceptionTest(double testValue)
        {
            Assert.Throws<OverflowException>(() => (BigInteger)testValue);
        }

        public static IEnumerable<object[]> RunDoubleImplicitCastToBigIntegerTestSources
        {
            get
            {
                yield return new object[] { double.MinValue, new BigInteger(double.MinValue) };
                yield return new object[] { double.Epsilon, new BigInteger(double.Epsilon) };
                yield return new object[] { double.MaxValue, new BigInteger(double.MaxValue) };
                yield return new object[] { -1, new BigInteger(-1) };
                yield return new object[] { 0, new BigInteger(0) };
                yield return new object[] { Math.Pow(2, -1022), new BigInteger(Math.Pow(2, -1022)) };
                yield return new object[] { 1 + Math.Pow(2, -52), new BigInteger(1 + Math.Pow(2, -52)) };
                yield return new object[] { 1, new BigInteger(1) };
                yield return new object[] { Math.Pow(2, 1023), new BigInteger(Math.Pow(2, 1023)) };
                double value = 0;
                for (int i = 1; i <= 53; ++i)
                {
                    value += Math.Pow(2, -i);
                }
                yield return new object[] { value, new BigInteger(value) };
                value = 0;
                for (int i = 1; i <= 52; ++i)
                {
                    value += Math.Pow(2, -i);
                }
                yield return new object[] { value, new BigInteger(value) };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    double currentValue = double.MinValue * s_random.NextDouble();
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    double currentValue = double.MaxValue * s_random.NextDouble();
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunDoubleImplicitCastToBigIntegerTestSources))]
        public static void RunDoubleExplicitCastToBigIntegerTests(double testValue, BigInteger expectedValue)
        {
            BigInteger actualValue;

            actualValue = (BigInteger)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunDecimalImplicitCastToBigIntegerTestSources
        {
            get
            {
                yield return new object[] { decimal.MinValue, new BigInteger(decimal.MinValue) };
                yield return new object[] { decimal.MaxValue, new BigInteger(decimal.MaxValue) };
                yield return new object[] { -1, new BigInteger(-1) };

                decimal value;
                value = new decimal(0, 0, 0, false, 28);
                yield return new object[] { value, new BigInteger(value) };

                yield return new object[] { 0, new BigInteger(0) };

                value = new decimal(1, 0, 0, false, 28);
                yield return new object[] { value, new BigInteger(value) };

                value = 1 - new decimal(1, 0, 0, false, 28);
                yield return new object[] { value, new BigInteger(value) };

                value = new decimal(1, 0, 0, false, 0);
                yield return new object[] { value, new BigInteger(value) };

                yield return new object[] { 1, new BigInteger(1) };
                
                value = 1 + new decimal(1, 0, 0, false, 28);
                yield return new object[] { value, new BigInteger(value) };
                value = 2 - new decimal(1, 0, 0, false, 28);
                yield return new object[] { value, new BigInteger(value) };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    decimal currentValue = new decimal(
                        s_random.Next(int.MinValue, int.MaxValue),
                        s_random.Next(int.MinValue, int.MaxValue),
                        s_random.Next(int.MinValue, int.MaxValue),
                        true,
                        (byte)s_random.Next(0, 29));
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    decimal currentValue = new decimal(
                        s_random.Next(int.MinValue, int.MaxValue),
                        s_random.Next(int.MinValue, int.MaxValue),
                        s_random.Next(int.MinValue, int.MaxValue),
                        false,
                        (byte)s_random.Next(0, 29));
                    yield return new object[] { currentValue, new BigInteger(currentValue) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunDecimalImplicitCastToBigIntegerTestSources))]
        public static void RunDecimalExplicitCastToBigIntegerTests(decimal testValue, BigInteger expectedValue)
        {
            BigInteger actualValue;

            actualValue = (BigInteger)testValue;

            Assert.Equal(expectedValue, actualValue);
        }
    }
}
