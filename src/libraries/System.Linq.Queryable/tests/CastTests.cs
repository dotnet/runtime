// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Tests
{
    public class CastTests : EnumerableBasedTests
    {
        [Fact]
        public void CastIntToLongThrows()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            var rst = q.AsQueryable().Cast<long>();

            Assert.Throws<InvalidCastException>(() => { foreach (var t in rst) ; });
        }

        [Fact]
        public void CastByteToUShortThrows()
        {
            var q = from x in new byte[] { 0, 255, 127, 128, 1, 33, 99 }
                    select x;

            var rst = q.AsQueryable().Cast<ushort>();
            Assert.Throws<InvalidCastException>(() => { foreach (var t in rst) ; });
        }

        [Fact]
        public void EmptySource()
        {
            object[] source = { };
            Assert.Empty(source.AsQueryable().Cast<int>());

        }

        [Fact]
        public void NullableIntFromAppropriateObjects()
        {
            int? i = 10;
            object[] source = { -4, 1, 2, 3, 9, i };
            int?[] expected = { -4, 1, 2, 3, 9, i };

            Assert.Equal(expected, source.AsQueryable().Cast<int?>());
        }
        
        [Fact]
        public void LongFromNullableIntInObjectsThrows()
        {
            int? i = 10;
            object[] source = { -4, 1, 2, 3, 9, i };

            IQueryable<long> cast = source.AsQueryable().Cast<long>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void LongFromNullableIntInObjectsIncludingNullThrows()
        {
            int? i = 10;
            object[] source = { -4, 1, 2, 3, 9, null, i };

            IQueryable<long?> cast = source.AsQueryable().Cast<long?>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void NullableIntFromAppropriateObjectsIncludingNull()
        {
            int? i = 10;
            object[] source = { -4, 1, 2, 3, 9, null, i };
            int?[] expected = { -4, 1, 2, 3, 9, null, i };

            Assert.Equal(expected, source.AsQueryable().Cast<int?>());
        }

        [Fact]
        public void ThrowOnUncastableItem()
        {
            object[] source = { -4, 1, 2, 3, 9, "45" };
            int[] expectedBeginning = { -4, 1, 2, 3, 9 };

            IQueryable<int> cast = source.AsQueryable().Cast<int>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
            Assert.Equal(expectedBeginning, cast.Take(5));
            Assert.Throws<InvalidCastException>(() => cast.ElementAt(5));
        }

        [Fact]
        public void ThrowCastingIntToDouble()
        {
            int[] source = new int[] { -4, 1, 2, 9 };

            IQueryable<double> cast = source.AsQueryable().Cast<double>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }
        
        private static void TestCastThrow<T>(object o)
        {
            byte? i = 10;
            object[] source = { -1, 0, o, i };

            IQueryable<T> cast = source.AsQueryable().Cast<T>();
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

            Assert.Equal(expected, source.AsQueryable().Cast<string>());
        }

        [Fact]
        public void ArrayConversionThrows()
        {
            Assert.Throws<InvalidCastException>(() => new[] { -4 }.AsQueryable().Cast<long>().ToList());
        }

        [Fact]
        public void FirstElementInvalidForCast()
        {
            object[] source = { "Test", 3, 5, 10 };

            IQueryable<int> cast = source.AsQueryable().Cast<int>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void LastElementInvalidForCast()
        {
            object[] source = { -5, 9, 0, 5, 9, "Test" };

            IQueryable<int> cast = source.AsQueryable().Cast<int>();
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

            IQueryable<long> cast = source.AsQueryable().Cast<long>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void ThrowCastingIntToNullableLong()
        {
            int[] source = new int[] { -4, 1, 2, 3, 9 };

            IQueryable<long?> cast = source.AsQueryable().Cast<long?>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void ThrowCastingNullableIntToLong()
        {
            int?[] source = new int?[] { -4, 1, 2, 3, 9 };

            IQueryable<long> cast = source.AsQueryable().Cast<long>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void ThrowCastingNullableIntToNullableLong()
        {
            int?[] source = new int?[] { -4, 1, 2, 3, 9, null };

            IQueryable<long?> cast = source.AsQueryable().Cast<long?>();
            Assert.Throws<InvalidCastException>(() => cast.ToList());
        }

        [Fact]
        public void CastingNullToNonnullableIsNullReferenceException()
        {
            int?[] source = new int?[] { -4, 1, null, 3 };
            IQueryable<int> cast = source.AsQueryable().Cast<int>();
            Assert.Throws<NullReferenceException>(() => cast.ToList());
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<object>)null).Cast<string>());
        }

        [Fact]
        public void Cast()
        {
            var count = (new object[] { 0, 1, 2 }).AsQueryable().Cast<int>().Count();
            Assert.Equal(3, count);
        }
    }
}
