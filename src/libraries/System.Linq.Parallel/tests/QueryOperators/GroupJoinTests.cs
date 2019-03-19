// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class GroupJoinTests
    {
        private const int KeyFactor = 8;
        private const int ElementFactor = 4;

        public static IEnumerable<object[]> GroupJoinUnorderedData(int[] counts)
        {
            foreach (int leftCount in counts)
            {
                foreach (int rightCount in counts)
                {
                    yield return new object[] { leftCount, rightCount };
                }
            }
        }

        public static IEnumerable<object[]> GroupJoinData(int[] counts)
        {
            counts = counts.DefaultIfEmpty(Sources.OuterLoopCount / 64).ToArray();
            // When dealing with joins, if there aren't multiple matches the ordering of the second operand is immaterial.
            foreach (object[] parms in Sources.Ranges(counts, i => counts))
            {
                yield return parms;
            }
        }

        public static IEnumerable<object[]> GroupJoinMultipleData(int[] counts)
        {
            counts = counts.DefaultIfEmpty(Sources.OuterLoopCount / 64).ToArray();
            foreach (object[] parms in UnorderedSources.BinaryRanges(counts, counts))
            {
                yield return new object[] { ((Labeled<ParallelQuery<int>>)parms[0]).Order(), parms[1], ((Labeled<ParallelQuery<int>>)parms[2]).Order(), parms[3] };
            }
        }

        //
        // GroupJoin
        //
        [Theory]
        [MemberData(nameof(GroupJoinUnorderedData), new[] { 0, 1, 2, KeyFactor * 2 })]
        public static void GroupJoin_Unordered(int leftCount, int rightCount)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(rightCount);
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

        [Fact]
        [OuterLoop]
        public static void GroupJoin_Unordered_Longrunning()
        {
            GroupJoin_Unordered(Sources.OuterLoopCount / 64, Sources.OuterLoopCount / 64);
        }

        [Theory]
        [MemberData(nameof(GroupJoinData), new[] { 0, 1, 2, KeyFactor * 2 })]
        public static void GroupJoin(Labeled<ParallelQuery<int>> left, int leftCount, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            int seen = 0;
            foreach (var p in leftQuery.GroupJoin(UnorderedSources.Default(rightCount), x => x * KeyFactor, y => y, (x, y) => KeyValuePair.Create(x, y)))
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
        [MemberData(nameof(GroupJoinData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void GroupJoin_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, int rightCount)
        {
            GroupJoin(left, leftCount, rightCount);
        }

        [Theory]
        [MemberData(nameof(GroupJoinUnorderedData), new[] { 0, 1, 2, KeyFactor * 2 })]
        public static void GroupJoin_Unordered_NotPipelined(int leftCount, int rightCount)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(rightCount);
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

        [Fact]
        [OuterLoop]
        public static void GroupJoin_Unordered_NotPipelined_Longrunning()
        {
            GroupJoin_Unordered_NotPipelined(Sources.OuterLoopCount / 64, Sources.OuterLoopCount / 64);
        }

        [Theory]
        [MemberData(nameof(GroupJoinData), new[] { 0, 1, 2, KeyFactor * 2 })]
        public static void GroupJoin_NotPipelined(Labeled<ParallelQuery<int>> left, int leftCount, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            int seen = 0;
            Assert.All(leftQuery.GroupJoin(UnorderedSources.Default(rightCount), x => x * KeyFactor, y => y, (x, y) => KeyValuePair.Create(x, y)).ToList(),
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
        [MemberData(nameof(GroupJoinData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void GroupJoin_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, int rightCount)
        {
            GroupJoin_NotPipelined(left, leftCount, rightCount);
        }

        [Theory]
        [MemberData(nameof(GroupJoinUnorderedData), new[] { 0, 1, 2, KeyFactor * 2 })]
        public static void GroupJoin_Unordered_Multiple(int leftCount, int rightCount)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(rightCount);
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

        [Fact]
        [OuterLoop]
        public static void GroupJoin_Unordered_Multiple_Longrunning()
        {
            GroupJoin_Unordered_Multiple(Sources.OuterLoopCount / 64, Sources.OuterLoopCount / 64);
        }

        [Theory]
        [MemberData(nameof(GroupJoinMultipleData), new[] { 0, 1, 2, KeyFactor * 2 - 1, KeyFactor * 2 })]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full framework doesn't preserve the right collection order (.NET core bug fix https://github.com/dotnet/corefx/pull/27930)")]
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
                        int seenInner = p.Key * KeyFactor;
                        Assert.All(p.Value, y =>
                           {
                               Assert.Equal(p.Key, y / KeyFactor);
                               Assert.Equal(seenInner++, y);
                           });
                        Assert.Equal(Math.Min((p.Key + 1) * KeyFactor, rightCount), seenInner);
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
        [MemberData(nameof(GroupJoinMultipleData), new int[] { /* Sources.OuterLoopCount */ })]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full framework doesn't preserve the right collection order (.NET core bug fix https://github.com/dotnet/corefx/pull/27930)")]
        public static void GroupJoin_Multiple_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            GroupJoin_Multiple(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData(nameof(GroupJoinMultipleData), new[] { 0, 1, 2, KeyFactor * 2 - 1, KeyFactor * 2 })]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full framework doesn't preserve the right collection order (.NET core bug fix https://github.com/dotnet/corefx/pull/27930)")]
        public static void GroupJoin_Multiple_LeftWithOrderingColisions(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item.AsUnordered().OrderBy(x => 0);
            ParallelQuery<int> rightQuery = right.Item;
            int seenNonEmpty = 0;
            int seenEmpty = 0;

            Assert.All(leftQuery.GroupJoin(rightQuery, x => x, y => y / KeyFactor, (x, y) => KeyValuePair.Create(x, y)),
                p =>
                {
                    if (p.Key < (rightCount + (KeyFactor - 1)) / KeyFactor)
                    {
                        Assert.Equal(seenNonEmpty++, p.Key);

                        int seenInner = p.Key * KeyFactor;
                        Assert.All(p.Value, y =>
                        {
                            Assert.Equal(p.Key, y / KeyFactor);
                            Assert.Equal(seenInner++, y);
                        });
                        Assert.Equal(Math.Min((p.Key + 1) * KeyFactor, rightCount), seenInner);
                    }
                    else
                    {
                        Assert.Equal(0, seenNonEmpty);
                        Assert.Empty(p.Value);

                        seenEmpty++;
                    }
                });
            Assert.Equal(leftCount, seenNonEmpty + seenEmpty);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(GroupJoinMultipleData), new int[] { /* Sources.OuterLoopCount */ })]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full framework doesn't preserve the right collection order (.NET core bug fix https://github.com/dotnet/corefx/pull/27930)")]
        public static void GroupJoin_Multiple_LeftWithOrderingColisions_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            GroupJoin_Multiple_LeftWithOrderingColisions(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData(nameof(GroupJoinUnorderedData), new[] { 0, 1, 2, KeyFactor * 2 })]
        public static void GroupJoin_Unordered_CustomComparator(int leftCount, int rightCount)
        {
            ParallelQuery<int> leftQuery = UnorderedSources.Default(leftCount);
            ParallelQuery<int> rightQuery = UnorderedSources.Default(rightCount);
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

        [Fact]
        [OuterLoop]
        public static void GroupJoin_Unordered_CustomComparator_Longrunning()
        {
            GroupJoin_Unordered_CustomComparator(Sources.OuterLoopCount / 64, Sources.OuterLoopCount / 64);
        }

        [Theory]
        [MemberData(nameof(GroupJoinMultipleData), new[] { 0, 1, 2, KeyFactor * 2 })]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full framework doesn't preserve the right collection order (.NET core bug fix https://github.com/dotnet/corefx/pull/27930)")]
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
                        int seenInner = p.Key % (KeyFactor / 2) - (KeyFactor / 2);
                        Assert.All(p.Value, y =>
                            {
                                Assert.Equal(p.Key % KeyFactor, y % (KeyFactor / 2));
                                Assert.Equal(seenInner += (KeyFactor / 2), y);
                            });
                        Assert.Equal(Math.Max(p.Key % (KeyFactor / 2), rightCount + (p.Key % (KeyFactor / 2) - (KeyFactor / 2))), seenInner);
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
        [MemberData(nameof(GroupJoinMultipleData), new int[] { /* Sources.OuterLoopCount */ })]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full framework doesn't preserve the right collection order (.NET core bug fix https://github.com/dotnet/corefx/pull/27930)")]
        public static void GroupJoin_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            GroupJoin_CustomComparator(left, leftCount, right, rightCount);
        }


        [Theory]
        [MemberData(nameof(GroupJoinMultipleData), new[] { 0, 1, 2, KeyFactor * 2 - 1, KeyFactor * 2 })]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full framework doesn't preserve the right collection order (.NET core bug fix https://github.com/dotnet/corefx/pull/27930)")]
        public static void GroupJoin_CustomComparator_LeftWithOrderingColisions(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item.AsUnordered().OrderBy(x => x / KeyFactor);
            ParallelQuery<int> rightQuery = right.Item;
            int seenNonEmpty = 0;
            int seenEmpty = 0;
            int seenLeftGroup = 0;
            int seenLeftCount = 0;

            Assert.All(leftQuery.GroupJoin(rightQuery, x => x, y => y % ElementFactor, (x, y) => KeyValuePair.Create(x, y), new ModularCongruenceComparer(KeyFactor)),
                p =>
                {
                    seenLeftCount++;

                    if (p.Key / KeyFactor > seenLeftGroup)
                    {
                        seenLeftGroup++;

                        try
                        {
                            Assert.Equal(KeyFactor, seenEmpty + seenNonEmpty);
                        }
                        finally
                        {
                            seenEmpty = 0;
                            seenNonEmpty = 0;
                        }
                    }
                    Assert.Equal(seenLeftGroup, p.Key / KeyFactor);

                    if (p.Key % KeyFactor < Math.Min(ElementFactor, rightCount))
                    {
                        try
                        {
                            Assert.Equal((seenLeftGroup * KeyFactor) + seenNonEmpty, p.Key);
                        }
                        finally
                        {
                            seenNonEmpty++;
                        }

                        int expectedInner = p.Key % ElementFactor;
                        int seenInnerCount = 0;
                        Assert.All(p.Value, y =>
                        {
                            seenInnerCount++;
                            Assert.Equal(p.Key % KeyFactor, y % ElementFactor);
                            try
                            {
                                Assert.Equal(expectedInner, y);
                            }
                            finally
                            {
                                expectedInner += ElementFactor;
                            }
                        });
                        Assert.Equal((rightCount / ElementFactor) + (((rightCount % ElementFactor) > (p.Key % KeyFactor)) ? 1 : 0), seenInnerCount);
                    }
                    else
                    {
                        seenEmpty++;

                        Assert.Equal(0, seenNonEmpty);
                        Assert.Empty(p.Value);
                    }
                });
            Assert.Equal(leftCount, seenLeftCount);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(GroupJoinMultipleData), new int[] { /* Sources.OuterLoopCount */ })]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full framework doesn't preserve the right collection order (.NET core bug fix https://github.com/dotnet/corefx/pull/27930)")]
        public static void GroupJoin_CustomComparator_LeftWithOrderingColisions_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            GroupJoin_CustomComparator_LeftWithOrderingColisions(left, leftCount, right, rightCount);
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
            AssertExtensions.Throws<ArgumentNullException>("outer", () => ((ParallelQuery<int>)null).GroupJoin(ParallelEnumerable.Range(0, 1), i => i, i => i, (i, j) => i));
            AssertExtensions.Throws<ArgumentNullException>("inner", () => ParallelEnumerable.Range(0, 1).GroupJoin((ParallelQuery<int>)null, i => i, i => i, (i, j) => i));
            AssertExtensions.Throws<ArgumentNullException>("outerKeySelector", () => ParallelEnumerable.Range(0, 1).GroupJoin(ParallelEnumerable.Range(0, 1), (Func<int, int>)null, i => i, (i, j) => i));
            AssertExtensions.Throws<ArgumentNullException>("innerKeySelector", () => ParallelEnumerable.Range(0, 1).GroupJoin(ParallelEnumerable.Range(0, 1), i => i, (Func<int, int>)null, (i, j) => i));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => ParallelEnumerable.Range(0, 1).GroupJoin(ParallelEnumerable.Range(0, 1), i => i, i => i, (Func<int, IEnumerable<int>, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("outer", () => ((ParallelQuery<int>)null).GroupJoin(ParallelEnumerable.Range(0, 1), i => i, i => i, (i, j) => i, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("inner", () => ParallelEnumerable.Range(0, 1).GroupJoin((ParallelQuery<int>)null, i => i, i => i, (i, j) => i, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("outerKeySelector", () => ParallelEnumerable.Range(0, 1).GroupJoin(ParallelEnumerable.Range(0, 1), (Func<int, int>)null, i => i, (i, j) => i, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("innerKeySelector", () => ParallelEnumerable.Range(0, 1).GroupJoin(ParallelEnumerable.Range(0, 1), i => i, (Func<int, int>)null, (i, j) => i, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => ParallelEnumerable.Range(0, 1).GroupJoin(ParallelEnumerable.Range(0, 1), i => i, i => i, (Func<int, IEnumerable<int>, int>)null, EqualityComparer<int>.Default));
        }
    }
}
