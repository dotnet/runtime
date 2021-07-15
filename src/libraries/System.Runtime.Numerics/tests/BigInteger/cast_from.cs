// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Numerics.Tests
{
    public class cast_fromTest
    {
        private const int NumberOfRandomIterations = 10;
        private static Random s_random = new Random(100);

        public static IEnumerable<object[]> RunByteExplicitCastFromBigIntegerTestSources
        {
            get
            {
                yield return new object[] { new BigInteger(byte.MinValue), byte.MinValue };
                yield return new object[] { new BigInteger(byte.MaxValue), byte.MaxValue };
                yield return new object[] { new BigInteger(0), 0 };
                yield return new object[] { new BigInteger(1), 1 };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    byte currentValue = (byte)s_random.Next(byte.MinValue, byte.MaxValue);
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunByteExplicitCastFromBigIntegerTestSources))]
        public void RunByteExplicitCastFromBigIntegerTest(BigInteger testValue, byte expectedValue)
        {
            byte actualValue;

            actualValue = (byte)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunByteExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources
        {
            get
            {
                yield return new object[] { byte.MinValue - BigInteger.One };
                yield return new object[] { byte.MaxValue + BigInteger.One };
                yield return new object[] { GenerateRandomBigIntegerLessThan(byte.MinValue, s_random) };
                yield return new object[] { GenerateRandomBigIntegerGreaterThan(byte.MaxValue, s_random) };
            }
        }

        [Theory]
        [MemberData(nameof(RunByteExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources))]
        public void RunByteExplicitCastFromBigIntegerThrowsOverflowExceptionTest(BigInteger testValue)
        {
            Assert.Throws<OverflowException>(() => (byte)testValue);
        }

        public static IEnumerable<object[]> RunSByteExplicitCastFromBigIntegerTestSources
        {
            get
            {
                yield return new object[] { new BigInteger(sbyte.MinValue), sbyte.MinValue };
                yield return new object[] { new BigInteger(sbyte.MaxValue), sbyte.MaxValue };
                yield return new object[] { new BigInteger(-1), -1 };
                yield return new object[] { new BigInteger(0), 0 };
                yield return new object[] { new BigInteger(1), 1 };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    sbyte currentValue = (sbyte)s_random.Next(sbyte.MinValue, sbyte.MaxValue);
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunSByteExplicitCastFromBigIntegerTestSources))]
        public void RunSByteExplicitCastFromBigIntegerTest(BigInteger testValue, sbyte expectedValue)
        {
            sbyte actualValue;

            actualValue = (sbyte)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunSByteExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources
        {
            get
            {
                yield return new object[] { sbyte.MinValue - BigInteger.One };
                yield return new object[] { sbyte.MaxValue + BigInteger.One };
                yield return new object[] { GenerateRandomBigIntegerLessThan(sbyte.MinValue, s_random) };
                yield return new object[] { GenerateRandomBigIntegerGreaterThan((ulong)sbyte.MaxValue, s_random) };
            }
        }

        [Theory]
        [MemberData(nameof(RunSByteExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources))]
        public void RunSByteExplicitCastFromBigIntegerThrowsOverflowExceptionTest(BigInteger testValue)
        {
            Assert.Throws<OverflowException>(() => (sbyte)testValue);
        }

        public static IEnumerable<object[]> RunUInt16ExplicitCastFromBigIntegerTestSources
        {
            get
            {
                yield return new object[] { new BigInteger(ushort.MinValue), ushort.MinValue };
                yield return new object[] { new BigInteger(ushort.MaxValue), ushort.MaxValue };
                yield return new object[] { new BigInteger(0), 0 };
                yield return new object[] { new BigInteger(1), 1 };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    ushort currentValue = (ushort)s_random.Next(ushort.MinValue, ushort.MaxValue);
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunUInt16ExplicitCastFromBigIntegerTestSources))]
        public void RunUInt16ExplicitCastFromBigIntegerTest(BigInteger testValue, ushort expectedValue)
        {
            ushort actualValue;

            actualValue = (ushort)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunUInt16ExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources
        {
            get
            {
                yield return new object[] { ushort.MinValue - BigInteger.One };
                yield return new object[] { ushort.MaxValue + BigInteger.One };
                yield return new object[] { GenerateRandomBigIntegerLessThan(ushort.MinValue, s_random) };
                yield return new object[] { GenerateRandomBigIntegerGreaterThan(ushort.MaxValue, s_random) };
            }
        }

        [Theory]
        [MemberData(nameof(RunUInt16ExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources))]
        public void RunUInt16ExplicitCastFromBigIntegerThrowsOverflowExceptionTest(BigInteger testValue)
        {
            Assert.Throws<OverflowException>(() => (ushort)testValue);
        }

        public static IEnumerable<object[]> RunInt16ExplicitCastFromBigIntegerTestSources
        {
            get
            {
                yield return new object[] { new BigInteger(short.MinValue), short.MinValue };
                yield return new object[] { new BigInteger(short.MaxValue), short.MaxValue };
                yield return new object[] { new BigInteger(-1), -1 };
                yield return new object[] { new BigInteger(0), 0 };
                yield return new object[] { new BigInteger(1), 1 };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    short currentValue = (short)s_random.Next(short.MinValue, short.MaxValue);
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunInt16ExplicitCastFromBigIntegerTestSources))]
        public void RunInt16ExplicitCastFromBigIntegerTest(BigInteger testValue, short expectedValue)
        {
            short actualValue;

            actualValue = (short)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunInt16ExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources
        {
            get
            {
                yield return new object[] { short.MinValue - BigInteger.One };
                yield return new object[] { short.MaxValue + BigInteger.One };
                yield return new object[] { GenerateRandomBigIntegerLessThan(short.MinValue, s_random) };
                yield return new object[] { GenerateRandomBigIntegerGreaterThan((long)short.MaxValue, s_random) };
            }
        }

        [Theory]
        [MemberData(nameof(RunInt16ExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources))]
        public void RunInt16ExplicitCastFromBigIntegerThrowsOverflowExceptionTest(BigInteger testValue)
        {
            Assert.Throws<OverflowException>(() => (short)testValue);
        }

        public static IEnumerable<object[]> RunUInt32ExplicitCastFromBigIntegerTestSources
        {
            get
            {
                yield return new object[] { new BigInteger(uint.MinValue), uint.MinValue };
                yield return new object[] { new BigInteger(uint.MaxValue), uint.MaxValue };
                yield return new object[] { new BigInteger(0), 0 };
                yield return new object[] { new BigInteger(1), 1 };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    uint currentValue = (uint)(uint.MaxValue * s_random.NextDouble());
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunUInt32ExplicitCastFromBigIntegerTestSources))]
        public void RunUInt32ExplicitCastFromBigIntegerTest(BigInteger testValue, uint expectedValue)
        {
            uint actualValue;

            actualValue = (uint)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunUInt32ExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources
        {
            get
            {
                yield return new object[] { uint.MinValue - BigInteger.One };
                yield return new object[] { uint.MaxValue + BigInteger.One };
                yield return new object[] { GenerateRandomBigIntegerLessThan(uint.MinValue, s_random) };
                yield return new object[] { GenerateRandomBigIntegerGreaterThan(uint.MaxValue, s_random) };
            }
        }

        [Theory]
        [MemberData(nameof(RunUInt32ExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources))]
        public void RunUInt32ExplicitCastFromBigIntegerThrowsOverflowExceptionTest(BigInteger testValue)
        {
            Assert.Throws<OverflowException>(() => (uint)testValue);
        }

        public static IEnumerable<object[]> RunInt32ExplicitCastFromBigIntegerTestSources
        {
            get
            {
                yield return new object[] { new BigInteger(int.MinValue), int.MinValue };
                yield return new object[] { new BigInteger(int.MaxValue), int.MaxValue };
                yield return new object[] { new BigInteger(-1), -1 };
                yield return new object[] { new BigInteger(0), 0 };
                yield return new object[] { new BigInteger(1), 1 };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    int currentValue = s_random.Next(int.MinValue, int.MaxValue);
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunInt32ExplicitCastFromBigIntegerTestSources))]
        public void RunInt32ExplicitCastFromBigIntegerTest(BigInteger testValue, int expectedValue)
        {
            int actualValue;

            actualValue = (int)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunInt32ExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources
        {
            get
            {
                yield return new object[] { int.MinValue - BigInteger.One };
                yield return new object[] { int.MaxValue + BigInteger.One };
                yield return new object[] { GenerateRandomBigIntegerLessThan(int.MinValue, s_random) };
                yield return new object[] { GenerateRandomBigIntegerGreaterThan(int.MaxValue, s_random) };
            }
        }

        [Theory]
        [MemberData(nameof(RunInt32ExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources))]
        public void RunInt32ExplicitCastFromBigIntegerThrowsOverflowExceptionTest(BigInteger testValue)
        {
            Assert.Throws<OverflowException>(() => (int)testValue);
        }

        public static IEnumerable<object[]> RunUInt64ExplicitCastFromBigIntegerTestSources
        {
            get
            {
                yield return new object[] { new BigInteger(ulong.MinValue), ulong.MinValue };
                yield return new object[] { new BigInteger(ulong.MaxValue), ulong.MaxValue };
                yield return new object[] { new BigInteger(0), 0 };
                yield return new object[] { new BigInteger(1), 1 };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    ulong currentValue = (ulong)(ulong.MaxValue * s_random.NextDouble());
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunUInt64ExplicitCastFromBigIntegerTestSources))]
        public void RunUInt64ExplicitCastFromBigIntegerTest(BigInteger testValue, ulong expectedValue)
        {
            ulong actualValue;

            actualValue = (ulong)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunUInt64ExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources
        {
            get
            {
                yield return new object[] { ulong.MinValue - BigInteger.One };
                yield return new object[] { ulong.MaxValue + BigInteger.One };
                yield return new object[] { GenerateRandomBigIntegerLessThan(0, s_random) };
                yield return new object[] { GenerateRandomBigIntegerGreaterThan(ulong.MaxValue, s_random) };
            }
        }

        [Theory]
        [MemberData(nameof(RunUInt64ExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources))]
        public void RunUInt64ExplicitCastFromBigIntegerThrowsOverflowExceptionTest(BigInteger testValue)
        {
            Assert.Throws<OverflowException>(() => (ulong)testValue);
        }

        public static IEnumerable<object[]> RunInt64ExplicitCastFromBigIntegerTestSources
        {
            get
            {
                yield return new object[] { new BigInteger(long.MinValue), long.MinValue };
                yield return new object[] { new BigInteger(long.MaxValue), long.MaxValue };
                yield return new object[] { new BigInteger(-1), -1 };
                yield return new object[] { new BigInteger(0), 0 };
                yield return new object[] { new BigInteger(1), 1 };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    long currentValue = (long)(long.MinValue * s_random.NextDouble());
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    long currentValue = (long) (long.MaxValue * s_random.NextDouble());
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunInt64ExplicitCastFromBigIntegerTestSources))]
        public void RunInt64ExplicitCastFromBigIntegerTest(BigInteger testValue, long expectedValue)
        {
            long actualValue;

            actualValue = (long)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunInt64ExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources
        {
            get
            {
                yield return new object[] { long.MinValue - BigInteger.One };
                yield return new object[] { long.MaxValue + BigInteger.One };
                yield return new object[] { GenerateRandomBigIntegerLessThan(long.MinValue, s_random) };
                yield return new object[] { GenerateRandomBigIntegerGreaterThan(long.MaxValue, s_random) };
            }
        }

        [Theory]
        [MemberData(nameof(RunInt64ExplicitCastFromBigIntegerThrowsOverflowExceptionTestSources))]
        public void RunInt64ExplicitCastFromBigIntegerThrowsOverflowExceptionTest(BigInteger testValue)
        {
            Assert.Throws<OverflowException>(() => (long)testValue);
        }

        public static IEnumerable<object[]> RunSingleExplicitCastFromBigIntegerTestSources
        {
            get
            {
                yield return new object[] { new BigInteger(float.MinValue), float.MinValue };
                yield return new object[] { new BigInteger(float.MaxValue), float.MaxValue };
                yield return new object[] { new BigInteger(float.MinValue) - BigInteger.One, float.MinValue };
                yield return new object[] { new BigInteger(float.MaxValue) + BigInteger.One, float.MaxValue };
                yield return new object[] { new BigInteger(-1f), -1f };
                yield return new object[] { new BigInteger(0f), 0f };
                yield return new object[] { new BigInteger(1f), 1f };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    float currentValue = (float)(float.MinValue * s_random.NextDouble());
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    float currentValue = (float)(float.MaxValue * s_random.NextDouble());
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunSingleExplicitCastFromBigIntegerTestSources))]
        public void RunSingleExplicitCastFromBigIntegerTest(BigInteger testValue, float expectedValue)
        {
            float actualValue;

            actualValue = (float)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunDoubleExplicitCastFromBigIntegerTestSources
        {
            get
            {
                yield return new object[] { new BigInteger(double.MinValue), double.MinValue };
                yield return new object[] { new BigInteger(double.MaxValue), double.MaxValue };
                yield return new object[] { new BigInteger(double.MinValue) - BigInteger.One, double.MinValue };
                yield return new object[] { new BigInteger(double.MaxValue) + BigInteger.One, double.MaxValue };
                yield return new object[] { new BigInteger(-1.0), -1.0 };
                yield return new object[] { new BigInteger(0.0), 0.0 };
                yield return new object[] { new BigInteger(1.0), 1.0 };
                yield return new object[] { new BigInteger(4611686018427387903), 4611686018427387903 };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    double currentValue = double.MinValue * s_random.NextDouble();
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    double currentValue = double.MaxValue * s_random.NextDouble();
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunDoubleExplicitCastFromBigIntegerTestSources))]
        public void RunDoubleExplicitCastFromBigIntegerTest(BigInteger testValue, double expectedValue)
        {
            double actualValue;

            actualValue = (double)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        public static IEnumerable<object[]> RunDecimalExplicitCastFromBigIntegerTestSources
        {
            get
            {
                yield return new object[] { new BigInteger(decimal.MinValue), decimal.MinValue };
                yield return new object[] { new BigInteger(decimal.MaxValue), decimal.MaxValue };
                yield return new object[] { new BigInteger(-1.0), -1.0 };
                yield return new object[] { new BigInteger(0.0), 0.0 };
                yield return new object[] { new BigInteger(1.0), 1.0 };
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    decimal currentValue = (decimal)((double)decimal.MinValue * s_random.NextDouble());
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    decimal currentValue = (decimal)((double)decimal.MaxValue * s_random.NextDouble());
                    yield return new object[] { new BigInteger(currentValue), currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunDecimalExplicitCastFromBigIntegerTestSources))]
        public void RunDecimalExplicitCastFromBigIntegerTest(BigInteger testValue, decimal expectedValue)
        {
            decimal actualValue;

            actualValue = (decimal)testValue;

            Assert.Equal(expectedValue, actualValue);
        }

        private static BigInteger GenerateRandomNegativeBigInteger(Random random)
        {
            BigInteger bigInteger;
            int arraySize = random.Next(1, 8) * 4;
            byte[] byteArray = new byte[arraySize];

            for (int i = 0; i < arraySize; i++)
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

            for (int i = 0; i < arraySize; i++)
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

        private static BigInteger GenerateRandomBigIntegerGreaterThan(ulong value, Random random)
        {
            return (GenerateRandomPositiveBigInteger(random) + value) + 1;
        }
    }
}
