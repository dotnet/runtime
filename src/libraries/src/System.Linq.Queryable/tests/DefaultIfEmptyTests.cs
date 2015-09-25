// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace System.Linq.Tests
{
    public class DefaultIfEmptyTests : EnumerableBasedTests
    {
        [Fact]
        public void EmptyNullableSourceNoDefaultPassed()
        {
            int?[] source = { };
            int?[] expected = { default(int?) };

            Assert.Equal(expected, source.AsQueryable().DefaultIfEmpty());
        }

        [Fact]
        public void EmptyNonNullableSourceNoDefaultPassed()
        {
            int[] source = { };
            int[] expected = { default(int) };

            Assert.Equal(expected, source.AsQueryable().DefaultIfEmpty());
        }

        [Fact]
        public void SeveralElementsNoDefaultPassed()
        {
            int[] source = { 3, -1, 0, 10, 15 };

            Assert.Equal(source, source.AsQueryable().DefaultIfEmpty());
        }

        [Fact]
        public void EmptyNullableDefaultValuePassed()
        {
            int?[] source = { };
            int? defaultValue = 9;
            int?[] expected = { defaultValue };

            Assert.Equal(expected, source.AsQueryable().DefaultIfEmpty(defaultValue));
        }

        [Fact]
        public void EmptyNonNullableDefaultValuePassed()
        {
            int[] source = { };
            int defaultValue = -10;
            int[] expected = { defaultValue };

            Assert.Equal(expected, source.AsQueryable().DefaultIfEmpty(defaultValue));
        }

        [Fact]
        public void NullSource()
        {
            IQueryable<int> source = null;
            
            Assert.Throws<ArgumentNullException>("source", () => source.DefaultIfEmpty());
            Assert.Throws<ArgumentNullException>("source", () => source.DefaultIfEmpty(42));
        }

        [Fact]
        public void DefaultIfEmpty1()
        {
            var count = (new int[] { }).AsQueryable().DefaultIfEmpty().Count();
            Assert.Equal(1, count);
        }

        [Fact]
        public void DefaultIfEmpty2()
        {
            var count = (new int[] { }).AsQueryable().DefaultIfEmpty(3).Count();
            Assert.Equal(1, count);
        }

    }
}
