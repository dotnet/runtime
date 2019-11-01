// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class AsEnumerableTests
    {
        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void AsEnumerable_Unordered(Labeled<ParallelQuery<int>> labeled, int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            IEnumerable<int> enumerable = labeled.Item.AsEnumerable();
            Assert.All(enumerable, x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnorderedSources.OuterLoopRanges), MemberType = typeof(UnorderedSources))]
        public static void AsEnumerable_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            AsEnumerable_Unordered(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void AsEnumerable(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int seen = 0;
            IEnumerable<int> enumerable = labeled.Item.AsEnumerable();
            Assert.All(enumerable, x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.OuterLoopRanges), MemberType = typeof(Sources))]
        public static void AsEnumerable_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            AsEnumerable(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 16 }, MemberType = typeof(Sources))]
        public static void AsEnumerable_LinqBinding(Labeled<ParallelQuery<int>> labeled, int count)
        {
            IEnumerable<int> enumerable = labeled.Item.AsEnumerable();

            // The LINQ Cast<T>() retains origin type for ParallelEnumerable  and Partitioner when unordered,
            // (and all when ordered, due to the extra wrapper)
            // although aliased as IEnumerable<T>, so further LINQ calls work as expected.
            // If this test starts failing, update this test, and maybe mention it in release notes.
            Assert.IsNotType<ParallelQuery<int>>(enumerable.Cast<int>());
            Assert.True(enumerable.Cast<int>() is ParallelQuery<int>);

            Assert.False(enumerable.Concat(Enumerable.Range(0, count)) is ParallelQuery<int>);
            Assert.False(enumerable.DefaultIfEmpty() is ParallelQuery<int>);
            Assert.False(enumerable.Distinct() is ParallelQuery<int>);
            Assert.False(enumerable.Except(Enumerable.Range(0, count)) is ParallelQuery<int>);
            Assert.False(enumerable.GroupBy(x => x) is ParallelQuery<int>);
            Assert.False(enumerable.GroupJoin(Enumerable.Range(0, count), x => x, y => y, (x, g) => x) is ParallelQuery<int>);
            Assert.False(enumerable.Intersect(Enumerable.Range(0, count)) is ParallelQuery<int>);
            Assert.False(enumerable.Join(Enumerable.Range(0, count), x => x, y => y, (x, y) => x) is ParallelQuery<int>);
            Assert.False(enumerable.OfType<int>() is ParallelQuery<int>);
            Assert.False(enumerable.OrderBy(x => x) is ParallelQuery<int>);
            Assert.False(enumerable.OrderByDescending(x => x) is ParallelQuery<int>);
            Assert.False(enumerable.Reverse() is ParallelQuery<int>);
            Assert.False(enumerable.Select(x => x) is ParallelQuery<int>);
            Assert.False(enumerable.SelectMany(x => new[] { x }) is ParallelQuery<int>);
            Assert.False(enumerable.Skip(count / 2) is ParallelQuery<int>);
            Assert.False(enumerable.SkipWhile(x => true) is ParallelQuery<int>);
            Assert.False(enumerable.Take(count / 2) is ParallelQuery<int>);
            Assert.False(enumerable.TakeWhile(x => true) is ParallelQuery<int>);
            Assert.False(enumerable.Union(Enumerable.Range(0, count)) is ParallelQuery<int>);
            Assert.False(enumerable.Where(x => true) is ParallelQuery<int>);
            Assert.False(enumerable.Zip(Enumerable.Range(0, count), (x, y) => x) is ParallelQuery<int>);
        }

        [Fact]
        public static void AsEnumerable_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).AsEnumerable());
        }
    }
}
