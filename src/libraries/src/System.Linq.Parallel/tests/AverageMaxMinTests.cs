// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Linq;

namespace Test
{
    public class AverageMaxMinTests
    {
        //
        // Average
        //
        [Fact]
        public static void RunAvgTests()
        {
            int count1 = 1, count2 = 2, count3 = 4, count4 = 13, count5 = 1024 * 8, count6 = 1024 * 16;
            Action<int>[] testActions = new Action<int>[]
            {
                (count) => RunAvgTest1<int>(count),
                (count) => RunAvgTest1<int?>(count),
                (count) => RunAvgTest1<long>(count),
                (count) => RunAvgTest1<long?>(count),
                (count) => RunAvgTest1<float>(count),
                (count) => RunAvgTest1<float?>(count),
                (count) => RunAvgTest1<double>(count),
                (count) => RunAvgTest1<double?>(count),
                (count) => RunAvgTest1<decimal>(count),
                (count) => RunAvgTest1<decimal?>(count)
            };

            for (int i = 0; i < testActions.Length; i++)
            {
                bool isFloat = i == 4 || i == 5;
                testActions[i](count1);
                testActions[i](count2);
                testActions[i](count3);
                testActions[i](count4);
                testActions[i](count5);
                if (!isFloat)
                    testActions[i](count6);
                Assert.Throws<InvalidOperationException>(
                   () => { testActions[i](0); });
            }
        }
        [Fact]
        public static void RunAvgTests_Negative()
        {
            Action<int>[] testActions = new Action<int>[]
            {
                (count) => RunAvgTest1<int>(count),
                (count) => RunAvgTest1<int?>(count),
                (count) => RunAvgTest1<long>(count),
                (count) => RunAvgTest1<long?>(count),
                (count) => RunAvgTest1<float>(count),
                (count) => RunAvgTest1<float?>(count),
                (count) => RunAvgTest1<double>(count),
                (count) => RunAvgTest1<double?>(count),
                (count) => RunAvgTest1<decimal>(count),
                (count) => RunAvgTest1<decimal?>(count)
            };

            for (int i = 0; i < testActions.Length; i++)
            {
                Assert.Throws<InvalidOperationException>(
                   () => { testActions[i](0); });
            }
        }

