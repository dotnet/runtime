// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Linq;

namespace Test
{
    public class SumTests
    {
        //
        // Sum
        //
        [Fact]
        public static void RunSumTests()
        {
            int count1 = 0, count2 = 1024 * 8, count3 = 1024 * 16, count4 = 1024 * 1024;

            Action<int>[] testActions = new Action<int>[]
            {
                (count) => RunSumTest1<int>(count),
                (count) => RunSumTest1<int?>(count),
                (count) => RunSumTest1<long>(count),
                (count) => RunSumTest1<long?>(count),
                (count) => RunSumTest1<float>(count),
                (count) => RunSumTest1<float?>(count),
                (count) => RunSumTest1<double>(count),
                (count) => RunSumTest1<double?>(count),
                (count) => RunSumTest1<decimal>(count),
                (count) => RunSumTest1<decimal?>(count)
            };

            for (int i = 0; i < testActions.Length; i++)
            {
                bool isLong = i == 2 || i == 3;
                bool isFloat = i == 4 || i == 5;

                testActions[i](count1);
                testActions[i](count2);
                if (!isFloat)
                    testActions[i](count3);
                if (isLong)
                    testActions[i](count4);
            }
        }

        private static void RunSumTest1<T>(int count)
        {
            if (typeof(T) == typeof(int))
            {
                int expectSum = 0;
                int[] ints = new int[count];
                for (int i = 0; i < ints.Length; i++)
                {
                    ints[i] = i;
                    expectSum += i;
                }

                int realSum = ints.AsParallel().Sum();
                if (!expectSum.Equals(realSum))
                    Assert.True(false, string.Format("RunSumTest1<{0}>(count={1}):  FAILED.  > Expect: {2}, real: {3}", typeof(T), count, expectSum, realSum));
            }
            else if (typeof(T) == typeof(long))
            {
                long expectSum = 0;
                long[] longs = new long[count];
                for (int i = 0; i < longs.Length; i++)
                {
                    longs[i] = i;
                    expectSum += i;
                }

                long realSum = longs.AsParallel().Sum();
                if (!expectSum.Equals(realSum))
                    Assert.True(false, string.Format("RunSumTest1<{0}>(count={1}):  FAILED.  > Expect: {2}, real: {3}", typeof(T), count, expectSum, realSum));
            }
            else if (typeof(T) == typeof(float))
            {
                float expectSum = 0;
                float[] floats = new float[count];
                for (int i = 0; i < floats.Length; i++)
                {
                    float val = (float)i / 10;
                    floats[i] = val;
                    expectSum += val;
                }

                float realSum = floats.AsParallel().Sum();
                if (!AreEpsEqual(expectSum, realSum))
                    Assert.True(false, string.Format("RunSumTest1<{0}>(count={1}):  FAILED.  > Expect: {2}, real: {3}", typeof(T), count, expectSum, realSum));
            }
            else if (typeof(T) == typeof(double))
            {
                double expectSum = 0;
                double[] doubles = new double[count];
                for (int i = 0; i < doubles.Length; i++)
                {
                    double val = (double)i / 100;
                    doubles[i] = val;
                    expectSum += val;
                }

                double realSum = doubles.AsParallel().Sum();
                if (!AreEpsEqual(expectSum, realSum))
                    Assert.True(false, string.Format("RunSumTest1<{0}>(count={1}):  FAILED.  > Expect: {2}, real: {3}", typeof(T), count, expectSum, realSum));
            }
            else if (typeof(T) == typeof(decimal))
            {
                decimal expectSum = 0;
                decimal[] decimals = new decimal[count];
                for (int i = 0; i < decimals.Length; i++)
                {
                    decimal val = (decimal)i / 100;
                    decimals[i] = val;
                    expectSum += val;
                }

                decimal realSum = decimals.AsParallel().Sum();
                //round the numbers for the comparison
                if (!AreEpsEqual(expectSum, realSum))
                    Assert.True(false, string.Format("RunSumTest1<{0}>(count={1}):  FAILED.  > Expect: {2}, real: {3}", typeof(T), count, expectSum, realSum));
            }
            else if (typeof(T) == typeof(int?))
            {
                int? expectSum = 0;
                int?[] ints = new int?[count];
                for (int i = 0; i < ints.Length; i++)
                {
                    ints[i] = i;
                    expectSum += i;
                }

                int? realSum = ints.AsParallel().Sum();

                if (!expectSum.Equals(realSum))
                    Assert.True(false, string.Format("RunSumTest1<{0}>(count={1}):  FAILED.  > Expect: {2}, real: {3}", typeof(T), count, expectSum, realSum));
            }
            else if (typeof(T) == typeof(long?))
            {
                long? expectSum = 0;
                long?[] longs = new long?[count];
                for (int i = 0; i < longs.Length; i++)
                {
                    longs[i] = i;
                    expectSum += i;
                }

                long? realSum = longs.AsParallel().Sum();

                if (!expectSum.Equals(realSum))
                    Assert.True(false, string.Format("RunSumTest1<{0}>(count={1}):  FAILED.  > Expect: {2}, real: {3}", typeof(T), count, expectSum, realSum));
            }
            else if (typeof(T) == typeof(float?))
            {
                float? expectSum = 0;
                float?[] floats = new float?[count];
                for (int i = 0; i < floats.Length; i++)
                {
                    float? val = (float)i / 10;
                    floats[i] = val;
                    expectSum += val;
                }

                bool passed = true;
                float? realSum = floats.AsParallel().Sum();

                if (!expectSum.HasValue || !realSum.HasValue)
                    passed = expectSum.HasValue.Equals(realSum.HasValue);
                else
                    passed = AreEpsEqual(expectSum.Value, realSum.Value);

                if (!passed)
                    Assert.True(false, string.Format("RunSumTest1<{0}>(count={1}):  FAILED.  > Expect: {2}, real: {3}", typeof(T), count, expectSum, realSum));
            }
            else if (typeof(T) == typeof(double?))
            {
                double? expectSum = 0;
                double?[] doubles = new double?[count];
                for (int i = 0; i < doubles.Length; i++)
                {
                    double? val = (double)i / 100;
                    doubles[i] = val;
                    expectSum += val;
                }

                double? realSum = doubles.AsParallel().Sum();

                bool passed = true;
                if (!expectSum.HasValue || !realSum.HasValue)
                    passed = expectSum.HasValue.Equals(realSum.HasValue);
                else
                    passed = AreEpsEqual(expectSum.Value, realSum.Value);

                if (!passed)
                    Assert.True(false, string.Format("RunSumTest1<{0}>(count={1}):  FAILED.  > Expect: {2}, real: {3}", typeof(T), count, expectSum, realSum));
            }
            else if (typeof(T) == typeof(decimal?))
            {
                decimal? expectSum = 0;
                decimal?[] decimals = new decimal?[count];
                for (int i = 0; i < decimals.Length; i++)
                {
                    decimal? val = (decimal)i / 100;
                    decimals[i] = val;
                    expectSum += val;
                }

                decimal? realSum = decimals.AsParallel().Sum();

                bool passed = true;
                if (!expectSum.HasValue || !realSum.HasValue)
                    passed = expectSum.HasValue.Equals(realSum.HasValue);
                else
                    passed = AreEpsEqual(expectSum.Value, realSum.Value);

                if (!passed)
                    Assert.True(false, string.Format("RunSumTest1<{0}>(count={1}):  FAILED.  > Expect: {2}, real: {3}", typeof(T), count, expectSum, realSum));
            }
        }

