// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Tests
{
    public partial class ZipTests
    {
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
    }
}
