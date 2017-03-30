// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ContainsTests : EnumerableBasedTests
    {
        [Fact]
        public void Empty()
        {
            int[] source = { };

            Assert.False(source.AsQueryable().Contains(6));
        }

        [Fact]
        public void NotPresent()
        {
            int[] source = { 8, 10, 3, 0, -8 };
            
            Assert.False(source.AsQueryable().Contains(6));
        }

        [Fact]
        public void MultipleMatches()
        {
            int[] source = { 8, 0, 10, 3, 0, -8, 0 };
            
            Assert.True(source.AsQueryable().Contains(0));
        }

        [Fact]
        public void DefaultComparerFromNull()
        {
            string[] source = { "Bob", "Robert", "Tim" };

            Assert.False(source.AsQueryable().Contains("trboeR", null));
            Assert.True(source.AsQueryable().Contains("Tim", null));
        }

        [Fact]
        public void CustomComparerFromNull()
        {
            string[] source = { "Bob", "Robert", "Tim" };
            
            Assert.True(source.AsQueryable().Contains("trboeR", new AnagramEqualityComparer()));
            Assert.False(source.AsQueryable().Contains("nevar", new AnagramEqualityComparer()));
        }
        
        [Fact]
        public void NullSource()
        {
            IQueryable<int> source = null;
            
            Assert.Throws<ArgumentNullException>("source", () => source.Contains(42));
            Assert.Throws<ArgumentNullException>("source", () => source.Contains(42, EqualityComparer<int>.Default));
        }

        [Fact]
        public void Contains1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Contains(1);
            Assert.True(val);
        }

        [Fact]
        public void Contains2()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Contains(1, EqualityComparer<int>.Default);
            Assert.True(val);
        }
    }
}
