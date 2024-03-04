// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class OfTypeTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.OfType<int>(), q.OfType<int>());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.OfType<int>(), q.OfType<int>());
        }

        [Fact]
        public void EmptySource()
        {
            object[] source = { };
            Assert.Empty(source.OfType<int>());
        }

        [Fact]
        public void LongSequenceFromIntSource()
        {
            int[] source = { 99, 45, 81 };
            Assert.Empty(source.OfType<long>());

        }

        [Fact]
        public void HeterogenousSourceNoAppropriateElements()
        {
            object[] source = { "Hello", 3.5, "Test" };
            Assert.Empty(source.OfType<int>());
        }

        [Fact]
        public void HeterogenousSourceOnlyFirstOfType()
        {
            object[] source = { 10, "Hello", 3.5, "Test" };
            int[] expected = { 10 };

            Assert.Equal(expected, source.OfType<int>());
        }

        [Fact]
        public void AllElementsOfNullableTypeNullsSkipped()
        {
            object[] source = { 10, -4, null, null, 4, 9 };
            int?[] expected = { 10, -4, 4, 9 };

            Assert.Equal(expected, source.OfType<int?>());
        }

        [Fact]
        public void HeterogenousSourceSomeOfType()
        {
            object[] source = { 3.5m, -4, "Test", "Check", 4, 8.0, 10.5, 9 };
            int[] expected = { -4, 4, 9 };

            Assert.Equal(expected, source.OfType<int>());
        }

        [Fact]
        public void RunOnce()
        {
            object[] source = { 3.5m, -4, "Test", "Check", 4, 8.0, 10.5, 9 };
            int[] expected = { -4, 4, 9 };

            Assert.Equal(expected, source.RunOnce().OfType<int>());
        }

        [Fact]
        public void IntFromNullableInt()
        {
            int[] source = { -4, 4, 9 };
            int?[] expected = { -4, 4, 9 };

            Assert.Equal(expected, source.OfType<int?>());
        }

        [Fact]
        public void IntFromNullableIntWithNulls()
        {
            int?[] source = { null, -4, 4, null, 9 };
            int[] expected = { -4, 4, 9 };

            Assert.Equal(expected, source.OfType<int>());
        }

        [Fact]
        public void NullableDecimalFromString()
        {
            string[] source = { "Test1", "Test2", "Test9" };
            Assert.Empty(source.OfType<decimal?>());
        }

        [Fact]
        public void LongFromDouble()
        {
            long[] source = { 99L, 45L, 81L };
            Assert.Empty(source.OfType<double>());
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<object>)null).OfType<string>());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).OfType<int>();
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ValueType_ReturnsOriginal()
        {
            IEnumerable<int> e = Enumerable.Range(0, 10);
            Assert.Same(e, e.OfType<int>());
        }

        [Fact]
        public void NullableValueType_ReturnsNewEnumerable()
        {
            IEnumerable<int?> e = Enumerable.Range(0, 10).Select(i => (int?)i);
            Assert.NotSame(e, e.OfType<int>());
            Assert.NotSame(e, e.OfType<int?>());
        }

        [Fact]
        public void ReferenceType_ReturnsNewEnumerable()
        {
            IEnumerable<object> e = Enumerable.Range(0, 10).Select(i => (object)i);
            Assert.NotSame(e, e.OfType<int>());
            Assert.NotSame(e, e.OfType<int?>());
            Assert.NotSame(e, e.OfType<object>());
            Assert.NotSame(e, e.OfType<object?>());
        }

        [Fact]
        public void ToArray()
        {
            IEnumerable<object> source = new object[] { 1, 2, 3, 4, 5 };
            Assert.Equal(new int[] { 1, 2, 3, 4, 5 }, source.OfType<int>().ToArray());
            Assert.Empty(source.OfType<double>().ToArray());
        }

        [Fact]
        public void ToList()
        {
            IEnumerable<object> source = new object[] { 1, 2, 3, 4, 5 };
            Assert.Equal(new int[] { 1, 2, 3, 4, 5 }, source.OfType<int>().ToList());
            Assert.Empty(source.OfType<double>().ToList());
        }

        [Fact]
        public void Count()
        {
            Assert.Equal(0, new object[] { }.OfType<string>().Count());
            Assert.Equal(1, new object[] { "abc" }.OfType<string>().Count());
            Assert.Equal(2, new object[] { "abc", "def" }.OfType<string>().Count());
            Assert.Equal(2, new object[] { "abc", 42, "def" }.OfType<string>().Count());
            Assert.Equal(2, new object[] { "abc", 42, null, "def" }.OfType<string>().Count());
            Assert.Equal(3, new object[] { null, new object(), null, new object(), new object(), null }.OfType<object>().Count());

            Assert.False(new object[] { "abc" }.OfType<string>().TryGetNonEnumeratedCount(out _));
            Assert.False(new object[] { "abc" }.OfType<int>().TryGetNonEnumeratedCount(out _));
            Assert.False(new int[] { 42 }.OfType<object>().TryGetNonEnumeratedCount(out _));
        }

        [Fact]
        public void First_Last_ElementAt()
        {
            IEnumerable<object> source = new object[] { 1, 2, 3, 4, 5 };

            Assert.Equal(1, source.OfType<int>().First());
            Assert.Equal(0, source.OfType<long>().FirstOrDefault());

            Assert.Equal(5, source.OfType<int>().Last());
            Assert.Equal(0, source.OfType<long>().LastOrDefault());

            Assert.Equal(4, source.OfType<int>().ElementAt(3));
            Assert.Equal(0, source.OfType<long>().ElementAtOrDefault(6));
        }

        [Fact]
        public void OfTypeSelect()
        {
            IEnumerable<object> objects = new object[] { "1", null, "22", null, 3, 4, "55555" };
            Assert.Equal(new int[] { 1, 2, 5 }, objects.OfType<string>().Select(s => s.Length));

            Assert.Equal(new int[] { 1, 2, 3, 4, 5 }, new int[] { 1, 2, 3, 4, 5 }.OfType<object>().Select(o => (int)o));
        }

        [Fact]
        public void MultipleIterations()
        {
            var orig = new object[] { null, null, null, null, null };
            IEnumerable<object> objects = orig.OfType<object>();

            for (int i = 0; i < orig.Length; i++)
            {
                orig[i] = i.ToString();

                int count = 0;
                foreach (object o in objects) count++;
                Assert.Equal(i + 1, count);
            }
        }
    }
}