        private static void RunAvgTest1<T>(int count)
        {
            string testMethodFail = string.Format("RunAvgTest1<{0}>(count={1}):  FAILED.", typeof(T), count);

            if (typeof(T) == typeof(int))
            {
                int[] ints = new int[count];
                double expectAvg = ((double)count - 1) / 2;

                for (int i = 0; i < ints.Length; i++) ints[i] = i;
                double realAvg = ints.AsParallel().Average();

                if (!expectAvg.Equals(realAvg))
                    Assert.True(false, string.Format(testMethodFail + " > LINQ says: {0}, Expect: {0}, real: {1}", Enumerable.Average(ints), expectAvg, realAvg));
            }
            else if (typeof(T) == typeof(int?))
            {
                int?[] ints = new int?[count];
                double expectAvg = ((double)count - 1) / 2;

                for (int i = 0; i < ints.Length; i++) ints[i] = i;
                double realAvg = ints.AsParallel().Average().Value;

                if (!expectAvg.Equals(realAvg))
                    Assert.True(false, string.Format(testMethodFail + " > LINQ says: {0}, Expect: {0}, real: {1}", Enumerable.Average(ints), expectAvg, realAvg));
            }
            if (typeof(T) == typeof(long))
            {
                long[] longs = new long[count];
                double expectAvg = ((double)count - 1) / 2;

                for (int i = 0; i < longs.Length; i++) longs[i] = i;
                double realAvg = longs.AsParallel().Average();

                if (!expectAvg.Equals(realAvg))
                    Assert.True(false, string.Format(testMethodFail + " > LINQ says: {0}, Expect: {0}, real: {1}", Enumerable.Average(longs), expectAvg, realAvg));
            }
            else if (typeof(T) == typeof(long?))
            {
                long?[] longs = new long?[count];
                double expectAvg = ((double)count - 1) / 2;

                for (int i = 0; i < longs.Length; i++) longs[i] = i;
                double realAvg = longs.AsParallel().Average().Value;

                if (!expectAvg.Equals(realAvg))
                    Assert.True(false, string.Format(testMethodFail + " > LINQ says: {0}, Expect: {0}, real: {1}", Enumerable.Average(longs), expectAvg, realAvg));
            }
            else if (typeof(T) == typeof(float))
            {
                float[] floats = new float[count];
                double expectAvg = ((double)count - 1) / 2;

                for (int i = 0; i < floats.Length; i++) floats[i] = i;
                double realAvg = floats.AsParallel().Average();

                if (!expectAvg.Equals(realAvg))
                    Assert.True(false, string.Format(testMethodFail + " > LINQ says: {0}, Expect: {0}, real: {1}", Enumerable.Average(floats), expectAvg, realAvg));
            }
            else if (typeof(T) == typeof(float?))
            {
                float?[] floats = new float?[count];
                double expectAvg = ((double)count - 1) / 2;

                for (int i = 0; i < floats.Length; i++) floats[i] = i;
                double realAvg = floats.AsParallel().Average().Value;

                if (!expectAvg.Equals(realAvg))
                    Assert.True(false, string.Format(testMethodFail + " > LINQ says: {0}, Expect: {0}, real: {1}", Enumerable.Average(floats), expectAvg, realAvg));
            }
            else if (typeof(T) == typeof(double))
            {
                double[] doubles = new double[count];
                double expectAvg = ((double)count - 1) / 2;

                for (int i = 0; i < doubles.Length; i++) doubles[i] = i;
                double realAvg = doubles.AsParallel().Average();

                if (!expectAvg.Equals(realAvg))
                    Assert.True(false, string.Format(testMethodFail + " > LINQ says: {0}, Expect: {0}, real: {1}", Enumerable.Average(doubles), expectAvg, realAvg));
            }
            else if (typeof(T) == typeof(double?))
            {
                double?[] doubles = new double?[count];
                double expectAvg = ((double)count - 1) / 2;

                for (int i = 0; i < doubles.Length; i++) doubles[i] = i;
                double realAvg = doubles.AsParallel().Average().Value;

                if (!expectAvg.Equals(realAvg))
                    Assert.True(false, string.Format(testMethodFail + " > LINQ says: {0}, Expect: {0}, real: {1}", Enumerable.Average(doubles), expectAvg, realAvg));
            }
            else if (typeof(T) == typeof(decimal))
            {
                decimal[] decimals = new decimal[count];
                decimal expectAvg = ((decimal)count - 1) / 2;

                for (int i = 0; i < decimals.Length; i++) decimals[i] = i;
                decimal realAvg = decimals.AsParallel().Average();

                if (!expectAvg.Equals(realAvg))
                    Assert.True(false, string.Format(testMethodFail + " > LINQ says: {0}, Expect: {0}, real: {1}", Enumerable.Average(decimals), expectAvg, realAvg));
            }
            else if (typeof(T) == typeof(decimal?))
            {
                decimal?[] decimals = new decimal?[count];
                decimal expectAvg = ((decimal)count - 1) / 2;

                for (int i = 0; i < decimals.Length; i++) decimals[i] = i;
                decimal realAvg = decimals.AsParallel().Average().Value;

                if (!expectAvg.Equals(realAvg))
                    Assert.True(false, string.Format(testMethodFail + " > LINQ says: {0}, Expect: {0}, real: {1}", Enumerable.Average(decimals), expectAvg, realAvg));
            }
        }

        //
        // Min
        //
        [Fact]
        public static void RunMinTests()
        {
            Action<int, int>[] testActions = new Action<int, int>[]
            {
                (dataSize, minSlot) => RunMinTest1<int>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<int?>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<long>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<long?>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<float>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<float?>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<double>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<double?>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<decimal>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<decimal?>(dataSize, minSlot)
            };

            for (int i = 0; i < testActions.Length; i++)
            {
                if (i % 2 != 0)
                    testActions[i](0, 0);
                testActions[i](1, 0);
                testActions[i](2, 0);
                testActions[i](2, 1);
                testActions[i](1024 * 4, 1024 * 2 - 1);
            }
        }
        [Fact]
        public static void RunMinTests_Negative()
        {
            Action<int, int>[] testActions = new Action<int, int>[]
            {
                (dataSize, minSlot) => RunMinTest1<int>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<int?>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<long>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<long?>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<float>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<float?>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<double>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<double?>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<decimal>(dataSize, minSlot),
                (dataSize, minSlot) => RunMinTest1<decimal?>(dataSize, minSlot)
            };

            for (int i = 0; i < testActions.Length; i++)
            {
                if (i % 2 == 0)
                    Assert.Throws<InvalidOperationException>(() => testActions[i](0, 0));
            }
        }

