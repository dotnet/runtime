// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class ExceptTests
    {
        private const int DuplicateFactor = 4;

        private static IEnumerable<int> RightCounts(int leftCount)
        {
            int upperBound = Math.Max(DuplicateFactor, leftCount);
            return new[] { 0, 1, upperBound, upperBound * 2 }.Distinct();
        }

        public static IEnumerable<object[]> ExceptUnorderedData(int[] leftCounts)
        {
            foreach (int leftCount in leftCounts.DefaultIfEmpty(Sources.OuterLoopCount / 4))
            {
                foreach (int rightCount in RightCounts(leftCount))
                {
                    int rightStart = 0 - rightCount / 2;
                    yield return new object[] { leftCount, rightStart, rightCount, rightStart + rightCount, Math.Max(0, leftCount - (rightCount + 1) / 2) };
                }
            }
        }

        public static IEnumerable<object[]> ExceptData(int[] leftCounts)
        {
            foreach (int leftCount in leftCounts.DefaultIfEmpty(Sources.OuterLoopCount / 4))
            {
                foreach (int rightCount in RightCounts(leftCount))
                {
                    int rightStart = 0 - rightCount / 2;
                    foreach (object[] left in Sources.Ranges(new[] { leftCount }))
                    {
                        yield return left.Concat(new object[] { UnorderedSources.Default(rightStart, rightCount), rightCount, rightStart + rightCount, Math.Max(0, leftCount - (rightCount + 1) / 2) }).ToArray();
                    }
                }
            }
        }

        public static IEnumerable<object[]> ExceptSourceMultipleData(int[] counts)
        {
            foreach (int leftCount in counts.DefaultIfEmpty(Sources.OuterLoopCount / DuplicateFactor / 2))
            {
                ParallelQuery<int> left = Enumerable.Range(0, leftCount * DuplicateFactor).Select(x => x % leftCount).ToArray().AsParallel().AsOrdered();
                foreach (int rightCount in RightCounts(leftCount))
                {
                    int rightStart = 0 - rightCount / 2;
                    yield return new object[] { left, leftCount, UnorderedSources.Default(rightStart, rightCount), rightCount, rightStart + rightCount, Math.Max(0, leftCount - (rightCount + 1) / 2) };
                }
            }
        }

        //
        // Except
        //
        [Theory]
        [MemberData(nameof(ExceptUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void Except_Unordered(int leftCount, int rightStart, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(rightStart, rightCount);
            IntegerRangeSet seen = new IntegerRangeSet(start, count);
            foreach (int i in leftQuery.Except(rightQuery))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ExceptUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Except_Unordered_Longrunning(int leftCount, int rightStart, int rightCount, int start, int count)
        {
            Except_Unordered(leftCount, rightStart, rightCount, start, count);
        }

        [Theory]
        [MemberData(nameof(ExceptData), new[] { 0, 1, 2, 16 })]
        public static void Except(Labeled<ParallelQuery<int>> left, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            int seen = start;
            foreach (int i in leftQuery.Except(rightQuery))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(count + start, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ExceptData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Except_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            Except(left, leftCount, rightQuery, rightCount, start, count);
        }

        [Theory]
        [MemberData(nameof(ExceptUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void Except_Unordered_NotPipelined(int leftCount, int rightStart, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(rightStart, rightCount);
            IntegerRangeSet seen = new IntegerRangeSet(start, count);
            Assert.All(leftQuery.Except(rightQuery).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ExceptUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Except_Unordered_NotPipelined_Longrunning(int leftCount, int rightStart, int rightCount, int start, int count)
        {
            Except_Unordered_NotPipelined(leftCount, rightStart, rightCount, start, count);
        }

        [Theory]
        [MemberData(nameof(ExceptData), new[] { 0, 1, 2, 16 })]
        public static void Except_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            int seen = start;
            Assert.All(leftQuery.Except(rightQuery).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(count + start, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ExceptData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Except_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            Except_NotPipelined(left, leftCount, rightQuery, rightCount, start, count);
        }

        [Theory]
        [MemberData(nameof(ExceptUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void Except_Unordered_Distinct(int leftCount, int rightStart, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(rightStart, rightCount);
            leftCount = Math.Min(DuplicateFactor * 2, leftCount);
            rightCount = Math.Min(DuplicateFactor, (rightCount + 1) / 2);
            int expectedCount = Math.Max(0, leftCount - rightCount);
            IntegerRangeSet seen = new IntegerRangeSet(leftCount - expectedCount, expectedCount);
            foreach (int i in leftQuery.Except(rightQuery.Select(x => Math.Abs(x) % DuplicateFactor), new ModularCongruenceComparer(DuplicateFactor * 2)))
            {
                seen.Add(i % (DuplicateFactor * 2));
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ExceptUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Except_Unordered_Distinct_Longrunning(int leftCount, int rightStart, int rightCount, int start, int count)
        {
            Except_Unordered_Distinct(leftCount, rightStart, rightCount, start, count);
        }

        [Theory]
        [MemberData(nameof(ExceptData), new[] { 0, 1, 2, 16 })]
        public static void Except_Distinct(Labeled<ParallelQuery<int>> left, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            leftCount = Math.Min(DuplicateFactor * 2, leftCount);
            rightCount = Math.Min(DuplicateFactor, (rightCount + 1) / 2);
            int expectedCount = Math.Max(0, leftCount - rightCount);
            int seen = expectedCount == 0 ? 0 : leftCount - expectedCount;
            foreach (int i in leftQuery.Except(rightQuery.Select(x => Math.Abs(x) % DuplicateFactor), new ModularCongruenceComparer(DuplicateFactor * 2)))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(expectedCount == 0 ? 0 : leftCount, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ExceptData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Except_Distinct_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            Except_Distinct(left, leftCount, rightQuery, rightCount, start, count);
        }

        [Theory]
        [MemberData(nameof(ExceptUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void Except_Unordered_Distinct_NotPipelined(int leftCount, int rightStart, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(rightStart, rightCount);
            leftCount = Math.Min(DuplicateFactor * 2, leftCount);
            rightCount = Math.Min(DuplicateFactor, (rightCount + 1) / 2);
            int expectedCount = Math.Max(0, leftCount - rightCount);
            IntegerRangeSet seen = new IntegerRangeSet(leftCount - expectedCount, expectedCount);
            Assert.All(leftQuery.Except(rightQuery.Select(x => Math.Abs(x) % DuplicateFactor),
                new ModularCongruenceComparer(DuplicateFactor * 2)).ToList(), x => seen.Add(x % (DuplicateFactor * 2)));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ExceptUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Except_Unordered_Distinct_NotPipelined_Longrunning(int leftCount, int rightStart, int rightCount, int start, int count)
        {
            Except_Unordered_Distinct_NotPipelined(leftCount, rightStart, rightCount, start, count);
        }

        [Theory]
        [MemberData(nameof(ExceptData), new[] { 0, 1, 2, 16 })]
        public static void Except_Distinct_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            leftCount = Math.Min(DuplicateFactor * 2, leftCount);
            rightCount = Math.Min(DuplicateFactor, (rightCount + 1) / 2);
            int expectedCount = Math.Max(0, leftCount - rightCount);
            int seen = expectedCount == 0 ? 0 : leftCount - expectedCount;
            Assert.All(leftQuery.Except(rightQuery.Select(x => Math.Abs(x) % DuplicateFactor),
                new ModularCongruenceComparer(DuplicateFactor * 2)).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(expectedCount == 0 ? 0 : leftCount, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ExceptData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Except_Distinct_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            Except_Distinct_NotPipelined(left, leftCount, rightQuery, rightCount, start, count);
        }

        [Theory]
        [MemberData(nameof(ExceptSourceMultipleData), new[] { 0, 1, 2, 16 })]
        public static void Except_Unordered_SourceMultiple(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            // The difference between this test and the previous, is that it's not possible to
            // get non-unique results from ParallelEnumerable.Range()...
            // Those tests either need modification of source (via .Select(x => x / DuplicateFactor) or similar,
            // or via a comparator that considers some elements equal.
            IntegerRangeSet seen = new IntegerRangeSet(start, count);
            Assert.All(leftQuery.AsUnordered().Except(rightQuery), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ExceptSourceMultipleData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Except_Unordered_SourceMultiple_Longrunning(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            Except_Unordered_SourceMultiple(leftQuery, leftCount, rightQuery, rightCount, start, count);
        }

        [Theory]
        [MemberData(nameof(ExceptSourceMultipleData), new[] { 0, 1, 2, 16 })]
        public static void Except_SourceMultiple(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            int seen = start;
            Assert.All(leftQuery.Except(rightQuery), x => Assert.Equal(seen++, x));
            Assert.Equal(start + count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(ExceptSourceMultipleData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Except_SourceMultiple_Longrunning(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            Except_SourceMultiple(leftQuery, leftCount, rightQuery, rightCount, start, count);
        }

        [Fact]
        public static void Except_NotSupportedException()
        {
#pragma warning disable 618
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).Except(Enumerable.Range(0, 1)));
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).Except(Enumerable.Range(0, 1), null));
#pragma warning restore 618
        }

        [Fact]
        // Should not get the same setting from both operands.
        public static void Except_NoDuplicateSettings()
        {
            CancellationToken t = new CancellationTokenSource().Token;
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithCancellation(t).Except(ParallelEnumerable.Range(0, 1).WithCancellation(t)));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1).Except(ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1)));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default).Except(ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default)));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default).Except(ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default)));
        }

        [Fact]
        public static void Except_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("first", () => ((ParallelQuery<int>)null).Except(ParallelEnumerable.Range(0, 1)));
            AssertExtensions.Throws<ArgumentNullException>("second", () => ParallelEnumerable.Range(0, 1).Except(null));

            AssertExtensions.Throws<ArgumentNullException>("first", () => ((ParallelQuery<int>)null).Except(ParallelEnumerable.Range(0, 1), EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("second", () => ParallelEnumerable.Range(0, 1).Except(null, EqualityComparer<int>.Default));
        }
    }
}
