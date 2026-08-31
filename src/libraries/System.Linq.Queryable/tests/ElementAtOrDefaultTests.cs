// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Linq.Tests
{
    public class ElementAtOrDefaultTests : EnumerableBasedTests
    {
        [Fact]
        public void IndexInvalid()
        {
            int?[] source = { 9, 8 };

            Assert.Null(source.AsQueryable().ElementAtOrDefault(-1));
            Assert.Null(source.AsQueryable().ElementAtOrDefault(int.MinValue));
            Assert.Null(source.AsQueryable().ElementAtOrDefault(3));
            Assert.Null(source.AsQueryable().ElementAtOrDefault(int.MaxValue));

            Assert.Null(source.AsQueryable().ElementAtOrDefault(^3));
            Assert.Null(source.AsQueryable().ElementAtOrDefault(^int.MaxValue));
            Assert.Null(source.AsQueryable().ElementAtOrDefault(new Index(3)));
            Assert.Null(source.AsQueryable().ElementAtOrDefault(new Index(int.MaxValue)));
        }

        [Fact]
        public void IndexEqualsCount()
        {
            int[] source = { 1, 2, 3, 4 };

            Assert.Equal(default, source.AsQueryable().ElementAtOrDefault(source.Length));
            Assert.Equal(default, source.AsQueryable().ElementAtOrDefault(new Index(source.Length)));
            Assert.Equal(default, source.AsQueryable().ElementAtOrDefault(^0));
        }

        [Fact]
        public void EmptyIndexZero()
        {
            int[] source = { };

            Assert.Equal(default, source.AsQueryable().ElementAtOrDefault(0));
            Assert.Equal(default, source.AsQueryable().ElementAtOrDefault(new Index(0)));
            Assert.Equal(default, source.AsQueryable().ElementAtOrDefault(^0));
        }

        [Fact]
        public void SingleElementIndexZero()
        {
            int[] source = { -4 };

            Assert.Equal(-4, source.ElementAtOrDefault(0));
            Assert.Equal(-4, source.ElementAtOrDefault(new Index(0)));
            Assert.Equal(-4, source.ElementAtOrDefault(^1));
        }

        [Fact]
        public void ManyElementsIndexTargetsLast()
        {
            int[] source = { 9, 8, 0, -5, 10 };

            Assert.Equal(10, source.AsQueryable().ElementAtOrDefault(source.Length - 1));
            Assert.Equal(10, source.AsQueryable().ElementAtOrDefault(new Index(source.Length - 1)));
            Assert.Equal(10, source.AsQueryable().ElementAtOrDefault(^1));
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).ElementAtOrDefault(2));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).ElementAtOrDefault(new Index(2)));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).ElementAtOrDefault(^2));
        }

        [Fact]
        public void ElementAtOrDefault()
        {
            var val1 = new[] { 0, 2, 1 }.AsQueryable().ElementAtOrDefault(1);
            Assert.Equal(2, val1);

            var val2 = new[] { 0, 2, 1 }.AsQueryable().ElementAtOrDefault(new Index(1));
            Assert.Equal(2, val2);

            var val3 = new[] { 0, 2, 1 }.AsQueryable().ElementAtOrDefault(^2);
            Assert.Equal(2, val3);
        }
    }
}
