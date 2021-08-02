// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Numerics.Tests
{
    public class logTest
    {
        private const int NumberOfRandomIterations = 10;
        private const int RequiredPrecision = 15;
        private static Random s_random = new Random(100);

        public static IEnumerable<object[]> RunLogOfZeroIsInfinityTestSources
        {
            get
            {
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    double currentValue = s_random.NextDouble();
                    yield return new object[] { currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunLogOfZeroIsInfinityTestSources))]
        public void RunLogOfZeroIsInfinityTest(double baseValue)
        {
            Assert.Equal(double.PositiveInfinity, BigInteger.Log(0, baseValue));
        }

        public static IEnumerable<object[]> RunLogToBaseZeroIsNaNTestSources
        {
            get
            {
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    BigInteger currentValue;
                    do
                    {
                        currentValue = new BigInteger(MyBigIntImp.GetRandomPosByteArray(s_random, 8));
                    }
                    while (currentValue == BigInteger.One);
                    yield return new object[] { currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunLogToBaseZeroIsNaNTestSources))]
        public void RunLogToBaseZeroIsNaNTest(BigInteger testValue)
        {
            Assert.True((double.IsNaN(BigInteger.Log(testValue, 0))));
        }

        public static IEnumerable<object[]> RunLogToBaseNaNIsNaNTestSources
        {
            get
            {
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    BigInteger currentValue = new BigInteger(MyBigIntImp.GetRandomByteArray(s_random, 10));
                    yield return new object[] { currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunLogToBaseNaNIsNaNTestSources))]
        public void RunLogToBaseNaNIsNaNTest(BigInteger testValue)
        {
            Assert.True((double.IsNaN(BigInteger.Log(testValue, double.NaN))));
        }

        public static IEnumerable<object[]> RunLogToBasePositiveInfinityIsNaNTestSources
        {
            get
            {
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    BigInteger currentValue = new BigInteger(MyBigIntImp.GetRandomByteArray(s_random, 10));
                    yield return new object[] { currentValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunLogToBasePositiveInfinityIsNaNTestSources))]
        public void RunLogToBasePositiveInfinityIsNaNTest(BigInteger testValue)
        {
            Assert.True((double.IsNaN(BigInteger.Log(testValue, double.PositiveInfinity))));
        }

        public static IEnumerable<object[]> RunLogToBasePositiveInfinityTestSources
        {
            get
            {
                yield return new object[] { 1, 0 };
            }
        }

        [Theory]
        [MemberData(nameof(RunLogToBasePositiveInfinityTestSources))]
        public void RunLogToBasePositiveInfinityTest(BigInteger testValue, double expectedValue)
        {
            Assert.Equal(expectedValue, BigInteger.Log(testValue, double.PositiveInfinity));
        }

        public static IEnumerable<object[]> RunLogToBaseNegativeIsNaNTestSources
        {
            get
            {
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    BigInteger currentValue = new BigInteger(MyBigIntImp.GetRandomByteArray(s_random, 10));
                    double baseValue = -s_random.NextDouble();
                    yield return new object[] { currentValue, baseValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunLogToBaseNegativeIsNaNTestSources))]
        public void RunLogToBaseNegativeIsNaNTest(BigInteger testValue, double baseValue)
        {
            Assert.True((double.IsNaN(BigInteger.Log(testValue, baseValue))));
        }

        public static IEnumerable<object[]> RunLogOfSmallValueTestSources
        {
            get
            {
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    BigInteger currentValue = new BigInteger(MyBigIntImp.GetRandomByteArray(s_random, 10));
                    double baseValue = Math.Min(s_random.NextDouble(), 0.5);
                    double expectedValue = Math.Log((double)currentValue, baseValue);
                    yield return new object[] { currentValue, baseValue, expectedValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunLogOfSmallValueTestSources))]
        public void RunLogOfSmallValueTest(BigInteger testValue, double baseValue, double expectedValue)
        {
            Assert.Equal(expectedValue, BigInteger.Log(testValue, baseValue), RequiredPrecision);
        }

        public static IEnumerable<object[]> RunLogOfLargeValueTestSources
        {
            get
            {
                for (int i = 0; i < NumberOfRandomIterations; i++)
                {
                    BigInteger currentValue = new BigInteger(MyBigIntImp.GetRandomPosByteArray(s_random, s_random.Next(1, 100)));
                    double baseValue = Math.Min(s_random.NextDouble(), 0.5);
                    double expectedValue = Math.Log((double)currentValue, baseValue);
                    yield return new object[] { currentValue, baseValue, expectedValue };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RunLogOfLargeValueTestSources))]
        public void RunLogOfLargeValueTest(BigInteger testValue, double baseValue, double expectedValue)
        {
            Assert.Equal(expectedValue, BigInteger.Log(testValue, baseValue), RequiredPrecision);
        }

        public static IEnumerable<object[]> RunLargeValueLogTestSources
        {
            get
            {
                yield return new object[] { 128, 1, 0, 1 };
                yield return new object[] { 0, 4, 64, 3 };
            }
        }

        [Theory]
        [MemberData(nameof(RunLargeValueLogTestSources))]
        public void RunLargeValueLogTest(int startShift, int bigShiftLoopLimit, int smallShift, int smallShiftLoopLimit)
        {
            LargeValueLogTests(startShift, bigShiftLoopLimit, smallShift, smallShiftLoopLimit);
        }

        /// <summary>
        /// Test Log Method on Very Large BigInteger more than (1 &lt;&lt; Int.MaxValue) by base 2
        /// Tested BigInteger are: pow(2, startShift + smallLoopShift * [1..smallLoopLimit] + Int32.MaxValue * [1..bigLoopLimit])
        /// Note:
        /// ToString() can not operate such large values
        /// VerifyLogString() can not operate such large values,
        /// Math.Log() can not operate such large values
        /// </summary>
        private static void LargeValueLogTests(int startShift, int bigShiftLoopLimit, int smallShift = 0, int smallShiftLoopLimit = 1)
        {
            const double logbase = 2D;
            BigInteger init = BigInteger.One << startShift;


            for (int i = 0; i < smallShiftLoopLimit; i++)
            {
                BigInteger temp = init << ((i + 1) * smallShift);

                for (int j = 0; j<bigShiftLoopLimit; j++)
                {
                    temp = temp << (int.MaxValue / 10);
                    double expected =
                        (double)startShift +
                        smallShift * (double)(i + 1) +
                        (int.MaxValue / 10) * (double)(j + 1);
                    Assert.Equal(expected, BigInteger.Log(temp, logbase), RequiredPrecision);
                }

            }
        }
    }
}
