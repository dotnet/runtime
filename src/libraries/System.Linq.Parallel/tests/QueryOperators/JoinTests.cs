// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class JoinTests
    {
        private const int KeyFactor = 8;

        public static IEnumerable<object[]> JoinData(int[] leftCounts, int[] rightCounts)
        {
            foreach (object[] parms in UnorderedSources.BinaryRanges(leftCounts, rightCounts))
            {
                yield return new object[] { ((Labeled<ParallelQuery<int>>)parms[0]).Order(), parms[1], parms[2], parms[3] };
            }
        }

        //
        // Join
        //
        [Theory]
        [MemberData("BinaryRanges", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 }, MemberType = typeof(UnorderedSources))]
        public static void Join_Unordered(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(leftCount, (rightCount + (KeyFactor - 1)) / KeyFactor));
            foreach (var p in leftQuery.Join(rightQuery, x => x * KeyFactor, y => y, (x, y) => KeyValuePair.Create(x, y)))
            {
                Assert.Equal(p.Key * KeyFactor, p.Value);
                seen.Add(p.Key);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("BinaryRanges", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 8, 1024 * 16 }, MemberType = typeof(UnorderedSources))]
        public static void Join_Unordered_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Join_Unordered(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("JoinData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 })]
        public static void Join(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seen = 0;
            foreach (var p in leftQuery.Join(rightQuery, x => x * KeyFactor, y => y, (x, y) => KeyValuePair.Create(x, y)))
            {
                Assert.Equal(seen++, p.Key);
                Assert.Equal(p.Key * KeyFactor, p.Value);
            }
            Assert.Equal(Math.Min(leftCount, (rightCount + (KeyFactor - 1)) / KeyFactor), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("JoinData", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 8, 1024 * 16 })]
        public static void Join_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Join(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("BinaryRanges", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 }, MemberType = typeof(UnorderedSources))]
        public static void Join_Unordered_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(leftCount, (rightCount + (KeyFactor - 1)) / KeyFactor));
            Assert.All(leftQuery.Join(rightQuery, x => x * KeyFactor, y => y, (x, y) => KeyValuePair.Create(x, y)).ToList(),
                p => { Assert.Equal(p.Key * KeyFactor, p.Value); seen.Add(p.Key); });
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("BinaryRanges", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 8, 1024 * 16 }, MemberType = typeof(UnorderedSources))]
        public static void Join_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Join_Unordered_NotPipelined(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("JoinData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 })]
        public static void Join_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seen = 0;
            Assert.All(leftQuery.Join(rightQuery, x => x * KeyFactor, y => y, (x, y) => KeyValuePair.Create(x, y)).ToList(),
                p => { Assert.Equal(seen++, p.Key); Assert.Equal(p.Key * KeyFactor, p.Value); });
            Assert.Equal(Math.Min(leftCount, (rightCount + (KeyFactor - 1)) / KeyFactor), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("JoinData", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 8, 1024 * 16 })]
        public static void Join_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Join_NotPipelined(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("BinaryRanges", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 }, MemberType = typeof(UnorderedSources))]
        public static void Join_Unordered_Multiple(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeSet seenOuter = new IntegerRangeSet(0, Math.Min(leftCount, (rightCount + (KeyFactor - 1)) / KeyFactor));
            IntegerRangeSet seenInner = new IntegerRangeSet(0, Math.Min(leftCount * KeyFactor, rightCount));
            Assert.All(leftQuery.Join(rightQuery, x => x, y => y / KeyFactor, (x, y) => KeyValuePair.Create(x, y)),
                p =>
                {
                    Assert.Equal(p.Key, p.Value / KeyFactor);
                    seenInner.Add(p.Value);
                    if (p.Value % KeyFactor == 0) seenOuter.Add(p.Key);
                });
            seenOuter.AssertComplete();
            seenInner.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("BinaryRanges", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 8, 1024 * 16 }, MemberType = typeof(UnorderedSources))]
        public static void Join_Unordered_Multiple_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Join_Unordered_Multiple(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("JoinData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 })]
        // Join doesn't always return items from the right ordered.  See Issue #1155
        public static void Join_Multiple(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seenOuter = 0;
            int previousOuter = -1;
            IntegerRangeSet seenInner = new IntegerRangeSet(0, 0);
            Assert.All(leftQuery.Join(rightQuery, x => x, y => y / KeyFactor, (x, y) => KeyValuePair.Create(x, y)),
                p =>
                {
                    if (p.Key != previousOuter)
                    {
                        Assert.Equal(seenOuter++, p.Key);
                        seenInner.AssertComplete();
                        seenInner = new IntegerRangeSet(p.Key * KeyFactor, Math.Min(rightCount - p.Key * KeyFactor, KeyFactor));
                        previousOuter = p.Key;
                    }
                    seenInner.Add(p.Value);
                    Assert.Equal(p.Key, p.Value / KeyFactor);
                });
            Assert.Equal(Math.Min(leftCount, (rightCount + (KeyFactor - 1)) / KeyFactor), seenOuter);
            seenInner.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("JoinData", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 8, 1024 * 16 })]
        public static void Join_Multiple_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Join_Multiple(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("BinaryRanges", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 }, MemberType = typeof(UnorderedSources))]
        public static void Join_Unordered_CustomComparator(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            IntegerRangeCounter seenOuter = new IntegerRangeCounter(0, leftCount);
            IntegerRangeCounter seenInner = new IntegerRangeCounter(0, rightCount);
            Assert.All(leftQuery.Join(rightQuery, x => x, y => y,
                (x, y) => KeyValuePair.Create(x, y), new ModularCongruenceComparer(KeyFactor)),
                p =>
                {
                    Assert.Equal(p.Key % KeyFactor, p.Value % KeyFactor);
                    seenOuter.Add(p.Key);
                    seenInner.Add(p.Value);
                });
            if (leftCount == 0 || rightCount == 0)
            {
                seenOuter.AssertEncountered(0);
                seenInner.AssertEncountered(0);
            }
            else
            {
                Func<int, int, int> cartesian = (key, other) => (other + (KeyFactor - 1) - key % KeyFactor) / KeyFactor;
                Assert.All(seenOuter, kv => Assert.Equal(cartesian(kv.Key, rightCount), kv.Value));
                Assert.All(seenInner, kv => Assert.Equal(cartesian(kv.Key, leftCount), kv.Value));
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("BinaryRanges", new int[] { 512, 1024 }, new int[] { 0, 1, 1024 }, MemberType = typeof(UnorderedSources))]
        public static void Join_Unordered_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Join_Unordered_CustomComparator(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("JoinData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 })]
        public static void Join_CustomComparator(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            int seenOuter = 0;
            int previousOuter = -1;
            IntegerRangeSet seenInner = new IntegerRangeSet(0, 0);
            Assert.All(leftQuery.Join(rightQuery, x => x, y => y,
                (x, y) => KeyValuePair.Create(x, y), new ModularCongruenceComparer(KeyFactor)),
                p =>
                {
                    if (p.Key != previousOuter)
                    {
                        Assert.Equal(seenOuter, p.Key);
                        // If there aren't sufficient elements in the RHS (< KeyFactor), the LHS skips an entry at the end of the mod cycle.
                        seenOuter = Math.Min(leftCount, seenOuter + (p.Key % KeyFactor + 1 == rightCount ? 8 - p.Key % KeyFactor : 1));
                        seenInner.AssertComplete();
                        previousOuter = p.Key;
                        seenInner = new IntegerRangeSet(0, (rightCount + (KeyFactor - 1) - p.Key % KeyFactor) / KeyFactor);
                    }
                    Assert.Equal(p.Key % KeyFactor, p.Value % KeyFactor);
                    seenInner.Add(p.Value / KeyFactor);
                });
            Assert.Equal(rightCount == 0 ? 0 : leftCount, seenOuter);
            seenInner.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("JoinData", new int[] { 512, 1024 }, new int[] { 0, 1, 1024 })]
        public static void Join_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Join_CustomComparator(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("JoinData", new int[] { 0, 1, 2, 16 }, new int[] { 0, 1, 16 })]
        public static void Join_InnerJoin_Ordered(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            ParallelQuery<int> middleQuery = ParallelEnumerable.Range(0, leftCount).AsOrdered();

            int seen = 0;
            Assert.All(leftQuery.Join(middleQuery.Join(rightQuery, x => x * KeyFactor / 2, y => y, (x, y) => KeyValuePair.Create(x, y)),
                z => z * 2, p => p.Key, (x, p) => KeyValuePair.Create(x, p)),
                pOuter =>
                {
                    KeyValuePair<int, int> pInner = pOuter.Value;
                    Assert.Equal(seen++, pOuter.Key);
                    Assert.Equal(pOuter.Key * 2, pInner.Key);
                    Assert.Equal(pOuter.Key * KeyFactor, pInner.Value);
                });
            Assert.Equal(Math.Min((leftCount + 1) / 2, (rightCount + (KeyFactor - 1)) / KeyFactor), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("JoinData", new int[] { 1024 * 4, 1024 * 8 }, new int[] { 0, 1, 1024 * 8, 1024 * 16 })]
        public static void Join_InnerJoin_Ordered_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            Join_InnerJoin_Ordered(left, leftCount, right, rightCount);
        }

        [Fact]
        public static void Join_NotSupportedException()
        {
#pragma warning disable 618
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).Join(Enumerable.Range(0, 1), i => i, i => i, (i, j) => i));
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).Join(Enumerable.Range(0, 1), i => i, i => i, (i, j) => i, null));
#pragma warning restore 618
        }

        [Fact]
        // Should not get the same setting from both operands.
        public static void Join_NoDuplicateSettings()
        {
            CancellationToken t = new CancellationTokenSource().Token;
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithCancellation(t).Join(ParallelEnumerable.Range(0, 1).WithCancellation(t), x => x, y => y, (l, r) => l));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1).Join(ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1), x => x, y => y, (l, r) => l));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default).Join(ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default), x => x, y => y, (l, r) => l));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default).Join(ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default), x => x, y => y, (l, r) => l));
        }

        [Fact]
        public static void Join_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).Join(ParallelEnumerable.Range(0, 1), i => i, i => i, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Join((ParallelQuery<int>)null, i => i, i => i, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Join(ParallelEnumerable.Range(0, 1), (Func<int, int>)null, i => i, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Join(ParallelEnumerable.Range(0, 1), i => i, (Func<int, int>)null, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Join(ParallelEnumerable.Range(0, 1), i => i, i => i, (Func<int, int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).Join(ParallelEnumerable.Range(0, 1), i => i, i => i, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Join((ParallelQuery<int>)null, i => i, i => i, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Join(ParallelEnumerable.Range(0, 1), (Func<int, int>)null, i => i, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Join(ParallelEnumerable.Range(0, 1), i => i, (Func<int, int>)null, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Join(ParallelEnumerable.Range(0, 1), i => i, i => i, (Func<int, int, int>)null, EqualityComparer<int>.Default));
        }
    }
}
