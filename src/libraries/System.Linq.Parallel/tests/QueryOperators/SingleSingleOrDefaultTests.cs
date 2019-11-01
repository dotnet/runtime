// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class SingleSingleOrDefaultTests
    {
        public static IEnumerable<object[]> SingleSpecificData(int[] counts)
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
        // Single and SingleOrDefault
        //
        [Theory]
        [InlineData(1)]
        [InlineData("string")]
        [InlineData((object)null)]
        public static void Single<T>(T element)
        {
            Assert.Equal(element, ParallelEnumerable.Repeat(element, 1).Single());
            Assert.Equal(element, ParallelEnumerable.Repeat(element, 1).Single(x => true));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData("string", 0)]
        [InlineData((object)null, 0)]
        [InlineData(1, 1)]
        [InlineData("string", 1)]
        [InlineData((object)null, 1)]
        public static void SingleOrDefault<T>(T element, int count)
        {
            Assert.Equal(count >= 1 ? element : default(T), ParallelEnumerable.Repeat(element, count).SingleOrDefault());
            Assert.Equal(count >= 1 ? element : default(T), ParallelEnumerable.Repeat(element, count).SingleOrDefault(x => true));
        }

        [Fact]
        public static void Single_Empty()
        {
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Single());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Single(x => true));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Single_NoMatch(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, count).Single(x => !seen.Add(x)));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Single_NoMatch_Longrunning()
        {
            Single_NoMatch(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void SingleOrDefault_NoMatch(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.Equal(default(int), ParallelEnumerable.Range(0, count).SingleOrDefault(x => !seen.Add(x)));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void SingleOrDefault_NoMatch_Longrunning()
        {
            SingleOrDefault_NoMatch(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(16)]
        public static void Single_AllMatch(int count)
        {
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, count).Single(x => true));
        }

        [Fact]
        [OuterLoop]
        public static void Single_AllMatch_Longrunning()
        {
            Single_AllMatch(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(16)]
        public static void SingleOrDefault_AllMatch(int count)
        {
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, count).SingleOrDefault(x => true));
        }

        [Fact]
        [OuterLoop]
        public static void SingleOrDefault_AllMatch_Longrunning()
        {
            SingleOrDefault_AllMatch(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(SingleSpecificData), new[] { 1, 2, 16 })]
        public static void Single_OneMatch(int count, int element)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.Equal(element, ParallelEnumerable.Range(0, count).Single(x => seen.Add(x) && x == element));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SingleSpecificData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Single_OneMatch_Longrunning(int count, int element)
        {
            Single_OneMatch(count, element);
        }

        [Theory]
        [MemberData(nameof(SingleSpecificData), new[] { 0, 1, 2, 16 })]
        public static void SingleOrDefault_OneMatch(int count, int element)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.Equal(element, ParallelEnumerable.Range(0, count).SingleOrDefault(x => seen.Add(x) && x == element));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SingleSpecificData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SingleOrDefault_OneMatch_Longrunning(int count, int element)
        {
            SingleOrDefault_OneMatch(count, element);
        }

        [Fact]
        public static void Single_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.Single(x => { canceler(); return false; }));
        }

        [Fact]
        public static void SingleOrDefault_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.SingleOrDefault(x => { canceler(); return false; }));
        }

        [Fact]
        public static void Single_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Single(x => { canceler(); return false; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Single(x => { canceler(); return false; }));
        }

        [Fact]
        public static void SingleOrDefault_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.SingleOrDefault(x => { canceler(); return false; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.SingleOrDefault(x => { canceler(); return false; }));
        }

        [Fact]
        public static void Single_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.Single());
            AssertThrows.AlreadyCanceled(source => source.Single(x => true));
        }

        [Fact]
        public static void SingleOrDefault_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.SingleOrDefault());
            AssertThrows.AlreadyCanceled(source => source.SingleOrDefault(x => true));
        }

        [Fact]
        public static void Single_AggregateException()
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Single(x => { throw new DeliberateTestException(); }));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).SingleOrDefault(x => { throw new DeliberateTestException(); }));
        }

        [Fact]
        public static void Single_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).Single());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).SingleOrDefault());

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<int>().Single(null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<int>().SingleOrDefault(null));
        }
    }
}
