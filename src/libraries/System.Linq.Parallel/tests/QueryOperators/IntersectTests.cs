// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class IntersectTests
    {
        private const int DuplicateFactor = 4;

        public static IEnumerable<object[]> IntersectUnorderedData(int[] leftCounts, int[] rightCounts)
        {
            foreach (object[] parms in UnorderedSources.BinaryRanges(leftCounts.Cast<int>(), (l, r) => 0 - r / 2, rightCounts.Cast<int>()))
            {
                parms[4] = Math.Min((int)parms[1], ((int)parms[3] + 1) / 2);
                yield return parms;
            }
        }

        public static IEnumerable<object[]> IntersectData(int[] leftCounts, int[] rightCounts)
        {
            foreach (object[] parms in IntersectUnorderedData(leftCounts, rightCounts))
            {
                parms[0] = ((Labeled<ParallelQuery<int>>)parms[0]).Order();
                yield return parms;
            }
        }

        public static IEnumerable<object[]> IntersectSourceMultipleData(int[] counts)
        {
            foreach (int leftCount in counts.Cast<int>())
            {
                ParallelQuery<int> left = Enumerable.Range(0, leftCount * DuplicateFactor).Select(x => x % leftCount).ToArray().AsParallel().AsOrdered();
                foreach (int rightCount in new int[] { 0, 1, Math.Max(DuplicateFactor * 2, leftCount), 2 * Math.Max(DuplicateFactor, leftCount * 2) })
                {
                    int rightStart = 0 - rightCount / 2;
                    yield return new object[] { left, leftCount,
                        ParallelEnumerable.Range(rightStart, rightCount), rightCount, Math.Min(leftCount, (rightCount + 1) / 2) };
                }
            }
        }

        //
        // Intersect
        //
        [Theory]
        [MemberData("IntersectUnorderedData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Intersect_Unordered(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (int i in leftQuery.Intersect(rightQuery))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("IntersectUnorderedData", new int[] { 512, 1024 * 16 }, new int[] { 0, 1, 512, 1024 * 32 })]
        public static void Intersect_Unordered_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            Intersect_Unordered(left, leftCount, right, rightCount, count);
        }

        [Theory]
        [MemberData("IntersectData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Intersect(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seen = 0;
            foreach (int i in leftQuery.Intersect(rightQuery))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("IntersectData", new int[] { 512, 1024 * 16 }, new int[] { 0, 1, 512, 1024 * 32 })]
        public static void Intersect_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            Intersect(left, leftCount, right, rightCount, count);
        }

        [Theory]
        [MemberData("IntersectUnorderedData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Intersect_Unordered_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(leftQuery.Intersect(rightQuery).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("IntersectUnorderedData", new int[] { 512, 1024 * 16 }, new int[] { 0, 1, 512, 1024 * 32 })]
        public static void Intersect_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            Intersect_Unordered_NotPipelined(left, leftCount, right, rightCount, count);
        }

        [Theory]
        [MemberData("IntersectData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Intersect_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seen = 0;
            Assert.All(leftQuery.Intersect(rightQuery).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("IntersectData", new int[] { 512, 1024 * 16 }, new int[] { 0, 1, 512, 1024 * 32 })]
        public static void Intersect_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            Intersect_NotPipelined(left, leftCount, right, rightCount, count);
        }

        [Theory]
        [MemberData("IntersectUnorderedData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Intersect_Unordered_Distinct(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            leftCount = Math.Min(DuplicateFactor * 2, leftCount);
            rightCount = Math.Min(DuplicateFactor, (rightCount + 1) / 2);
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(leftCount, rightCount));
            foreach (int i in leftQuery.Intersect(rightQuery.Select(x => Math.Abs(x) % DuplicateFactor), new ModularCongruenceComparer(DuplicateFactor * 2)))
            {
                seen.Add(i % (DuplicateFactor * 2));
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("IntersectUnorderedData", new int[] { 512, 1024 * 16 }, new int[] { 0, 1, 512, 1024 * 32 })]
        public static void Intersect_Unordered_Distinct_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            Intersect_Unordered_Distinct(left, leftCount, right, rightCount, count);
        }

        [Theory]
        [MemberData("IntersectData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Intersect_Distinct(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            leftCount = Math.Min(DuplicateFactor * 2, leftCount);
            rightCount = Math.Min(DuplicateFactor, (rightCount + 1) / 2);
            int seen = 0;
            foreach (int i in leftQuery.Intersect(rightQuery.Select(x => Math.Abs(x) % DuplicateFactor), new ModularCongruenceComparer(DuplicateFactor * 2)))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(Math.Min(leftCount, rightCount), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("IntersectData", new int[] { 512, 1024 * 16 }, new int[] { 0, 1, 512, 1024 * 32 })]
        public static void Intersect_Distinct_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            Intersect_Distinct(left, leftCount, right, rightCount, count);
        }

        [Theory]
        [MemberData("IntersectUnorderedData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Intersect_Unordered_Distinct_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            leftCount = Math.Min(DuplicateFactor * 2, leftCount);
            rightCount = Math.Min(DuplicateFactor, (rightCount + 1) / 2);
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(leftCount, rightCount));
            Assert.All(leftQuery.Intersect(rightQuery.Select(x => Math.Abs(x) % DuplicateFactor), new ModularCongruenceComparer(DuplicateFactor * 2)).ToList(), x => seen.Add(x % (DuplicateFactor * 2)));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("IntersectUnorderedData", new int[] { 512, 1024 * 16 }, new int[] { 0, 1, 512, 1024 * 32 })]
        public static void Intersect_Unordered_Distinct_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            Intersect_Unordered_Distinct_NotPipelined(left, leftCount, right, rightCount, count);
        }

        [Theory]
        [MemberData("IntersectData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 8 })]
        public static void Intersect_Distinct_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            leftCount = Math.Min(DuplicateFactor * 2, leftCount);
            rightCount = Math.Min(DuplicateFactor, (rightCount + 1) / 2);
            int seen = 0;
            Assert.All(leftQuery.Intersect(rightQuery.Select(x => Math.Abs(x) % DuplicateFactor), new ModularCongruenceComparer(DuplicateFactor * 2)).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Min(leftCount, rightCount), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("IntersectData", new int[] { 512, 1024 * 16 }, new int[] { 0, 1, 512, 1024 * 32 })]
        public static void Intersect_Distinct_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount, int count)
        {
            Intersect_Distinct_NotPipelined(left, leftCount, right, rightCount, count);
        }

        [Theory]
        [MemberData("IntersectSourceMultipleData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Intersect_Unordered_SourceMultiple(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int count)
        {
            // The difference between this test and the previous, is that it's not possible to
            // get non-unique results from ParallelEnumerable.Range()...
            // Those tests either need modification of source (via .Select(x => x /DuplicateFactor) or similar,
            // or via a comparator that considers some elements equal.
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(leftQuery.AsUnordered().Intersect(rightQuery), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("IntersectSourceMultipleData", (object)(new int[] { 512, 1024 * 16 }))]
        public static void Intersect_Unordered_SourceMultiple_Longrunning(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int count)
        {
            Intersect_Unordered_SourceMultiple(leftQuery, leftCount, rightQuery, rightCount, count);
        }

        [Theory]
        [MemberData("IntersectSourceMultipleData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Intersect_SourceMultiple(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int count)
        {
            int seen = 0;
            Assert.All(leftQuery.Intersect(rightQuery), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("IntersectSourceMultipleData", (object)(new int[] { 512, 1024 * 16 }))]
        public static void Intersect_SourceMultiple_Longrunning(ParallelQuery<int> leftQuery, int leftCount, ParallelQuery<int> rightQuery, int rightCount, int count)
        {
            Intersect_SourceMultiple(leftQuery, leftCount, rightQuery, rightCount, count);
        }

        [Fact]
        public static void Intersect_NotSupportedException()
        {
#pragma warning disable 618
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).Intersect(Enumerable.Range(0, 1)));
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).Intersect(Enumerable.Range(0, 1), null));
#pragma warning restore 618
        }

        [Fact]
        // Should not get the same setting from both operands.
        public static void Intersect_NoDuplicateSettings()
        {
            CancellationToken t = new CancellationTokenSource().Token;
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithCancellation(t).Intersect(ParallelEnumerable.Range(0, 1).WithCancellation(t)));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1).Intersect(ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1)));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default).Intersect(ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default)));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default).Intersect(ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default)));
        }

        [Fact]
        public static void Intersect_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).Intersect(ParallelEnumerable.Range(0, 1)));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Intersect(null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).Intersect(ParallelEnumerable.Range(0, 1), EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Intersect(null, EqualityComparer<int>.Default));
        }
    }
}
