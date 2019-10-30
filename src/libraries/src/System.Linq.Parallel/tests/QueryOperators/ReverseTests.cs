// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class ReverseTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Reverse_Unordered(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (int i in UnorderedSources.Default(count).Reverse())
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Reverse_Unordered_Longrunning()
        {
            Reverse_Unordered(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void Reverse(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = count;
            foreach (int i in query.Reverse())
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(0, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.OuterLoopRanges), MemberType = typeof(Sources))]
        public static void Reverse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Reverse(labeled, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Reverse_Unordered_NotPipelined(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(UnorderedSources.Default(count).Reverse().ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Reverse_Unordered_NotPipelined_Longrunning()
        {
            Reverse_Unordered_NotPipelined(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void Reverse_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = count;
            Assert.All(query.Reverse().ToList(), x => Assert.Equal(--seen, x));
            Assert.Equal(0, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.OuterLoopRanges), MemberType = typeof(Sources))]
        public static void Reverse_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Reverse_NotPipelined(labeled, count);
        }

        [Fact]
        public static void Reverse_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<object>)null).Reverse());
        }
    }
}
