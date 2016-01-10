// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace System.Linq.Tests
{
    public class SkipTests : EnumerableTests
    {
        private static IEnumerable<T> GuaranteeNotIList<T>(IEnumerable<T> source)
        {
            foreach (T element in source)
                yield return element;
        }

        [Fact]
        public void SkipSome()
        {
            Assert.Equal(Enumerable.Range(10, 10), NumberRangeGuaranteedNotCollectionType(0, 20).Skip(10));
        }

        [Fact]
        public void SkipSomeIList()
        {
            Assert.Equal(Enumerable.Range(10, 10), NumberRangeGuaranteedNotCollectionType(0, 20).ToList().Skip(10));
        }

        [Fact]
        public void SkipNone()
        {
            Assert.Equal(Enumerable.Range(0, 20), NumberRangeGuaranteedNotCollectionType(0, 20).Skip(0));
        }

        [Fact]
        public void SkipNoneIList()
        {
            Assert.Equal(Enumerable.Range(0, 20), NumberRangeGuaranteedNotCollectionType(0, 20).ToList().Skip(0));
        }

        [Fact]
        public void SkipExcessive()
        {
            Assert.Equal(Enumerable.Empty<int>(), NumberRangeGuaranteedNotCollectionType(0, 20).Skip(42));
        }

        [Fact]
        public void SkipExcessiveIList()
        {
            Assert.Equal(Enumerable.Empty<int>(), NumberRangeGuaranteedNotCollectionType(0, 20).ToList().Skip(42));
        }

        [Fact]
        public void SkipAllExactly()
        {
            Assert.False(NumberRangeGuaranteedNotCollectionType(0, 20).Skip(20).Any());
        }

        [Fact]
        public void SkipAllExactlyIList()
        {
            Assert.False(NumberRangeGuaranteedNotCollectionType(0, 20).Skip(20).ToList().Any());
        }

        [Fact]
        public void SkipThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<DateTime>)null).Skip(3));
        }

        [Fact]
        public void SkipThrowsOnNullIList()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((List<DateTime>)null).Skip(3));
            Assert.Throws<ArgumentNullException>("source", () => ((IList<DateTime>)null).Skip(3));
        }

        [Fact]
        public void SkipOnEmpty()
        {
            Assert.Equal(Enumerable.Empty<int>(), GuaranteeNotIList(Enumerable.Empty<int>()).Skip(0));
            Assert.Equal(Enumerable.Empty<string>(), GuaranteeNotIList(Enumerable.Empty<string>()).Skip(-1));
            Assert.Equal(Enumerable.Empty<double>(), GuaranteeNotIList(Enumerable.Empty<double>()).Skip(1));
        }

        [Fact]
        public void SkipOnEmptyIList()
        {
            // Enumerable.Empty does return an IList, but not guaranteed as such
            // by the spec.
            Assert.Equal(Enumerable.Empty<int>(), Enumerable.Empty<int>().ToList().Skip(0));
            Assert.Equal(Enumerable.Empty<string>(), Enumerable.Empty<string>().ToList().Skip(-1));
            Assert.Equal(Enumerable.Empty<double>(), Enumerable.Empty<double>().ToList().Skip(1));
        }

        [Fact]
        public void SkipNegative()
        {
            Assert.Equal(Enumerable.Range(0, 20), NumberRangeGuaranteedNotCollectionType(0, 20).Skip(-42));
        }

        [Fact]
        public void SkipNegativeIList()
        {
            Assert.Equal(Enumerable.Range(0, 20), NumberRangeGuaranteedNotCollectionType(0, 20).ToList().Skip(-42));
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = GuaranteeNotIList(from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x);

            Assert.Equal(q.Skip(0), q.Skip(0));
        }

        [Fact]
        public void SameResultsRepeatCallsIntQueryIList()
        {
            var q = (from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x).ToList();

            Assert.Equal(q.Skip(0), q.Skip(0));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = GuaranteeNotIList(from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x);

            Assert.Equal(q.Skip(0), q.Skip(0));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQueryIList()
        {
            var q = (from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x).ToList();

            Assert.Equal(q.Skip(0), q.Skip(0));
        }

        [Fact]
        public void SkipOne()
        {
            int?[] source = { 3, 100, 4, null, 10 };
            int?[] expected = { 100, 4, null, 10 };
            
            Assert.Equal(expected, source.Skip(1));
        }

        [Fact]
        public void SkipOneNotIList()
        {
            int?[] source = { 3, 100, 4, null, 10 };
            int?[] expected = { 100, 4, null, 10 };

            Assert.Equal(expected, GuaranteeNotIList(source).Skip(1));
        }

        [Fact]
        public void SkipAllButOne()
        {
            int?[] source = { 3, 100, null, 4, 10 };
            int?[] expected = { 10 };
            
            Assert.Equal(expected, source.Skip(source.Length - 1));
        }

        [Fact]
        public void SkipAllButOneNotIList()
        {
            int?[] source = { 3, 100, null, 4, 10 };
            int?[] expected = { 10 };

            Assert.Equal(expected, GuaranteeNotIList(source.Skip(source.Length - 1)));
        }

        [Fact]
        public void SkipOneMoreThanAll()
        {
            int[] source = { 3, 100, 4, 10 };
            Assert.Empty(source.Skip(source.Length + 1));
        }

        [Fact]
        public void SkipOneMoreThanAllNotIList()
        {
            int[] source = { 3, 100, 4, 10 };
            Assert.Empty(GuaranteeNotIList(source).Skip(source.Length + 1));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Skip(2);
            // Don't insist on this behaviour, but check its correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateIList()
        {
            var iterator = (new[] { 0, 1, 2 }).Skip(2);
            // Don't insist on this behaviour, but check its correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }
    }
}
