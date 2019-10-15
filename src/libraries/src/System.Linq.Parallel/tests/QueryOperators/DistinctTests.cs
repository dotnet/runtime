// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class DistinctTests
    {
        private const int DuplicateFactor = 4;

        public static IEnumerable<object[]> DistinctUnorderedData(int[] counts)
        {
            foreach (int count in counts)
            {
                yield return new object[] { count * DuplicateFactor, count };
            }
        }

        public static IEnumerable<object[]> DistinctData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.Select(x => x * DuplicateFactor).DefaultIfEmpty(Sources.OuterLoopCount)))
            {
                yield return new object[] { results[0], ((int)results[1]) / DuplicateFactor };
            }
        }

        public static IEnumerable<object[]> DistinctSourceMultipleData(int[] counts)
        {
            foreach (int count in counts.DefaultIfEmpty(Sources.OuterLoopCount))
            {
                int[] source = Enumerable.Range(0, count * DuplicateFactor).Select(x => x % count).ToArray();

                yield return new object[] { Labeled.Label("Array", source.AsParallel().AsOrdered()), count };
                yield return new object[] { Labeled.Label("List", source.ToList().AsParallel().AsOrdered()), count };
                yield return new object[] { Labeled.Label("Enumerable", source.AsEnumerable().AsParallel().AsOrdered()), count };
            }
        }

        //
        // Distinct
        //
        [Theory]
        [MemberData(nameof(DistinctUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void Distinct_Unordered(int count, int uniqueCount)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, uniqueCount);
            foreach (int i in UnorderedSources.Default(count).Distinct(new ModularCongruenceComparer(uniqueCount)))
            {
                seen.Add(i % uniqueCount);
            }
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Distinct_Unordered_Longrunning()
        {
            Distinct_Unordered(Sources.OuterLoopCount, Sources.OuterLoopCount / DuplicateFactor);
        }

        [Theory]
        [MemberData(nameof(DistinctData), new[] { 0, 1, 2, 16 })]
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
        [MemberData(nameof(DistinctData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Distinct_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Distinct(labeled, count);
        }

        [Theory]
        [MemberData(nameof(DistinctUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void Distinct_Unordered_NotPipelined(int count, int uniqueCount)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, uniqueCount);
            Assert.All(UnorderedSources.Default(count).Distinct(new ModularCongruenceComparer(uniqueCount)).ToList(), x => seen.Add(x % uniqueCount));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Distinct_Unordered_NotPipelined_Longrunning()
        {
            Distinct_Unordered_NotPipelined(Sources.OuterLoopCount, Sources.OuterLoopCount / DuplicateFactor);
        }

        [Theory]
        [MemberData(nameof(DistinctData), new[] { 0, 1, 2, 16 })]
        public static void Distinct_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.Distinct(new ModularCongruenceComparer(count)).ToList(), x => Assert.Equal(seen++, x % count));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(DistinctData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Distinct_NotPiplined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Distinct_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(DistinctSourceMultipleData), new[] { 0, 1, 2, 16 })]
        public static void Distinct_Unordered_SourceMultiple(Labeled<ParallelQuery<int>> labeled, int count)
        {
            // The difference between this test and the previous, is that it's not possible to
            // get non-unique results from ParallelEnumerable.Range()...
            // Those tests either need modification of source (via .Select(x => x / DuplicateFactor) or similar,
            // or via a comparator that considers some elements equal.
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(labeled.Item.AsUnordered().Distinct(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(DistinctSourceMultipleData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Distinct_Unordered_SourceMultiple_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Distinct_Unordered_SourceMultiple(labeled, count);
        }

        [Theory]
        [MemberData(nameof(DistinctSourceMultipleData), new[] { 0, 1, 2, 16 })]
        public static void Distinct_SourceMultiple(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int seen = 0;
            Assert.All(labeled.Item.Distinct(), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(DistinctSourceMultipleData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Distinct_SourceMultiple_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Distinct_SourceMultiple(labeled, count);
        }

        [Fact]
        public static void Distinct_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).Distinct());
        }
    }
}
