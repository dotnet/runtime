// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class GroupJoinTests
    {
        private const int KeyFactor = 8;
        private const int ElementFactor = 4;

        public static IEnumerable<object[]> GroupJoinData(int[] leftCounts, int[] rightCounts)
        {
            foreach (object[] parms in UnorderedSources.BinaryRanges(leftCounts, rightCounts))
            {
                yield return new object[] { ((Labeled<ParallelQuery<int>>)parms[0]).Order(), parms[1], parms[2], parms[3] };
            }
        }

        //
        // GroupJoin
        //
        [Theory]
        [MemberData("BinaryRanges", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 }, MemberType = typeof(UnorderedSources))]
        public static void GroupJoin_Unordered(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, leftCount);
            foreach (var p in leftQuery.GroupJoin(rightQuery, x => x * KeyFactor, y => y, (x, y) => KeyValuePair.Create(x, y)))
            {
                seen.Add(p.Key);
                if (p.Key < (rightCount + (KeyFactor - 1)) / KeyFactor)
                {
                    Assert.Equal(p.Key * KeyFactor, Assert.Single(p.Value));
                }
                else
                {
                    Assert.Empty(p.Value);
                }
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("BinaryRanges", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 4, 1024 * 8 }, MemberType = typeof(UnorderedSources))]
        public static void GroupJoin_Unordered_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            GroupJoin_Unordered(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("GroupJoinData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 })]
        public static void GroupJoin(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seen = 0;
            foreach (var p in leftQuery.GroupJoin(rightQuery, x => x * KeyFactor, y => y, (x, y) => KeyValuePair.Create(x, y)))
            {
                Assert.Equal(seen++, p.Key);
                if (p.Key < (rightCount + (KeyFactor - 1)) / KeyFactor)
                {
                    Assert.Equal(p.Key * KeyFactor, Assert.Single(p.Value));
                }
                else
                {
                    Assert.Empty(p.Value);
                }
            }
            Assert.Equal(leftCount, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("GroupJoinData", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 4, 1024 * 8 })]
        public static void GroupJoin_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            GroupJoin(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("BinaryRanges", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 }, MemberType = typeof(UnorderedSources))]
        public static void GroupJoin_Unordered_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, leftCount);
            Assert.All(leftQuery.GroupJoin(rightQuery, x => x * KeyFactor, y => y, (x, y) => KeyValuePair.Create(x, y)).ToList(),
                p =>
                {
                    seen.Add(p.Key);
                    if (p.Key < (rightCount + (KeyFactor - 1)) / KeyFactor)
                    {
                        Assert.Equal(p.Key * KeyFactor, Assert.Single(p.Value));
                    }
                    else
                    {
                        Assert.Empty(p.Value);
                    }
                });
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("BinaryRanges", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 4, 1024 * 8 }, MemberType = typeof(UnorderedSources))]
        public static void GroupJoin_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            GroupJoin_Unordered_NotPipelined(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("GroupJoinData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 })]
        public static void GroupJoin_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seen = 0;
            Assert.All(leftQuery.GroupJoin(rightQuery, x => x * KeyFactor, y => y, (x, y) => KeyValuePair.Create(x, y)).ToList(),
                p =>
                {
                    Assert.Equal(seen++, p.Key);
                    if (p.Key < (rightCount + (KeyFactor - 1)) / KeyFactor)
                    {
                        Assert.Equal(p.Key * KeyFactor, Assert.Single(p.Value));
                    }
                    else
                    {
                        Assert.Empty(p.Value);
                    }
                });
            Assert.Equal(leftCount, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("GroupJoinData", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 4, 1024 * 8 })]
        public static void GroupJoin_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            GroupJoin_NotPipelined(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("BinaryRanges", new int[] { 0, 1, 2, 15, 16 }, new int[] { 0, 1, 16 }, MemberType = typeof(UnorderedSources))]
        public static void GroupJoin_Unordered_Multiple(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seenOuter = new IntegerRangeSet(0, leftCount);
            Assert.All(leftQuery.GroupJoin(rightQuery, x => x, y => y / KeyFactor, (x, y) => KeyValuePair.Create(x, y)),
                p =>
                {
                    seenOuter.Add(p.Key);
                    if (p.Key < (rightCount + (KeyFactor - 1)) / KeyFactor)
                    {
                        IntegerRangeSet seenInner = new IntegerRangeSet(p.Key * KeyFactor, Math.Min(rightCount - p.Key * KeyFactor, KeyFactor));
                        Assert.All(p.Value, y => { Assert.Equal(p.Key, y / KeyFactor); seenInner.Add(y); });
                        seenInner.AssertComplete();
                    }
                    else
                    {
                        Assert.Empty(p.Value);
                    }
                });
            seenOuter.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("BinaryRanges", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 4, 1024 * 8 }, MemberType = typeof(UnorderedSources))]
        public static void GroupJoin_Unordered_Multiple_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            GroupJoin_Unordered_Multiple(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("GroupJoinData", new int[] { 0, 1, 2, 15, 16 }, new int[] { 0, 1, 16 })]
        // GroupJoin doesn't always return elements from the right in order.  See Issue #1155
        public static void GroupJoin_Multiple(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seenOuter = 0;
            Assert.All(leftQuery.GroupJoin(rightQuery, x => x, y => y / KeyFactor, (x, y) => KeyValuePair.Create(x, y)),
                p =>
                {
                    Assert.Equal(seenOuter++, p.Key);
                    if (p.Key < (rightCount + (KeyFactor - 1)) / KeyFactor)
                    {
                        IntegerRangeSet seenInner = new IntegerRangeSet(p.Key * KeyFactor, Math.Min(rightCount - p.Key * KeyFactor, KeyFactor));
                        Assert.All(p.Value, y => { Assert.Equal(p.Key, y / KeyFactor); seenInner.Add(y); });
                        seenInner.AssertComplete();
                    }
                    else
                    {
                        Assert.Empty(p.Value);
                    }
                });
            Assert.Equal(leftCount, seenOuter);
        }

        [Theory]
        [OuterLoop]
        [MemberData("GroupJoinData", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 4, 1024 * 8 })]
        public static void GroupJoin_Multiple_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            GroupJoin_Multiple(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("BinaryRanges", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 }, MemberType = typeof(UnorderedSources))]
        public static void GroupJoin_Unordered_CustomComparator(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seenOuter = new IntegerRangeSet(0, leftCount);
            Assert.All(leftQuery.GroupJoin(rightQuery, x => x, y => y % ElementFactor, (x, y) => KeyValuePair.Create(x, y), new ModularCongruenceComparer(KeyFactor)),
                p =>
                {
                    seenOuter.Add(p.Key);
                    if (p.Key % KeyFactor < Math.Min(ElementFactor, rightCount))
                    {
                        IntegerRangeSet seenInner = new IntegerRangeSet(0, (rightCount + (ElementFactor - 1) - p.Key % ElementFactor) / ElementFactor);
                        Assert.All(p.Value, y => { Assert.Equal(p.Key % KeyFactor, y % ElementFactor); seenInner.Add(y / ElementFactor); });
                        seenInner.AssertComplete();
                    }
                    else
                    {
                        Assert.Empty(p.Value);
                    }
                });
            seenOuter.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("BinaryRanges", new int[] { 512, 1024 }, new int[] { 0, 1, 1024, 1024 * 4 }, MemberType = typeof(UnorderedSources))]
        public static void GroupJoin_Unordered_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            GroupJoin_Unordered_CustomComparator(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("GroupJoinData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 })]
        public static void GroupJoin_CustomComparator(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seenOuter = 0;
            Assert.All(leftQuery.GroupJoin(rightQuery, x => x, y => y % ElementFactor, (x, y) => KeyValuePair.Create(x, y), new ModularCongruenceComparer(KeyFactor)),
                p =>
                {
                    Assert.Equal(seenOuter++, p.Key);
                    if (p.Key % KeyFactor < Math.Min(ElementFactor, rightCount))
                    {
                        IntegerRangeSet seenInner = new IntegerRangeSet(0, (rightCount + (ElementFactor - 1) - p.Key % ElementFactor) / ElementFactor);
                        Assert.All(p.Value, y => { Assert.Equal(p.Key % KeyFactor, y % ElementFactor); seenInner.Add(y / ElementFactor); });
                        seenInner.AssertComplete();
                    }
                    else
                    {
                        Assert.Empty(p.Value);
                    }
                });
            Assert.Equal(leftCount, seenOuter);
        }

        [Theory]
        [OuterLoop]
        [MemberData("GroupJoinData", new int[] { 512, 1024 }, new int[] { 0, 1, 1024, 1024 * 4 })]
        public static void GroupJoin_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            GroupJoin_CustomComparator(left, leftCount, right, rightCount);
        }

        [Fact]
        public static void GroupJoin_NotSupportedException()
        {
#pragma warning disable 618
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).GroupJoin(Enumerable.Range(0, 1), i => i, i => i, (i, j) => i));
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).GroupJoin(Enumerable.Range(0, 1), i => i, i => i, (i, j) => i, null));
#pragma warning restore 618
        }

        [Fact]
        // Should not get the same setting from both operands.
        public static void GroupJoin_NoDuplicateSettings()
        {
            CancellationToken t = new CancellationTokenSource().Token;
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithCancellation(t).GroupJoin(ParallelEnumerable.Range(0, 1).WithCancellation(t), x => x, y => y, (x, e) => e));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1).GroupJoin(ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1), x => x, y => y, (x, e) => e));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default).GroupJoin(ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default), x => x, y => y, (x, e) => e));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default).GroupJoin(ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default), x => x, y => y, (x, e) => e));
        }

        [Fact]
        public static void GroupJoin_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).GroupJoin(ParallelEnumerable.Range(0, 1), i => i, i => i, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupJoin((ParallelQuery<int>)null, i => i, i => i, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupJoin(ParallelEnumerable.Range(0, 1), (Func<int, int>)null, i => i, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupJoin(ParallelEnumerable.Range(0, 1), i => i, (Func<int, int>)null, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupJoin(ParallelEnumerable.Range(0, 1), i => i, i => i, (Func<int, IEnumerable<int>, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).GroupJoin(ParallelEnumerable.Range(0, 1), i => i, i => i, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupJoin((ParallelQuery<int>)null, i => i, i => i, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupJoin(ParallelEnumerable.Range(0, 1), (Func<int, int>)null, i => i, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupJoin(ParallelEnumerable.Range(0, 1), i => i, (Func<int, int>)null, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupJoin(ParallelEnumerable.Range(0, 1), i => i, i => i, (Func<int, IEnumerable<int>, int>)null, EqualityComparer<int>.Default));
        }
    }
}
