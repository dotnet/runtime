// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class ZipTests : EnumerableBasedTests
    {
        [Fact]
        public void CorrectResults()
        {
            int[] first = new int[] { 1, 2, 3 };
            int[] second = new int[] { 2, 5, 9 };
            int[] expected = new int[] { 3, 7, 12 };
            Assert.Equal(expected, first.AsQueryable().Zip(second.AsQueryable(), (x, y) => x + y));
        }

        [Fact]
        public void FirstIsNull()
        {
            IQueryable<int> first = null;
            int[] second = new int[] { 2, 5, 9 };
            AssertExtensions.Throws<ArgumentNullException>("source1", () => first.Zip(second.AsQueryable(), (x, y) => x + y));
        }

        [Fact]
        public void SecondIsNull()
        {
            int[] first = new int[] { 1, 2, 3 };
            IQueryable<int> second = null;
            AssertExtensions.Throws<ArgumentNullException>("source2", () => first.AsQueryable().Zip(second, (x, y) => x + y));
        }

        [Fact]
        public void FuncIsNull()
        {
            IQueryable<int> first = new int[] { 1, 2, 3 }.AsQueryable();
            IQueryable<int> second = new int[] { 2, 4, 6 }.AsQueryable();
            Expression<Func<int, int, int>> func = null;
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => first.Zip(second, func));
        }

        [Fact]
        public void Zip()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Zip((new int[] { 10, 11, 12 }).AsQueryable(), (n1, n2) => n1 + n2).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void Zip2_CorrectResults()
        {
            int[] first = new int[] { 1, 2, 3 };
            int[] second = new int[] { 2, 5, 9 };
            var expected = new (int, int)[] { (1, 2), (2, 5), (3, 9) };
            Assert.Equal(expected, first.AsQueryable().Zip(second.AsQueryable()));
        }

        [Fact]
        public void Zip2_FirstIsNull()
        {
            IQueryable<int> first = null;
            int[] second = new int[] { 2, 5, 9 };
            AssertExtensions.Throws<ArgumentNullException>("source1", () => first.Zip(second.AsQueryable()));
        }

        [Fact]
        public void Zip2_SecondIsNull()
        {
            int[] first = new int[] { 1, 2, 3 };
            IQueryable<int> second = null;
            AssertExtensions.Throws<ArgumentNullException>("source2", () => first.AsQueryable().Zip(second));
        }

        [Fact]
        public void Zip2()
        {
            int count = (new int[] { 0, 1, 2 }).AsQueryable().Zip((new int[] { 10, 11, 12 }).AsQueryable()).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void TupleNames()
        {
            int[] first = new int[] { 1 };
            int[] second = new int[] { 2 };
            var tuple = first.AsQueryable().Zip(second.AsQueryable()).First();
            Assert.Equal(tuple.Item1, tuple.First);
            Assert.Equal(tuple.Item2, tuple.Second);
        }

        [Fact]
        public void Zip3_CorrectResults()
        {
            int[] first = new int[] { 1, 3, 5 };
            int[] second = new int[] { 2, 6, 8 };
            int[] third = new int[] { 1, 7, 2 };
            var expected = new (int, int, int)[] { (1, 2, 1), (3, 6, 7), (5, 8, 2) };
            Assert.Equal(expected, first.AsQueryable().Zip(second.AsQueryable(), third.AsQueryable()));
        }


        [Fact]
        public void Zip3_FirstIsNull()
        {
            IQueryable<int> first = null;
            int[] second = new int[] { 2, 6, 8 };
            int[] third = new int[] { 1, 7, 2 };
            AssertExtensions.Throws<ArgumentNullException>("source1", () => first.Zip(second.AsQueryable(), third.AsQueryable()));
        }

        [Fact]
        public void Zip3_SecondIsNull()
        {
            int[] first = new int[] { 1, 3, 5 };
            IQueryable<int> second = null;
            int[] third = new int[] { 1, 7, 2 };
            AssertExtensions.Throws<ArgumentNullException>("source2", () => first.AsQueryable().Zip(second, third.AsQueryable()));
        }

        [Fact]
        public void Zip3_ThirdIsNull()
        {
            int[] first = new int[] { 1, 3, 5 };
            int[] second = new int[] { 2, 6, 8 };
            IQueryable<int> third = null;
            AssertExtensions.Throws<ArgumentNullException>("source3", () => first.AsQueryable().Zip(second.AsQueryable(), third));
        }

        [Fact]
        public void Zip3_TupleNames()
        {
            int[] first = new int[] { 1 };
            int[] second = new int[] { 2 };
            int[] third = new int[] { 3 };
            var tuple = first.AsQueryable().Zip(second.AsQueryable(), third.AsQueryable()).First();
            Assert.Equal(tuple.Item1, tuple.First);
            Assert.Equal(tuple.Item2, tuple.Second);
            Assert.Equal(tuple.Item3, tuple.Third);
        }
    }
}
