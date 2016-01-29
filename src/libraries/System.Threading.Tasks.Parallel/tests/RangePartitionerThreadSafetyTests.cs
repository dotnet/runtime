// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ThreadSafetyTests.cs
//
//
// Contains tests for ensuring thread safety of Range Partitioner:
//  - For different overload of Range Partitioner, calling from multiple threads
//    will not results in any exception, and will do the partition correctly
// 
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Concurrent;
using System.Collections.Generic;

using Xunit;

namespace System.Threading.Tasks.Tests
{
    public static class RangePartitionerThreadSafetyTests
    {
        /// <summary>
        /// Make sure that range Partitioner.Create can be called from multiple threads
        /// </summary>
        [Fact]
        public static void IntPartitionerThreadSafety()
        {
            ConcurrentBag<OrderablePartitioner<Tuple<int, int>>> bag = new ConcurrentBag<OrderablePartitioner<Tuple<int, int>>>();

            Parallel.Invoke(
                    () => bag.Add(Partitioner.Create(0, 1000)),
                    () => bag.Add(Partitioner.Create(1000, 2000)),
                    () => bag.Add(Partitioner.Create(2000, 3000)),
                    () => bag.Add(Partitioner.Create(3000, 4000)),
                    () => bag.Add(Partitioner.Create(4000, 5000)),
                    () => bag.Add(Partitioner.Create(5000, 6000)),
                    () => bag.Add(Partitioner.Create(6000, 7000)),
                    () => bag.Add(Partitioner.Create(7000, 8000)),
                    () => bag.Add(Partitioner.Create(8000, 9000))
            );

            foreach (var partitioner in bag)
            {
                // Test one of the GetPartitions* method to make sure that partitioner is in a good state
                //var elements = partitioner.GetDynamicPartitions().SelectMany(tuple => tuple.UnRoll()).ToArray();
                IList<int> elements = new List<int>();
                foreach (var tuple in partitioner.GetDynamicPartitions())
                {
                    foreach (var item in tuple.UnRoll())
                        elements.Add(item);
                }
                int from = elements[0];
                elements.CompareSequences<int>(RangePartitionerHelpers.IntEnumerable(from, from + 1000));
                from = from + 1000;
            }
        }

        /// <summary>
        /// Make sure that range Partitioner.Create(long overload) can be called from multiple threads
        /// </summary>
        [Fact]
        public static void LongPartitionerThreadSafety()
        {
            ConcurrentBag<OrderablePartitioner<Tuple<long, long>>> bag = new ConcurrentBag<OrderablePartitioner<Tuple<long, long>>>();

            Parallel.Invoke(
                    () => bag.Add(Partitioner.Create((long)0, (long)1000)),
                    () => bag.Add(Partitioner.Create((long)1000, (long)2000)),
                    () => bag.Add(Partitioner.Create((long)2000, (long)3000)),
                    () => bag.Add(Partitioner.Create((long)3000, (long)4000)),
                    () => bag.Add(Partitioner.Create((long)4000, (long)5000)),
                    () => bag.Add(Partitioner.Create((long)5000, (long)6000)),
                    () => bag.Add(Partitioner.Create((long)6000, (long)7000)),
                    () => bag.Add(Partitioner.Create((long)7000, (long)8000)),
                    () => bag.Add(Partitioner.Create((long)8000, (long)9000))
            );

            foreach (var partitioner in bag)
            {
                // Test one of the GetPartitions* method to make sure that partitioner is in a good state
                //var elements = partitioner.GetDynamicPartitions().SelectMany(tuple => tuple.UnRoll()).ToArray();
                IList<long> elements = new List<long>();
                foreach (var tuple in partitioner.GetDynamicPartitions())
                {
                    foreach (var item in tuple.UnRoll())
                        elements.Add(item);
                }
                long from = elements[0];
                elements.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(from, from + 1000));
            }
        }
    }

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
        /// Compares 2 enumerables and returns true if they contain the same elements
        /// in the same order. Similar to SequenceEqual but with extra diagnostic messages
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <returns></returns>
        public static void CompareSequences<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
        {
            using (var e1 = expected.GetEnumerator())
            using (var e2 = actual.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    // 'actual' ran out of elements before expected.
                    Assert.True(e2.MoveNext(), String.Format("Partitioner returned fewer elements. Next element expected: {0}", e1.Current));

                    Assert.Equal(e1.Current, e2.Current);
                }

                Assert.False(e2.MoveNext(), String.Format("Partitioner returned more elements. Next element returned by partitioner: {0}", e2.Current));
            }
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
