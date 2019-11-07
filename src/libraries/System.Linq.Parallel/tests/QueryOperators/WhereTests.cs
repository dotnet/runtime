// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class WhereTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(15)]
        [InlineData(16)]
        public static void Where_Unordered(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, (count + 1) / 2);
            foreach (int i in UnorderedSources.Default(count).Where(x => x % 2 == 0))
            {
                Assert.Equal(0, i % 2);
                seen.Add(i / 2);
            }
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Where_Unordered_Longrunning()
        {
            Where_Unordered(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 15, 16 }, MemberType = typeof(Sources))]
        public static void Where(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (int i in query.Where(x => x % 2 == 0))
            {
                Assert.Equal(seen, i);
                seen += 2;
            }
            Assert.Equal(count + (count % 2), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.OuterLoopRanges), MemberType = typeof(Sources))]
        public static void Where_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Where(labeled, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(15)]
        [InlineData(16)]
        public static void Where_Unordered_NotPipelined(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, (count + 1) / 2);
            Assert.All(UnorderedSources.Default(count).Where(x => x % 2 == 0).ToList(), x => seen.Add(x / 2));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Where_Unordered_NotPipelined_Longrunning()
        {
            Where_Unordered_NotPipelined(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 15, 16 }, MemberType = typeof(Sources))]
        public static void Where_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.Where(x => x % 2 == 0).ToList(), x => { Assert.Equal(seen, x); seen += 2; });
            Assert.Equal(count + (count % 2), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.OuterLoopRanges), MemberType = typeof(Sources))]
        public static void Where_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Where_NotPipelined(labeled, count);
        }

        // Uses an element's index to calculate an output value.  If order preservation isn't
        // working, this would PROBABLY fail.  Unfortunately, this isn't deterministic.  But choosing
        // larger input sizes increases the probability that it will.

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(15)]
        [InlineData(16)]
        public static void Where_Indexed_Unordered(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (int i in UnorderedSources.Default(count).Where((x, index) => x == index))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Where_Indexed_Unordered_Longrunning()
        {
            Where_Indexed_Unordered(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 15, 16 }, MemberType = typeof(Sources))]
        public static void Where_Indexed(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (int i in query.Where((x, index) => x == index))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.OuterLoopRanges), MemberType = typeof(Sources))]
        public static void Where_Indexed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Where_Indexed(labeled, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(15)]
        [InlineData(16)]
        public static void Where_Indexed_Unordered_NotPipelined(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(UnorderedSources.Default(count).Where((x, index) => x == index).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Where_Indexed_Unordered_NotPipelined_Longrunning()
        {
            Where_Indexed_Unordered_NotPipelined(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 15, 16 }, MemberType = typeof(Sources))]
        public static void Where_Indexed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.Where((x, index) => x == index).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.OuterLoopRanges), MemberType = typeof(Sources))]
        public static void Where_Indexed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Where_Indexed_NotPipelined(labeled, count);
        }

        [Fact]
        public static void Where_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).Where(x => x));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).Where((x, index) => x));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<bool>().Where((Func<bool, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<bool>().Where((Func<bool, int, bool>)null));
        }
    }
}
