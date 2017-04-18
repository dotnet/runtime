// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class LastLastOrDefaultTests
    {
        private static Func<int, IEnumerable<int>> Positions = x => new[] { 1, x / 2 + 1, Math.Max(1, x - 1) }.Distinct();

        public static IEnumerable<object[]> LastUnorderedData(int[] counts)
        {
            foreach (int count in counts.DefaultIfEmpty(Sources.OuterLoopCount))
            {
                foreach (int position in Positions(count))
                {
                    yield return new object[] { Labeled.Label("UnorderedDefault", UnorderedSources.Default(count)), count, position };
                }
            }
        }

        public static IEnumerable<object[]> LastData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.DefaultIfEmpty(Sources.OuterLoopCount), Positions)) yield return results;
        }

        //
        // Last and LastOrDefault
        //
        [Theory]
        [MemberData(nameof(LastUnorderedData), new[] { 1, 2, 16 })]
        [MemberData(nameof(LastData), new[] { 1, 2, 16 })]
        public static void Last(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            // For unordered collections, which element is chosen isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be split, and possibly mentioned in release notes.
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count - 1, query.Last());
            Assert.Equal(position - 1, query.Last(x => x < position));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(LastUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        [MemberData(nameof(LastData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Last_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            Last(labeled, count, position);
        }

        [Theory]
        [MemberData(nameof(LastUnorderedData), new[] { 1, 2, 16 })]
        [MemberData(nameof(LastData), new[] { 1, 2, 16 })]
        public static void LastOrDefault(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            // For unordered collections, which element is chosen isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be split, and possibly mentioned in release notes.
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count - 1, query.Last());
            Assert.Equal(position - 1, query.Last(x => x < position));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(LastUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        [MemberData(nameof(LastData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void LastOrDefault_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            LastOrDefault(labeled, count, position);
        }

        [Theory]
        [MemberData(nameof(LastUnorderedData), new[] { 0 })]
        [MemberData(nameof(LastData), new[] { 0 })]
        public static void Last_Empty(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Throws<InvalidOperationException>(() => query.Last());
        }

        [Theory]
        [MemberData(nameof(LastUnorderedData), new[] { 0 })]
        [MemberData(nameof(LastData), new[] { 0 })]
        public static void LastOrDefault_Empty(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(default(int), query.LastOrDefault());
        }

        [Theory]
        [MemberData(nameof(LastUnorderedData), new[] { 1, 2, 16 })]
        [MemberData(nameof(LastData), new[] { 1, 2, 16 })]
        public static void Last_NoMatch(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.Throws<InvalidOperationException>(() => query.Last(x => !seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(LastUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        [MemberData(nameof(LastData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Last_NoMatch_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            Last_NoMatch(labeled, count, position);
        }

        [Theory]
        [MemberData(nameof(LastUnorderedData), new[] { 1, 2, 16 })]
        [MemberData(nameof(LastData), new[] { 1, 2, 16 })]
        public static void LastOrDefault_NoMatch(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.Equal(default(int), query.LastOrDefault(x => !seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(LastUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        [MemberData(nameof(LastData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void LastOrDefault_NoMatch_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            LastOrDefault_NoMatch(labeled, count, position);
        }

        [Fact]
        public static void Last_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.Last(x => { canceler(); return true; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.LastOrDefault(x => { canceler(); return true; }));
        }

        [Fact]
        public static void Last_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Last(x => { canceler(); return true; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.LastOrDefault(x => { canceler(); return false; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Last(x => { canceler(); return true; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.LastOrDefault(x => { canceler(); return false; }));
        }

        [Fact]
        public static void Last_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.Last());
            AssertThrows.AlreadyCanceled(source => source.Last(x => true));

            AssertThrows.AlreadyCanceled(source => source.LastOrDefault());
            AssertThrows.AlreadyCanceled(source => source.LastOrDefault(x => true));
        }

        [Fact]
        public static void Last_AggregateException()
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Last(x => { throw new DeliberateTestException(); }));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).LastOrDefault(x => { throw new DeliberateTestException(); }));
        }

        [Fact]
        public static void Last_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).Last());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).LastOrDefault());

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<int>().Last(null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<int>().LastOrDefault(null));
        }
    }
}
