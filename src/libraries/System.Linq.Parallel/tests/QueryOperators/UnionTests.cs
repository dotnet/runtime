// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class UnionTests
    {
        private const int DuplicateFactor = 8;

        // Get two ranges, with the right starting where the left ends
        public static IEnumerable<object[]> UnionUnorderedCountData(int[] counts)
        {
            foreach (int left in counts)
            {
                foreach (int right in counts)
                {
                    yield return new object[] { left, right };
                }
            }
        }

        private static IEnumerable<object[]> UnionUnorderedData(int[] counts)
        {
            foreach (object[] parms in UnorderedSources.BinaryRanges(counts, (l, r) => l, counts))
            {
                yield return parms.Take(4).ToArray();
            }
        }

        // Union returns only the ordered portion ordered.
        // Get two ranges, both ordered.
        public static IEnumerable<object[]> UnionData(int[] counts)
        {
            foreach (object[] parms in UnionUnorderedData(counts))
            {
                yield return new object[] { ((Labeled<ParallelQuery<int>>)parms[0]).Order(), parms[1], ((Labeled<ParallelQuery<int>>)parms[2]).Order(), parms[3] };
            }
        }

        // Get two ranges, with only the left being ordered.
        public static IEnumerable<object[]> UnionFirstOrderedData(int[] counts)
        {
            foreach (object[] parms in UnionUnorderedData(counts))
            {
                yield return new object[] { ((Labeled<ParallelQuery<int>>)parms[0]).Order(), parms[1], parms[2], parms[3] };
            }
        }

        // Get two ranges, with only the right being ordered.
        public static IEnumerable<object[]> UnionSecondOrderedData(int[] counts)
        {
            foreach (object[] parms in UnionUnorderedData(counts))
            {
                yield return new object[] { parms[0], parms[1], ((Labeled<ParallelQuery<int>>)parms[2]).Order(), parms[3] };
            }
        }

        // Get two ranges, both sourced from arrays, with duplicate items in each array.
        // Used in distinctness tests, in contrast to relying on a Select predicate to generate duplicate items.
        public static IEnumerable<object[]> UnionSourceMultipleData(int[] counts)
        {
            foreach (int leftCount in counts)
            {
                ParallelQuery<int> left = Enumerable.Range(0, leftCount * DuplicateFactor).Select(x => x % leftCount).ToArray().AsParallel();
                foreach (int rightCount in new[] { 0, 1, Math.Max(DuplicateFactor, leftCount / 2), Math.Max(DuplicateFactor, leftCount) }.Distinct())
                {
                    int rightStart = leftCount - Math.Min(leftCount, rightCount) / 2;
                    ParallelQuery<int> right = Enumerable.Range(0, rightCount * DuplicateFactor).Select(x => x % rightCount + rightStart).ToArray().AsParallel();
                    yield return new object[] { left, leftCount, right, rightCount, Math.Max(leftCount, rightCount) + (Math.Min(leftCount, rightCount) + 1) / 2 };
                }
            }
        }

        //
        // Union
        //
        [Theory]
        [MemberData(nameof(UnionUnorderedCountData), new[] { 0, 1, 2, 16 })]
        public static void Union_Unordered(int leftCount, int rightCount)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(leftCount, rightCount);
            IntegerRangeSet seen = new IntegerRangeSet(0, leftCount + rightCount);
            foreach (int i in leftQuery.Union(rightQuery))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Union_Unordered_Longrunning()
        {
            Union_Unordered(Sources.OuterLoopCount, Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(UnionData), new[] { 0, 1, 2, 16 })]
        public static void Union(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seen = 0;
            foreach (int i in leftQuery.Union(rightQuery))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(leftCount + rightCount, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnionData), new[] { 512, 1024 * 8 })]
        public static void Union_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Union(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData(nameof(UnionFirstOrderedData), new[] { 0, 1, 2, 16 })]
        public static void Union_FirstOrdered(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seenUnordered = new IntegerRangeSet(leftCount, rightCount);
            int seen = 0;
            foreach (int i in leftQuery.Union(rightQuery))
            {
                if (i < leftCount)
                {
                    Assert.Equal(seen++, i);
                }
                else
                {
                    seenUnordered.Add(i);
                }
            }
            Assert.Equal(leftCount, seen);
            seenUnordered.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnionFirstOrderedData), new[] { 512, 1024 * 8 })]
        public static void Union_FirstOrdered_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Union_FirstOrdered(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData(nameof(UnionSecondOrderedData), new[] { 0, 1, 2, 16 })]
        public static void Union_SecondOrdered(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seenUnordered = new IntegerRangeSet(0, leftCount);
            int seen = leftCount;
            foreach (int i in leftQuery.Union(rightQuery))
            {
                if (i >= leftCount)
                {
                    Assert.Equal(seen++, i);
                }
                else
                {
                    seenUnordered.Add(i);
                }
            }
            Assert.Equal(leftCount + rightCount, seen);
            seenUnordered.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnionSecondOrderedData), new[] { 512, 1024 * 8 })]
        public static void Union_SecondOrdered_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Union_SecondOrdered(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData(nameof(UnionUnorderedCountData), new[] { 0, 1, 2, 16 })]
        public static void Union_Unordered_NotPipelined(int leftCount, int rightCount)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(leftCount, rightCount);
            IntegerRangeSet seen = new IntegerRangeSet(0, leftCount + rightCount);
            Assert.All(leftQuery.Union(rightQuery).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Union_Unordered_NotPipelined_Longrunning()
        {
            Union_Unordered_NotPipelined(Sources.OuterLoopCount, Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(UnionData), new[] { 0, 1, 2, 16 })]
        public static void Union_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seen = 0;
            Assert.All(leftQuery.Union(rightQuery).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(leftCount + rightCount, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnionData), new[] { 512, 1024 * 8 })]
        public static void Union_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Union_NotPipelined(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData(nameof(UnionFirstOrderedData), new[] { 0, 1, 2, 16 })]
        public static void Union_FirstOrdered_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seenUnordered = new IntegerRangeSet(leftCount, rightCount);
            int seen = 0;
            Assert.All(leftQuery.Union(rightQuery).ToList(), x =>
            {
                if (x < leftCount) Assert.Equal(seen++, x);
                else seenUnordered.Add(x);
            });
            Assert.Equal(leftCount, seen);
            seenUnordered.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnionFirstOrderedData), new[] { 512, 1024 * 8 })]
        public static void Union_FirstOrdered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Union_FirstOrdered_NotPipelined(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData(nameof(UnionSecondOrderedData), new[] { 0, 1, 2, 16 })]
        public static void Union_SecondOrdered_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seenUnordered = new IntegerRangeSet(0, leftCount);
            int seen = leftCount;
            Assert.All(leftQuery.Union(rightQuery).ToList(), x =>
            {
                if (x >= leftCount) Assert.Equal(seen++, x);
                else seenUnordered.Add(x);
            });
            Assert.Equal(leftCount + rightCount, seen);
            seenUnordered.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnionSecondOrderedData), new[] { 512, 1024 * 8 })]
        public static void Union_SecondOrdered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Union_SecondOrdered_NotPipelined(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData(nameof(UnionUnorderedCountData), new[] { 0, 1, 2, 16 })]
        public static void Union_Unordered_Distinct(int leftCount, int rightCount)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(leftCount, rightCount);
            leftCount = Math.Min(DuplicateFactor, leftCount);
            rightCount = Math.Min(DuplicateFactor, rightCount);
            int offset = leftCount - Math.Min(leftCount, rightCount) / 2;
            int expectedCount = Math.Max(leftCount, rightCount) + (Math.Min(leftCount, rightCount) + 1) / 2;
            IntegerRangeSet seen = new IntegerRangeSet(0, expectedCount);
            foreach (int i in leftQuery.Select(x => x % DuplicateFactor).Union(rightQuery.Select(x => (x - leftCount) % DuplicateFactor + offset), new ModularCongruenceComparer(DuplicateFactor + DuplicateFactor / 2)))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Union_Unordered_Distinct_Longrunning()
        {
            Union_Unordered_Distinct(Sources.OuterLoopCount, Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(UnionData), new[] { 0, 1, 2, 16 })]
        public static void Union_Distinct(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            leftCount = Math.Min(DuplicateFactor, leftCount);
            rightCount = Math.Min(DuplicateFactor, rightCount);
            int offset = leftCount - Math.Min(leftCount, rightCount) / 2;
            int expectedCount = Math.Max(leftCount, rightCount) + (Math.Min(leftCount, rightCount) + 1) / 2;
            int seen = 0;
            foreach (int i in leftQuery.Select(x => x % DuplicateFactor).Union(rightQuery.Select(x => (x - leftCount) % DuplicateFactor + offset), new ModularCongruenceComparer(DuplicateFactor + DuplicateFactor / 2)))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(expectedCount, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnionData), new[] { 512, 1024 * 8 })]
        public static void Union_Distinct_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Union_Distinct(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData(nameof(UnionUnorderedCountData), new[] { 0, 1, 2, 16 })]
        public static void Union_Unordered_Distinct_NotPipelined(int leftCount, int rightCount)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(leftCount, rightCount);
            leftCount = Math.Min(DuplicateFactor, leftCount);
            rightCount = Math.Min(DuplicateFactor, rightCount);
            int offset = leftCount - Math.Min(leftCount, rightCount) / 2;
            int expectedCount = Math.Max(leftCount, rightCount) + (Math.Min(leftCount, rightCount) + 1) / 2;
            IntegerRangeSet seen = new IntegerRangeSet(0, expectedCount);
            Assert.All(leftQuery.Select(x => x % DuplicateFactor).Union(rightQuery.Select(x => (x - leftCount) % DuplicateFactor + offset), new ModularCongruenceComparer(DuplicateFactor + DuplicateFactor / 2)).ToList(),
                x => seen.Add(x));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Union_Unordered_Distinct_NotPipelined_Longrunning()
        {
            Union_Unordered_Distinct_NotPipelined(Sources.OuterLoopCount, Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(UnionData), new[] { 0, 1, 2, DuplicateFactor * 2 })]
        public static void Union_Distinct_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            leftCount = Math.Min(DuplicateFactor, leftCount);
            rightCount = Math.Min(DuplicateFactor, rightCount);
            int offset = leftCount - Math.Min(leftCount, rightCount) / 2;
            int expectedCount = Math.Max(leftCount, rightCount) + (Math.Min(leftCount, rightCount) + 1) / 2;
            int seen = 0;

            Assert.All(leftQuery.Select(x => x % DuplicateFactor).Union(rightQuery.Select(x => (x - leftCount) % DuplicateFactor + offset), new ModularCongruenceComparer(DuplicateFactor + DuplicateFactor / 2)).ToList(),
              x => Assert.Equal(seen++, x));
            Assert.Equal(expectedCount, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnionData), new[] { 512, 1024 * 8 })]
        public static void Union_Distinct_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Union_Distinct_NotPipelined(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData(nameof(UnionSourceMultipleData), new[] { 0, 1, 2, DuplicateFactor * 2 })]
        public static void Union_Unordered_SourceMultiple(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int count)
        {
            // The difference between this test and the previous, is that it's not possible to
            // get non-unique results from ParallelEnumerable.Range()...
            // Those tests either need modification of source (via .Select(x => x / DuplicateFactor) or similar,
            // or via a comparator that considers some elements equal.
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(leftQuery.Union(rightQuery), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnionSourceMultipleData), new[] { 512, 1024 * 8 })]
        public static void Union_Unordered_SourceMultiple_Longrunning(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int count)
        {
            Union_Unordered_SourceMultiple(leftQuery, leftCount, rightQuery, rightCount, count);
        }

        [Theory]
        [MemberData(nameof(UnionSourceMultipleData), new[] { 0, 1, 2, DuplicateFactor * 2 })]
        public static void Union_SourceMultiple(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int count)
        {
            int seen = 0;
            Assert.All(leftQuery.AsOrdered().Union(rightQuery.AsOrdered()), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnionSourceMultipleData), new[] { 512, 1024 * 8 })]
        public static void Union_SourceMultiple_Longrunning(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int count)
        {
            Union_SourceMultiple(leftQuery, leftCount, rightQuery, rightCount, count);
        }

        [Fact]
        public static void Union_NotSupportedException()
        {
#pragma warning disable 618
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).Union(Enumerable.Range(0, 1)));
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).Union(Enumerable.Range(0, 1), null));
#pragma warning restore 618
        }

        [Fact]
        // Should not get the same setting from both operands.
        public static void Union_NoDuplicateSettings()
        {
            CancellationToken t = new CancellationTokenSource().Token;
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithCancellation(t).Union(ParallelEnumerable.Range(0, 1).WithCancellation(t)));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1).Union(ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1)));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default).Union(ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default)));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default).Union(ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default)));
        }

        [Fact]
        public static void Union_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("first", () => ((ParallelQuery<int>)null).Union(ParallelEnumerable.Range(0, 1)));
            AssertExtensions.Throws<ArgumentNullException>("second", () => ParallelEnumerable.Range(0, 1).Union(null));

            AssertExtensions.Throws<ArgumentNullException>("first", () => ((ParallelQuery<int>)null).Union(ParallelEnumerable.Range(0, 1), EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("second", () => ParallelEnumerable.Range(0, 1).Union(null, EqualityComparer<int>.Default));
        }
    }
}
