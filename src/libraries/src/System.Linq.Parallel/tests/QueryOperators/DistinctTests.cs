// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class DistinctTests
    {
        private const int DuplicateFactor = 4;

        public static IEnumerable<object[]> DistinctUnorderedData(int[] counts)
        {
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>().Select(x => x * DuplicateFactor)))
            {
                yield return new object[] { results[0], ((int)results[1]) / DuplicateFactor };
            }
        }

        public static IEnumerable<object[]> DistinctData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.Cast<int>().Select(x => x * DuplicateFactor)))
            {
                yield return new object[] { results[0], ((int)results[1]) / DuplicateFactor };
            }
        }

        public static IEnumerable<object[]> DistinctSourceMultipleData(int[] counts)
        {
            foreach (int count in counts.Cast<int>())
            {
                yield return new object[] { Enumerable.Range(0, count * DuplicateFactor).Select(x => x % count).ToArray().AsParallel().AsOrdered(), count };
            }
        }

        //
        // Distinct
        //
        [Theory]
        [MemberData("DistinctUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Distinct_Unordered(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (int i in query.Distinct(new ModularCongruenceComparer(count)))
            {
                seen.Add(i % count);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("DistinctUnorderedData", (object)(new int[] { 1024 * 4, 1024 * 128 }))]
        public static void Distinct_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Distinct_Unordered(labeled, count);
        }

        [Theory]
        [MemberData("DistinctData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Distinct(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (int i in query.Distinct(new ModularCongruenceComparer(count)))
            {
                Assert.Equal(seen++, i % count);
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("DistinctData", (object)(new int[] { 1024 * 4, 1024 * 128 }))]
        public static void Distinct_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Distinct(labeled, count);
        }

        [Theory]
        [MemberData("DistinctUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Distinct_Unordered_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(query.Distinct(new ModularCongruenceComparer(count)).ToList(), x => seen.Add(x % count));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("DistinctUnorderedData", (object)(new int[] { 1024 * 4, 1024 * 128 }))]
        public static void Distinct_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Distinct_Unordered_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("DistinctData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Distinct_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.Distinct(new ModularCongruenceComparer(count)).ToList(), x => Assert.Equal(seen++, x % count));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("DistinctData", (object)(new int[] { 1024 * 4, 1024 * 128 }))]
        public static void Distinct_NotPiplined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Distinct_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("DistinctSourceMultipleData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Distinct_Unordered_SourceMultiple(ParallelQuery<int> query, int count)
        {
            // The difference between this test and the previous, is that it's not possible to
            // get non-unique results from ParallelEnumerable.Range()...
            // Those tests either need modification of source (via .Select(x => x / DuplicateFactor) or similar,
            // or via a comparator that considers some elements equal.
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(query.AsUnordered().Distinct(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("DistinctSourceMultipleData", (object)(new int[] { 1024 * 4, 1024 * 128 }))]
        public static void Distinct_Unordered_SourceMultiple_Longrunning(ParallelQuery<int> query, int count)
        {
            Distinct_Unordered_SourceMultiple(query, count);
        }

        [Theory]
        [MemberData("DistinctSourceMultipleData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Distinct_SourceMultiple(ParallelQuery<int> query, int count)
        {
            int seen = 0;
            Assert.All(query.Distinct(), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("DistinctSourceMultipleData", (object)(new int[] { 1024 * 4, 1024 * 128 }))]
        public static void Distinct_SourceMultiple_Longrunning(ParallelQuery<int> query, int count)
        {
            Distinct_SourceMultiple(query, count);
        }

        [Fact]
        public static void Distinct_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).Distinct());
        }
    }
}
