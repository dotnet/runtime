// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public partial class ZipTests
    {
        [Fact]
        public void Zip2_ImplicitTypeParameters()
        {
            IEnumerable<int> first = new int[] { 1, 2, 3 };
            IEnumerable<int> second = new int[] { 2, 5, 9 };
            IEnumerable<(int, int)> expected = new (int,int)[] { (1,2), (2,5), (3,9) };

            Assert.Equal(expected, first.Zip(second));
        }
        
        [Fact]
        public void Zip2_ExplicitTypeParameters()
        {
            IEnumerable<int> first = new int[] { 1, 2, 3 };
            IEnumerable<int> second = new int[] { 2, 5, 9 };
            IEnumerable<(int, int)> expected = new (int,int)[] { (1,2), (2,5), (3,9) };

            Assert.Equal(expected, first.Zip<int, int>(second));
        }

        [Fact]
        public void Zip2_FirstIsNull()
        {
            IEnumerable<int> first = null;
            IEnumerable<int> second = new int[] { 2, 5, 9 };

            AssertExtensions.Throws<ArgumentNullException>("first", () => first.Zip<int, int>(second));
        }

        [Fact]
        public void Zip2_SecondIsNull()
        {
            IEnumerable<int> first = new int[] { 1, 2, 3 };
            IEnumerable<int> second = null;

            AssertExtensions.Throws<ArgumentNullException>("second", () => first.Zip<int, int>(second));
        }

        [Fact]
        public void Zip2_ExceptionThrownFromFirstsEnumerator()
        {
            ThrowsOnMatchEnumerable<int> first = new ThrowsOnMatchEnumerable<int>(new int[] { 1, 3, 3 }, 2);
            IEnumerable<int> second = new int[] { 2, 4, 6 };
            IEnumerable<(int, int)> expected = new (int,int)[] { (1,2), (3,4), (3,6) };
            
            Assert.Equal(expected, first.Zip(second));

            first = new ThrowsOnMatchEnumerable<int>(new int[] { 1, 2, 3 }, 2);

            IEnumerable<(int, int)> zip = first.Zip(second);
            
            Assert.Throws<Exception>(() => zip.ToList());
        }

        [Fact]
        public void Zip2_ExceptionThrownFromSecondsEnumerator()
        {
            ThrowsOnMatchEnumerable<int> second = new ThrowsOnMatchEnumerable<int>(new int[] { 1, 3, 3 }, 2);
            IEnumerable<int> first = new int[] { 2, 4, 6 };
            IEnumerable<(int, int)> expected = new (int,int)[] { (2,1), (4,3), (6,3) };

            Assert.Equal(expected, first.Zip(second));

            second = new ThrowsOnMatchEnumerable<int>(new int[] { 1, 2, 3 }, 2);

            IEnumerable<(int, int)> zip = first.Zip(second);

            Assert.Throws<Exception>(() => zip.ToList());
        }

        [Fact]
        public void Zip2_FirstAndSecondEmpty()
        {
            IEnumerable<int> first = new int[] { };
            IEnumerable<int> second = new int[] { };
            IEnumerable<(int, int)> expected = new (int, int)[] { };

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_FirstEmptySecondSingle()
        {
            IEnumerable<int> first = new int[] { };
            IEnumerable<int> second = new int[] { 2 };
            IEnumerable<(int, int)> expected = new (int, int)[] { };

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_FirstEmptySecondMany()
        {
            IEnumerable<int> first = new int[] { };
            IEnumerable<int> second = new int[] { 2, 4, 8 };
            IEnumerable<(int, int)> expected = new (int, int)[] { };

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_SecondEmptyFirstSingle()
        {
            IEnumerable<int> first = new int[] { 1 };
            IEnumerable<int> second = new int[] { };
            IEnumerable<(int, int)> expected = new (int, int)[] { };

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_SecondEmptyFirstMany()
        {
            IEnumerable<int> first = new int[] { 1, 2, 3 };
            IEnumerable<int> second = new int[] { };
            IEnumerable<(int, int)> expected = new (int, int)[] { };

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_FirstAndSecondSingle()
        {
            IEnumerable<int> first = new int[] { 1 };
            IEnumerable<int> second = new int[] { 2 };
            IEnumerable<(int, int)> expected = new (int, int)[] { (1, 2) };

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_FirstAndSecondEqualSize()
        {
            IEnumerable<int> first = new int[] { 1, 2, 3 };
            IEnumerable<int> second = new int[] { 2, 3, 4 };
            IEnumerable<(int, int)> expected = new (int, int)[] { (1, 2), (2, 3), (3, 4) };

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_SecondOneMoreThanFirst()
        {
            IEnumerable<int> first = new int[] { 1, 2 };
            IEnumerable<int> second = new int[] { 2, 4, 8 };
            IEnumerable<(int, int)> expected = new (int, int)[] { (1, 2), (2, 4) };

            Assert.Equal(expected, first.Zip(second));
        }


        [Fact]
        public void Zip2_SecondManyMoreThanFirst()
        {
            IEnumerable<int> first = new int[] { 1, 2 };
            IEnumerable<int> second = new int[] { 2, 4, 8, 16 };
            IEnumerable<(int, int)> expected = new (int, int)[] { (1, 2), (2, 4) };

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_FirstOneMoreThanSecond()
        {
            IEnumerable<int> first = new int[] { 1, 2, 3 };
            IEnumerable<int> second = new int[] { 2, 4 };
            IEnumerable<(int, int)> expected = new (int, int)[] { (1, 2), (2, 4) };

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_FirstManyMoreThanSecond()
        {
            IEnumerable<int> first = new int[] { 1, 2, 3, 4 };
            IEnumerable<int> second = new int[] { 2, 4 };
            IEnumerable<(int, int)> expected = new (int, int)[] { (1, 2), (2, 4) };

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_RunOnce()
        {
            IEnumerable<int?> first = new[] { 1, (int?)null, 3 };
            IEnumerable<int> second = new[] { 2, 4, 6, 8 };
            IEnumerable<(int?, int)> expected = new (int?, int)[] { (1, 2), (null, 4), (3, 6) };

            Assert.Equal(expected, first.RunOnce().Zip(second.RunOnce()));
        }

        [Fact]
        public void Zip2_NestedTuple()
        {
            IEnumerable<int> first = new[] { 1, 3, 5 };
            IEnumerable<int> second = new[] { 2, 4, 6 };
            IEnumerable<(int, int)> third = new[] { (1, 2), (3, 4), (5, 6) };

            Assert.Equal(third, first.Zip(second));

            IEnumerable<string> fourth = new[] { "one", "two", "three" };

            IEnumerable<((int, int), string)> final = new[] { ((1, 2), "one"), ((3, 4), "two"), ((5, 6), "three") };
            Assert.Equal(final, third.Zip(fourth));
        }

        [Fact]
        public void Zip2_TupleNames()
        {
            var t = new[] { 1, 2, 3 }.Zip(new[] { 2, 4, 6 }).First();
            Assert.Equal(t.Item1, t.First);
            Assert.Equal(t.Item2, t.Second);
        }
    }
}
