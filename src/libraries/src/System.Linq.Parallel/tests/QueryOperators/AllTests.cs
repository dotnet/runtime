// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class AllTests
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
        // All
        //
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void All_AllFalse(int count)
        {
            Assert.Equal(count == 0, UnorderedSources.Default(count).All(x => x < 0));
        }

        [Fact]
        [OuterLoop]
        public static void All_AllFalse_Longrunning()
        {
            All_AllFalse(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void All_AllTrue(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.True(UnorderedSources.Default(count).All(x => seen.Add(x)));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void All_AllTrue_Longrunning()
        {
            All_AllTrue(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(OnlyOneData), new[] { 2, 16 })]
        public static void All_OneFalse(int count, int position)
        {
            Assert.False(UnorderedSources.Default(count).All(x => !(x == position)));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(OnlyOneData), new int[] {/* Sources.OuterLoopCount */ })]
        public static void All_OneFalse_Longrunning(int count, int position)
        {
            All_OneFalse(count, position);
        }

        [Theory]
        [MemberData(nameof(OnlyOneData), new[] { 2, 16 })]
        public static void All_OneTrue(int count, int position)
        {
            Assert.False(UnorderedSources.Default(count).All(x => x == position));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(OnlyOneData), new int[] {/* Sources.OuterLoopCount */ })]
        public static void All_OneTrue_Longrunning(int count, int position)
        {
            All_OneTrue(count, position);
        }

        [Fact]
        public static void All_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.All(x => { canceler(); return true; }));
        }

        [Fact]
        public static void All_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.All(x => { canceler(); return true; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.All(x => { canceler(); return true; }));
        }

        [Fact]
        public static void All_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.All(x => true));
        }

        [Fact]
        public static void All_AggregateException()
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).All(x => { throw new DeliberateTestException(); }));
        }

        [Fact]
        public static void All_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).All(x => x));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<bool>().All(null));
        }
    }
}
