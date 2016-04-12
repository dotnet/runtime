// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class TakeTests : EnumerableTests
    {
        private static IEnumerable<T> GuaranteeNotIList<T>(IEnumerable<T> source)
        {
            foreach (T element in source)
                yield return element;
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.Take(9), q.Take(9));
        }

        [Fact]
        public void SameResultsRepeatCallsIntQueryIList()
        {
            var q = (from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                     where x > Int32.MinValue
                     select x).ToList();

            Assert.Equal(q.Take(9), q.Take(9));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Take(7), q.Take(7));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQueryIList()
        {
            var q = (from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                     where !String.IsNullOrEmpty(x)
                     select x).ToList();

            Assert.Equal(q.Take(7), q.Take(7));
        }

        [Fact]
        public void SourceEmptyCountPositive()
        {
            int[] source = { };
            Assert.Empty(source.Take(5));
        }

        [Fact]
        public void SourceEmptyCountPositiveNotIList()
        {
            var source = NumberRangeGuaranteedNotCollectionType(0, 0);
            Assert.Empty(source.Take(5));
        }

        [Fact]
        public void SourceNonEmptyCountNegative()
        {
            int[] source = { 2, 5, 9, 1 };
            Assert.Empty(source.Take(-5));
        }

        [Fact]
        public void SourceNonEmptyCountNegativeNotIList()
        {
            var source = GuaranteeNotIList(new[] { 2, 5, 9, 1 });
            Assert.Empty(source.Take(-5));
        }

        [Fact]
        public void SourceNonEmptyCountZero()
        {
            int[] source = { 2, 5, 9, 1 };
            Assert.Empty(source.Take(0));
        }

        [Fact]
        public void SourceNonEmptyCountZeroNotIList()
        {
            var source = GuaranteeNotIList(new[] { 2, 5, 9, 1 });
            Assert.Empty(source.Take(0));
        }

        [Fact]
        public void SourceNonEmptyCountOne()
        {
            int[] source = { 2, 5, 9, 1 };
            int[] expected = { 2 };

            Assert.Equal(expected, source.Take(1));
        }

        [Fact]
        public void SourceNonEmptyCountOneNotIList()
        {
            var source = GuaranteeNotIList(new[] { 2, 5, 9, 1 });
            int[] expected = { 2 };

            Assert.Equal(expected, source.Take(1));
        }

        [Fact]
        public void SourceNonEmptyTakeAllExactly()
        {
            int[] source = { 2, 5, 9, 1 };

            Assert.Equal(source, source.Take(source.Length));
        }

        [Fact]
        public void SourceNonEmptyTakeAllExactlyNotIList()
        {
            var source = GuaranteeNotIList(new[] { 2, 5, 9, 1 });

            Assert.Equal(source, source.Take(source.Count()));
        }

        [Fact]
        public void SourceNonEmptyTakeAllButOne()
        {
            int[] source = { 2, 5, 9, 1 };
            int[] expected = { 2, 5, 9 };

            Assert.Equal(expected, source.Take(3));
        }

        [Fact]
        public void SourceNonEmptyTakeAllButOneNotIList()
        {
            var source = GuaranteeNotIList(new[] { 2, 5, 9, 1 });
            int[] expected = { 2, 5, 9 };

            Assert.Equal(expected, source.Take(3));
        }

        [Fact]
        public void SourceNonEmptyTakeExcessive()
        {
            int?[] source = { 2, 5, null, 9, 1 };

            Assert.Equal(source, source.Take(source.Length + 1));
        }

        [Fact]
        public void SourceNonEmptyTakeExcessiveNotIList()
        {
            var source = GuaranteeNotIList(new int?[] { 2, 5, null, 9, 1 });

            Assert.Equal(source, source.Take(source.Count() + 1));
        }

        [Fact]
        public void ThrowsOnNullSource()
        {
            int[] source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.Take(5));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Take(2);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void Count()
        {
            Assert.Equal(2, NumberRangeGuaranteedNotCollectionType(0, 3).Take(2).Count());
            Assert.Equal(2, new[] { 1, 2, 3 }.Take(2).Count());
            Assert.Equal(0, NumberRangeGuaranteedNotCollectionType(0, 3).Take(0).Count());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateIList()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).ToList().Take(2);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void FollowWithTake()
        {
            var source = new[] { 5, 6, 7, 8 };
            var expected = new[] { 5, 6 };
            Assert.Equal(expected, source.Take(5).Take(3).Take(2).Take(40));
        }

        [Fact]
        public void FollowWithTakeNotIList()
        {
            var source = NumberRangeGuaranteedNotCollectionType(5, 4);
            var expected = new[] { 5, 6 };
            Assert.Equal(expected, source.Take(5).Take(3).Take(2));
        }

        [Fact]
        public void FollowWithSkip()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6 };
            var expected = new[] { 3, 4, 5 };
            Assert.Equal(expected, source.Take(5).Skip(2).Skip(-4));
        }

        [Fact]
        public void FollowWithSkipNotIList()
        {
            var source = NumberRangeGuaranteedNotCollectionType(1, 6);
            var expected = new[] { 3, 4, 5 };
            Assert.Equal(expected, source.Take(5).Skip(2).Skip(-4));
        }

        [Fact]
        public void ElementAt()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6 };
            var taken = source.Take(3);
            Assert.Equal(1, taken.ElementAt(0));
            Assert.Equal(3, taken.ElementAt(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken.ElementAt(3));
        }

        [Fact]
        public void ElementAtNotIList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5, 6 });
            var taken = source.Take(3);
            Assert.Equal(1, taken.ElementAt(0));
            Assert.Equal(3, taken.ElementAt(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => taken.ElementAt(3));
        }

        [Fact]
        public void ElementAtOrDefault()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6 };
            var taken = source.Take(3);
            Assert.Equal(1, taken.ElementAtOrDefault(0));
            Assert.Equal(3, taken.ElementAtOrDefault(2));
            Assert.Equal(0, taken.ElementAtOrDefault(-1));
            Assert.Equal(0, taken.ElementAtOrDefault(3));
        }

        [Fact]
        public void ElementAtOrDefaultNotIList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5, 6 });
            var taken = source.Take(3);
            Assert.Equal(1, taken.ElementAtOrDefault(0));
            Assert.Equal(3, taken.ElementAtOrDefault(2));
            Assert.Equal(0, taken.ElementAtOrDefault(-1));
            Assert.Equal(0, taken.ElementAtOrDefault(3));
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
        }

        [Fact]
        public void FirstNotIList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(1, source.Take(1).First());
            Assert.Equal(1, source.Take(4).First());
            Assert.Equal(1, source.Take(40).First());
            Assert.Throws<InvalidOperationException>(() => source.Take(0).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Take(10).First());
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
        }

        [Fact]
        public void FirstOrDefaultNotIList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(1, source.Take(1).FirstOrDefault());
            Assert.Equal(1, source.Take(4).FirstOrDefault());
            Assert.Equal(1, source.Take(40).FirstOrDefault());
            Assert.Equal(0, source.Take(0).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).Take(10).FirstOrDefault());
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
        }

        [Fact]
        public void LastNotIList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(1, source.Take(1).Last());
            Assert.Equal(5, source.Take(5).Last());
            Assert.Equal(5, source.Take(40).Last());
            Assert.Throws<InvalidOperationException>(() => source.Take(0).Last());
            Assert.Throws<InvalidOperationException>(() => GuaranteeNotIList(Array.Empty<int>()).Take(40).Last());
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
        }

        [Fact]
        public void LastOrDefaultNotIList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(1, source.Take(1).LastOrDefault());
            Assert.Equal(5, source.Take(5).LastOrDefault());
            Assert.Equal(5, source.Take(40).LastOrDefault());
            Assert.Equal(0, source.Take(0).LastOrDefault());
            Assert.Equal(0, GuaranteeNotIList(Array.Empty<int>()).Take(40).LastOrDefault());
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
        }

        [Fact]
        public void ToArrayNotList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(5).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(6).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(40).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(4).ToArray());
            Assert.Equal(1, source.Take(1).ToArray().Single());
            Assert.Empty(source.Take(0).ToArray());
            Assert.Empty(source.Take(-10).ToArray());
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
        }

        [Fact]
        public void ToListNotList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(5).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(6).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Take(40).ToList());
            Assert.Equal(new[] { 1, 2, 3, 4 }, source.Take(4).ToList());
            Assert.Equal(1, source.Take(1).ToList().Single());
            Assert.Empty(source.Take(0).ToList());
            Assert.Empty(source.Take(-10).ToList());
        }

        [Fact]
        public void TakeCanOnlyBeOneList()
        {
            var source = new[] { 2, 4, 6, 8, 10 };
            Assert.Equal(new[] { 2 }, source.Take(1));
            Assert.Equal(new[] { 4 }, source.Skip(1).Take(1));
            Assert.Equal(new[] { 6 }, source.Take(3).Skip(2));
            Assert.Equal(new[] { 2 }, source.Take(3).Take(1));
        }

        [Fact]
        public void TakeCanOnlyBeOneNotList()
        {
            var source = GuaranteeNotIList(new[] { 2, 4, 6, 8, 10 });
            Assert.Equal(new[] { 2 }, source.Take(1));
            Assert.Equal(new[] { 4 }, source.Skip(1).Take(1));
            Assert.Equal(new[] { 6 }, source.Take(3).Skip(2));
            Assert.Equal(new[] { 2 }, source.Take(3).Take(1));
        }

        [Fact]
        public void RepeatEnumerating()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            var taken = source.Take(3);
            Assert.Equal(taken, taken);
        }

        [Fact]
        public void RepeatEnumeratingNotList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            var taken = source.Take(3);
            Assert.Equal(taken, taken);
        }
    }
}
