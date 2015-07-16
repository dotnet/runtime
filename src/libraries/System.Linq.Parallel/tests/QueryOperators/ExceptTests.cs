// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class ExceptTests

    {
        private const int DuplicateFactor = 4;

        public static IEnumerable<object[]> ExceptUnorderedData(int[] leftCounts, int[] rightCounts)
        {
            foreach (object[] parms in UnorderedSources.BinaryRanges(leftCounts.Cast<int>(), (l, r) => 0 - r / 2, rightCounts.Cast<int>()))
            {
                yield return parms.Take(4).Concat(new object[] { (int)parms[3] + (int)parms[4], Math.Max(0, (int)parms[1] - ((int)parms[3] + 1) / 2) }).ToArray();
            }
        }

        public static IEnumerable<object[]> ExceptData(int[] leftCounts, int[] rightCounts)
        {
            foreach (object[] parms in ExceptUnorderedData(leftCounts, rightCounts))
            {
                parms[0] = ((Labeled<ParallelQuery<int>>)parms[0]).Order();
                yield return parms;
            }
        }

        public static IEnumerable<object[]> ExceptSourceMultipleData(int[] counts)
        {
            foreach (int leftCount in counts.Cast<int>())
            {
                ParallelQuery<int> left = Enumerable.Range(0, leftCount * DuplicateFactor).Select(x => x % leftCount).ToArray().AsParallel().AsOrdered();
                foreach (int rightCount in new int[] { 0, 1, Math.Max(DuplicateFactor * 2, leftCount), 2 * Math.Max(DuplicateFactor, leftCount) })
                {
                    int rightStart = 0 - rightCount / 2;
                    yield return new object[] { left, leftCount,
                        ParallelEnumerable.Range(rightStart, rightCount), rightCount, rightStart + rightCount, Math.Max(0, leftCount - (rightCount + 1) / 2) };
                }
            }
        }

        //
        // Except
        //
        [Theory]
        [MemberData("ExceptUnorderedData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Except_Unordered(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seen = new IntegerRangeSet(start, count);
            foreach (int i in leftQuery.Except(rightQuery))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("ExceptUnorderedData", new int[] { 1024, 1024 * 16 }, new int[] { 0, 1024, 1024 * 32 })]
        public static void Except_Unordered_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            Except_Unordered(left, leftCount, right, rightCount, start, count);
        }

        [Theory]
        [MemberData("ExceptData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Except(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seen = start;
            foreach (int i in leftQuery.Except(rightQuery))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(count + start, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("ExceptData", new int[] { 1024, 1024 * 16 }, new int[] { 0, 1024, 1024 * 32 })]
        public static void Except_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            Except(left, leftCount, right, rightCount, start, count);
        }

        [Theory]
        [MemberData("ExceptUnorderedData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Except_Unordered_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seen = new IntegerRangeSet(start, count);
            Assert.All(leftQuery.Except(rightQuery).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("ExceptUnorderedData", new int[] { 1024, 1024 * 16 }, new int[] { 0, 1024, 1024 * 32 })]
        public static void Except_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            Except_Unordered_NotPipelined(left, leftCount, right, rightCount, start, count);
        }

        [Theory]
        [MemberData("ExceptData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Except_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seen = start;
            Assert.All(leftQuery.Except(rightQuery).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(count + start, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("ExceptData", new int[] { 1024, 1024 * 16 }, new int[] { 0, 1024, 1024 * 32 })]
        public static void Except_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            Except_NotPipelined(left, leftCount, right, rightCount, start, count);
        }

        [Theory]
        [MemberData("ExceptUnorderedData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Except_Unordered_Distinct(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
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
        [MemberData("ExceptUnorderedData", new int[] { 1024, 1024 * 16 }, new int[] { 0, 1024, 1024 * 32 })]
        public static void Except_Unordered_Distinct_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            Except_Unordered_Distinct(left, leftCount, right, rightCount, start, count);
        }

        [Theory]
        [MemberData("ExceptData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Except_Distinct(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
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
        [MemberData("ExceptData", new int[] { 1024, 1024 * 16 }, new int[] { 0, 1024, 1024 * 32 })]
        public static void Except_Distinct_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            Except_Distinct(left, leftCount, right, rightCount, start, count);
        }

        [Theory]
        [MemberData("ExceptUnorderedData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Except_Unordered_Distinct_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
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
        [MemberData("ExceptData", new int[] { 1024, 1024 * 16 }, new int[] { 0, 1024, 1024 * 32 })]
        public static void Except_Unordered_Distinct_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            Except_Unordered_Distinct_NotPipelined(left, leftCount, right, rightCount, start, count);
        }

        [Theory]
        [MemberData("ExceptData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Except_Distinct_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
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
        [MemberData("ExceptData", new int[] { 1024, 1024 * 16 }, new int[] { 0, 1024, 1024 * 32 })]
        public static void Except_Distinct_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int start, int count)
        {
            Except_Distinct_NotPipelined(left, leftCount, right, rightCount, start, count);
        }

        [Theory]
        [MemberData("ExceptSourceMultipleData", (object)(new int[] { 0, 1, 2, 16 }))]
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
        [MemberData("ExceptSourceMultipleData", (object)(new int[] { 1024, 1024 * 16 }))]
        public static void Except_Unordered_SourceMultiple_Longrunning(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            Except_Unordered_SourceMultiple(leftQuery, leftCount, rightQuery, rightCount, start, count);
        }

        [Theory]
        [MemberData("ExceptSourceMultipleData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Except_SourceMultiple(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int start, int count)
        {
            int seen = start;
            Assert.All(leftQuery.Except(rightQuery), x => Assert.Equal(seen++, x));
            Assert.Equal(start + count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("ExceptSourceMultipleData", (object)(new int[] { 1024, 1024 * 16 }))]
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
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).Except(ParallelEnumerable.Range(0, 1)));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Except(null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).Except(ParallelEnumerable.Range(0, 1), EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Except(null, EqualityComparer<int>.Default));
        }
    }
}
