// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// PartitionerHelpers.cs
//
// Helper class containing extensions methods to unroll ranges into individual elements, 
// find the size of ranges for different returns value of Partitioners
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public static class RangePartitionerHelpers
    {
        /// <summary>
        /// Helpers to extract individual elements from Long range partitioner
        /// </summary>
        /// <param name="tuple"></param>
        /// <returns></returns>
        public static IEnumerable<long> UnRoll(this Tuple<long, long> tuple)
        {
            for (long i = tuple.Item1; i < tuple.Item2; i++)
                yield return i;
        }

        /// <summary>
        /// Helpers to extract individual elements from Long range partitioner
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        public static IEnumerable<long> UnRoll(this KeyValuePair<long, Tuple<long, long>> pair)
        {
            return pair.Value.UnRoll();
        }

        /// <summary>
        /// Helpers to extract individual elements from Long range partitioner
        /// </summary>
        /// <param name="tupleEnumerator"></param>
        /// <returns></returns>
        public static IEnumerable<long> UnRoll(this IEnumerator<Tuple<long, long>> tupleEnumerator)
        {
            while (tupleEnumerator.MoveNext())
            {
                for (long i = tupleEnumerator.Current.Item1; i < tupleEnumerator.Current.Item2; i++)
                    yield return i;
            }
        }

        /// <summary>
        /// Helpers to extract individual elements from Long range partitioner
        /// </summary>
        /// <param name="pairEnumerator"></param>
        /// <returns></returns>
        public static IEnumerable<long> UnRoll(this IEnumerator<KeyValuePair<long, Tuple<long, long>>> pairEnumerator)
        {
            long key = -1;
            while (pairEnumerator.MoveNext())
            {
                // Ensure that keys are normalized
                if (key != -1)
                {
                    Assert.True(
                        key.Equals(pairEnumerator.Current.Key - 1),
                        String.Format("Keys are not normalized {0} {1}", key, pairEnumerator.Current.Key));
                }
                key = pairEnumerator.Current.Key;
                for (long i = pairEnumerator.Current.Value.Item1; i < pairEnumerator.Current.Value.Item2; i++)
                    yield return i;
            }
        }

        /// <summary>
        /// Helper to extract individual indices from Long range partitioner
        /// </summary>
        /// <param name="pairEnumerator"></param>
        /// <returns></returns>
        public static IEnumerable<long> UnRollIndices(this IEnumerator<KeyValuePair<long, Tuple<long, long>>> pairEnumerator)
        {
            long key = -1;
            while (pairEnumerator.MoveNext())
            {
                // Ensure that keys are normalized
                if (key != -1)
                {
                    Assert.True(
                        key.Equals(pairEnumerator.Current.Key - 1),
                        String.Format("Keys are not normalized {0} {1}", key, pairEnumerator.Current.Key));
                }
                key = pairEnumerator.Current.Key;
                var tuple = pairEnumerator.Current.Value;
                yield return key;
            }
        }

        /// <summary>
        /// Helpers to extract individual range sizes from Long range partitioner
        /// </summary>
        /// <param name="tuple"></param>
        /// <returns></returns>
        public static long GetRangeSize(this Tuple<long, long> tuple)
        {
            long rangeSize = 0;
            for (long i = tuple.Item1; i < tuple.Item2; i++)
            {
                rangeSize++;
            }
            return rangeSize;
        }

        /// <summary>
        /// Helpers to extract individual range sizes from Long range partitioner
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        public static long GetRangeSize(this KeyValuePair<long, Tuple<long, long>> pair)
        {
            return GetRangeSize(pair.Value);
        }

        /// <summary>
        /// Helpers to extract individual range sizes from Long range partitioner
        /// </summary>
        /// <param name="tupleEnumerator"></param>
        /// <returns></returns>
        public static IEnumerable<long> GetRangeSize(this IEnumerator<Tuple<long, long>> tupleEnumerator)
        {
            while (tupleEnumerator.MoveNext())
            {
                yield return GetRangeSize(tupleEnumerator.Current);
            }
        }

        /// <summary>
        /// Helpers to extract individual range sizes from Long range partitioner
        /// </summary>
        /// <param name="pairEnumerator"></param>
        /// <returns></returns>
        public static IEnumerable<long> GetRangeSize(this IEnumerator<KeyValuePair<long, Tuple<long, long>>> pairEnumerator)
        {
            while (pairEnumerator.MoveNext())
            {
                yield return GetRangeSize(pairEnumerator.Current.Value);
            }
        }

        /// <summary>
        /// Helpers to extract individual elements from int range partitioner
        /// </summary>
        /// <param name="tuple"></param>
        /// <returns></returns>
        public static IEnumerable<int> UnRoll(this Tuple<int, int> tuple)
        {
            for (int i = tuple.Item1; i < tuple.Item2; i++)
                yield return i;
        }

        /// <summary>
        /// Helpers to extract individual elements from int range partitioner
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        public static IEnumerable<int> UnRoll(this KeyValuePair<long, Tuple<int, int>> pair)
        {
            return pair.Value.UnRoll();
        }

        /// <summary>
        /// Helpers to extract individual elements from int range partitioner
        /// </summary>
        /// <param name="tupleEnumerator"></param>
        /// <returns></returns>
        public static IEnumerable<int> UnRoll(this IEnumerator<Tuple<int, int>> tupleEnumerator)
        {
            while (tupleEnumerator.MoveNext())
            {
                for (int i = tupleEnumerator.Current.Item1; i < tupleEnumerator.Current.Item2; i++)
                    yield return i;
            }
        }

        /// <summary>
        /// Helpers to extract individual elements from int range partitioner
        /// </summary>
        /// <param name="pairEnumerator"></param>
        /// <returns></returns>
        public static IEnumerable<int> UnRoll(this IEnumerator<KeyValuePair<long, Tuple<int, int>>> pairEnumerator)
        {
            long key = -1;
            while (pairEnumerator.MoveNext())
            {
                // Ensure that keys are normalized
                if (key != -1)
                {
                    Assert.True(
                        key.Equals(pairEnumerator.Current.Key - 1),
                        String.Format("Keys are not normalized {0} {1}", key, pairEnumerator.Current.Key));
                }
                key = pairEnumerator.Current.Key;
                for (int i = pairEnumerator.Current.Value.Item1; i < pairEnumerator.Current.Value.Item2; i++)
                    yield return i;
            }
        }

        /// <summary>
        /// Helpers to extract individual indices from int range partitioner
        /// </summary>
        /// <param name="pairEnumerator"></param>
        /// <returns></returns>
        public static IEnumerable<long> UnRollIndices(this IEnumerator<KeyValuePair<long, Tuple<int, int>>> pairEnumerator)
        {
            long key = -1;
            while (pairEnumerator.MoveNext())
            {
                // Ensure that keys are normalized
                if (key != -1)
                {
                    Assert.True(
                        key.Equals(pairEnumerator.Current.Key - 1),
                        String.Format("Keys are not normalized {0} {1}", key, pairEnumerator.Current.Key));
                }
                key = pairEnumerator.Current.Key;
                var tuple = pairEnumerator.Current.Value;
                yield return key;
            }
        }

        /// <summary>
        /// Helpers to extract individual range sizes from int range partitioner
        /// </summary>
        /// <param name="tuple"></param>
        /// <returns></returns>
        public static int GetRangeSize(this Tuple<int, int> tuple)
        {
            int rangeSize = 0;
            for (int i = tuple.Item1; i < tuple.Item2; i++)
            {
                rangeSize++;
            }
            return rangeSize;
        }

        /// <summary>
        /// Helpers to extract individual range sizes from int range partitioner
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        public static int GetRangeSize(this KeyValuePair<long, Tuple<int, int>> pair)
        {
            return GetRangeSize(pair.Value);
        }

        /// <summary>
        /// Helpers to extract individual range sizes from int range partitioner
        /// </summary>
        /// <param name="tupleEnumerator"></param>
        /// <returns></returns>
        public static IEnumerable<int> GetRangeSize(this IEnumerator<Tuple<int, int>> tupleEnumerator)
        {
            while (tupleEnumerator.MoveNext())
            {
                yield return GetRangeSize(tupleEnumerator.Current);
            }
        }

        /// <summary>
        /// Helpers to extract individual range sizes from int range partitioner
        /// </summary>
        /// <param name="pairEnumerator"></param>
        /// <returns></returns>
        public static IEnumerable<int> GetRangeSize(this IEnumerator<KeyValuePair<long, Tuple<int, int>>> pairEnumerator)
        {
            while (pairEnumerator.MoveNext())
            {
                yield return GetRangeSize(pairEnumerator.Current.Value);
            }
        }

        /// <summary>
        /// Compares 2 enumerables and returns true if they contain the same elements
        /// in the same order. Similar to SequenceEqual but with extra diagnostic messages
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <returns></returns>
        public static bool CompareSequences<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
        {
            using (var e1 = expected.GetEnumerator())
            using (var e2 = actual.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    // 'actual' ran out of elements before expected.
                    if (!e2.MoveNext())
                    {
                        Console.WriteLine("Partitioner returned fewer elements. Next element expected: {0}", e1.Current);
                        return false;
                    }

                    if (!e1.Current.Equals(e2.Current))
                    {
                        Console.WriteLine("Mismatching elements. Expected: {0}, Actual: {1}", e1.Current, e2.Current);
                        return false;
                    }
                }

                // 'actual' still has elements
                if (e2.MoveNext())
                {
                    Console.WriteLine("Partitioner returned more elements. Next element returned by partitioner: {0}", e2.Current);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Helper to yield an enumerable of long
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static IEnumerable<long> LongEnumerable(long from, long to)
        {
            for (long i = from; i < to; i++)
                yield return i;
        }

        /// <summary>
        /// Helper to yield an enumerable of ints
        /// used instead of Enumerable.Range since it doesn't support negative values
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static IEnumerable<int> IntEnumerable(int from, int to)
        {
            for (int i = from; i < to; i++)
                yield return i;
        }
    }
}
