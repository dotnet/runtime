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
        public void CastOnMultidimensionalArraySucceeds()
        {
            Array array = Array.CreateInstance(typeof(int), 2, 3);
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    array.SetValue(i * 3 + j, i, j);
                }
            }

            int[] result = array.Cast<int>().ToArray();
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal(i, result[i]);
            }
        }

        [Fact]
        public void CastCountReturnsExpectedLength()
        {
            object[] objects = new object[] { "hello", "world" };
            Assert.Equal(2, objects.Cast<string>().Count());
        }

        [Fact]
        public void CastFirstReturnsFirstElement()
        {
            object[] objects = new object[] { "hello", "world" };
            Assert.Equal("hello", objects.Cast<string>().First());
        }

        [Fact]
        public void CastFirstOnEmptySequenceThrows()
        {
            object[] objects = Array.Empty<object>();
            Assert.Throws<InvalidOperationException>(() => objects.Cast<string>().First());
        }

        [Fact]
        public void CastLastReturnsLastElement()
        {
            object[] objects = new object[] { "hello", "world" };
            Assert.Equal("world", objects.Cast<string>().Last());
        }

        [Fact]
        public void CastElementAtReturnsExpectedElement()
        {
            object[] objects = new object[] { "hello", "world" };
            Assert.Equal("world", objects.Cast<string>().ElementAt(1));
        }

        [Fact]
        public void CastElementAtOutOfRangeThrows()
        {
            object[] objects = new object[] { "hello", "world" };
            Assert.Throws<ArgumentOutOfRangeException>(() => objects.Cast<string>().ElementAt(2));
        }

        [Fact]
        public void CastLastOnEmptySequenceThrows()
        {
            object[] objects = Array.Empty<object>();
            Assert.Throws<InvalidOperationException>(() => objects.Cast<string>().Last());
        }

        [Fact]
        public void CastSelectProcessesEachElement()
        {
            object[] objects = new object[] { "hello", "world!" };
            Assert.Equal(new[] { 5, 6 }, objects.Cast<string>().Select(s => s.Length));
        }

        [Fact]
        public void CastSkipSkipsElements()
        {
            object[] objects = new object[] { "hello", "there", "world" };
            Assert.Equal(new[] { "world" }, objects.Cast<string>().Skip(2));
        }

        [Fact]
        public void CastTakeTakesElements()
        {
            object[] objects = new object[] { "hello", "there", "world" };
            Assert.Equal(new[] { "hello", "there" }, objects.Cast<string>().Take(2));
        }
    }
}
