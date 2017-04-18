// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class SingleTests : EnumerableBasedTests
    {
        [Fact]
        public void Empty()
        {
            int[] source = { };            
            Assert.Throws<InvalidOperationException>(() => source.AsQueryable().Single());
        }

        [Fact]
        public void SingleElement()
        {
            int[] source = { 4 };
            Assert.Equal(4, source.AsQueryable().Single());
        }

        [Fact]
        public void ManyElement()
        {
            int[] source = { 4, 4, 4, 4, 4 };
            Assert.Throws<InvalidOperationException>(() => source.AsQueryable().Single());
        }

        [Fact]
        public void EmptySourceWithPredicate()
        {
            int[] source = { };
            Assert.Throws<InvalidOperationException>(() => source.AsQueryable().Single(i => i % 2 == 0));
        }

        [Fact]
        public void ManyElementsPredicateFalseForAll()
        {
            int[] source = { 3, 1, 7, 9, 13, 19 };
            Assert.Throws<InvalidOperationException>(() => source.AsQueryable().Single(i => i % 2 == 0));
        }

        [Fact]
        public void ManyElementsPredicateTrueForLast()
        {
            int[] source = { 3, 1, 7, 9, 13, 19, 20 };
            Assert.Equal(20, source.AsQueryable().Single(i => i % 2 == 0));
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(42, 100)]
        public void FindSingleMatch(int target, int range)
        {
            Assert.Equal(target, Enumerable.Range(0, range).AsQueryable().Single(i => i == target));
        }
        
        [Fact]
        public void ThrowsOnNullSource()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Single());
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Single(i => i % 2 == 0));
        }

        [Fact]
        public void ThrowsOnNullPredicate()
        {
            int[] source = { };
            Expression<Func<int, bool>> nullPredicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => source.AsQueryable().Single(nullPredicate));
        }

        [Fact]
        public void Single1()
        {
            var val = (new int[] { 2 }).AsQueryable().Single();
            Assert.Equal(2, val);
        }

        [Fact]
        public void Single2()
        {
            var val = (new int[] { 2 }).AsQueryable().Single(n => n > 1);
            Assert.Equal(2, val);
        }
    }
}
