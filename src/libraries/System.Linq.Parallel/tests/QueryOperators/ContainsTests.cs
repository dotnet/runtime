// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class ContainsTests
    {
        public static IEnumerable<object[]> OnlyOneData(int[] counts)
        {
            foreach (int count in counts.DefaultIfEmpty(Sources.OuterLoopCount))
            {
                foreach (int position in new[] { 0, count / 2, Math.Max(0, count - 1) }.Distinct())
                {
                    yield return new object[] { count, position };
                }
            }
        }

        //
        // Contains
        //
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Contains_NoMatching(int count)
        {
            Assert.False(ParallelEnumerable.Range(0, count).Contains(count));
            Assert.False(ParallelEnumerable.Range(0, count).Contains(count, null));
            Assert.False(ParallelEnumerable.Range(0, count).Contains(count, DelegatingComparer.Create<int>((l, r) => false, i => 0)));
        }

        [Fact]
        [OuterLoop]
        public static void Contains_NoMatching_Longrunning()
        {
            Contains_NoMatching(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(16)]
        public static void Contains_MultipleMatching(int count)
        {
            Assert.True(ParallelEnumerable.Range(0, count).Contains(count, DelegatingComparer.Create<int>((l, r) => (l % 2) == (r % 2), i => i % 2)));
        }

        [Fact]
        [OuterLoop]
        public static void Contains_MultipleMatching_Longrunning()
        {
            Contains_MultipleMatching(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(OnlyOneData), new[] { 2, 16 })]
        public static void Contains_OneMatching(int count, int position)
        {
            Assert.True(ParallelEnumerable.Range(0, count).Contains(position));
            Assert.True(ParallelEnumerable.Range(0, count).Contains(position, null));
            Assert.True(ParallelEnumerable.Range(0, count).Contains(position, DelegatingComparer.Create<int>((l, r) => l == position && r == position, i => i.GetHashCode())));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(OnlyOneData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Contains_OneMatching_Longrunning(int count, int position)
        {
            Contains_OneMatching(count, position);
        }

        [Fact]
        public static void Contains_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.Contains(-1, new CancelingEqualityComparer<int>(canceler)));
        }

        [Fact]
        public static void Contains_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Contains(-1, new CancelingEqualityComparer<int>(canceler)));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Contains(-1, new CancelingEqualityComparer<int>(canceler)));
        }

        [Fact]
        public static void Contains_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.Contains(0));
            AssertThrows.AlreadyCanceled(source => source.Contains(0, EqualityComparer<int>.Default));
        }

        [Fact]
        public static void Contains_AggregateException()
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Contains(1, new FailingEqualityComparer<int>()));
        }

        [Fact]
        public static void Contains_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).Contains(false));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).Contains(false, EqualityComparer<bool>.Default));
        }
    }
}
