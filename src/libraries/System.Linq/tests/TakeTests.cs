// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.Linq.Tests
{
    public class TakeTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.Take(9), q.Take(9));

            Assert.Equal(q.Take(0..9), q.Take(0..9));
            Assert.Equal(q.Take(^9..9), q.Take(^9..9));
            Assert.Equal(q.Take(0..^0), q.Take(0..^0));
            Assert.Equal(q.Take(^9..^0), q.Take(^9..^0));
        }

        [Fact]
        public void SameResultsRepeatCallsIntQueryIList()
        {
            var q = (from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                     where x > Int32.MinValue
                     select x).ToList();

            Assert.Equal(q.Take(9), q.Take(9));

            Assert.Equal(q.Take(0..9), q.Take(0..9));
            Assert.Equal(q.Take(^9..9), q.Take(^9..9));
            Assert.Equal(q.Take(0..^0), q.Take(0..^0));
            Assert.Equal(q.Take(^9..^0), q.Take(^9..^0));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Take(7), q.Take(7));

            Assert.Equal(q.Take(0..7), q.Take(0..7));
            Assert.Equal(q.Take(^7..7), q.Take(^7..7));
            Assert.Equal(q.Take(0..^0), q.Take(0..^0));
            Assert.Equal(q.Take(^7..^0), q.Take(^7..^0));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQueryIList()
        {
            var q = (from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                     where !String.IsNullOrEmpty(x)
                     select x).ToList();

            Assert.Equal(q.Take(7), q.Take(7));

            Assert.Equal(q.Take(0..7), q.Take(0..7));
            Assert.Equal(q.Take(^7..7), q.Take(^7..7));
            Assert.Equal(q.Take(0..^0), q.Take(0..^0));
            Assert.Equal(q.Take(^7..^0), q.Take(^7..^0));
        }

        [Fact]
        public void SourceEmptyCountPositive()
        {
            var source = new int[] { };
            Assert.Empty(source.Take(5));

            Assert.Empty(source.Take(0..5));
            Assert.Empty(source.Take(^5..5));
            Assert.Empty(source.Take(0..^0));
            Assert.Empty(source.Take(^5..^0));
        }

        [Fact]
        public void SourceEmptyCountPositiveNotIList()
        {
            var source = NumberRangeGuaranteedNotCollectionType(0, 0);
            Assert.Empty(source.Take(5));

            Assert.Empty(source.Take(0..5));
            Assert.Empty(source.Take(^5..5));
            Assert.Empty(source.Take(0..^0));
            Assert.Empty(source.Take(^5..^0));
        }

        [Fact]
        public void SourceNonEmptyCountNegative()
        {
            var source = new[] { 2, 5, 9, 1 };
            Assert.Empty(source.Take(-5));

            Assert.Empty(source.Take(^9..0));
        }

        [Fact]
        public void SourceNonEmptyCountNegativeNotIList()
        {
            var source = ForceNotCollection(new[] { 2, 5, 9, 1 });
            Assert.Empty(source.Take(-5));

            Assert.Empty(source.Take(^9..0));
        }

        [Fact]
        public void SourceNonEmptyCountZero()
        {
            var source = new[] { 2, 5, 9, 1 };
            Assert.Empty(source.Take(0));

            Assert.Empty(source.Take(0..0));
            Assert.Empty(source.Take(^4..0));
            Assert.Empty(source.Take(0..^4));
            Assert.Empty(source.Take(^4..^4));
        }

        [Fact]
        public void SourceNonEmptyCountZeroNotIList()
        {
            var source = ForceNotCollection(new[] { 2, 5, 9, 1 });
            Assert.Empty(source.Take(0));

            Assert.Empty(source.Take(0..0));
            Assert.Empty(source.Take(^4..0));
            Assert.Empty(source.Take(0..^4));
            Assert.Empty(source.Take(^4..^4));
        }

        [Fact]
        public void SourceNonEmptyCountOne()
        {
            var source = new[] { 2, 5, 9, 1 };
            int[] expected = { 2 };

            Assert.Equal(expected, source.Take(1));

            Assert.Equal(expected, source.Take(0..1));
            Assert.Equal(expected, source.Take(^4..1));
            Assert.Equal(expected, source.Take(0..^3));
            Assert.Equal(expected, source.Take(^4..^3));
        }

        [Fact]
        public void SourceNonEmptyCountOneNotIList()
        {
            var source = ForceNotCollection(new[] { 2, 5, 9, 1 });
            int[] expected = { 2 };

            Assert.Equal(expected, source.Take(1));

            Assert.Equal(expected, source.Take(0..1));
            Assert.Equal(expected, source.Take(^4..1));
            Assert.Equal(expected, source.Take(0..^3));
            Assert.Equal(expected, source.Take(^4..^3));
        }

        [Fact]
        public void SourceNonEmptyTakeAllExactly()
        {
            var source = new[] { 2, 5, 9, 1 };

            Assert.Equal(source, source.Take(source.Length));

            Assert.Equal(source, source.Take(0..source.Length));
            Assert.Equal(source, source.Take(^source.Length..source.Length));
            Assert.Equal(source, source.Take(0..^0));
            Assert.Equal(source, source.Take(^source.Length..^0));
        }

        [Fact]
        public void SourceNonEmptyTakeAllExactlyNotIList()
        {
            var source = ForceNotCollection(new[] { 2, 5, 9, 1 });

            Assert.Equal(source, source.Take(source.Count()));

            Assert.Equal(source, source.Take(0..source.Count()));
            Assert.Equal(source, source.Take(^source.Count()..source.Count()));
            Assert.Equal(source, source.Take(0..^0));
            Assert.Equal(source, source.Take(^source.Count()..^0));
        }

        [Fact]
        public void SourceNonEmptyTakeAllButOne()
        {
            var source = new[] { 2, 5, 9, 1 };
            int[] expected = { 2, 5, 9 };

            Assert.Equal(expected, source.Take(3));

            Assert.Equal(expected, source.Take(0..3));
            Assert.Equal(expected, source.Take(^4..3));
            Assert.Equal(expected, source.Take(0..^1));
            Assert.Equal(expected, source.Take(^4..^1));
        }

        [Fact]
        public void RunOnce()
        {
            var source = new[] { 2, 5, 9, 1 };
            int[] expected = { 2, 5, 9 };

            Assert.Equal(expected, source.RunOnce().Take(3));

            Assert.Equal(expected, source.RunOnce().Take(0..3));
            Assert.Equal(expected, source.RunOnce().Take(^4..3));
            Assert.Equal(expected, source.RunOnce().Take(0..^1));
            Assert.Equal(expected, source.RunOnce().Take(^4..^1));
        }

        [Fact]
        public void SourceNonEmptyTakeAllButOneNotIList()
        {
            var source = ForceNotCollection(new[] { 2, 5, 9, 1 });
            int[] expected = { 2, 5, 9 };

            Assert.Equal(expected, source.RunOnce().Take(3));

            Assert.Equal(expected, source.RunOnce().Take(0..3));
            Assert.Equal(expected, source.RunOnce().Take(^4..3));
            Assert.Equal(expected, source.RunOnce().Take(0..^1));
            Assert.Equal(expected, source.RunOnce().Take(^4..^1));
        }

        [Fact]
        public void SourceNonEmptyTakeExcessive()
        {
            var source = new int?[] { 2, 5, null, 9, 1 };

            Assert.Equal(source, source.Take(source.Length + 1));

            Assert.Equal(source, source.Take(0..(source.Length + 1)));
            Assert.Equal(source, source.Take(^(source.Length + 1)..(source.Length + 1)));
        }

        [Fact]
        public void SourceNonEmptyTakeExcessiveNotIList()
        {
            var source = ForceNotCollection(new int?[] { 2, 5, null, 9, 1 });

            Assert.Equal(source, source.Take(source.Count() + 1));

            Assert.Equal(source, source.Take(0..(source.Count() + 1)));
            Assert.Equal(source, source.Take(^(source.Count() + 1)..(source.Count() + 1)));
        }

        [Fact]
        public void ThrowsOnNullSource()
        {
            int[] source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.Take(5));

            Assert.Throws<ArgumentNullException>("source", () => source.Take(0..5));
            Assert.Throws<ArgumentNullException>("source", () => source.Take(^5..5));
            Assert.Throws<ArgumentNullException>("source", () => source.Take(0..^0));
            Assert.Throws<ArgumentNullException>("source", () => source.Take(^5..^0));
        }

        [Fact]
        public void ForcedToEnumeratorDoesNotEnumerate()
        {
            var iterator1 = NumberRangeGuaranteedNotCollectionType(0, 3).Take(2);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en1 = iterator1 as IEnumerator<int>;
            Assert.False(en1 is not null && en1.MoveNext());

            var iterator2 = NumberRangeGuaranteedNotCollectionType(0, 3).Take(0..2);
            var en2 = iterator2 as IEnumerator<int>;
            Assert.False(en2 is not null && en2.MoveNext());

            var iterator3 = NumberRangeGuaranteedNotCollectionType(0, 3).Take(^3..2);
            var en3 = iterator3 as IEnumerator<int>;
            Assert.False(en3 is not null && en3.MoveNext());

            var iterator4 = NumberRangeGuaranteedNotCollectionType(0, 3).Take(0..^1);
            var en4 = iterator4 as IEnumerator<int>;
            Assert.False(en4 is not null && en4.MoveNext());

            var iterator5 = NumberRangeGuaranteedNotCollectionType(0, 3).Take(^3..^1);
            var en5 = iterator5 as IEnumerator<int>;
            Assert.False(en5 is not null && en5.MoveNext());
        }

        [Fact]
        public void Count()
        {
            Assert.Equal(2, NumberRangeGuaranteedNotCollectionType(0, 3).Take(2).Count());
            Assert.Equal(2, new[] { 1, 2, 3 }.Take(2).Count());
            Assert.Equal(0, NumberRangeGuaranteedNotCollectionType(0, 3).Take(0).Count());

            Assert.Equal(2, NumberRangeGuaranteedNotCollectionType(0, 3).Take(0..2).Count());
            Assert.Equal(2, new[] { 1, 2, 3 }.Take(0..2).Count());
            Assert.Equal(0, NumberRangeGuaranteedNotCollectionType(0, 3).Take(0..0).Count());

            Assert.Equal(2, NumberRangeGuaranteedNotCollectionType(0, 3).Take(^3..2).Count());
            Assert.Equal(2, new[] { 1, 2, 3 }.Take(^3..2).Count());
            Assert.Equal(0, NumberRangeGuaranteedNotCollectionType(0, 3).Take(^3..0).Count());

            Assert.Equal(2, NumberRangeGuaranteedNotCollectionType(0, 3).Take(0..^1).Count());
            Assert.Equal(2, new[] { 1, 2, 3 }.Take(0..^1).Count());
            Assert.Equal(0, NumberRangeGuaranteedNotCollectionType(0, 3).Take(0..^3).Count());

            Assert.Equal(2, NumberRangeGuaranteedNotCollectionType(0, 3).Take(^3..^1).Count());
            Assert.Equal(2, new[] { 1, 2, 3 }.Take(^3..^1).Count());
            Assert.Equal(0, NumberRangeGuaranteedNotCollectionType(0, 3).Take(^3..^3).Count());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateIList()
        {
            var iterator1 = NumberRangeGuaranteedNotCollectionType(0, 3).ToList().Take(2);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en1 = iterator1 as IEnumerator<int>;
            Assert.False(en1 is not null && en1.MoveNext());

            var iterator2 = NumberRangeGuaranteedNotCollectionType(0, 3).ToList().Take(0..2);
            var en2 = iterator2 as IEnumerator<int>;
            Assert.False(en2 is not null && en2.MoveNext());

            var iterator3 = NumberRangeGuaranteedNotCollectionType(0, 3).ToList().Take(^3..2);
            var en3 = iterator3 as IEnumerator<int>;
            Assert.False(en3 is not null && en3.MoveNext());

            var iterator4 = NumberRangeGuaranteedNotCollectionType(0, 3).ToList().Take(0..^1);
            var en4 = iterator4 as IEnumerator<int>;
            Assert.False(en4 is not null && en4.MoveNext());

            var iterator5 = NumberRangeGuaranteedNotCollectionType(0, 3).ToList().Take(^3..^1);
            var en5 = iterator5 as IEnumerator<int>;
            Assert.False(en5 is not null && en5.MoveNext());
        }

        [Fact]
        public void FollowWithTake()
        {
            var source = new[] { 5, 6, 7, 8 };
            var expected = new[] { 5, 6 };
            Assert.Equal(expected, source.Take(5).Take(3).Take(2).Take(40));

            Assert.Equal(expected, source.Take(0..5).Take(0..3).Take(0..2).Take(0..40));
            Assert.Equal(expected, source.Take(^4..5).Take(^4..3).Take(^3..2).Take(^2..40));
            Assert.Equal(expected, source.Take(0..^0).Take(0..^1).Take(0..^1).Take(0..^0));
            Assert.Equal(expected, source.Take(^4..^0).Take(^4..^1).Take(^3..^1).Take(^2..^0));
        }

        [Fact]
        public void FollowWithTakeNotIList()
        {
            var source = NumberRangeGuaranteedNotCollectionType(5, 4);
            var expected = new[] { 5, 6 };
            Assert.Equal(expected, source.Take(5).Take(3).Take(2));

            Assert.Equal(expected, source.Take(0..5).Take(0..3).Take(0..2));
            Assert.Equal(expected, source.Take(^4..5).Take(^4..3).Take(^3..2));
            Assert.Equal(expected, source.Take(0..^0).Take(0..^1).Take(0..^1));
            Assert.Equal(expected, source.Take(^4..^0).Take(^4..^1).Take(^3..^1));
        }

        [Fact]
        public void FollowWithSkip()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6 };
            var expected = new[] { 3, 4, 5 };
            Assert.Equal(expected, source.Take(5).Skip(2).Skip(-4));

            Assert.Equal(expected, source.Take(0..5).Skip(2).Skip(-4));
            Assert.Equal(expected, source.Take(^6..5).Skip(2).Skip(-4));
            Assert.Equal(expected, source.Take(0..^1).Skip(2).Skip(-4));
            Assert.Equal(expected, source.Take(^6..^1).Skip(2).Skip(-4));
        }

        [Fact]
        public void FollowWithSkipNotIList()
        {
            var source = NumberRangeGuaranteedNotCollectionType(1, 6);
            var expected = new[] { 3, 4, 5 };
            Assert.Equal(expected, source.Take(5).Skip(2).Skip(-4));

            Assert.Equal(expected, source.Take(0..5).Skip(2).Skip(-4));
            Assert.Equal(expected, source.Take(^6..5).Skip(2).Skip(-4));
            Assert.Equal(expected, source.Take(0..^1).Skip(2).Skip(-4));
            Assert.Equal(expected, source.Take(^6..^1).Skip(2).Skip(-4));
        }

        [Fact]
        public void ElementAt()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6 };
            var taken0 = source.Take(3);
            Assert.Equal(1, taken0.ElementAt(0));
            Assert.Equal(3, taken0.ElementAt(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken0.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken0.ElementAt(3));

            var taken1 = source.Take(0..3);
            Assert.Equal(1, taken1.ElementAt(0));
            Assert.Equal(3, taken1.ElementAt(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken1.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken1.ElementAt(3));

            var taken2 = source.Take(^6..3);
            Assert.Equal(1, taken2.ElementAt(0));
            Assert.Equal(3, taken2.ElementAt(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken2.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken2.ElementAt(3));

            var taken3 = source.Take(0..^3);
            Assert.Equal(1, taken3.ElementAt(0));
            Assert.Equal(3, taken3.ElementAt(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken3.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken3.ElementAt(3));

            var taken4 = source.Take(^6..^3);
            Assert.Equal(1, taken4.ElementAt(0));
            Assert.Equal(3, taken4.ElementAt(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken4.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken4.ElementAt(3));
        }

        [Fact]
        public void ElementAtNotIList()
        {
            var source = ForceNotCollection(new[] { 1, 2, 3, 4, 5, 6 });
            var taken0 = source.Take(3);
            Assert.Equal(1, taken0.ElementAt(0));
            Assert.Equal(3, taken0.ElementAt(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken0.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken0.ElementAt(3));

            var taken1 = source.Take(0..3);
            Assert.Equal(1, taken1.ElementAt(0));
            Assert.Equal(3, taken1.ElementAt(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken1.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken1.ElementAt(3));

            var taken2 = source.Take(^6..3);
            Assert.Equal(1, taken2.ElementAt(0));
            Assert.Equal(3, taken2.ElementAt(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken2.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken2.ElementAt(3));

            var taken3 = source.Take(0..^3);
            Assert.Equal(1, taken3.ElementAt(0));
            Assert.Equal(3, taken3.ElementAt(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken3.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken3.ElementAt(3));

            var taken4 = source.Take(^6..^3);
            Assert.Equal(1, taken4.ElementAt(0));
            Assert.Equal(3, taken4.ElementAt(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken4.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken4.ElementAt(3));
        }

        [Fact]
        public void ElementAtOrDefault()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6 };
            var taken0 = source.Take(3);
            Assert.Equal(1, taken0.ElementAtOrDefault(0));
            Assert.Equal(3, taken0.ElementAtOrDefault(2));
            Assert.Equal(0, taken0.ElementAtOrDefault(-1));
            Assert.Equal(0, taken0.ElementAtOrDefault(3));

            var taken1 = source.Take(0..3);
            Assert.Equal(1, taken1.ElementAtOrDefault(0));
            Assert.Equal(3, taken1.ElementAtOrDefault(2));
            Assert.Equal(0, taken1.ElementAtOrDefault(-1));
            Assert.Equal(0, taken1.ElementAtOrDefault(3));

            var taken2 = source.Take(^6..3);
            Assert.Equal(1, taken2.ElementAtOrDefault(0));
            Assert.Equal(3, taken2.ElementAtOrDefault(2));
            Assert.Equal(0, taken2.ElementAtOrDefault(-1));
            Assert.Equal(0, taken2.ElementAtOrDefault(3));

            var taken3 = source.Take(0..^3);
            Assert.Equal(1, taken3.ElementAtOrDefault(0));
            Assert.Equal(3, taken3.ElementAtOrDefault(2));
            Assert.Equal(0, taken3.ElementAtOrDefault(-1));
            Assert.Equal(0, taken3.ElementAtOrDefault(3));

            var taken4 = source.Take(^6..^3);
            Assert.Equal(1, taken4.ElementAtOrDefault(0));
            Assert.Equal(3, taken4.ElementAtOrDefault(2));
            Assert.Equal(0, taken4.ElementAtOrDefault(-1));
            Assert.Equal(0, taken4.ElementAtOrDefault(3));
        }

        [Fact]
        public void ElementAtOrDefaultNotIList()
        {
            var source = ForceNotCollection(new[] { 1, 2, 3, 4, 5, 6 });
            var taken0 = source.Take(3);
            Assert.Equal(1, taken0.ElementAtOrDefault(0));
            Assert.Equal(3, taken0.ElementAtOrDefault(2));
            Assert.Equal(0, taken0.ElementAtOrDefault(-1));
            Assert.Equal(0, taken0.ElementAtOrDefault(3));

            var taken1 = source.Take(0..3);
            Assert.Equal(1, taken1.ElementAtOrDefault(0));
            Assert.Equal(3, taken1.ElementAtOrDefault(2));
            Assert.Equal(0, taken1.ElementAtOrDefault(-1));
            Assert.Equal(0, taken1.ElementAtOrDefault(3));

            var taken2 = source.Take(^6..3);
            Assert.Equal(1, taken2.ElementAtOrDefault(0));
            Assert.Equal(3, taken2.ElementAtOrDefault(2));
            Assert.Equal(0, taken2.ElementAtOrDefault(-1));
            Assert.Equal(0, taken2.ElementAtOrDefault(3));

            var taken3 = source.Take(0..^3);
            Assert.Equal(1, taken3.ElementAtOrDefault(0));
            Assert.Equal(3, taken3.ElementAtOrDefault(2));
            Assert.Equal(0, taken3.ElementAtOrDefault(-1));
            Assert.Equal(0, taken3.ElementAtOrDefault(3));

            var taken4 = source.Take(^6..^3);
            Assert.Equal(1, taken4.ElementAtOrDefault(0));
            Assert.Equal(3, taken4.ElementAtOrDefault(2));
            Assert.Equal(0, taken4.ElementAtOrDefault(-1));
            Assert.Equal(0, taken4.ElementAtOrDefault(3));
        }

        [Fact]
        public void First()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(1, source.Take(1).First());
            Assert.Equal(1, source.Take(4).First());
            Assert.Equal(1, source.Take(40).First());
            Assert.Throws<InvalidOperationException>(() => source.Take(0).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Take(10).First());

            Assert.Equal(1, source.Take(0..1).First());
            Assert.Equal(1, source.Take(0..4).First());
            Assert.Equal(1, source.Take(0..40).First());
            Assert.Throws<InvalidOperationException>(() => source.Take(0..0).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Take(0..10).First());

            Assert.Equal(1, source.Take(^5..1).First());
            Assert.Equal(1, source.Take(^5..4).First());
            Assert.Equal(1, source.Take(^5..40).First());
            Assert.Throws<InvalidOperationException>(() => source.Take(^5..0).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Take(^5..10).First());

            Assert.Equal(1, source.Take(0..^4).First());
            Assert.Equal(1, source.Take(0..^1).First());
            Assert.Equal(1, source.Take(0..^0).First());
            Assert.Throws<InvalidOperationException>(() => source.Take(0..^5).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Take(0..^5).First());

            Assert.Equal(1, source.Take(^5..^4).First());
            Assert.Equal(1, source.Take(^5..^1).First());
            Assert.Equal(1, source.Take(^5..^0).First());
            Assert.Throws<InvalidOperationException>(() => source.Take(^5..^5).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Take(^10..^0).First());
        }

        [Fact]
        public void FirstNotIList()
        {
            var source = ForceNotCollection(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(1, source.Take(1).First());
            Assert.Equal(1, source.Take(4).First());
            Assert.Equal(1, source.Take(40).First());
            Assert.Throws<InvalidOperationException>(() => source.Take(0).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Take(10).First());

            Assert.Equal(1, source.Take(0..1).First());
            Assert.Equal(1, source.Take(0..4).First());
            Assert.Equal(1, source.Take(0..40).First());
            Assert.Throws<InvalidOperationException>(() => source.Take(0..0).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Take(0..10).First());

            Assert.Equal(1, source.Take(^5..1).First());
            Assert.Equal(1, source.Take(^5..4).First());
            Assert.Equal(1, source.Take(^5..40).First());
            Assert.Throws<InvalidOperationException>(() => source.Take(^5..0).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Take(^5..10).First());

            Assert.Equal(1, source.Take(0..^4).First());
            Assert.Equal(1, source.Take(0..^1).First());
            Assert.Equal(1, source.Take(0..^0).First());
            Assert.Throws<InvalidOperationException>(() => source.Take(0..^5).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Take(0..^5).First());

            Assert.Equal(1, source.Take(^5..^4).First());
            Assert.Equal(1, source.Take(^5..^1).First());
            Assert.Equal(1, source.Take(^5..^0).First());
            Assert.Throws<InvalidOperationException>(() => source.Take(^5..^5).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Take(^10..^0).First());
        }

        [Fact]
        public void FirstOrDefault()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(1, source.Take(1).FirstOrDefault());
            Assert.Equal(1, source.Take(4).FirstOrDefault());
            Assert.Equal(1, source.Take(40).FirstOrDefault());
            Assert.Equal(0, source.Take(0).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).Take(10).FirstOrDefault());

            Assert.Equal(1, source.Take(0..1).FirstOrDefault());
            Assert.Equal(1, source.Take(0..4).FirstOrDefault());
            Assert.Equal(1, source.Take(0..40).FirstOrDefault());
            Assert.Equal(0, source.Take(0..0).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).Take(0..10).FirstOrDefault());

            Assert.Equal(1, source.Take(^5..1).FirstOrDefault());
            Assert.Equal(1, source.Take(^5..4).FirstOrDefault());
            Assert.Equal(1, source.Take(^5..40).FirstOrDefault());
            Assert.Equal(0, source.Take(^5..0).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).Take(^10..10).FirstOrDefault());

            Assert.Equal(1, source.Take(0..^4).FirstOrDefault());
            Assert.Equal(1, source.Take(0..^1).FirstOrDefault());
            Assert.Equal(1, source.Take(0..^0).FirstOrDefault());
            Assert.Equal(0, source.Take(0..^5).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).Take(0..^10).FirstOrDefault());

            Assert.Equal(1, source.Take(^5..^4).FirstOrDefault());
            Assert.Equal(1, source.Take(^5..^1).FirstOrDefault());
            Assert.Equal(1, source.Take(^5..^0).FirstOrDefault());
            Assert.Equal(0, source.Take(^5..^5).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).Take(^10..^0).FirstOrDefault());
        }

        [Fact]
        public void FirstOrDefaultNotIList()
        {
            var source = ForceNotCollection(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(1, source.Take(1).FirstOrDefault());
            Assert.Equal(1, source.Take(4).FirstOrDefault());
            Assert.Equal(1, source.Take(40).FirstOrDefault());
            Assert.Equal(0, source.Take(0).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).Take(10).FirstOrDefault());

            Assert.Equal(1, source.Take(0..1).FirstOrDefault());
            Assert.Equal(1, source.Take(0..4).FirstOrDefault());
            Assert.Equal(1, source.Take(0..40).FirstOrDefault());
            Assert.Equal(0, source.Take(0..0).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).Take(0..10).FirstOrDefault());

            Assert.Equal(1, source.Take(^5..1).FirstOrDefault());
            Assert.Equal(1, source.Take(^5..4).FirstOrDefault());
            Assert.Equal(1, source.Take(^5..40).FirstOrDefault());
            Assert.Equal(0, source.Take(^5..0).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).Take(^10..10).FirstOrDefault());

            Assert.Equal(1, source.Take(0..^4).FirstOrDefault());
            Assert.Equal(1, source.Take(0..^1).FirstOrDefault());
            Assert.Equal(1, source.Take(0..^0).FirstOrDefault());
            Assert.Equal(0, source.Take(0..^5).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).Take(0..^10).FirstOrDefault());

            Assert.Equal(1, source.Take(^5..^4).FirstOrDefault());
            Assert.Equal(1, source.Take(^5..^1).FirstOrDefault());
            Assert.Equal(1, source.Take(^5..^0).FirstOrDefault());
            Assert.Equal(0, source.Take(^5..^5).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).Take(^10..^0).FirstOrDefault());
        }

        [Fact]
        public void Last()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(1, source.Take(1).Last());
            Assert.Equal(5, source.Take(5).Last());
            Assert.Equal(5, source.Take(40).Last());
            Assert.Throws<InvalidOperationException>(() => source.Take(0).Last());
            Assert.Throws<InvalidOperationException>(() => Array.Empty<int>().Take(40).Last());

            Assert.Equal(1, source.Take(0..1).Last());
            Assert.Equal(5, source.Take(0..5).Last());
            Assert.Equal(5, source.Take(0..40).Last());
            Assert.Throws<InvalidOperationException>(() => source.Take(0..0).Last());
            Assert.Throws<InvalidOperationException>(() => Array.Empty<int>().Take(0..40).Last());

            Assert.Equal(1, source.Take(^5..1).Last());
            Assert.Equal(5, source.Take(^5..5).Last());
            Assert.Equal(5, source.Take(^5..40).Last());
            Assert.Throws<InvalidOperationException>(() => source.Take(^5..0).Last());
            Assert.Throws<InvalidOperationException>(() => Array.Empty<int>().Take(^5..40).Last());

            Assert.Equal(1, source.Take(0..^4).Last());
            Assert.Equal(5, source.Take(0..^0).Last());
            Assert.Equal(5, source.Take(3..^0).Last());
            Assert.Throws<InvalidOperationException>(() => source.Take(0..^5).Last());
            Assert.Throws<InvalidOperationException>(() => Array.Empty<int>().Take(0..^0).Last());

            Assert.Equal(1, source.Take(^5..^4).Last());
            Assert.Equal(5, source.Take(^5..^0).Last());
            Assert.Equal(5, source.Take(^5..^0).Last());
            Assert.Throws<InvalidOperationException>(() => source.Take(^5..^5).Last());
            Assert.Throws<InvalidOperationException>(() => Array.Empty<int>().Take(^40..^0).Last());
        }

        [Fact]
        public void LastNotIList()
        {
            var source = ForceNotCollection(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(1, source.Take(1).Last());
            Assert.Equal(5, source.Take(5).Last());
            Assert.Equal(5, source.Take(40).Last());
            Assert.Throws<InvalidOperationException>(() => source.Take(0).Last());
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Array.Empty<int>()).Take(40).Last());

            Assert.Equal(1, source.Take(0..1).Last());
            Assert.Equal(5, source.Take(0..5).Last());
            Assert.Equal(5, source.Take(0..40).Last());
            Assert.Throws<InvalidOperationException>(() => source.Take(0..0).Last());
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Array.Empty<int>()).Take(0..40).Last());

            Assert.Equal(1, source.Take(^5..1).Last());
            Assert.Equal(5, source.Take(^5..5).Last());
            Assert.Equal(5, source.Take(^5..40).Last());
            Assert.Throws<InvalidOperationException>(() => source.Take(^5..0).Last());
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Array.Empty<int>()).Take(^5..40).Last());

            Assert.Equal(1, source.Take(0..^4).Last());
            Assert.Equal(5, source.Take(0..^0).Last());
            Assert.Equal(5, source.Take(3..^0).Last());
            Assert.Throws<InvalidOperationException>(() => source.Take(0..^5).Last());
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Array.Empty<int>()).Take(0..^0).Last());

            Assert.Equal(1, source.Take(^5..^4).Last());
            Assert.Equal(5, source.Take(^5..^0).Last());
            Assert.Equal(5, source.Take(^5..^0).Last());
            Assert.Throws<InvalidOperationException>(() => source.Take(^5..^5).Last());
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Array.Empty<int>()).Take(^40..^0).Last());
        }

        [Fact]
        public void LastOrDefault()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(1, source.Take(1).LastOrDefault());
            Assert.Equal(5, source.Take(5).LastOrDefault());
            Assert.Equal(5, source.Take(40).LastOrDefault());
            Assert.Equal(0, source.Take(0).LastOrDefault());
            Assert.Equal(0, Array.Empty<int>().Take(40).LastOrDefault());

            Assert.Equal(1, source.Take(0..1).LastOrDefault());
            Assert.Equal(5, source.Take(0..5).LastOrDefault());
            Assert.Equal(5, source.Take(0..40).LastOrDefault());
            Assert.Equal(0, source.Take(0..0).LastOrDefault());
            Assert.Equal(0, Array.Empty<int>().Take(0..40).LastOrDefault());

            Assert.Equal(1, source.Take(^5..1).LastOrDefault());
            Assert.Equal(5, source.Take(^5..5).LastOrDefault());
            Assert.Equal(5, source.Take(^5..40).LastOrDefault());
            Assert.Equal(0, source.Take(^5..0).LastOrDefault());
            Assert.Equal(0, Array.Empty<int>().Take(^5..40).LastOrDefault());

            Assert.Equal(1, source.Take(0..^4).LastOrDefault());
            Assert.Equal(5, source.Take(0..^0).LastOrDefault());
            Assert.Equal(5, source.Take(3..^0).LastOrDefault());
            Assert.Equal(0, source.Take(0..^5).LastOrDefault());
            Assert.Equal(0, Array.Empty<int>().Take(0..^0).LastOrDefault());

            Assert.Equal(1, source.Take(^5..^4).LastOrDefault());
            Assert.Equal(5, source.Take(^5..^0).LastOrDefault());
            Assert.Equal(5, source.Take(^40..^0).LastOrDefault());
            Assert.Equal(0, source.Take(^5..^5).LastOrDefault());
            Assert.Equal(0, Array.Empty<int>().Take(^40..^0).LastOrDefault());
        }

        [Fact]
        public void LastOrDefaultNotIList()
        {
            var source = ForceNotCollection(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(1, source.Take(1).LastOrDefault());
            Assert.Equal(5, source.Take(5).LastOrDefault());
            Assert.Equal(5, source.Take(40).LastOrDefault());
            Assert.Equal(0, source.Take(0).LastOrDefault());
            Assert.Equal(0, ForceNotCollection(Array.Empty<int>()).Take(40).LastOrDefault());

            Assert.Equal(1, source.Take(0..1).LastOrDefault());
            Assert.Equal(5, source.Take(0..5).LastOrDefault());
            Assert.Equal(5, source.Take(0..40).LastOrDefault());
            Assert.Equal(0, source.Take(0..0).LastOrDefault());
            Assert.Equal(0, ForceNotCollection(Array.Empty<int>()).Take(0..40).LastOrDefault());

            Assert.Equal(1, source.Take(^5..1).LastOrDefault());
            Assert.Equal(5, source.Take(^5..5).LastOrDefault());
            Assert.Equal(5, source.Take(^5..40).LastOrDefault());
            Assert.Equal(0, source.Take(^5..0).LastOrDefault());
            Assert.Equal(0, ForceNotCollection(Array.Empty<int>()).Take(^5..40).LastOrDefault());

            Assert.Equal(1, source.Take(0..^4).LastOrDefault());
            Assert.Equal(5, source.Take(0..^0).LastOrDefault());
            Assert.Equal(5, source.Take(3..^0).LastOrDefault());
            Assert.Equal(0, source.Take(0..^5).LastOrDefault());
            Assert.Equal(0, ForceNotCollection(Array.Empty<int>()).Take(0..^0).LastOrDefault());

            Assert.Equal(1, source.Take(^5..^4).LastOrDefault());
            Assert.Equal(5, source.Take(^5..^0).LastOrDefault());
            Assert.Equal(5, source.Take(^40..^0).LastOrDefault());
            Assert.Equal(0, source.Take(^5..^5).LastOrDefault());
            Assert.Equal(0, ForceNotCollection(Array.Empty<int>()).Take(^40..^0).LastOrDefault());
        }

        [Fact]
        public void ToArray()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(5).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(6).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(40).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(4).ToArray());
            Assert.Equal(1, source.Take(1).ToArray().Single());
            Assert.Empty(source.Take(0).ToArray());
            Assert.Empty(source.Take(-10).ToArray());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..5).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..6).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..40).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(0..4).ToArray());
            Assert.Equal(1, source.Take(0..1).ToArray().Single());
            Assert.Empty(source.Take(0..0).ToArray());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..5).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..6).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..40).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(^5..4).ToArray());
            Assert.Equal(1, source.Take(^5..1).ToArray().Single());
            Assert.Empty(source.Take(^5..0).ToArray());
            Assert.Empty(source.Take(^15..0).ToArray());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..^0).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(0..^1).ToArray());
            Assert.Equal(1, source.Take(0..^4).ToArray().Single());
            Assert.Empty(source.Take(0..^5).ToArray());
            Assert.Empty(source.Take(0..^15).ToArray());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..^0).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^6..^0).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^45..^0).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(^5..^1).ToArray());
            Assert.Equal(1, source.Take(^5..^4).ToArray().Single());
            Assert.Empty(source.Take(^5..^5).ToArray());
            Assert.Empty(source.Take(^15..^5).ToArray());
        }

        [Fact]
        public void ToArrayNotList()
        {
            var source = ForceNotCollection(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(5).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(6).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(40).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(4).ToArray());
            Assert.Equal(1, source.Take(1).ToArray().Single());
            Assert.Empty(source.Take(0).ToArray());
            Assert.Empty(source.Take(-10).ToArray());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..5).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..6).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..40).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(0..4).ToArray());
            Assert.Equal(1, source.Take(0..1).ToArray().Single());
            Assert.Empty(source.Take(0..0).ToArray());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..5).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..6).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..40).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(^5..4).ToArray());
            Assert.Equal(1, source.Take(^5..1).ToArray().Single());
            Assert.Empty(source.Take(^5..0).ToArray());
            Assert.Empty(source.Take(^15..0).ToArray());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..^0).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(0..^1).ToArray());
            Assert.Equal(1, source.Take(0..^4).ToArray().Single());
            Assert.Empty(source.Take(0..^5).ToArray());
            Assert.Empty(source.Take(0..^15).ToArray());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..^0).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^6..^0).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^45..^0).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(^5..^1).ToArray());
            Assert.Equal(1, source.Take(^5..^4).ToArray().Single());
            Assert.Empty(source.Take(^5..^5).ToArray());
            Assert.Empty(source.Take(^15..^5).ToArray());
        }

        [Fact]
        public void ToList()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(5).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(6).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(40).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(4).ToList());
            Assert.Equal(1, source.Take(1).ToList().Single());
            Assert.Empty(source.Take(0).ToList());
            Assert.Empty(source.Take(-10).ToList());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..5).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..6).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..40).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(0..4).ToList());
            Assert.Equal(1, source.Take(0..1).ToList().Single());
            Assert.Empty(source.Take(0..0).ToList());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..5).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..6).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..40).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(^5..4).ToList());
            Assert.Equal(1, source.Take(^5..1).ToList().Single());
            Assert.Empty(source.Take(^5..0).ToList());
            Assert.Empty(source.Take(^15..0).ToList());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..^0).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(0..^1).ToList());
            Assert.Equal(1, source.Take(0..^4).ToList().Single());
            Assert.Empty(source.Take(0..^5).ToList());
            Assert.Empty(source.Take(0..^15).ToList());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..^0).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^6..^0).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^45..^0).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(^5..^1).ToList());
            Assert.Equal(1, source.Take(^5..^4).ToList().Single());
            Assert.Empty(source.Take(^5..^5).ToList());
            Assert.Empty(source.Take(^15..^5).ToList());
        }

        [Fact]
        public void ToListNotList()
        {
            var source = ForceNotCollection(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(5).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(6).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(40).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(4).ToList());
            Assert.Equal(1, source.Take(1).ToList().Single());
            Assert.Empty(source.Take(0).ToList());
            Assert.Empty(source.Take(-10).ToList());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..5).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..6).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..40).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(0..4).ToList());
            Assert.Equal(1, source.Take(0..1).ToList().Single());
            Assert.Empty(source.Take(0..0).ToList());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..5).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..6).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..40).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(^5..4).ToList());
            Assert.Equal(1, source.Take(^5..1).ToList().Single());
            Assert.Empty(source.Take(^5..0).ToList());
            Assert.Empty(source.Take(^15..0).ToList());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(0..^0).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(0..^1).ToList());
            Assert.Equal(1, source.Take(0..^4).ToList().Single());
            Assert.Empty(source.Take(0..^5).ToList());
            Assert.Empty(source.Take(0..^15).ToList());

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^5..^0).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^6..^0).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(^45..^0).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(^5..^1).ToList());
            Assert.Equal(1, source.Take(^5..^4).ToList().Single());
            Assert.Empty(source.Take(^5..^5).ToList());
            Assert.Empty(source.Take(^15..^5).ToList());
        }

        [Fact]
        public void TakeCanOnlyBeOneList()
        {
            var source = new[] { 2, 4, 6, 8, 10 };
            Assert.Equal(new[] { 2 }, source.Take(1));
            Assert.Equal(new[] { 4 }, source.Skip(1).Take(1));
            Assert.Equal(new[] { 6 }, source.Take(3).Skip(2));
            Assert.Equal(new[] { 2 }, source.Take(3).Take(1));

            Assert.Equal(new[] { 2 }, source.Take(0..1));
            Assert.Equal(new[] { 4 }, source.Skip(1).Take(0..1));
            Assert.Equal(new[] { 6 }, source.Take(0..3).Skip(2));
            Assert.Equal(new[] { 2 }, source.Take(0..3).Take(0..1));

            Assert.Equal(new[] { 2 }, source.Take(^5..1));
            Assert.Equal(new[] { 4 }, source.Skip(1).Take(^4..1));
            Assert.Equal(new[] { 6 }, source.Take(^5..3).Skip(2));
            Assert.Equal(new[] { 2 }, source.Take(^5..3).Take(^4..1));

            Assert.Equal(new[] { 2 }, source.Take(0..^4));
            Assert.Equal(new[] { 4 }, source.Skip(1).Take(0..^3));
            Assert.Equal(new[] { 6 }, source.Take(0..^2).Skip(2));
            Assert.Equal(new[] { 2 }, source.Take(0..^2).Take(0..^2));

            Assert.Equal(new[] { 2 }, source.Take(^5..^4));
            Assert.Equal(new[] { 4 }, source.Skip(1).Take(^4..^3));
            Assert.Equal(new[] { 6 }, source.Take(^5..^2).Skip(2));
            Assert.Equal(new[] { 2 }, source.Take(^5..^2).Take(^4..^2));
        }

        [Fact]
        public void TakeCanOnlyBeOneNotList()
        {
            var source = ForceNotCollection(new[] { 2, 4, 6, 8, 10 });
            Assert.Equal(new[] { 2 }, source.Take(1));
            Assert.Equal(new[] { 4 }, source.Skip(1).Take(1));
            Assert.Equal(new[] { 6 }, source.Take(3).Skip(2));
            Assert.Equal(new[] { 2 }, source.Take(3).Take(1));

            Assert.Equal(new[] { 2 }, source.Take(0..1));
            Assert.Equal(new[] { 4 }, source.Skip(1).Take(0..1));
            Assert.Equal(new[] { 6 }, source.Take(0..3).Skip(2));
            Assert.Equal(new[] { 2 }, source.Take(0..3).Take(0..1));

            Assert.Equal(new[] { 2 }, source.Take(^5..1));
            Assert.Equal(new[] { 4 }, source.Skip(1).Take(^4..1));
            Assert.Equal(new[] { 6 }, source.Take(^5..3).Skip(2));
            Assert.Equal(new[] { 2 }, source.Take(^5..3).Take(^4..1));

            Assert.Equal(new[] { 2 }, source.Take(0..^4));
            Assert.Equal(new[] { 4 }, source.Skip(1).Take(0..^3));
            Assert.Equal(new[] { 6 }, source.Take(0..^2).Skip(2));
            Assert.Equal(new[] { 2 }, source.Take(0..^2).Take(0..^2));

            Assert.Equal(new[] { 2 }, source.Take(^5..^4));
            Assert.Equal(new[] { 4 }, source.Skip(1).Take(^4..^3));
            Assert.Equal(new[] { 6 }, source.Take(^5..^2).Skip(2));
            Assert.Equal(new[] { 2 }, source.Take(^5..^2).Take(^4..^2));
        }

        [Fact]
        public void RepeatEnumerating()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            var taken1 = source.Take(3);
            Assert.Equal(taken1, taken1);

            var taken2 = source.Take(0..3);
            Assert.Equal(taken2, taken2);

            var taken3 = source.Take(^5..3);
            Assert.Equal(taken3, taken3);

            var taken4 = source.Take(0..^2);
            Assert.Equal(taken4, taken4);

            var taken5 = source.Take(^5..^2);
            Assert.Equal(taken5, taken5);
        }

        [Fact]
        public void RepeatEnumeratingNotList()
        {
            var source = ForceNotCollection(new[] { 1, 2, 3, 4, 5 });
            var taken1 = source.Take(3);
            Assert.Equal(taken1, taken1);

            var taken2 = source.Take(0..3);
            Assert.Equal(taken2, taken2);

            var taken3 = source.Take(^5..3);
            Assert.Equal(taken3, taken3);

            var taken4 = source.Take(0..^2);
            Assert.Equal(taken4, taken4);

            var taken5 = source.Take(^5..^2);
            Assert.Equal(taken5, taken5);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        [InlineData(1000)]
        [InlineData(1000000)]
        [InlineData(int.MaxValue)]
        public void LazySkipAllTakenForLargeNumbers(int largeNumber)
        {
            Assert.Empty(new FastInfiniteEnumerator<int>().Take(largeNumber).Skip(largeNumber));
            Assert.Empty(new FastInfiniteEnumerator<int>().Take(largeNumber).Skip(largeNumber).Skip(42));
            Assert.Empty(new FastInfiniteEnumerator<int>().Take(largeNumber).Skip(largeNumber / 2).Skip(largeNumber / 2 + 1));

            Assert.Empty(new FastInfiniteEnumerator<int>().Take(0..largeNumber).Skip(largeNumber));
            Assert.Empty(new FastInfiniteEnumerator<int>().Take(0..largeNumber).Skip(largeNumber).Skip(42));
            Assert.Empty(new FastInfiniteEnumerator<int>().Take(0..largeNumber).Skip(largeNumber / 2).Skip(largeNumber / 2 + 1));
        }

        [Fact]
        public void LazyOverflowRegression()
        {
            var range = NumberRangeGuaranteedNotCollectionType(1, 100);
            var skipped = range.Skip(42); // Min index is 42.
            var taken1 = skipped.Take(int.MaxValue); // May try to calculate max index as 42 + int.MaxValue, leading to integer overflow.
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken1);
            Assert.Equal(100 - 42, taken1.Count());
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken1.ToArray());
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken1.ToList());

            var taken2 = NumberRangeGuaranteedNotCollectionType(1, 100).Take(42..int.MaxValue);
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken2);
            Assert.Equal(100 - 42, taken2.Count());
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken2.ToArray());
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken2.ToList());

            var taken3 = NumberRangeGuaranteedNotCollectionType(1, 100).Take(^(100 - 42)..int.MaxValue);
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken3);
            Assert.Equal(100 - 42, taken3.Count());
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken3.ToArray());
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken3.ToList());

            var taken4 = NumberRangeGuaranteedNotCollectionType(1, 100).Take(42..^0);
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken4);
            Assert.Equal(100 - 42, taken4.Count());
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken4.ToArray());
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken4.ToList());

            var taken5 = NumberRangeGuaranteedNotCollectionType(1, 100).Take(^(100 - 42)..^0);
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken5);
            Assert.Equal(100 - 42, taken5.Count());
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken5.ToArray());
            Assert.Equal(Enumerable.Range(43, 100 - 42), taken5.ToList());
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(1, 1, 1)]
        [InlineData(0, int.MaxValue, 100)]
        [InlineData(int.MaxValue, 0, 0)]
        [InlineData(0xffff, 1, 0)]
        [InlineData(1, 0xffff, 99)]
        [InlineData(int.MaxValue, int.MaxValue, 0)]
        [InlineData(1, int.MaxValue, 99)] // Regression test: The max index is precisely int.MaxValue.
        [InlineData(0, 100, 100)]
        [InlineData(10, 100, 90)]
        public void CountOfLazySkipTakeChain(int skip, int take, int expected)
        {
            int totalCount = 100;
            var partition1 = NumberRangeGuaranteedNotCollectionType(1, totalCount).Skip(skip).Take(take);
            Assert.Equal(expected, partition1.Count());
            Assert.Equal(expected, partition1.Select(i => i).Count());
            Assert.Equal(expected, partition1.Select(i => i).ToArray().Length);

            int end;
            try
            {
                end = checked(skip + take);
            }
            catch (OverflowException)
            {
                end = int.MaxValue;
            }

            var partition2 = NumberRangeGuaranteedNotCollectionType(1, totalCount).Take(skip..end);
            Assert.Equal(expected, partition2.Count());
            Assert.Equal(expected, partition2.Select(i => i).Count());
            Assert.Equal(expected, partition2.Select(i => i).ToArray().Length);

            var partition3 = NumberRangeGuaranteedNotCollectionType(1, totalCount).Take(^Math.Max(totalCount - skip, 0)..end);
            Assert.Equal(expected, partition3.Count());
            Assert.Equal(expected, partition3.Select(i => i).Count());
            Assert.Equal(expected, partition3.Select(i => i).ToArray().Length);

            var partition4 = NumberRangeGuaranteedNotCollectionType(1, totalCount).Take(skip..^Math.Max(totalCount - end, 0));
            Assert.Equal(expected, partition4.Count());
            Assert.Equal(expected, partition4.Select(i => i).Count());
            Assert.Equal(expected, partition4.Select(i => i).ToArray().Length);

            var partition5 = NumberRangeGuaranteedNotCollectionType(1, totalCount).Take(^Math.Max(totalCount - skip, 0)..^Math.Max(totalCount - end, 0));
            Assert.Equal(expected, partition5.Count());
            Assert.Equal(expected, partition5.Select(i => i).Count());
            Assert.Equal(expected, partition5.Select(i => i).ToArray().Length);
        }

        [Theory]
        [InlineData(new[] { 1, 2, 3, 4 }, 1, 3, 2, 4)]
        [InlineData(new[] { 1 }, 0, 1, 1, 1)]
        [InlineData(new[] { 1, 2, 3, 5, 8, 13 }, 1, int.MaxValue, 2, 13)] // Regression test: The max index is precisely int.MaxValue.
        [InlineData(new[] { 1, 2, 3, 5, 8, 13 }, 0, 2, 1, 2)]
        [InlineData(new[] { 1, 2, 3, 5, 8, 13 }, 500, 2, 0, 0)]
        [InlineData(new int[] { }, 10, 8, 0, 0)]
        public void FirstAndLastOfLazySkipTakeChain(int[] source, int skip, int take, int first, int last)
        {
            var partition1 = ForceNotCollection(source).Skip(skip).Take(take);

            Assert.Equal(first, partition1.FirstOrDefault());
            Assert.Equal(first, partition1.ElementAtOrDefault(0));
            Assert.Equal(last, partition1.LastOrDefault());
            Assert.Equal(last, partition1.ElementAtOrDefault(partition1.Count() - 1));

            int end;
            try
            {
                end = checked(skip + take);
            }
            catch (OverflowException)
            {
                end = int.MaxValue;
            }

            var partition2 = ForceNotCollection(source).Take(skip..end);

            Assert.Equal(first, partition2.FirstOrDefault());
            Assert.Equal(first, partition2.ElementAtOrDefault(0));
            Assert.Equal(last, partition2.LastOrDefault());
            Assert.Equal(last, partition2.ElementAtOrDefault(partition2.Count() - 1));

            var partition3 = ForceNotCollection(source).Take(^Math.Max(source.Length - skip, 0)..end);

            Assert.Equal(first, partition3.FirstOrDefault());
            Assert.Equal(first, partition3.ElementAtOrDefault(0));
            Assert.Equal(last, partition3.LastOrDefault());
            Assert.Equal(last, partition3.ElementAtOrDefault(partition3.Count() - 1));

            var partition4 = ForceNotCollection(source).Take(skip..^Math.Max(source.Length - end, 0));

            Assert.Equal(first, partition4.FirstOrDefault());
            Assert.Equal(first, partition4.ElementAtOrDefault(0));
            Assert.Equal(last, partition4.LastOrDefault());
            Assert.Equal(last, partition4.ElementAtOrDefault(partition4.Count() - 1));

            var partition5 = ForceNotCollection(source).Take(^Math.Max(source.Length - skip, 0)..^Math.Max(source.Length - end, 0));

            Assert.Equal(first, partition5.FirstOrDefault());
            Assert.Equal(first, partition5.ElementAtOrDefault(0));
            Assert.Equal(last, partition5.LastOrDefault());
            Assert.Equal(last, partition5.ElementAtOrDefault(partition5.Count() - 1));
        }

        [Theory]
        [InlineData(new[] { 1, 2, 3, 4, 5 }, 1, 3, new[] { -1, 0, 1, 2 }, new[] { 0, 2, 3, 4 })]
        [InlineData(new[] { 0xfefe, 7000, 123 }, 0, 3, new[] { -1, 0, 1, 2 }, new[] { 0, 0xfefe, 7000, 123 })]
        [InlineData(new[] { 0xfefe }, 100, 100, new[] { -1, 0, 1, 2 }, new[] { 0, 0, 0, 0 })]
        [InlineData(new[] { 0xfefe, 123, 456, 7890, 5555, 55 }, 1, 10, new[] { -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, new[] { 0, 123, 456, 7890, 5555, 55, 0, 0, 0, 0, 0, 0, 0 })]
        public void ElementAtOfLazySkipTakeChain(int[] source, int skip, int take, int[] indices, int[] expectedValues)
        {
            var partition1 = ForceNotCollection(source).Skip(skip).Take(take);

            Assert.Equal(indices.Length, expectedValues.Length);
            for (int i = 0; i < indices.Length; i++)
            {
                Assert.Equal(expectedValues[i], partition1.ElementAtOrDefault(indices[i]));
            }

            int end;
            try
            {
                end = checked(skip + take);
            }
            catch (OverflowException)
            {
                end = int.MaxValue;
            }

            var partition2 = ForceNotCollection(source).Take(skip..end);
            for (int i = 0; i < indices.Length; i++)
            {
                Assert.Equal(expectedValues[i], partition2.ElementAtOrDefault(indices[i]));
            }

            var partition3 = ForceNotCollection(source).Take(^Math.Max(source.Length - skip, 0)..end);
            for (int i = 0; i < indices.Length; i++)
            {
                Assert.Equal(expectedValues[i], partition3.ElementAtOrDefault(indices[i]));
            }

            var partition4 = ForceNotCollection(source).Take(skip..^Math.Max(source.Length - end, 0));
            for (int i = 0; i < indices.Length; i++)
            {
                Assert.Equal(expectedValues[i], partition4.ElementAtOrDefault(indices[i]));
            }

            var partition5 = ForceNotCollection(source).Take(^Math.Max(source.Length - skip, 0)..^Math.Max(source.Length - end, 0));
            for (int i = 0; i < indices.Length; i++)
            {
                Assert.Equal(expectedValues[i], partition5.ElementAtOrDefault(indices[i]));
            }
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        [InlineData(2, 2)]
        [InlineData(2, 3)]
        public void DisposeSource(int sourceCount, int count)
        {
            bool[] isIteratorDisposed = new bool[5];

            var source = Repeat(
                index =>
                {
                    int state = 0;
                    return new DelegateIterator<int>(
                        moveNext: () => ++state <= sourceCount,
                        current: () => 0,
                        dispose: () => { state = -1; isIteratorDisposed[index] = true; });
                },
                5);

            IEnumerator<int> iterator0 = source[0].Take(count).GetEnumerator();
            int iteratorCount0 = Math.Min(sourceCount, Math.Max(0, count));
            Assert.All(Enumerable.Range(0, iteratorCount0), _ => Assert.True(iterator0.MoveNext()));

            Assert.False(iterator0.MoveNext());

            // Unlike Skip, Take can tell straightaway that it can return a sequence with no elements if count <= 0.
            // The enumerable it returns is a specialized empty iterator that has no connections to the source. Hence,
            // after MoveNext returns false under those circumstances, it won't invoke Dispose on our enumerator.
            bool isItertorNotEmpty0 = count > 0;
            Assert.Equal(isItertorNotEmpty0, isIteratorDisposed[0]);

            int end = Math.Max(0, count);
            IEnumerator<int> iterator1 = source[1].Take(0..end).GetEnumerator();
            Assert.All(Enumerable.Range(0, Math.Min(sourceCount, Math.Max(0, count))), _ => Assert.True(iterator1.MoveNext()));
            Assert.False(iterator1.MoveNext());
            // When startIndex end and endIndex are both not from end and startIndex >= endIndex, Take(Range) returns an empty array.
            bool isItertorNotEmpty1 = end != 0;
            Assert.Equal(isItertorNotEmpty1, isIteratorDisposed[1]);

            int startIndexFromEnd = Math.Max(sourceCount, end);
            int endIndexFromEnd = Math.Max(0, sourceCount - end);

            IEnumerator<int> iterator2 = source[2].Take(^startIndexFromEnd..end).GetEnumerator();
            Assert.All(Enumerable.Range(0, Math.Min(sourceCount, Math.Max(0, count))), _ => Assert.True(iterator2.MoveNext()));
            Assert.False(iterator2.MoveNext());
            // When startIndex is ^0, Take(Range) returns an empty array.
            bool isIteratorNotEmpty2 = startIndexFromEnd != 0;
            Assert.Equal(isIteratorNotEmpty2, isIteratorDisposed[2]);

            IEnumerator<int> iterator3 = source[3].Take(0..^endIndexFromEnd).GetEnumerator();
            Assert.All(Enumerable.Range(0, Math.Min(sourceCount, Math.Max(0, count))), _ => Assert.True(iterator3.MoveNext()));
            Assert.False(iterator3.MoveNext());
            Assert.True(isIteratorDisposed[3]);

            IEnumerator<int> iterator4 = source[4].Take(^startIndexFromEnd..^endIndexFromEnd).GetEnumerator();
            Assert.All(Enumerable.Range(0, Math.Min(sourceCount, Math.Max(0, count))), _ => Assert.True(iterator4.MoveNext()));
            Assert.False(iterator4.MoveNext());
            // When startIndex is ^0,
            // or when startIndex and endIndex are both from end and startIndex <= endIndexFromEnd, Take(Range) returns an empty array.
            bool isIteratorNotEmpty4 = startIndexFromEnd != 0 && startIndexFromEnd > endIndexFromEnd;
            Assert.Equal(isIteratorNotEmpty4, isIteratorDisposed[4]);
        }

        [Fact]
        public void DisposeSource_StartIndexFromEnd_ShouldDisposeOnFirstElement()
        {
            const int count = 5;
            int state = 0;
            var source = new DelegateIterator<int>(
                moveNext: () => ++state <= count,
                current: () => state,
                dispose: () => state = -1);

            using var e = source.Take(^3..).GetEnumerator();
            Assert.True(e.MoveNext());
            Assert.Equal(3, e.Current);

            Assert.Equal(-1, state);
            Assert.True(e.MoveNext());
        }

        [Fact]
        public void DisposeSource_EndIndexFromEnd_ShouldDisposeOnCompletedEnumeration()
        {
            const int count = 5;
            int state = 0;
            var source = new DelegateIterator<int>(
                moveNext: () => ++state <= count,
                current: () => state,
                dispose: () => state = -1);

            using var e = source.Take(..^3).GetEnumerator();

            Assert.True(e.MoveNext());
            Assert.Equal(4, state);
            Assert.Equal(1, e.Current);

            Assert.True(e.MoveNext());
            Assert.Equal(5, state);
            Assert.Equal(2, e.Current);

            Assert.False(e.MoveNext());
            Assert.Equal(-1, state);
        }

        [Fact]
        public void OutOfBoundNoException()
        {
            Func<int[]> source = () => new[] { 1, 2, 3, 4, 5 };

            Assert.Equal(source(), source().Take(0..6));
            Assert.Equal(source(), source().Take(0..int.MaxValue));

            Assert.Equal(new int[] { 1, 2, 3, 4 }, source().Take(^10..4));
            Assert.Equal(new int[] { 1, 2, 3, 4 }, source().Take(^int.MaxValue..4));
            Assert.Equal(source(), source().Take(^10..6));
            Assert.Equal(source(), source().Take(^int.MaxValue..6));
            Assert.Equal(source(), source().Take(^10..int.MaxValue));
            Assert.Equal(source(), source().Take(^int.MaxValue..int.MaxValue));

            Assert.Empty(source().Take(0..^6));
            Assert.Empty(source().Take(0..^int.MaxValue));
            Assert.Empty(source().Take(4..^6));
            Assert.Empty(source().Take(4..^int.MaxValue));
            Assert.Empty(source().Take(6..^6));
            Assert.Empty(source().Take(6..^int.MaxValue));
            Assert.Empty(source().Take(int.MaxValue..^6));
            Assert.Empty(source().Take(int.MaxValue..^int.MaxValue));

            Assert.Equal(new int[] { 1, 2, 3, 4 }, source().Take(^10..^1));
            Assert.Equal(new int[] { 1, 2, 3, 4 }, source().Take(^int.MaxValue..^1));
            Assert.Empty(source().Take(^0..^6));
            Assert.Empty(source().Take(^1..^6));
            Assert.Empty(source().Take(^6..^6));
            Assert.Empty(source().Take(^10..^6));
            Assert.Empty(source().Take(^int.MaxValue..^6));
            Assert.Empty(source().Take(^0..^int.MaxValue));
            Assert.Empty(source().Take(^1..^int.MaxValue));
            Assert.Empty(source().Take(^6..^int.MaxValue));
            Assert.Empty(source().Take(^int.MaxValue..^int.MaxValue));
        }

        [Fact]
        public void OutOfBoundNoExceptionNotList()
        {
            var source = new[] { 1, 2, 3, 4, 5 };

            Assert.Equal(source, ForceNotCollection(source).Take(0..6));
            Assert.Equal(source, ForceNotCollection(source).Take(0..int.MaxValue));

            Assert.Equal(new int[] { 1, 2, 3, 4 }, ForceNotCollection(source).Take(^10..4));
            Assert.Equal(new int[] { 1, 2, 3, 4 }, ForceNotCollection(source).Take(^int.MaxValue..4));
            Assert.Equal(source, ForceNotCollection(source).Take(^10..6));
            Assert.Equal(source, ForceNotCollection(source).Take(^int.MaxValue..6));
            Assert.Equal(source, ForceNotCollection(source).Take(^10..int.MaxValue));
            Assert.Equal(source, ForceNotCollection(source).Take(^int.MaxValue..int.MaxValue));

            Assert.Empty(ForceNotCollection(source).Take(0..^6));
            Assert.Empty(ForceNotCollection(source).Take(0..^int.MaxValue));
            Assert.Empty(ForceNotCollection(source).Take(4..^6));
            Assert.Empty(ForceNotCollection(source).Take(4..^int.MaxValue));
            Assert.Empty(ForceNotCollection(source).Take(6..^6));
            Assert.Empty(ForceNotCollection(source).Take(6..^int.MaxValue));
            Assert.Empty(ForceNotCollection(source).Take(int.MaxValue..^6));
            Assert.Empty(ForceNotCollection(source).Take(int.MaxValue..^int.MaxValue));

            Assert.Equal(new int[] { 1, 2, 3, 4 }, ForceNotCollection(source).Take(^10..^1));
            Assert.Equal(new int[] { 1, 2, 3, 4 }, ForceNotCollection(source).Take(^int.MaxValue..^1));
            Assert.Empty(ForceNotCollection(source).Take(^0..^6));
            Assert.Empty(ForceNotCollection(source).Take(^1..^6));
            Assert.Empty(ForceNotCollection(source).Take(^6..^6));
            Assert.Empty(ForceNotCollection(source).Take(^10..^6));
            Assert.Empty(ForceNotCollection(source).Take(^int.MaxValue..^6));
            Assert.Empty(ForceNotCollection(source).Take(^0..^int.MaxValue));
            Assert.Empty(ForceNotCollection(source).Take(^1..^int.MaxValue));
            Assert.Empty(ForceNotCollection(source).Take(^6..^int.MaxValue));
            Assert.Empty(ForceNotCollection(source).Take(^int.MaxValue..^int.MaxValue));
        }

        [Fact]
        public void OutOfBoundNoExceptionListPartition()
        {
            var source = new[] { 1, 2, 3, 4, 5 };

            Assert.Equal(source, ListPartitionOrEmpty(source).Take(0..6));
            Assert.Equal(source, ListPartitionOrEmpty(source).Take(0..int.MaxValue));

            Assert.Equal(new int[] { 1, 2, 3, 4 }, ListPartitionOrEmpty(source).Take(^10..4));
            Assert.Equal(new int[] { 1, 2, 3, 4 }, ListPartitionOrEmpty(source).Take(^int.MaxValue..4));
            Assert.Equal(source, ListPartitionOrEmpty(source).Take(^10..6));
            Assert.Equal(source, ListPartitionOrEmpty(source).Take(^int.MaxValue..6));
            Assert.Equal(source, ListPartitionOrEmpty(source).Take(^10..int.MaxValue));
            Assert.Equal(source, ListPartitionOrEmpty(source).Take(^int.MaxValue..int.MaxValue));

            Assert.Empty(ListPartitionOrEmpty(source).Take(0..^6));
            Assert.Empty(ListPartitionOrEmpty(source).Take(0..^int.MaxValue));
            Assert.Empty(ListPartitionOrEmpty(source).Take(4..^6));
            Assert.Empty(ListPartitionOrEmpty(source).Take(4..^int.MaxValue));
            Assert.Empty(ListPartitionOrEmpty(source).Take(6..^6));
            Assert.Empty(ListPartitionOrEmpty(source).Take(6..^int.MaxValue));
            Assert.Empty(ListPartitionOrEmpty(source).Take(int.MaxValue..^6));
            Assert.Empty(ListPartitionOrEmpty(source).Take(int.MaxValue..^int.MaxValue));

            Assert.Equal(new int[] { 1, 2, 3, 4 }, ListPartitionOrEmpty(source).Take(^10..^1));
            Assert.Equal(new int[] { 1, 2, 3, 4 }, ListPartitionOrEmpty(source).Take(^int.MaxValue..^1));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^0..^6));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^1..^6));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^6..^6));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^10..^6));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^int.MaxValue..^6));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^0..^int.MaxValue));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^1..^int.MaxValue));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^6..^int.MaxValue));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^int.MaxValue..^int.MaxValue));
        }

        [Fact]
        public void OutOfBoundNoExceptionEnumerablePartition()
        {
            var source = new[] { 1, 2, 3, 4, 5 };

            Assert.Equal(source, EnumerablePartitionOrEmpty(source).Take(0..6));
            Assert.Equal(source, EnumerablePartitionOrEmpty(source).Take(0..int.MaxValue));

            Assert.Equal(new int[] { 1, 2, 3, 4 }, EnumerablePartitionOrEmpty(source).Take(^10..4));
            Assert.Equal(new int[] { 1, 2, 3, 4 }, EnumerablePartitionOrEmpty(source).Take(^int.MaxValue..4));
            Assert.Equal(source, EnumerablePartitionOrEmpty(source).Take(^10..6));
            Assert.Equal(source, EnumerablePartitionOrEmpty(source).Take(^int.MaxValue..6));
            Assert.Equal(source, EnumerablePartitionOrEmpty(source).Take(^10..int.MaxValue));
            Assert.Equal(source, EnumerablePartitionOrEmpty(source).Take(^int.MaxValue..int.MaxValue));

            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(0..^6));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(0..^int.MaxValue));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(4..^6));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(4..^int.MaxValue));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(6..^6));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(6..^int.MaxValue));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(int.MaxValue..^6));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(int.MaxValue..^int.MaxValue));

            Assert.Equal(new int[] { 1, 2, 3, 4 }, EnumerablePartitionOrEmpty(source).Take(^10..^1));
            Assert.Equal(new int[] { 1, 2, 3, 4 }, EnumerablePartitionOrEmpty(source).Take(^int.MaxValue..^1));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^0..^6));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^1..^6));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^6..^6));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^10..^6));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^int.MaxValue..^6));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^0..^int.MaxValue));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^1..^int.MaxValue));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^6..^int.MaxValue));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^int.MaxValue..^int.MaxValue));
        }

        [Fact]
        public void MutableSource()
        {
            var source1 = new List<int>() { 0, 1, 2, 3, 4 };
            var query1 = source1.Take(3);
            source1.RemoveAt(0);
            source1.InsertRange(2, new[] { -1, -2 });
            Assert.Equal(new[] { 1, 2, -1 }, query1);

            var source2 = new List<int>() { 0, 1, 2, 3, 4 };
            var query2 = source2.Take(0..3);
            source2.RemoveAt(0);
            source2.InsertRange(2, new[] { -1, -2 });
            Assert.Equal(new[] { 1, 2, -1 }, query2);

            var source3 = new List<int>() { 0, 1, 2, 3, 4 };
            var query3 = source3.Take(^6..3);
            source3.RemoveAt(0);
            source3.InsertRange(2, new[] { -1, -2 });
            Assert.Equal(new[] { 1, 2, -1 }, query3);

            var source4 = new List<int>() { 0, 1, 2, 3, 4 };
            var query4 = source4.Take(^6..^3);
            source4.RemoveAt(0);
            source4.InsertRange(2, new[] { -1, -2 });
            Assert.Equal(new[] { 1, 2, -1 }, query4);
        }

        [Fact]
        public void MutableSourceNotList()
        {
            var source1 = new List<int>() { 0, 1, 2, 3, 4 };
            var query1 = ForceNotCollection(source1).Select(i => i).Take(3);
            source1.RemoveAt(0);
            source1.InsertRange(2, new[] { -1, -2 });
            Assert.Equal(new[] { 1, 2, -1 }, query1);

            var source2 = new List<int>() { 0, 1, 2, 3, 4 };
            var query2 = ForceNotCollection(source2).Select(i => i).Take(0..3);
            source2.RemoveAt(0);
            source2.InsertRange(2, new[] { -1, -2 });
            Assert.Equal(new[] { 1, 2, -1 }, query2);

            var source3 = new List<int>() { 0, 1, 2, 3, 4 };
            var query3 = ForceNotCollection(source3).Select(i => i).Take(^6..3);
            source3.RemoveAt(0);
            source3.InsertRange(2, new[] { -1, -2 });
            Assert.Equal(new[] { 1, 2, -1 }, query3);

            var source4 = new List<int>() { 0, 1, 2, 3, 4 };
            var query4 = ForceNotCollection(source4).Select(i => i).Take(^6..^3);
            source4.RemoveAt(0);
            source4.InsertRange(2, new[] { -1, -2 });
            Assert.Equal(new[] { 1, 2, -1 }, query4);
        }

        [Fact]
        public void NonEmptySource_ConsistencyWithCountable()
        {
            Func<int[]> source = () => new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            // Multiple elements in the middle.
            Assert.Equal(source()[^9..5], source().Take(^9..5));
            Assert.Equal(source()[2..7], source().Take(2..7));
            Assert.Equal(source()[2..^4], source().Take(2..^4));
            Assert.Equal(source()[^7..^4], source().Take(^7..^4));

            // Range with default index.
            Assert.Equal(source()[^9..], source().Take(^9..));
            Assert.Equal(source()[2..], source().Take(2..));
            Assert.Equal(source()[..^4], source().Take(..^4));
            Assert.Equal(source()[..6], source().Take(..6));

            // All.
            Assert.Equal(source()[..], source().Take(..));

            // Single element in the middle.
            Assert.Equal(source()[^9..2], source().Take(^9..2));
            Assert.Equal(source()[2..3], source().Take(2..3));
            Assert.Equal(source()[2..^7], source().Take(2..^7));
            Assert.Equal(source()[^5..^4], source().Take(^5..^4));

            // Single element at start.
            Assert.Equal(source()[^10..1], source().Take(^10..1));
            Assert.Equal(source()[0..1], source().Take(0..1));
            Assert.Equal(source()[0..^9], source().Take(0..^9));
            Assert.Equal(source()[^10..^9], source().Take(^10..^9));

            // Single element at end.
            Assert.Equal(source()[^1..10], source().Take(^1..10));
            Assert.Equal(source()[9..10], source().Take(9..10));
            Assert.Equal(source()[9..^0], source().Take(9..^0));
            Assert.Equal(source()[^1..^0], source().Take(^1..^0));

            // No element.
            Assert.Equal(source()[3..3], source().Take(3..3));
            Assert.Equal(source()[6..^4], source().Take(6..^4));
            Assert.Equal(source()[3..^7], source().Take(3..^7));
            Assert.Equal(source()[^3..7], source().Take(^3..7));
            Assert.Equal(source()[^6..^6], source().Take(^6..^6));
        }

        [Fact]
        public void NonEmptySource_ConsistencyWithCountable_NotList()
        {
            int[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            // Multiple elements in the middle.
            Assert.Equal(source[^9..5], ForceNotCollection(source).Take(^9..5));
            Assert.Equal(source[2..7], ForceNotCollection(source).Take(2..7));
            Assert.Equal(source[2..^4], ForceNotCollection(source).Take(2..^4));
            Assert.Equal(source[^7..^4], ForceNotCollection(source).Take(^7..^4));

            // Range with default index.
            Assert.Equal(source[^9..], ForceNotCollection(source).Take(^9..));
            Assert.Equal(source[2..], ForceNotCollection(source).Take(2..));
            Assert.Equal(source[..^4], ForceNotCollection(source).Take(..^4));
            Assert.Equal(source[..6], ForceNotCollection(source).Take(..6));

            // All.
            Assert.Equal(source[..], ForceNotCollection(source).Take(..));

            // Single element in the middle.
            Assert.Equal(source[^9..2], ForceNotCollection(source).Take(^9..2));
            Assert.Equal(source[2..3], ForceNotCollection(source).Take(2..3));
            Assert.Equal(source[2..^7], ForceNotCollection(source).Take(2..^7));
            Assert.Equal(source[^5..^4], ForceNotCollection(source).Take(^5..^4));

            // Single element at start.
            Assert.Equal(source[^10..1], ForceNotCollection(source).Take(^10..1));
            Assert.Equal(source[0..1], ForceNotCollection(source).Take(0..1));
            Assert.Equal(source[0..^9], ForceNotCollection(source).Take(0..^9));
            Assert.Equal(source[^10..^9], ForceNotCollection(source).Take(^10..^9));

            // Single element at end.
            Assert.Equal(source[^1..10], ForceNotCollection(source).Take(^1..10));
            Assert.Equal(source[9..10], ForceNotCollection(source).Take(9..10));
            Assert.Equal(source[9..^0], ForceNotCollection(source).Take(9..^0));
            Assert.Equal(source[^1..^0], ForceNotCollection(source).Take(^1..^0));

            // No element.
            Assert.Equal(source[3..3], ForceNotCollection(source).Take(3..3));
            Assert.Equal(source[6..^4], ForceNotCollection(source).Take(6..^4));
            Assert.Equal(source[3..^7], ForceNotCollection(source).Take(3..^7));
            Assert.Equal(source[^3..7], ForceNotCollection(source).Take(^3..7));
            Assert.Equal(source[^6..^6], ForceNotCollection(source).Take(^6..^6));
        }

        [Fact]
        public void NonEmptySource_ConsistencyWithCountable_ListPartition()
        {
            int[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            // Multiple elements in the middle.
            Assert.Equal(source[^9..5], ListPartitionOrEmpty(source).Take(^9..5));
            Assert.Equal(source[2..7], ListPartitionOrEmpty(source).Take(2..7));
            Assert.Equal(source[2..^4], ListPartitionOrEmpty(source).Take(2..^4));
            Assert.Equal(source[^7..^4], ListPartitionOrEmpty(source).Take(^7..^4));

            // Range with default index.
            Assert.Equal(source[^9..], ListPartitionOrEmpty(source).Take(^9..));
            Assert.Equal(source[2..], ListPartitionOrEmpty(source).Take(2..));
            Assert.Equal(source[..^4], ListPartitionOrEmpty(source).Take(..^4));
            Assert.Equal(source[..6], ListPartitionOrEmpty(source).Take(..6));

            // All.
            Assert.Equal(source[..], ListPartitionOrEmpty(source).Take(..));

            // Single element in the middle.
            Assert.Equal(source[^9..2], ListPartitionOrEmpty(source).Take(^9..2));
            Assert.Equal(source[2..3], ListPartitionOrEmpty(source).Take(2..3));
            Assert.Equal(source[2..^7], ListPartitionOrEmpty(source).Take(2..^7));
            Assert.Equal(source[^5..^4], ListPartitionOrEmpty(source).Take(^5..^4));

            // Single element at start.
            Assert.Equal(source[^10..1], ListPartitionOrEmpty(source).Take(^10..1));
            Assert.Equal(source[0..1], ListPartitionOrEmpty(source).Take(0..1));
            Assert.Equal(source[0..^9], ListPartitionOrEmpty(source).Take(0..^9));
            Assert.Equal(source[^10..^9], ListPartitionOrEmpty(source).Take(^10..^9));

            // Single element at end.
            Assert.Equal(source[^1..10], ListPartitionOrEmpty(source).Take(^1..10));
            Assert.Equal(source[9..10], ListPartitionOrEmpty(source).Take(9..10));
            Assert.Equal(source[9..^0], ListPartitionOrEmpty(source).Take(9..^0));
            Assert.Equal(source[^1..^0], ListPartitionOrEmpty(source).Take(^1..^0));

            // No element.
            Assert.Equal(source[3..3], ListPartitionOrEmpty(source).Take(3..3));
            Assert.Equal(source[6..^4], ListPartitionOrEmpty(source).Take(6..^4));
            Assert.Equal(source[3..^7], ListPartitionOrEmpty(source).Take(3..^7));
            Assert.Equal(source[^3..7], ListPartitionOrEmpty(source).Take(^3..7));
            Assert.Equal(source[^6..^6], ListPartitionOrEmpty(source).Take(^6..^6));
        }

        [Fact]
        public void NonEmptySource_ConsistencyWithCountable_EnumerablePartition()
        {
            int[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            // Multiple elements in the middle.
            Assert.Equal(source[^9..5], EnumerablePartitionOrEmpty(source).Take(^9..5));
            Assert.Equal(source[2..7], EnumerablePartitionOrEmpty(source).Take(2..7));
            Assert.Equal(source[2..^4], EnumerablePartitionOrEmpty(source).Take(2..^4));
            Assert.Equal(source[^7..^4], EnumerablePartitionOrEmpty(source).Take(^7..^4));

            // Range with default index.
            Assert.Equal(source[^9..], EnumerablePartitionOrEmpty(source).Take(^9..));
            Assert.Equal(source[2..], EnumerablePartitionOrEmpty(source).Take(2..));
            Assert.Equal(source[..^4], EnumerablePartitionOrEmpty(source).Take(..^4));
            Assert.Equal(source[..6], EnumerablePartitionOrEmpty(source).Take(..6));

            // All.
            Assert.Equal(source[..], EnumerablePartitionOrEmpty(source).Take(..));

            // Single element in the middle.
            Assert.Equal(source[^9..2], EnumerablePartitionOrEmpty(source).Take(^9..2));
            Assert.Equal(source[2..3], EnumerablePartitionOrEmpty(source).Take(2..3));
            Assert.Equal(source[2..^7], EnumerablePartitionOrEmpty(source).Take(2..^7));
            Assert.Equal(source[^5..^4], EnumerablePartitionOrEmpty(source).Take(^5..^4));

            // Single element at start.
            Assert.Equal(source[^10..1], EnumerablePartitionOrEmpty(source).Take(^10..1));
            Assert.Equal(source[0..1], EnumerablePartitionOrEmpty(source).Take(0..1));
            Assert.Equal(source[0..^9], EnumerablePartitionOrEmpty(source).Take(0..^9));
            Assert.Equal(source[^10..^9], EnumerablePartitionOrEmpty(source).Take(^10..^9));

            // Single element at end.
            Assert.Equal(source[^1..10], EnumerablePartitionOrEmpty(source).Take(^1..10));
            Assert.Equal(source[9..10], EnumerablePartitionOrEmpty(source).Take(9..10));
            Assert.Equal(source[9..^0], EnumerablePartitionOrEmpty(source).Take(9..^0));
            Assert.Equal(source[^1..^0], EnumerablePartitionOrEmpty(source).Take(^1..^0));

            // No element.
            Assert.Equal(source[3..3], EnumerablePartitionOrEmpty(source).Take(3..3));
            Assert.Equal(source[6..^4], EnumerablePartitionOrEmpty(source).Take(6..^4));
            Assert.Equal(source[3..^7], EnumerablePartitionOrEmpty(source).Take(3..^7));
            Assert.Equal(source[^3..7], EnumerablePartitionOrEmpty(source).Take(^3..7));
            Assert.Equal(source[^6..^6], EnumerablePartitionOrEmpty(source).Take(^6..^6));
        }

        [Fact]
        public void NonEmptySource_DoNotThrowException()
        {
            Func<int[]> source = () => new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Empty(source().Take(3..2));
            Assert.Empty(source().Take(6..^5));
            Assert.Empty(source().Take(3..^8));
            Assert.Empty(source().Take(^6..^7));
        }

        [Fact]
        public void NonEmptySource_DoNotThrowException_NotList()
        {
            int[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Empty(ForceNotCollection(source).Take(3..2));
            Assert.Empty(ForceNotCollection(source).Take(6..^5));
            Assert.Empty(ForceNotCollection(source).Take(3..^8));
            Assert.Empty(ForceNotCollection(source).Take(^6..^7));
        }

        [Fact]
        public void NonEmptySource_DoNotThrowException_ListPartition()
        {
            int[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Empty(ListPartitionOrEmpty(source).Take(3..2));
            Assert.Empty(ListPartitionOrEmpty(source).Take(6..^5));
            Assert.Empty(ListPartitionOrEmpty(source).Take(3..^8));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^6..^7));
        }

        [Fact]
        public void NonEmptySource_DoNotThrowException_EnumerablePartition()
        {
            int[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(3..2));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(6..^5));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(3..^8));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^6..^7));
        }

        [Fact]
        public void EmptySource_DoNotThrowException()
        {
            Func<int[]> source = () => new int[] { };

            // Multiple elements in the middle.
            Assert.Empty(source().Take(^9..5));
            Assert.Empty(source().Take(2..7));
            Assert.Empty(source().Take(2..^4));
            Assert.Empty(source().Take(^7..^4));

            // Range with default index.
            Assert.Empty(source().Take(^9..));
            Assert.Empty(source().Take(2..));
            Assert.Empty(source().Take(..^4));
            Assert.Empty(source().Take(..6));

            // All.
            Assert.Equal(source()[..], source().Take(..));

            // Single element in the middle.
            Assert.Empty(source().Take(^9..2));
            Assert.Empty(source().Take(2..3));
            Assert.Empty(source().Take(2..^7));
            Assert.Empty(source().Take(^5..^4));

            // Single element at start.
            Assert.Empty(source().Take(^10..1));
            Assert.Empty(source().Take(0..1));
            Assert.Empty(source().Take(0..^9));
            Assert.Empty(source().Take(^10..^9));

            // Single element at end.
            Assert.Empty(source().Take(^1..^10));
            Assert.Empty(source().Take(9..10));
            Assert.Empty(source().Take(9..^9));
            Assert.Empty(source().Take(^1..^9));

            // No element.
            Assert.Empty(source().Take(3..3));
            Assert.Empty(source().Take(6..^4));
            Assert.Empty(source().Take(3..^7));
            Assert.Empty(source().Take(^3..7));
            Assert.Empty(source().Take(^6..^6));

            // Invalid range.
            Assert.Empty(source().Take(3..2));
            Assert.Empty(source().Take(6..^5));
            Assert.Empty(source().Take(3..^8));
            Assert.Empty(source().Take(^6..^7));
        }

        [Fact]
        public void EmptySource_DoNotThrowException_NotList()
        {
            int[] source = { };

            // Multiple elements in the middle.
            Assert.Empty(ForceNotCollection(source).Take(^9..5));
            Assert.Empty(ForceNotCollection(source).Take(2..7));
            Assert.Empty(ForceNotCollection(source).Take(2..^4));
            Assert.Empty(ForceNotCollection(source).Take(^7..^4));

            // Range with default index.
            Assert.Empty(ForceNotCollection(source).Take(^9..));
            Assert.Empty(ForceNotCollection(source).Take(2..));
            Assert.Empty(ForceNotCollection(source).Take(..^4));
            Assert.Empty(ForceNotCollection(source).Take(..6));

            // All.
            Assert.Equal(source[..], ForceNotCollection(source).Take(..));

            // Single element in the middle.
            Assert.Empty(ForceNotCollection(source).Take(^9..2));
            Assert.Empty(ForceNotCollection(source).Take(2..3));
            Assert.Empty(ForceNotCollection(source).Take(2..^7));
            Assert.Empty(ForceNotCollection(source).Take(^5..^4));

            // Single element at start.
            Assert.Empty(ForceNotCollection(source).Take(^10..1));
            Assert.Empty(ForceNotCollection(source).Take(0..1));
            Assert.Empty(ForceNotCollection(source).Take(0..^9));
            Assert.Empty(ForceNotCollection(source).Take(^10..^9));

            // Single element at end.
            Assert.Empty(ForceNotCollection(source).Take(^1..^10));
            Assert.Empty(ForceNotCollection(source).Take(9..10));
            Assert.Empty(ForceNotCollection(source).Take(9..^9));
            Assert.Empty(ForceNotCollection(source).Take(^1..^9));

            // No element.
            Assert.Empty(ForceNotCollection(source).Take(3..3));
            Assert.Empty(ForceNotCollection(source).Take(6..^4));
            Assert.Empty(ForceNotCollection(source).Take(3..^7));
            Assert.Empty(ForceNotCollection(source).Take(^3..7));
            Assert.Empty(ForceNotCollection(source).Take(^6..^6));

            // Invalid range.
            Assert.Empty(ForceNotCollection(source).Take(3..2));
            Assert.Empty(ForceNotCollection(source).Take(6..^5));
            Assert.Empty(ForceNotCollection(source).Take(3..^8));
            Assert.Empty(ForceNotCollection(source).Take(^6..^7));
        }

        [Fact]
        public void EmptySource_DoNotThrowException_ListPartition()
        {
            int[] source = { };

            // Multiple elements in the middle.
            Assert.Empty(ListPartitionOrEmpty(source).Take(^9..5));
            Assert.Empty(ListPartitionOrEmpty(source).Take(2..7));
            Assert.Empty(ListPartitionOrEmpty(source).Take(2..^4));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^7..^4));

            // Range with default index.
            Assert.Empty(ListPartitionOrEmpty(source).Take(^9..));
            Assert.Empty(ListPartitionOrEmpty(source).Take(2..));
            Assert.Empty(ListPartitionOrEmpty(source).Take(..^4));
            Assert.Empty(ListPartitionOrEmpty(source).Take(..6));

            // All.
            Assert.Equal(source[..], ListPartitionOrEmpty(source).Take(..));

            // Single element in the middle.
            Assert.Empty(ListPartitionOrEmpty(source).Take(^9..2));
            Assert.Empty(ListPartitionOrEmpty(source).Take(2..3));
            Assert.Empty(ListPartitionOrEmpty(source).Take(2..^7));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^5..^4));

            // Single element at start.
            Assert.Empty(ListPartitionOrEmpty(source).Take(^10..1));
            Assert.Empty(ListPartitionOrEmpty(source).Take(0..1));
            Assert.Empty(ListPartitionOrEmpty(source).Take(0..^9));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^10..^9));

            // Single element at end.
            Assert.Empty(ListPartitionOrEmpty(source).Take(^1..^10));
            Assert.Empty(ListPartitionOrEmpty(source).Take(9..10));
            Assert.Empty(ListPartitionOrEmpty(source).Take(9..^9));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^1..^9));

            // No element.
            Assert.Empty(ListPartitionOrEmpty(source).Take(3..3));
            Assert.Empty(ListPartitionOrEmpty(source).Take(6..^4));
            Assert.Empty(ListPartitionOrEmpty(source).Take(3..^7));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^3..7));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^6..^6));

            // Invalid range.
            Assert.Empty(ListPartitionOrEmpty(source).Take(3..2));
            Assert.Empty(ListPartitionOrEmpty(source).Take(6..^5));
            Assert.Empty(ListPartitionOrEmpty(source).Take(3..^8));
            Assert.Empty(ListPartitionOrEmpty(source).Take(^6..^7));
        }

        [Fact]
        public void EmptySource_DoNotThrowException_EnumerablePartition()
        {
            int[] source = { };

            // Multiple elements in the middle.
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^9..5));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(2..7));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(2..^4));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^7..^4));

            // Range with default index.
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^9..));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(2..));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(..^4));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(..6));

            // All.
            Assert.Equal(source[..], EnumerablePartitionOrEmpty(source).Take(..));

            // Single element in the middle.
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^9..2));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(2..3));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(2..^7));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^5..^4));

            // Single element at start.
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^10..1));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(0..1));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(0..^9));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^10..^9));

            // Single element at end.
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^1..^10));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(9..10));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(9..^9));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^1..^9));

            // No element.
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(3..3));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(6..^4));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(3..^7));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^3..7));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^6..^6));

            // Invalid range.
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(3..2));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(6..^5));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(3..^8));
            Assert.Empty(EnumerablePartitionOrEmpty(source).Take(^6..^7));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        public void SkipTakeOnIListIsIList()
        {
            IList<int> list = new ReadOnlyCollection<int>(Enumerable.Range(0, 100).ToList());
            IList<int> skipTake = Assert.IsAssignableFrom<IList<int>>(list.Skip(10).Take(20));

            Assert.True(skipTake.IsReadOnly);
            Assert.Equal(20, skipTake.Count);
            int[] results = new int[20];
            skipTake.CopyTo(results, 0);
            for (int i = 0; i < 20; i++)
            {
                Assert.Equal(i + 10, skipTake[i]);
                Assert.Equal(i + 10, results[i]);
                Assert.True(skipTake.Contains(i + 10));
                Assert.True(skipTake.IndexOf(i + 10) == i);
            }

            Assert.False(skipTake.Contains(9));
            Assert.False(skipTake.Contains(30));

            Assert.Throws<ArgumentOutOfRangeException>(() => skipTake[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => skipTake[20]);

            Assert.Throws<NotSupportedException>(() => skipTake.Add(42));
            Assert.Throws<NotSupportedException>(() => skipTake.Clear());
            Assert.Throws<NotSupportedException>(() => skipTake.Insert(0, 42));
            Assert.Throws<NotSupportedException>(() => skipTake.Remove(42));
            Assert.Throws<NotSupportedException>(() => skipTake.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => skipTake[0] = 42);
        }
    }
}