        private static void RunMinTest1<T>(int dataSize, int minSlot)
        {
            string methodFailed = string.Format("RunMinTest1<{0}>(dataSize={1}, minSlot={2}):  FAILED.", typeof(T), dataSize, minSlot);
            if (typeof(T) == typeof(int))
            {
                const int minNum = -100;
                int[] ints = new int[dataSize];

                for (int i = 0; i < ints.Length; i++) ints[i] = i;
                if (dataSize > 0) ints[minSlot] = minNum;

                int min = ints.AsParallel().Min();
                if (!minNum.Equals(min))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", minNum, min));
            }
            else if (typeof(T) == typeof(int?))
            {
                int? minNum = dataSize == 0 ? (int?)null : -100;
                int?[] ints = new int?[dataSize];

                for (int i = 0; i < ints.Length; i++) ints[i] = i;
                if (dataSize > 0) ints[minSlot] = minNum;

                int? min = ints.AsParallel().Min();
                if (!minNum.Equals(min))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", minNum, min));
            }
            if (typeof(T) == typeof(long))
            {
                const long minNum = -100;
                long[] longs = new long[dataSize];

                for (int i = 0; i < longs.Length; i++) longs[i] = i;
                if (dataSize > 0) longs[minSlot] = minNum;

                long min = longs.AsParallel().Min();
                if (!minNum.Equals(min))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", minNum, min));
            }
            else if (typeof(T) == typeof(long?))
            {
                long? minNum = dataSize == 0 ? (long?)null : -100;
                long?[] longs = new long?[dataSize];

                for (int i = 0; i < longs.Length; i++) longs[i] = i;
                if (dataSize > 0) longs[minSlot] = minNum;

                long? min = longs.AsParallel().Min();
                if (!minNum.Equals(min))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", minNum, min));
            }
            if (typeof(T) == typeof(float))
            {
                const float minNum = -100;
                float[] floats = new float[dataSize];

                for (int i = 0; i < floats.Length; i++) floats[i] = i;
                if (dataSize > 0) floats[minSlot] = minNum;

                float min = floats.AsParallel().Min();
                if (!minNum.Equals(min))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", minNum, min));
            }
            else if (typeof(T) == typeof(float?))
            {
                float? minNum = dataSize == 0 ? (float?)null : -100;
                float?[] floats = new float?[dataSize];

                for (int i = 0; i < floats.Length; i++) floats[i] = i;
                if (dataSize > 0) floats[minSlot] = minNum;

                float? min = floats.AsParallel().Min();
                if (!minNum.Equals(min))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", minNum, min));
            }
            if (typeof(T) == typeof(double))
            {
                const double minNum = -100;
                double[] doubles = new double[dataSize];

                for (int i = 0; i < doubles.Length; i++) doubles[i] = i;
                if (dataSize > 0) doubles[minSlot] = minNum;

                double min = doubles.AsParallel().Min();
                if (!minNum.Equals(min))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", minNum, min));
            }
            else if (typeof(T) == typeof(double?))
            {
                double? minNum = dataSize == 0 ? (double?)null : -100;
                double?[] doubles = new double?[dataSize];

                for (int i = 0; i < doubles.Length; i++) doubles[i] = i;
                if (dataSize > 0) doubles[minSlot] = minNum;

                double? min = doubles.AsParallel().Min();
                if (!minNum.Equals(min))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", minNum, min));
            }
            if (typeof(T) == typeof(decimal))
            {
                const decimal minNum = -100;
                decimal[] decimals = new decimal[dataSize];

                for (int i = 0; i < decimals.Length; i++) decimals[i] = i;
                if (dataSize > 0) decimals[minSlot] = minNum;

                decimal min = decimals.AsParallel().Min();
                if (!minNum.Equals(min))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", minNum, min));
            }
            else if (typeof(T) == typeof(decimal?))
            {
                decimal? minNum = dataSize == 0 ? (decimal?)null : -100;
                decimal?[] decimals = new decimal?[dataSize];

                for (int i = 0; i < decimals.Length; i++) decimals[i] = i;
                if (dataSize > 0) decimals[minSlot] = minNum;

                decimal? min = decimals.AsParallel().Min();
                if (!minNum.Equals(min))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", minNum, min));
            }
        }

        //
        // Max
        //
        [Fact]
        public static void RunMaxTests()
        {
            Action<int, int>[] testActions = new Action<int, int>[]
            {
                (dataSize, maxSlot) => RunMaxTest1<int>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<int?>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<long>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<long?>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<float>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<float?>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<double>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<double?>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<decimal>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<decimal?>(dataSize, maxSlot)
            };

            for (int i = 0; i < testActions.Length; i++)
            {
                if (i % 2 != 0)
                    testActions[i](0, 0);
                testActions[i](1, 0);
                testActions[i](2, 0);
                testActions[i](2, 1);
                testActions[i](1024 * 4, 1024 * 2 - 1);
            }
        }

        [Fact]
        public static void RunMaxTests_Negative()
        {
            Action<int, int>[] testActions = new Action<int, int>[]
            {
                (dataSize, maxSlot) => RunMaxTest1<int>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<int?>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<long>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<long?>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<float>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<float?>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<double>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<double?>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<decimal>(dataSize, maxSlot),
                (dataSize, maxSlot) => RunMaxTest1<decimal?>(dataSize, maxSlot)
            };

            for (int i = 0; i < testActions.Length; i++)
            {
                if (i % 2 == 0)
                    Assert.Throws<InvalidOperationException>(() => testActions[i](0, 0));
            }
        }

