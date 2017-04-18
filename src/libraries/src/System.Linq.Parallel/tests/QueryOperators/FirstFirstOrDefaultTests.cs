// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class FirstFirstOrDefaultTests
    {
        private static readonly Func<int, IEnumerable<int>> Positions = x => new[] { 0, x / 2, Math.Max(0, x - 1) }.Distinct();

        public static IEnumerable<object[]> FirstUnorderedData(int[] counts)
        {
            foreach (int count in counts.DefaultIfEmpty(Sources.OuterLoopCount))
            {
                foreach (int position in Positions(count))
                {
                    yield return new object[] { Labeled.Label("UnorderedDefault", UnorderedSources.Default(count)), count, position };
                }
            }
        }

        public static IEnumerable<object[]> FirstData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.DefaultIfEmpty(Sources.OuterLoopCount), Positions)) yield return results;
        }

        //
        // First and FirstOrDefault
        //
        [Theory]
        [MemberData(nameof(FirstUnorderedData), new[] { 1, 2, 16 })]
        [MemberData(nameof(FirstData), new[] { 1, 2, 16 })]
        public static void First(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            // For unordered collections, which element is chosen isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be split, and possibly mentioned in release notes.
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.First());
            Assert.Equal(position, query.First(x => x >= position));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(FirstUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        [MemberData(nameof(FirstData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void First_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            First(labeled, count, position);
        }

        [Theory]
        [MemberData(nameof(FirstUnorderedData), new[] { 1, 2, 16 })]
        [MemberData(nameof(FirstData), new[] { 1, 2, 16 })]
        public static void FirstOrDefault(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            // For unordered collections, which element is chosen isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be split, and possibly mentioned in release notes.
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.FirstOrDefault());
            Assert.Equal(position, query.FirstOrDefault(x => x >= position));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(FirstUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        [MemberData(nameof(FirstData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void FirstOrDefault_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            FirstOrDefault(labeled, count, position);
        }

        [Theory]
        [MemberData(nameof(FirstUnorderedData), new[] { 0 })]
        [MemberData(nameof(FirstData), new[] { 0 })]
        public static void First_Empty(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Throws<InvalidOperationException>(() => query.First());
        }

        [Theory]
        [MemberData(nameof(FirstUnorderedData), new[] { 0 })]
        [MemberData(nameof(FirstData), new[] { 0 })]
        public static void FirstOrDefault_Empty(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(default(int), query.FirstOrDefault());
        }

        [Theory]
        [MemberData(nameof(FirstUnorderedData), new[] { 1, 2, 16 })]
        [MemberData(nameof(FirstData), new[] { 0, 1, 2, 16 })]
        public static void First_NoMatch(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.Throws<InvalidOperationException>(() => query.First(x => !seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(FirstUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        [MemberData(nameof(FirstData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void First_NoMatch_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            First_NoMatch(labeled, count, position);
        }

        [Theory]
        [MemberData(nameof(FirstUnorderedData), new[] { 1, 2, 16 })]
        [MemberData(nameof(FirstData), new[] { 0, 1, 2, 16 })]
        public static void FirstOrDefault_NoMatch(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.Equal(default(int), query.FirstOrDefault(x => !seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(FirstUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        [MemberData(nameof(FirstData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void FirstOrDefault_NoMatch_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int position)
        {
            FirstOrDefault_NoMatch(labeled, count, position);
        }

        [Fact]
        public static void First_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.First(x => { canceler(); return false; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.FirstOrDefault(x => { canceler(); return false; }));
        }

        [Fact]
        public static void First_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.First(x => { canceler(); return false; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.FirstOrDefault(x => { canceler(); return false; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.First(x => { canceler(); return false; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.FirstOrDefault(x => { canceler(); return false; }));
        }

        [Fact]
        public static void First_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.First());
            AssertThrows.AlreadyCanceled(source => source.First(x => true));

            AssertThrows.AlreadyCanceled(source => source.FirstOrDefault());
            AssertThrows.AlreadyCanceled(source => source.FirstOrDefault(x => true));
        }

        [Fact]
        public static void First_AggregateException()
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).First(x => { throw new DeliberateTestException(); }));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).FirstOrDefault(x => { throw new DeliberateTestException(); }));
        }

        [Fact]
        public static void First_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).First());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).FirstOrDefault());

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<int>().First(null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<int>().FirstOrDefault(null));
        }
    }
}
