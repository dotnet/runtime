// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class CastTests : EnumerableTests
    {
        [Fact]
        public void CastIntToLongThrows()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            var rst = q.Cast<long>();

            Assert.Throws<InvalidCastException>(() => { foreach (var t in rst) ; });
        }

        [Fact]
        public void CastByteToUShortThrows()
        {
            var q = from x in new byte[] { 0, 255, 127, 128, 1, 33, 99 }
                    select x;

            var rst = q.Cast<ushort>();
            Assert.Throws<InvalidCastException>(() => { foreach (var t in rst) ; });
        }

        [Fact]
        public void EmptySource()
        {
            object[] source = { };
            Assert.Empty(source.Cast<int>());
        }

        [Fact]
        public void NullableIntFromAppropriateObjects()
        {
            int? i = 10;
            object[] source = { -4, 1, 2, 3, 9, i };
            int?[] expected = { -4, 1, 2, 3, 9, i };

            Assert.Equal(expected, source.Cast<int?>());
        }

        [Fact]
        public void NullableIntFromAppropriateObjectsRunOnce()
        {
            int? i = 10;
            object[] source = { -4, 1, 2, 3, 9, i };
            int?[] expected = { -4, 1, 2, 3, 9, i };

            Assert.Equal(expected, source.RunOnce().Cast<int?>());
        }

        [Fact]
        public void LongFromNullableIntInObjectsThrows()
        {
            int? i = 10;
            object[] source = { -4, 1, 2, 3, 9, i };

            IEnumerable<long> cast = source.Cast<long>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void LongFromNullableIntInObjectsIncludingNullThrows()
        {
            int? i = 10;
            object[] source = { -4, 1, 2, 3, 9, null, i };

            IEnumerable<long?> cast = source.Cast<long?>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void NullableIntFromAppropriateObjectsIncludingNull()
        {
            int? i = 10;
            object[] source = { -4, 1, 2, 3, 9, null, i };
            int?[] expected = { -4, 1, 2, 3, 9, null, i };

            Assert.Equal(expected, source.Cast<int?>());
        }

        [Fact]
        public void ThrowOnUncastableItem()
        {
            object[] source = { -4, 1, 2, 3, 9, "45" };
            int[] expectedBeginning = { -4, 1, 2, 3, 9 };

            IEnumerable<int> cast = source.Cast<int>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
            Assert.Equal(expectedBeginning, cast.Take(5));
            Assert.Throws<InvalidCastException>(() => cast.ElementAt(5));
        }

        [Fact]
        public void ThrowCastingIntToDouble()
        {
            int[] source = new int[] { -4, 1, 2, 9 };

            IEnumerable<double> cast = source.Cast<double>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        private static void TestCastThrow<T>(object o)
        {
            byte? i = 10;
            object[] source = { -1, 0, o, i };

            IEnumerable<T> cast = source.Cast<T>();

            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void ThrowOnHeterogenousSource()
        {
            TestCastThrow<long?>(null);
            TestCastThrow<long>(9L);
        }

        [Fact]
        public void CastToString()
        {
            object[] source = { "Test1", "4.5", null, "Test2" };
            string[] expected = { "Test1", "4.5", null, "Test2" };

            Assert.Equal(expected, source.Cast<string>());
        }

        [Fact]
        public void CastToStringRunOnce()
        {
            object[] source = { "Test1", "4.5", null, "Test2" };
            string[] expected = { "Test1", "4.5", null, "Test2" };

            Assert.Equal(expected, source.RunOnce().Cast<string>());
        }

        [Fact]
        public void ArrayConversionThrows()
        {
            Assert.Throws<InvalidCastException>(() => new[] { -4 }.Cast<long>().ToList());
        }

        [Fact]
        public void FirstElementInvalidForCast()
        {
            object[] source = { "Test", 3, 5, 10 };

            IEnumerable<int> cast = source.Cast<int>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void LastElementInvalidForCast()
        {
            object[] source = { -5, 9, 0, 5, 9, "Test" };

            IEnumerable<int> cast = source.Cast<int>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void NullableIntFromNullsAndInts()
        {
            object[] source = { 3, null, 5, -4, 0, null, 9 };
            int?[] expected = { 3, null, 5, -4, 0, null, 9 };

            Assert.Equal(expected, source.Cast<int?>());
        }

        [Fact]
        public void ThrowCastingIntToLong()
        {
            int[] source = new int[] { -4, 1, 2, 3, 9 };

            IEnumerable<long> cast = source.Cast<long>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void ThrowCastingIntToNullableLong()
        {
            int[] source = new int[] { -4, 1, 2, 3, 9 };

            IEnumerable<long?> cast = source.Cast<long?>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void ThrowCastingNullableIntToLong()
        {
            int?[] source = new int?[] { -4, 1, 2, 3, 9 };

            IEnumerable<long> cast = source.Cast<long>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void ThrowCastingNullableIntToNullableLong()
        {
            int?[] source = new int?[] { -4, 1, 2, 3, 9, null };

            IEnumerable<long?> cast = source.Cast<long?>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void CastingNullToNonnullableIsNullReferenceException()
        {
            int?[] source = new int?[] { -4, 1, null, 3 };
            IEnumerable<int> cast = source.Cast<int>();
            Assert.Throws<NullReferenceException>(() => cast.ToList());
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<object>)null).Cast<string>());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = new object[0].Where(i => i != null).Cast<string>();
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<string>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void TargetTypeIsSourceType_Nop()
        {
            object[] values = new string[] { "hello", "world" };
            Assert.Same(values, values.Cast<string>());
        }

        [Fact]
        public void CastProducesIListFromIList()
        {
            object[] values = new object[] { "hello", "world" };

            IList<string> castList = (IList<string>)values.Cast<string>();
            Assert.NotNull(castList);

            Assert.Equal(2, castList.Count);
            Assert.Equal("hello", castList[0]);
            Assert.Equal("world", castList[1]);
            Assert.True(castList.IsReadOnly);
            Assert.True(castList.Contains("hello"));
            Assert.True(castList.Contains("world"));
            Assert.False(castList.Contains("hello, world"));
            Assert.Equal(0, castList.IndexOf("hello"));
            Assert.Equal(1, castList.IndexOf("world"));
            Assert.Equal(-1, castList.IndexOf("hello, world"));

            Assert.Throws<NotSupportedException>(() => castList.Add("foo"));
            Assert.Throws<NotSupportedException>(() => castList.Insert(0, "foo"));
            Assert.Throws<NotSupportedException>(() => castList.Remove("foo"));
            Assert.Throws<NotSupportedException>(() => castList.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => castList.Clear());
            Assert.Throws<IndexOutOfRangeException>(() => castList[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => castList[2]);
            Assert.Throws<ArgumentNullException>(() => castList.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => castList.CopyTo(new string[1], 0));

            IReadOnlyList<string> castReadOnlyList = (IReadOnlyList<string>)castList;
            Assert.Equal(2, castReadOnlyList.Count);
            Assert.Equal("hello", castReadOnlyList[0]);
            Assert.Equal("world", castReadOnlyList[1]);
        }

        [Fact]
        public void CastProducesNonIListFromNonIList()
        {
            IEnumerable<string> e = new object[] { "hello", "world" }.Select(s => s).Cast<string>();
            Assert.IsNotAssignableFrom<IList<string>>(e);
        }
    }
}
