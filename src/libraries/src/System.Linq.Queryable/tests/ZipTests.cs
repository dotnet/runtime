// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
