// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Test
{
    public class ElementAtPartitionerTests
    {
        //
        // ElementAt and ElementAtOrDefault
        //

        [Fact]
        public static void RunElementAtTest1()
        {
            RunElementAtTest1Core(1024, 512);
            RunElementAtTest1Core(0, 512);
            RunElementAtTest1Core(1, 512);
            RunElementAtTest1Core(1024, 1024);
            RunElementAtTest1Core(1024 * 1024, 1024);
        }

        [Fact]
        public static void RunElementAtOrDefaultTest1()
        {
            RunElementAtOrDefaultTest1Core(1024, 512);
            RunElementAtOrDefaultTest1Core(0, 512);
            RunElementAtOrDefaultTest1Core(1, 512);
            RunElementAtOrDefaultTest1Core(1024, 1024);
            RunElementAtOrDefaultTest1Core(1024 * 1024, 1024);
        }

        private static void RunElementAtTest1Core(int size, int elementAt)
        {
            string methodInfo = string.Format("RunElementAtTest1(size={0}, elementAt={1})", size, elementAt);

            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            bool expectExcept = elementAt >= size;
            try
            {
                int q = ints.AsParallel().ElementAt(elementAt);

                if (expectExcept)
                {
                    Assert.True(false, string.Format(methodInfo + "  > Failure: Expected an exception, but didn't get one"));
                }
                else
                {
                    if (q != ints[elementAt])
                    {
                        Assert.True(false, string.Format(methodInfo + "  > FAILED.  Expected return value of {0}, saw {1} instead", ints[elementAt], q));
                    }
                }
            }
            catch (ArgumentOutOfRangeException ioex)
            {
                if (!expectExcept)
                {
                    Assert.True(false, string.Format(methodInfo + "  > Failure: Got exception, but didn't expect it  {0}", ioex));
                }
            }
        }

        private static void RunElementAtOrDefaultTest1Core(int size, int elementAt)
        {
            string methodInfo = string.Format("RunElementAtOrDefaultTest1(size={0}, elementAt={1})", size, elementAt);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            int q = ints.AsParallel().ElementAtOrDefault(elementAt);

            int expectValue = (elementAt >= size) ? default(int) : ints[elementAt];
            if (q != expectValue)
            {
                Assert.True(false, string.Format(methodInfo + "  > Expected return value of {0}, saw {1} instead", expectValue, q));
            }
        }

        //
        // Custom Partitioner tests
        //

        [Fact]
        public static void RunPartitionerTest1()
        {
            RunPartitionerTest1Core(0);
            RunPartitionerTest1Core(1);
            RunPartitionerTest1Core(999);
            RunPartitionerTest1Core(1024);
        }

        [Fact]
        public static void RunOrderablePartitionerTest1()
        {
            RunOrderablePartitionerTest1Core(0, true, true);
            RunOrderablePartitionerTest1Core(1, true, true);
            RunOrderablePartitionerTest1Core(999, true, true);

            RunOrderablePartitionerTest1Core(1024, true, true);
            RunOrderablePartitionerTest1Core(1024, false, true);
            RunOrderablePartitionerTest1Core(1024, true, false);
            RunOrderablePartitionerTest1Core(1024, false, false);
        }

        private static void RunPartitionerTest1Core(int size)
        {
            string methodInfo = string.Format("RunPartitionerTest1(size={0})", size);
            int[] arr = Enumerable.Range(0, size).ToArray();
            Partitioner<int> partitioner = new ListPartitioner<int>(arr);

            // Without ordering:
            int[] res = partitioner.AsParallel().Select(x => -x).ToArray().Select(x => -x).OrderBy(x => x).ToArray();
            if (!res.OrderBy(i => i).SequenceEqual(arr))
            {
                Assert.True(false, string.Format(methodInfo + "  > Failure: Incorrect output {0}", String.Join(" ", res.Select(x => x.ToString()).ToArray())));
            }

            // With ordering, expect an exception:
            bool gotException = false;
            try
            {
                partitioner.AsParallel().AsOrdered().Select(x => -x).ToArray();
            }
            catch (InvalidOperationException)
            {
                gotException = true;
            }

            if (!gotException)
            {
                Assert.True(false, string.Format(methodInfo + "  > Failure: Expected an exception, but didn't get one"));
            }
        }

        private static void RunOrderablePartitionerTest1Core(int size, bool keysIncreasingInEachPartition, bool keysNormalized)
        {
            string methodInfo = string.Format("RunOrderablePartitionerTest1(size={0}, keysIncreasingInEachPartition={1}, keysNormalized={2})",
                size, keysIncreasingInEachPartition, keysNormalized);
            int[] arr = Enumerable.Range(0, size).ToArray();
            Partitioner<int> partitioner = new OrderableListPartitioner<int>(arr, keysIncreasingInEachPartition, keysNormalized);

            // Without ordering:
            int[] res = partitioner.AsParallel().Select(x => -x).ToArray().Select(x => -x).OrderBy(x => x).ToArray();
            if (!res.OrderBy(i => i).SequenceEqual(arr))
            {
                Assert.True(false, string.Format(methodInfo + "  > Failure: Incorrect output"));
            }

            // With ordering:
            int[] resOrdered = partitioner.AsParallel().AsOrdered().Select(x => -x).ToArray().Select(x => -x).ToArray();
            if (!resOrdered.SequenceEqual(arr))
            {
                Assert.True(false, string.Format(methodInfo + "  > Failure: Incorrect output"));
            }
        }

        //
        // This query used to assert because PLINQ would call MoveNext twice on an enumerator from the partitioner.
        // This issue should now be fixed from both ends - PLINQ should not call MoveNext twice, and PartitionerStatic should not assert
        // on an extra MoveNext.
        //
        [Fact]
        public static void RunPartitionerTest_Min()
        {
            try
            {
                Partitioner.Create(Enumerable.Range(0, 1))
                    .AsParallel()
                    .Min();
            }
            catch (Exception ex)
            {
                Assert.True(false, string.Format("RunPartitionerTest_Min:  FAILED. Exception thrown: " + ex));
            }
        }

        // An unordered partitioner for lists, used by the partitioner tests.
        private class ListPartitioner<TSource> : Partitioner<TSource>
        {
            private OrderablePartitioner<TSource> _partitioner;

            public ListPartitioner(IList<TSource> source)
            {
                _partitioner = new OrderableListPartitioner<TSource>(source, true, true);
            }

            public override IList<IEnumerator<TSource>> GetPartitions(int partitionCount)
            {
                return _partitioner.GetPartitions(partitionCount);
            }

            public override IEnumerable<TSource> GetDynamicPartitions()
            {
                return _partitioner.GetDynamicPartitions();
            }
        }

        //
        // An orderable partitioner for lists, used by the partitioner tests
        //
        private class OrderableListPartitioner<TSource> : OrderablePartitioner<TSource>
        {
            private readonly IList<TSource> _input;
            private readonly bool _keysOrderedInEachPartition;
            private readonly bool _keysNormalized;

            public OrderableListPartitioner(IList<TSource> input, bool keysOrderedInEachPartition, bool keysNormalized)
                : base(keysOrderedInEachPartition, false, keysNormalized)
            {
                _input = input;
                _keysOrderedInEachPartition = keysOrderedInEachPartition;
                _keysNormalized = keysNormalized;
            }

            public override IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
            {
                IEnumerable<KeyValuePair<long, TSource>> dynamicPartitions = GetOrderableDynamicPartitions();
                IEnumerator<KeyValuePair<long, TSource>>[] partitions = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];

                for (int i = 0; i < partitionCount; i++)
                {
                    partitions[i] = dynamicPartitions.GetEnumerator();
                }
                return partitions;
            }

            public override IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
            {
                return new ListDynamicPartitions(_input, _keysOrderedInEachPartition, _keysNormalized);
            }

            private class ListDynamicPartitions : IEnumerable<KeyValuePair<long, TSource>>
            {
                private IList<TSource> _input;
                private int _pos = 0;
                private bool _keysOrderedInEachPartition;
                private bool _keysNormalized;

                internal ListDynamicPartitions(IList<TSource> input, bool keysOrderedInEachPartition, bool keysNormalized)
                {
                    _input = input;
                    _keysOrderedInEachPartition = keysOrderedInEachPartition;
                    _keysNormalized = keysNormalized;
                }

                public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
                {
                    while (true)
                    {
                        int elemIndex = Interlocked.Increment(ref _pos) - 1;

                        if (elemIndex >= _input.Count)
                        {
                            yield break;
                        }

                        if (!_keysOrderedInEachPartition)
                        {
                            elemIndex = _input.Count - 1 - elemIndex;
                        }

                        long key = _keysNormalized ? elemIndex : (elemIndex * 2);

                        yield return new KeyValuePair<long, TSource>(key, _input[elemIndex]);
                    }
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((IEnumerable<KeyValuePair<long, TSource>>)this).GetEnumerator();
                }
            }
        }
    }
}