        private static void RunMaxTest1<T>(int dataSize, int maxSlot)
        {
            string methodFailed = string.Format("RunMaxTest1<{0}>(dataSize={1}, maxSlot={2}):  FAILED.", typeof(T), dataSize, maxSlot);
            if (typeof(T) == typeof(int))
            {
                int maxNum = dataSize + 100;
                int[] ints = new int[dataSize];

                for (int i = 0; i < ints.Length; i++) ints[i] = i;
                if (dataSize > 0) ints[maxSlot] = maxNum;

                int max = ints.AsParallel().Max();

                if (!maxNum.Equals(max))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", maxNum, max));
            }
            if (typeof(T) == typeof(int?))
            {
                int? maxNum = dataSize == 0 ? (int?)null : dataSize + 100;
                int?[] ints = new int?[dataSize];

                for (int i = 0; i < ints.Length; i++) ints[i] = i;
                if (dataSize > 0) ints[maxSlot] = maxNum;

                int? max = ints.AsParallel().Max();
                if (!maxNum.Equals(max))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", maxNum, max));
            }
            if (typeof(T) == typeof(long))
            {
                long maxNum = dataSize + 100;
                long[] longs = new long[dataSize];

                for (int i = 0; i < longs.Length; i++) longs[i] = i;
                if (dataSize > 0) longs[maxSlot] = maxNum;

                long max = longs.AsParallel().Max();
                if (!maxNum.Equals(max))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", maxNum, max));
            }
            if (typeof(T) == typeof(long?))
            {
                long? maxNum = dataSize == 0 ? (long?)null : dataSize + 100;
                long?[] longs = new long?[dataSize];

                for (int i = 0; i < longs.Length; i++) longs[i] = i;
                if (dataSize > 0) longs[maxSlot] = maxNum;

                long? max = longs.AsParallel().Max();
                if (!maxNum.Equals(max))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", maxNum, max));
            }
            if (typeof(T) == typeof(float))
            {
                float maxNum = dataSize + 100;
                float[] floats = new float[dataSize];

                for (int i = 0; i < floats.Length; i++) floats[i] = i;
                if (dataSize > 0) floats[maxSlot] = maxNum;

                float max = floats.AsParallel().Max();
                if (!maxNum.Equals(max))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", maxNum, max));
            }
            if (typeof(T) == typeof(float?))
            {
                float? maxNum = dataSize == 0 ? (float?)null : dataSize + 100;
                float?[] floats = new float?[dataSize];

                for (int i = 0; i < floats.Length; i++) floats[i] = i;
                if (dataSize > 0) floats[maxSlot] = maxNum;

                float? max = floats.AsParallel().Max();
                if (!maxNum.Equals(max))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", maxNum, max));
            }
            if (typeof(T) == typeof(double))
            {
                double maxNum = dataSize + 100;
                double[] doubles = new double[dataSize];

                for (int i = 0; i < doubles.Length; i++) doubles[i] = i;
                if (dataSize > 0) doubles[maxSlot] = maxNum;

                double max = doubles.AsParallel().Max();
                if (!maxNum.Equals(max))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", maxNum, max));
            }
            if (typeof(T) == typeof(double?))
            {
                double? maxNum = dataSize == 0 ? (double?)null : dataSize + 100;
                double?[] doubles = new double?[dataSize];

                for (int i = 0; i < doubles.Length; i++) doubles[i] = i;
                if (dataSize > 0) doubles[maxSlot] = maxNum;

                double? max = doubles.AsParallel().Max();
                if (!maxNum.Equals(max))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", maxNum, max));
            }
            if (typeof(T) == typeof(decimal))
            {
                decimal maxNum = dataSize + 100;
                decimal[] decimals = new decimal[dataSize];

                for (int i = 0; i < decimals.Length; i++) decimals[i] = i;
                if (dataSize > 0) decimals[maxSlot] = maxNum;

                decimal max = decimals.AsParallel().Max();
                if (!maxNum.Equals(max))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", maxNum, max));
            }
            if (typeof(T) == typeof(decimal?))
            {
                decimal? maxNum = dataSize == 0 ? (decimal?)null : dataSize + 100;
                decimal?[] decimals = new decimal?[dataSize];

                for (int i = 0; i < decimals.Length; i++) decimals[i] = i;
                if (dataSize > 0) decimals[maxSlot] = maxNum;

                decimal? max = decimals.AsParallel().Max();
                if (!maxNum.Equals(max))
                    Assert.True(false, string.Format(methodFailed + "  > Expect: {0}, real: {1}", maxNum, max));
            }
        }
    }
}
