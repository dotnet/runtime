// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Tests
{
    public class ElementAtOrDefaultTests : EnumerableBasedTests
    {
        [Fact]
        public void IndexNegative()
        {
            int?[] source = { 9, 8 };
            
            Assert.Null(source.AsQueryable().ElementAtOrDefault(-1));
        }

        [Fact]
        public void IndexEqualsCount()
        {
            int[] source = { 1, 2, 3, 4 };
            
            Assert.Equal(default(int), source.AsQueryable().ElementAtOrDefault(source.Length));
        }

        [Fact]
        public void EmptyIndexZero()
        {
            int[] source = { };
            
            Assert.Equal(default(int), source.AsQueryable().ElementAtOrDefault(0));
        }

        [Fact]
        public void SingleElementIndexZero()
        {
            int[] source = { -4 };
            
            Assert.Equal(-4, source.ElementAtOrDefault(0));
        }

        [Fact]
        public void ManyElementsIndexTargetsLast()
        {
            int[] source = { 9, 8, 0, -5, 10 };
            
            Assert.Equal(10, source.AsQueryable().ElementAtOrDefault(source.Length - 1));
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).ElementAtOrDefault(2));
        }

        [Fact]
        public void ElementAtOrDefault()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().ElementAtOrDefault(1);
            Assert.Equal(2, val);
        }
    }
}
