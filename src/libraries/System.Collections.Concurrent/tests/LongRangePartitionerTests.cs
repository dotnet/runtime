// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// LongRangePartitionerTests.cs - Tests for range partitioner for long integer range.
// 
// PLEASE NOTE !! - For tests that need to iterate the elements inside the partitions more 
// than once, we need to call GetPartitions for the second time. Iterating a second times
// over the first enumerable<tuples> / IList<IEnumerator<tuples> will yield no elements
//
// PLEASE NOTE!! - we use lazy evaluation wherever possible to allow for more than Int32.MaxValue
// elements. ToArray / toList will result in an OOM
//
// Taken from dev11 branch:
// \qa\clr\testsrc\pfx\Functional\Common\Partitioner\YetiTests\RangePartitioner\LongRangePartitionerTests.cs
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class LongRangePartitionerTests
    {
        /// <summary>
        /// Ensure that the partitioner returned has properties set correctly
        /// </summary>
        [Fact]
        public static void CheckKeyProperties()
        {
            var partitioner = Partitioner.Create(0, 9223372036854774807);
            Assert.True(partitioner.KeysOrderedInEachPartition, "Expected KeysOrderedInEachPartition to be set to true");
            Assert.False(partitioner.KeysOrderedAcrossPartitions, "KeysOrderedAcrossPartitions to be set to false");
            Assert.True(partitioner.KeysNormalized, "Expected KeysNormalized to be set to true");

            partitioner = Partitioner.Create(0, 9223372036854774807, 90);
            Assert.True(partitioner.KeysOrderedInEachPartition, "Expected KeysOrderedInEachPartition to be set to true");
            Assert.False(partitioner.KeysOrderedAcrossPartitions, "KeysOrderedAcrossPartitions to be set to false");
            Assert.True(partitioner.KeysNormalized, "Expected KeysNormalized to be set to true");
        }

        /// <summary>
        /// GetPartitions returns an IList<IEnumerator<Tuple<long, long>>
        /// We unroll the tuples and flatten them to a single sequence
        /// The single sequence is compared to the original range for verification
        /// </summary>
        [Fact]
        public static void CheckGetPartitions()
        {
            CheckGetPartitions(0, 1, 1);
            CheckGetPartitions(1, 1999, 3);
            CheckGetPartitions(2147473647, 9999, 4);
            CheckGetPartitions(2147484647, 1000, 8);
            CheckGetPartitions(-2147484647, 1000, 16);
            CheckGetPartitions(-1999, 5000, 63);
            CheckGetPartitions(9223372036854774807, 999, 13); // close to Int64.Max
        }

        public static void CheckGetPartitions(long from, long count, int dop)
        {
            long to = from + count;
            var partitioner = Partitioner.Create(from, to);

            //var elements = partitioner.GetPartitions(dop).SelectMany(enumerator => enumerator.UnRoll());
            IList<long> elements = new List<long>();
            foreach (var partition in partitioner.GetPartitions(dop))
            {
                foreach (var item in partition.UnRoll())
                    elements.Add(item);
            }
            Assert.True(elements.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(from, to)), "GetPartitions element mismatch");
        }

        /// <summary>
        /// CheckGetDynamicPartitions returns an IEnumerable<Tuple<long, long>>
        /// We unroll the tuples and flatten them to a single sequence
        /// The single sequence is compared to the original range for verification
        /// </summary>
        [Fact]
        public static void CheckGetDynamicPartitions()
        {
            CheckGetDynamicPartitions(0, 1);
            CheckGetDynamicPartitions(1, 1999);
            CheckGetDynamicPartitions(2147473647, 9999);
            CheckGetDynamicPartitions(2147484647, 1000);
            CheckGetDynamicPartitions(-2147484647, 1000);
            CheckGetDynamicPartitions(-1999, 5000);
            CheckGetDynamicPartitions(9223372036854774807, 999); // close to Int64.Max
        }

        public static void CheckGetDynamicPartitions(long from, long count)
        {
            long to = from + count;
            var partitioner = Partitioner.Create(from, to);

            //var elements = partitioner.GetDynamicPartitions().SelectMany(tuple => tuple.UnRoll());
            IList<long> elements = new List<long>();
            foreach (var partition in partitioner.GetDynamicPartitions())
            {
                foreach (var item in partition.UnRoll())
                    elements.Add(item);
            }
            Assert.True(elements.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(from, to)), "GetDynamicPartitions Element mismatch");
        }

        /// <summary>
        /// GetOrderablePartitions returns an IList<IEnumerator<KeyValuePair<long, Tuple<long, long>>>
        /// We unroll the tuples and flatten them to a single sequence
        /// The single sequence is compared to the original range for verification
        /// Also the indices are extracted to ensure that they are ordered & normalized
        /// </summary>
        [Fact]
        public static void CheckGetOrderablePartitions()
        {
            CheckGetOrderablePartitions(0, 1, 1);
            CheckGetOrderablePartitions(1, 1999, 3);
            CheckGetOrderablePartitions(2147473647, 9999, 4);
            CheckGetOrderablePartitions(2147484647, 1000, 8);
            CheckGetOrderablePartitions(-2147484647, 1000, 16);
            CheckGetOrderablePartitions(-1999, 5000, 63);
            CheckGetOrderablePartitions(9223372036854774807, 999, 13); // close to Int64.Max
        }

        public static void CheckGetOrderablePartitions(long from, long count, int dop)
        {
            long to = from + count;
            var partitioner = Partitioner.Create(from, to);

            //var elements = partitioner.GetOrderablePartitions(dop).SelectMany(enumerator => enumerator.UnRoll());
            IList<long> elements = new List<long>();
            foreach (var partition in partitioner.GetPartitions(dop))
            {
                foreach (var item in partition.UnRoll())
                    elements.Add(item);
            }

            Assert.True(elements.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(from, to)), "GetOrderablePartitions Element mismatch");

            //var keys = partitioner.GetOrderablePartitions(dop).SelectMany(enumerator => enumerator.UnRollIndices()).ToArray();
            IList<long> keys = new List<long>();
            foreach (var partition in partitioner.GetOrderablePartitions(dop))
            {
                foreach (var item in partition.UnRollIndices())
                    keys.Add(item);
            }
            Assert.True(keys.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(keys[0], keys.Count)), "GetOrderablePartitions key mismatch");
        }

        /// <summary>
        /// GetOrderableDynamicPartitions returns an IEnumerable<KeyValuePair<long, Tuple<long, long>>
        /// We unroll the tuples and flatten them to a single sequence
        /// The single sequence is compared to the original range for verification
        /// Also the indices are extracted to ensure that they are ordered & normalized
        /// </summary>
        /// <param name="from"></param>
        /// <param name="count"></param>
        [Fact]
        public static void GetOrderableDynamicPartitions()
        {
            GetOrderableDynamicPartitions(0, 1);
            GetOrderableDynamicPartitions(1, 1999);
            GetOrderableDynamicPartitions(2147473647, 9999);
            GetOrderableDynamicPartitions(2147484647, 1000);
            GetOrderableDynamicPartitions(-2147484647, 1000);
            GetOrderableDynamicPartitions(-1999, 5000);
            GetOrderableDynamicPartitions(9223372036854774807, 999); // close to Int64.Max
        }

        public static void GetOrderableDynamicPartitions(long from, long count)
        {
            long to = from + count;
            var partitioner = Partitioner.Create(from, to);

            //var elements = partitioner.GetOrderableDynamicPartitions().SelectMany(tuple => tuple.UnRoll());
            IList<long> elements = new List<long>();
            foreach (var partition in partitioner.GetOrderableDynamicPartitions())
            {
                foreach (var item in partition.UnRoll())
                    elements.Add(item);
            }
            Assert.True(elements.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(from, to)), "GetOrderableDynamicPartitions Element mismatch");

            //var keys = partitioner.GetOrderableDynamicPartitions().Select(tuple => tuple.Key).ToArray();
            IList<long> keys = new List<long>();
            foreach (var tuple in partitioner.GetOrderableDynamicPartitions())
            {
                keys.Add(tuple.Key);
            }
            Assert.True(keys.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(keys[0], keys.Count)), "GetOrderableDynamicPartitions key mismatch");
        }

        /// <summary>
        /// GetPartitions returns an IList<IEnumerator<Tuple<long, long>>
        /// We unroll the tuples and flatten them to a single sequence
        /// The single sequence is compared to the original range for verification
        /// This method tests the partitioner created with user provided desiredRangeSize
        /// The range sizes for individual ranges are checked to see if they are equal to 
        /// desiredRangeSize. The last range may have less than or equal to desiredRangeSize.
        /// </summary>
        [Fact]
        public static void CheckGetPartitionsWithRange()
        {
            CheckGetPartitionsWithRange(1999, 1000, 20, 1);
            CheckGetPartitionsWithRange(-1999, 1000, 100, 2);
            CheckGetPartitionsWithRange(1999, 1, 2000, 3);
            CheckGetPartitionsWithRange(9223372036854774807, 999, 600, 4);
            CheckGetPartitionsWithRange(-9223372036854774807, 1000, 19, 63);
        }

        public static void CheckGetPartitionsWithRange(long from, long count, long desiredRangeSize, int dop)
        {
            long to = from + count;
            var partitioner = Partitioner.Create(from, to, desiredRangeSize);

            //var elements = partitioner.GetPartitions(dop).SelectMany(enumerator => enumerator.UnRoll());
            IList<long> elements = new List<long>();
            foreach (var partition in partitioner.GetPartitions(dop))
            {
                foreach (var item in partition.UnRoll())
                    elements.Add(item);
            }
            Assert.True(elements.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(from, to)), "GetPartitions element mismatch");

            //var rangeSizes = partitioner.GetPartitions(dop).SelectMany(enumerator => enumerator.GetRangeSize()).ToArray();
            IList<long> rangeSizes = new List<long>();
            foreach (var partition in partitioner.GetPartitions(dop))
            {
                foreach (var item in partition.GetRangeSize())
                    rangeSizes.Add(item);
            }
            ValidateRangeSize(desiredRangeSize, rangeSizes);
        }

        /// <summary>
        /// CheckGetDynamicPartitionsWithRange returns an IEnumerable<Tuple<long, long>>
        /// We unroll the tuples and flatten them to a single sequence
        /// The single sequence is compared to the original range for verification
        /// This method tests the partitioner created with user provided desiredRangeSize
        /// The range sizes for individual ranges are checked to see if they are equal to 
        /// desiredRangeSize. The last range may have less than or equal to desiredRangeSize.
        /// </summary>
        [Fact]
        public static void CheckGetDynamicPartitionsWithRange()
        {
            CheckGetDynamicPartitionsWithRange(1999, 1000, 20);
            CheckGetDynamicPartitionsWithRange(-1999, 1000, 100);
            CheckGetDynamicPartitionsWithRange(1999, 1, 2000);
            CheckGetDynamicPartitionsWithRange(9223372036854774807, 999, 600);
            CheckGetDynamicPartitionsWithRange(-9223372036854774807, 1000, 19);
        }

        public static void CheckGetDynamicPartitionsWithRange(long from, long count, long desiredRangeSize)
        {
            long to = from + count;
            var partitioner = Partitioner.Create(from, to, desiredRangeSize);

            //var elements = partitioner.GetDynamicPartitions().SelectMany(tuple => tuple.UnRoll());
            IList<long> elements = new List<long>();
            foreach (var partition in partitioner.GetDynamicPartitions())
            {
                foreach (var item in partition.UnRoll())
                    elements.Add(item);
            }
            Assert.True(elements.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(from, to)), "GetDynamicPartitions Element mismatch");

            //var rangeSizes = partitioner.GetDynamicPartitions().Select(tuple => tuple.GetRangeSize()).ToArray();
            IList<long> rangeSizes = new List<long>();
            foreach (var partition in partitioner.GetDynamicPartitions())
            {
                rangeSizes.Add(partition.GetRangeSize());
            }
            ValidateRangeSize(desiredRangeSize, rangeSizes);
        }

        /// <summary>
        /// GetOrderablePartitions returns an IList<IEnumerator<KeyValuePair<long, Tuple<long, long>>>
        /// We unroll the tuples and flatten them to a single sequence
        /// The single sequence is compared to the original range for verification
        /// Also the indices are extracted to ensure that they are ordered & normalized
        /// This method tests the partitioner created with user provided desiredRangeSize
        /// The range sizes for individual ranges are checked to see if they are equal to 
        /// desiredRangeSize. The last range may have less than or equal to desiredRangeSize.
        /// </summary>
        [Fact]
        public static void CheckGetOrderablePartitionsWithRange()
        {
            CheckGetOrderablePartitionsWithRange(1999, 1000, 20, 1);
            CheckGetOrderablePartitionsWithRange(-1999, 1000, 100, 2);
            CheckGetOrderablePartitionsWithRange(1999, 1, 2000, 3);
            CheckGetOrderablePartitionsWithRange(9223372036854774807, 999, 600, 4);
            CheckGetOrderablePartitionsWithRange(-9223372036854774807, 1000, 19, 63);
        }

        public static void CheckGetOrderablePartitionsWithRange(long from, long count, long desiredRangeSize, int dop)
        {
            long to = from + count;
            var partitioner = Partitioner.Create(from, to, desiredRangeSize);

            //var elements = partitioner.GetOrderablePartitions(dop).SelectMany(enumerator => enumerator.UnRoll());
            IList<long> elements = new List<long>();
            foreach (var partition in partitioner.GetOrderablePartitions(dop))
            {
                foreach (var item in partition.UnRoll())
                    elements.Add(item);
            }
            Assert.True(elements.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(from, to)), "GetOrderablePartitions Element mismatch");

            //var keys = partitioner.GetOrderablePartitions(dop).SelectMany(enumerator => enumerator.UnRollIndices()).ToArray();
            IList<long> keys = new List<long>();
            foreach (var partition in partitioner.GetOrderablePartitions(dop))
            {
                foreach (var item in partition.UnRollIndices())
                    keys.Add(item);
            }
            Assert.True(keys.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(keys[0], keys.Count)), "GetOrderablePartitions key mismatch");

            //var rangeSizes = partitioner.GetOrderablePartitions(dop).SelectMany(enumerator => enumerator.GetRangeSize()).ToArray();
            IList<long> rangeSizes = new List<long>();
            foreach (var partition in partitioner.GetOrderablePartitions(dop))
            {
                foreach (var item in partition.GetRangeSize())
                    rangeSizes.Add(item);
            }

            ValidateRangeSize(desiredRangeSize, rangeSizes);
        }

        /// <summary>
        /// GetOrderableDynamicPartitions returns an IEnumerable<KeyValuePair<long, Tuple<long, long>>
        /// We unroll the tuples and flatten them to a single sequence
        /// The single sequence is compared to the original range for verification
        /// Also the indices are extracted to ensure that they are ordered & normalized
        /// This method tests the partitioner created with user provided desiredRangeSize
        /// The range sizes for individual ranges are checked to see if they are equal to 
        /// desiredRangeSize. The last range may have less than or equal to desiredRangeSize.
        /// </summary>
        [Fact]
        public static void GetOrderableDynamicPartitionsWithRange()
        {
            GetOrderableDynamicPartitionsWithRange(1999, 1000, 20);
            GetOrderableDynamicPartitionsWithRange(-1999, 1000, 100);
            GetOrderableDynamicPartitionsWithRange(1999, 1, 2000);
            GetOrderableDynamicPartitionsWithRange(9223372036854774807, 999, 600);
            GetOrderableDynamicPartitionsWithRange(-9223372036854774807, 1000, 19);
        }

        public static void GetOrderableDynamicPartitionsWithRange(long from, long count, long desiredRangeSize)
        {
            long to = from + count;
            var partitioner = Partitioner.Create(from, to, desiredRangeSize);

            //var elements = partitioner.GetOrderableDynamicPartitions().SelectMany(tuple => tuple.UnRoll());
            IList<long> elements = new List<long>();
            foreach (var tuple in partitioner.GetOrderableDynamicPartitions())
            {
                foreach (var item in tuple.UnRoll())
                    elements.Add(item);
            }
            Assert.True(elements.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(from, to)), "GetOrderableDynamicPartitions Element mismatch");

            //var keys = partitioner.GetOrderableDynamicPartitions().Select(tuple => tuple.Key).ToArray();
            IList<long> keys = new List<long>();
            foreach (var tuple in partitioner.GetOrderableDynamicPartitions())
            {
                keys.Add(tuple.Key);
            }

            Assert.True(keys.CompareSequences<long>(RangePartitionerHelpers.LongEnumerable(keys[0], keys.Count)), "GetOrderableDynamicPartitions key mismatch");

            //var rangeSizes = partitioner.GetOrderableDynamicPartitions().Select(tuple => tuple.GetRangeSize()).ToArray();
            IList<long> rangeSizes = new List<long>();
            foreach (var partition in partitioner.GetOrderableDynamicPartitions())
            {
                rangeSizes.Add(partition.GetRangeSize());
            }
            ValidateRangeSize(desiredRangeSize, rangeSizes);
        }

        /// <summary>
        /// Helper function to validate the range size of the partitioners match what the user specified 
        /// (desiredRangeSize).
        /// The last range may have less than or equal to desiredRangeSize.
        /// </summary>
        /// <param name="desiredRangeSize"></param>
        /// <param name="rangeSizes"></param>
        /// <returns></returns>
        public static void ValidateRangeSize(long desiredRangeSize, IList<long> rangeSizes)
        {
            //var rangesWithDifferentRangeSize = rangeSizes.Take(rangeSizes.Length - 1).Where(r => r != desiredRangeSize).ToArray();
            IList<long> rangesWithDifferentRangeSize = new List<long>();
            // ensuring that every range, size from the last one is the same.
            int numToTake = rangeSizes.Count - 1;
            for (int i = 0; i < numToTake; i++)
            {
                long range = rangeSizes[i];
                if (range != desiredRangeSize)
                    rangesWithDifferentRangeSize.Add(range);
            }
            Assert.Equal(0, rangesWithDifferentRangeSize.Count);
            Assert.InRange(rangeSizes[rangeSizes.Count - 1], 0, desiredRangeSize);
        }

        /// <summary>
        /// Ensure that the range partitioner doesn't chunk up elements i.e. uses chunk size = 1
        /// </summary>
        [Fact]
        public static void RangePartitionerChunking()
        {
            RangePartitionerChunking(2147473647, 9999, 4);
            RangePartitionerChunking(2147484647, 1000, -1);
        }

        public static void RangePartitionerChunking(long from, long count, long rangeSize)
        {
            long to = from + count;

            var partitioner = (rangeSize == -1) ? Partitioner.Create(from, to) : Partitioner.Create(from, to, rangeSize);

            // Check static partitions
            var partitions = partitioner.GetPartitions(2);

            // Initialize the from / to values from the first element
            if (!partitions[0].MoveNext()) return;
            Assert.Equal(from, partitions[0].Current.Item1);
            if (rangeSize == -1)
            {
                rangeSize = partitions[0].Current.Item2 - partitions[0].Current.Item1;
            }

            long nextExpectedFrom = partitions[0].Current.Item2;
            long nextExpectedTo = (nextExpectedFrom + rangeSize) > to ? to : (nextExpectedFrom + rangeSize);

            // Ensure that each partition gets one range only
            // we check this by alternating partitions asking for elements and make sure
            // that we get ranges in a sequence. If chunking were to happen then we wouldn't see a sequence
            long actualCount = partitions[0].Current.Item2 - partitions[0].Current.Item1;
            while (true)
            {
                if (!partitions[0].MoveNext()) break;

                Assert.Equal(nextExpectedFrom, partitions[0].Current.Item1);
                Assert.Equal(nextExpectedTo, partitions[0].Current.Item2);
                nextExpectedFrom = (nextExpectedFrom + rangeSize) > to ? to : (nextExpectedFrom + rangeSize);
                nextExpectedTo = (nextExpectedTo + rangeSize) > to ? to : (nextExpectedTo + rangeSize);
                actualCount += partitions[0].Current.Item2 - partitions[0].Current.Item1;

                if (!partitions[1].MoveNext()) break;

                Assert.Equal(nextExpectedFrom, partitions[1].Current.Item1);
                Assert.Equal(nextExpectedTo, partitions[1].Current.Item2);
                nextExpectedFrom = (nextExpectedFrom + rangeSize) > to ? to : (nextExpectedFrom + rangeSize);
                nextExpectedTo = (nextExpectedTo + rangeSize) > to ? to : (nextExpectedTo + rangeSize);
                actualCount += partitions[1].Current.Item2 - partitions[1].Current.Item1;

                if (!partitions[1].MoveNext()) break;

                Assert.Equal(nextExpectedFrom, partitions[1].Current.Item1);
                Assert.Equal(nextExpectedTo, partitions[1].Current.Item2);
                nextExpectedFrom = (nextExpectedFrom + rangeSize) > to ? to : (nextExpectedFrom + rangeSize);
                nextExpectedTo = (nextExpectedTo + rangeSize) > to ? to : (nextExpectedTo + rangeSize);
                actualCount += partitions[1].Current.Item2 - partitions[1].Current.Item1;

                if (!partitions[0].MoveNext()) break;

                Assert.Equal(nextExpectedFrom, partitions[0].Current.Item1);
                Assert.Equal(nextExpectedTo, partitions[0].Current.Item2);
                nextExpectedFrom = (nextExpectedFrom + rangeSize) > to ? to : (nextExpectedFrom + rangeSize);
                nextExpectedTo = (nextExpectedTo + rangeSize) > to ? to : (nextExpectedTo + rangeSize);
                actualCount += partitions[0].Current.Item2 - partitions[0].Current.Item1;
            }

            // Verifying that all items are there
            Assert.Equal(count, actualCount);
        }

        /// <summary>
        /// Ensure that the range partitioner doesn't chunk up elements i.e. uses chunk size = 1
        /// </summary>
        [Fact]
        public static void RangePartitionerDynamicChunking()
        {
            RangePartitionerDynamicChunking(2147473647, 9999, 4);
            RangePartitionerDynamicChunking(2147484647, 1000, -1);
        }

        public static void RangePartitionerDynamicChunking(long from, long count, long rangeSize)
        {
            long to = from + count;

            var partitioner = (rangeSize == -1) ? Partitioner.Create(from, to) : Partitioner.Create(from, to, rangeSize);

            // Check static partitions
            var partitions = partitioner.GetDynamicPartitions();
            var partition1 = partitions.GetEnumerator();
            var partition2 = partitions.GetEnumerator();

            // Initialize the from / to values from the first element
            if (!partition1.MoveNext()) return;
            Assert.Equal(from, partition1.Current.Item1);
            if (rangeSize == -1)
            {
                rangeSize = partition1.Current.Item2 - partition1.Current.Item1;
            }

            long nextExpectedFrom = partition1.Current.Item2;
            long nextExpectedTo = (nextExpectedFrom + rangeSize) > to ? to : (nextExpectedFrom + rangeSize);

            // Ensure that each partition gets one range only
            // we check this by alternating partitions asking for elements and make sure
            // that we get ranges in a sequence. If chunking were to happen then we wouldn't see a sequence
            long actualCount = partition1.Current.Item2 - partition1.Current.Item1;
            while (true)
            {
                if (!partition1.MoveNext()) break;

                Assert.Equal(nextExpectedFrom, partition1.Current.Item1);
                Assert.Equal(nextExpectedTo, partition1.Current.Item2);
                nextExpectedFrom = (nextExpectedFrom + rangeSize) > to ? to : (nextExpectedFrom + rangeSize);
                nextExpectedTo = (nextExpectedTo + rangeSize) > to ? to : (nextExpectedTo + rangeSize);
                actualCount += partition1.Current.Item2 - partition1.Current.Item1;

                if (!partition2.MoveNext()) break;

                Assert.Equal(nextExpectedFrom, partition2.Current.Item1);
                Assert.Equal(nextExpectedTo, partition2.Current.Item2);
                nextExpectedFrom = (nextExpectedFrom + rangeSize) > to ? to : (nextExpectedFrom + rangeSize);
                nextExpectedTo = (nextExpectedTo + rangeSize) > to ? to : (nextExpectedTo + rangeSize);
                actualCount += partition2.Current.Item2 - partition2.Current.Item1;

                if (!partition2.MoveNext()) break;

                Assert.Equal(nextExpectedFrom, partition2.Current.Item1);
                Assert.Equal(nextExpectedTo, partition2.Current.Item2);
                nextExpectedFrom = (nextExpectedFrom + rangeSize) > to ? to : (nextExpectedFrom + rangeSize);
                nextExpectedTo = (nextExpectedTo + rangeSize) > to ? to : (nextExpectedTo + rangeSize);
                actualCount += partition2.Current.Item2 - partition2.Current.Item1;

                if (!partition1.MoveNext()) break;

                Assert.Equal(nextExpectedFrom, partition1.Current.Item1);
                Assert.Equal(nextExpectedTo, partition1.Current.Item2);
                nextExpectedFrom = (nextExpectedFrom + rangeSize) > to ? to : (nextExpectedFrom + rangeSize);
                nextExpectedTo = (nextExpectedTo + rangeSize) > to ? to : (nextExpectedTo + rangeSize);
                actualCount += partition1.Current.Item2 - partition1.Current.Item1;
            }

            // Verifying that all items are there
            Assert.Equal(count, actualCount);
        }
    }
}
