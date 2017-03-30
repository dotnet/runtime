// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class DistinctTests : EnumerableBasedTests
    {
        [Fact]
        public void EmptySource()
        {
            int[] source = { };
            Assert.Empty(source.AsQueryable().Distinct());
        }

        [Fact]
        public void SingleNullElementExplicitlyUseDefaultComparer()
        {
            string[] source = { null };
            string[] expected = { null };

            Assert.Equal(expected, source.AsQueryable().Distinct(EqualityComparer<string>.Default));
        }

        [Fact]
        public void EmptyStringDistinctFromNull()
        {
            string[] source = { null, null, string.Empty };
            string[] expected = { null, string.Empty };

            Assert.Equal(expected, source.AsQueryable().Distinct(EqualityComparer<string>.Default));
        }

        [Fact]
        public void SourceAllDuplicates()
        {
            int[] source = { 5, 5, 5, 5, 5, 5 };
            int[] expected = { 5 };

            Assert.Equal(expected, source.AsQueryable().Distinct());
        }

        [Fact]
        public void AllUnique()
        {
            int[] source = { 2, -5, 0, 6, 10, 9 };

            Assert.Equal(source, source.AsQueryable().Distinct());
        }

        [Fact]
        public void NullSource()
        {
            IQueryable<string> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.Distinct());
        }

        [Fact]
        public void NullSourceCustomComparer()
        {
            IQueryable<string> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.Distinct(StringComparer.Ordinal));
        }

        [Fact]
        public void Distinct1()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().Distinct().Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void Distinct2()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().Distinct(EqualityComparer<int>.Default).Count();
            Assert.Equal(3, count);
        }
    }
}