        //
        // Tests summing an ordered list.
        //
        private static void RunSumTestOrderBy1(int count)
        {
            int expectSum = 0;
            int[] ints = new int[count];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                expectSum += i;
            }

            int realSum = ints.AsParallel().OrderBy(x => x).Sum();

            if (realSum != expectSum)
                Assert.True(false, string.Format("RunSumTestOrderBy1(count={2}): FAILED.  > Expect: {0}, real: {1}", expectSum, realSum, count));
        }

        #region Helper Methods

        /// <summary>
        /// Returns whether two float values are approximately equal.
        /// The values compare as equal if either the relative or absolute
        /// error is less than 1e-4f.
        /// </summary>
        internal static bool AreEpsEqual(float a, float b)
        {
            const float eps = 1e-4f;

            float absA = a >= 0 ? a : -a;
            float absB = b >= 0 ? b : -b;
            float maximumAB = absA >= absB ? absA : absB;
            const float oneF = 1.0f;
            float maxABOneF = oneF >= maximumAB ? oneF : maximumAB;

            float absASubB = (a - b) >= 0 ? (a - b) : -(a - b);
            bool isEqual = (absASubB / maxABOneF) < eps;
            return isEqual;
        }

        /// <summary>
        /// Returns whether two double values are approximately equal.
        /// The values compare as equal if either the relative or absolute
        /// error is less than 1e-9.
        /// </summary>
        internal static bool AreEpsEqual(double a, double b)
        {
            const double eps = 1e-9;

            double absA = a >= 0 ? a : -a;
            double absB = b >= 0 ? b : -b;
            double maximumAB = absA >= absB ? absA : absB;
            const double oneD = 1.0;
            double maxABOne = oneD >= maximumAB ? oneD : maximumAB;

            double absASubB = (a - b) >= 0 ? (a - b) : -(a - b);
            bool isEqual = (absASubB / maxABOne) < eps;
            return isEqual;
        }

        /// <summary>
        /// Returns whether two decimal values are approximately equal.
        /// The values compare as equal if either the relative or absolute
        /// error is less than 1e-9m.
        /// </summary>
        internal static bool AreEpsEqual(decimal a, decimal b)
        {
            const decimal eps = 1e-9m;

            decimal absA = a >= 0 ? a : -a;
            decimal absB = b >= 0 ? b : -b;
            decimal maximumAB = absA >= absB ? absA : absB;
            const decimal oneD = 1.0m;
            decimal maxABOne = oneD >= maximumAB ? oneD : maximumAB;

            decimal absASubB = (a - b) >= 0 ? (a - b) : -(a - b);
            bool isEqual = (absASubB / maxABOne) < eps;

            return isEqual;
        }
        #endregion
    }
}
