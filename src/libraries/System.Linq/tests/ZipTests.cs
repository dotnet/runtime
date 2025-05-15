// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ZipTests : EnumerableTests
    {
        [Fact]
        public void ImplicitTypeParameters()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [2, 5, 9];
            IEnumerable<int> expected = [3, 7, 12];

            Assert.Equal(expected, first.Zip(second, (x, y) => x + y));
        }

        [Fact]
        public void ExplicitTypeParameters()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [2, 5, 9];
            IEnumerable<int> expected = [3, 7, 12];

            Assert.Equal(expected, first.Zip<int, int, int>(second, (x, y) => x + y));
        }

        [Fact]
        public void FirstIsNull()
        {
            IEnumerable<int> first = null;
            IEnumerable<int> second = [2, 5, 9];

            AssertExtensions.Throws<ArgumentNullException>("first", () => first.Zip<int, int, int>(second, (x, y) => x + y));
        }

        [Fact]
        public void SecondIsNull()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = null;

            AssertExtensions.Throws<ArgumentNullException>("second", () => first.Zip<int, int, int>(second, (x, y) => x + y));
        }

        [Fact]
        public void FuncIsNull()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [2, 4, 6];
            Func<int, int, int> func = null;

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => first.Zip(second, func));
        }

        [Fact]
        public void ExceptionThrownFromFirstsEnumerator()
        {
            ThrowsOnMatchEnumerable<int> first = new ThrowsOnMatchEnumerable<int>([1, 3, 3], 2);
            IEnumerable<int> second = [2, 4, 6];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [3, 7, 9];

            Assert.Equal(expected, first.Zip(second, func));

            first = new ThrowsOnMatchEnumerable<int>([1, 2, 3], 2);

            var zip = first.Zip(second, func);

            Assert.Throws<Exception>(() => zip.ToList());
        }

        [Fact]
        public void ExceptionThrownFromSecondsEnumerator()
        {
            ThrowsOnMatchEnumerable<int> second = new ThrowsOnMatchEnumerable<int>([1, 3, 3], 2);
            IEnumerable<int> first = [2, 4, 6];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [3, 7, 9];

            Assert.Equal(expected, first.Zip(second, func));

            second = new ThrowsOnMatchEnumerable<int>([1, 2, 3], 2);

            var zip = first.Zip(second, func);

            Assert.Throws<Exception>(() => zip.ToList());
        }

        [Fact]
        public void FirstAndSecondEmpty()
        {
            IEnumerable<int> first = [];
            IEnumerable<int> second = [];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [];

            Assert.Equal(expected, first.Zip(second, func));
        }


        [Fact]
        public void FirstEmptySecondSingle()
        {
            IEnumerable<int> first = [];
            IEnumerable<int> second = [2];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void FirstEmptySecondMany()
        {
            IEnumerable<int> first = [];
            IEnumerable<int> second = [2, 4, 8];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [];

            Assert.Equal(expected, first.Zip(second, func));
        }


        [Fact]
        public void SecondEmptyFirstSingle()
        {
            IEnumerable<int> first = [1];
            IEnumerable<int> second = [];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void SecondEmptyFirstMany()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void FirstAndSecondSingle()
        {
            IEnumerable<int> first = [1];
            IEnumerable<int> second = [2];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [3];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void FirstAndSecondEqualSize()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [2, 3, 4];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [3, 5, 7];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void SecondOneMoreThanFirst()
        {
            IEnumerable<int> first = [1, 2];
            IEnumerable<int> second = [2, 4, 8];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [3, 6];

            Assert.Equal(expected, first.Zip(second, func));
        }


        [Fact]
        public void SecondManyMoreThanFirst()
        {
            IEnumerable<int> first = [1, 2];
            IEnumerable<int> second = [2, 4, 8, 16];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [3, 6];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void FirstOneMoreThanSecond()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [2, 4];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [3, 6];

            Assert.Equal(expected, first.Zip(second, func));
        }


        [Fact]
        public void FirstManyMoreThanSecond()
        {
            IEnumerable<int> first = [1, 2, 3, 4];
            IEnumerable<int> second = [2, 4];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [3, 6];

            Assert.Equal(expected, first.Zip(second, func));
        }


        [Fact]
        public void DelegateFuncChanged()
        {
            IEnumerable<int> first = [1, 2, 3, 4];
            IEnumerable<int> second = [2, 4, 8];
            Func<int, int, int> func = (x, y) => x + y;
            IEnumerable<int> expected = [3, 6, 11];

            Assert.Equal(expected, first.Zip(second, func));

            func = (x, y) => x - y;
            expected = [-1, -2, -5];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void LambdaFuncChanged()
        {
            IEnumerable<int> first = [1, 2, 3, 4];
            IEnumerable<int> second = [2, 4, 8];
            IEnumerable<int> expected = [3, 6, 11];

            Assert.Equal(expected, first.Zip(second, (x, y) => x + y));

            expected = [-1, -2, -5];

            Assert.Equal(expected, first.Zip(second, (x, y) => x - y));
        }

        [Fact]
        public void FirstHasFirstElementNull()
        {
            IEnumerable<int?> first = [(int?)null, 2, 3, 4];
            IEnumerable<int> second = [2, 4, 8];
            Func<int?, int, int?> func = (x, y) => x + y;
            IEnumerable<int?> expected = [null, 6, 11];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void FirstHasLastElementNull()
        {
            IEnumerable<int?> first = [1, 2, (int?)null];
            IEnumerable<int> second = [2, 4, 6, 8];
            Func<int?, int, int?> func = (x, y) => x + y;
            IEnumerable<int?> expected = [3, 6, null];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void FirstHasMiddleNullValue()
        {
            IEnumerable<int?> first = [1, (int?)null, 3];
            IEnumerable<int> second = [2, 4, 6, 8];
            Func<int?, int, int?> func = (x, y) => x + y;
            IEnumerable<int?> expected = [3, null, 9];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void FirstAllElementsNull()
        {
            IEnumerable<int?> first = [null, null, null];
            IEnumerable<int> second = [2, 4, 6, 8];
            Func<int?, int, int?> func = (x, y) => x + y;
            IEnumerable<int?> expected = [null, null, null];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void SecondHasFirstElementNull()
        {
            IEnumerable<int> first = [1, 2, 3, 4];
            IEnumerable<int?> second = [null, 4, 6];
            Func<int, int?, int?> func = (x, y) => x + y;
            IEnumerable<int?> expected = [null, 6, 9];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void SecondHasLastElementNull()
        {
            IEnumerable<int> first = [1, 2, 3, 4];
            IEnumerable<int?> second = [2, 4, null];
            Func<int, int?, int?> func = (x, y) => x + y;
            IEnumerable<int?> expected = [3, 6, null];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void SecondHasMiddleElementNull()
        {
            IEnumerable<int> first = [1, 2, 3, 4];
            IEnumerable<int?> second = [2, null, 6];
            Func<int, int?, int?> func = (x, y) => x + y;
            IEnumerable<int?> expected = [3, null, 9];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void SecondHasAllElementsNull()
        {
            IEnumerable<int> first = [1, 2, 3, 4];
            IEnumerable<int?> second = [null, null, null];
            Func<int, int?, int?> func = (x, y) => x + y;
            IEnumerable<int?> expected = [null, null, null];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void SecondLargerFirstAllNull()
        {
            IEnumerable<int?> first = [null, null, null, null];
            IEnumerable<int?> second = [null, null, null];
            Func<int?, int?, int?> func = (x, y) => x + y;
            IEnumerable<int?> expected = [null, null, null];

            Assert.Equal(expected, first.Zip(second, func));
        }


        [Fact]
        public void FirstSameSizeSecondAllNull()
        {
            IEnumerable<int?> first = [null, null, null];
            IEnumerable<int?> second = [null, null, null];
            Func<int?, int?, int?> func = (x, y) => x + y;
            IEnumerable<int?> expected = [null, null, null];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void FirstSmallerSecondAllNull()
        {
            IEnumerable<int?> first = [null, null, null];
            IEnumerable<int?> second = [null, null, null, null];
            Func<int?, int?, int?> func = (x, y) => x + y;
            IEnumerable<int?> expected = [null, null, null];

            Assert.Equal(expected, first.Zip(second, func));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Zip(Enumerable.Range(0, 3), (x, y) => x + y);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en is not null && en.MoveNext());
        }

        [Fact]
        public void RunOnce()
        {
            IEnumerable<int?> first = [1, (int?)null, 3];
            IEnumerable<int> second = [2, 4, 6, 8];
            Func<int?, int, int?> func = (x, y) => x + y;
            IEnumerable<int?> expected = [3, null, 9];

            Assert.Equal(expected, first.RunOnce().Zip(second.RunOnce(), func));
        }

        [Fact]
        public void Zip2_ImplicitTypeParameters()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [2, 5, 9];
            IEnumerable<(int, int)> expected = [(1, 2), (2, 5), (3, 9)];

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_ExplicitTypeParameters()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [2, 5, 9];
            IEnumerable<(int, int)> expected = [(1, 2), (2, 5), (3, 9)];

            Assert.Equal(expected, first.Zip<int, int>(second));
        }

        [Fact]
        public void Zip2_FirstIsNull()
        {
            IEnumerable<int> first = null;
            IEnumerable<int> second = [2, 5, 9];

            AssertExtensions.Throws<ArgumentNullException>("first", () => first.Zip<int, int>(second));
        }

        [Fact]
        public void Zip2_SecondIsNull()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = null;

            AssertExtensions.Throws<ArgumentNullException>("second", () => first.Zip<int, int>(second));
        }

        [Fact]
        public void Zip2_ExceptionThrownFromFirstsEnumerator()
        {
            ThrowsOnMatchEnumerable<int> first = new ThrowsOnMatchEnumerable<int>([1, 3, 3], 2);
            IEnumerable<int> second = [2, 4, 6];
            IEnumerable<(int, int)> expected = [(1, 2), (3, 4), (3, 6)];

            Assert.Equal(expected, first.Zip(second));

            first = new ThrowsOnMatchEnumerable<int>([1, 2, 3], 2);

            IEnumerable<(int, int)> zip = first.Zip(second);

            Assert.Throws<Exception>(() => zip.ToList());
        }

        [Fact]
        public void Zip2_ExceptionThrownFromSecondsEnumerator()
        {
            ThrowsOnMatchEnumerable<int> second = new ThrowsOnMatchEnumerable<int>([1, 3, 3], 2);
            IEnumerable<int> first = [2, 4, 6];
            IEnumerable<(int, int)> expected = [(2, 1), (4, 3), (6, 3)];

            Assert.Equal(expected, first.Zip(second));

            second = new ThrowsOnMatchEnumerable<int>([1, 2, 3], 2);

            IEnumerable<(int, int)> zip = first.Zip(second);

            Assert.Throws<Exception>(() => zip.ToList());
        }

        [Fact]
        public void Zip2_FirstAndSecondEmpty()
        {
            IEnumerable<int> first = [];
            IEnumerable<int> second = [];
            IEnumerable<(int, int)> expected = [];

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_FirstEmptySecondSingle()
        {
            IEnumerable<int> first = [];
            IEnumerable<int> second = [2];
            IEnumerable<(int, int)> expected = [];

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_FirstEmptySecondMany()
        {
            IEnumerable<int> first = [];
            IEnumerable<int> second = [2, 4, 8];
            IEnumerable<(int, int)> expected = [];

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_SecondEmptyFirstSingle()
        {
            IEnumerable<int> first = [1];
            IEnumerable<int> second = [];
            IEnumerable<(int, int)> expected = [];

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_SecondEmptyFirstMany()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [];
            IEnumerable<(int, int)> expected = [];

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_FirstAndSecondSingle()
        {
            IEnumerable<int> first = [1];
            IEnumerable<int> second = [2];
            IEnumerable<(int, int)> expected = [(1, 2)];

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_FirstAndSecondEqualSize()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [2, 3, 4];
            IEnumerable<(int, int)> expected = [(1, 2), (2, 3), (3, 4)];

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_SecondOneMoreThanFirst()
        {
            IEnumerable<int> first = [1, 2];
            IEnumerable<int> second = [2, 4, 8];
            IEnumerable<(int, int)> expected = [(1, 2), (2, 4)];

            Assert.Equal(expected, first.Zip(second));
        }


        [Fact]
        public void Zip2_SecondManyMoreThanFirst()
        {
            IEnumerable<int> first = [1, 2];
            IEnumerable<int> second = [2, 4, 8, 16];
            IEnumerable<(int, int)> expected = [(1, 2), (2, 4)];

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_FirstOneMoreThanSecond()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [2, 4];
            IEnumerable<(int, int)> expected = [(1, 2), (2, 4)];

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_FirstManyMoreThanSecond()
        {
            IEnumerable<int> first = [1, 2, 3, 4];
            IEnumerable<int> second = [2, 4];
            IEnumerable<(int, int)> expected = [(1, 2), (2, 4)];

            Assert.Equal(expected, first.Zip(second));
        }

        [Fact]
        public void Zip2_RunOnce()
        {
            IEnumerable<int?> first = [1, (int?)null, 3];
            IEnumerable<int> second = [2, 4, 6, 8];
            IEnumerable<(int?, int)> expected = [(1, 2), (null, 4), (3, 6)];

            Assert.Equal(expected, first.RunOnce().Zip(second.RunOnce()));
        }

        [Fact]
        public void Zip2_NestedTuple()
        {
            IEnumerable<int> first = [1, 3, 5];
            IEnumerable<int> second = [2, 4, 6];
            IEnumerable<(int, int)> third = [(1, 2), (3, 4), (5, 6)];

            Assert.Equal(third, first.Zip(second));

            IEnumerable<string> fourth = ["one", "two", "three"];

            IEnumerable<((int, int), string)> final = [((1, 2), "one"), ((3, 4), "two"), ((5, 6), "three")];
            Assert.Equal(final, third.Zip(fourth));
        }

        [Fact]
        public void Zip2_TupleNames()
        {
            var t = new[] { 1, 2, 3 }.Zip([2, 4, 6]).First();
            Assert.Equal(t.Item1, t.First);
            Assert.Equal(t.Item2, t.Second);
        }

        [Fact]
        public void Zip3_FirstIsNull()
        {
            IEnumerable<int> first = null;
            IEnumerable<int> second = [4, 5, 6];
            IEnumerable<int> third = [7, 8, 9];

            AssertExtensions.Throws<ArgumentNullException>("first", () => first.Zip(second, third));
        }

        [Fact]
        public void Zip3_SecondIsNull()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = null;
            IEnumerable<int> third = [4, 5, 6];

            AssertExtensions.Throws<ArgumentNullException>("second", () => first.Zip(second, third));
        }

        [Fact]
        public void Zip3_ThirdIsNull()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [4, 5, 6];
            IEnumerable<int> third = null;

            AssertExtensions.Throws<ArgumentNullException>("third", () => first.Zip(second, third));
        }

        [Fact]
        public void Zip3_ThirdEmpty()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [4, 5, 6];
            IEnumerable<int> third = [];
            IEnumerable<(int, int, int)> expected = [];

            Assert.Equal(expected, first.Zip(second, third));
        }

        [Fact]
        public void Zip3_ImplicitTypeParameters()
        {
            IEnumerable<int> first = [1, 2];
            IEnumerable<int> second = [3, 4];
            IEnumerable<int> third = [5, 6];
            IEnumerable<(int, int, int)> expected = [(1, 3, 5), (2, 4, 6)];

            Assert.Equal(expected, first.Zip(second, third));
        }

        [Fact]
        public void Zip3_ExplicitTypeParameters()
        {
            IEnumerable<int> first = [1, 2];
            IEnumerable<int> second = [3, 4];
            IEnumerable<int> third = [5, 6];
            IEnumerable<(int, int, int)> expected = [(1, 3, 5), (2, 4, 6)];

            Assert.Equal(expected, first.Zip<int, int, int>(second, third));
        }

        [Fact]
        public void Zip3_ThirdOneMore()
        {
            IEnumerable<int> first = [1, 2];
            IEnumerable<int> second = [3, 4];
            IEnumerable<int> third = [5, 6, 7];
            IEnumerable<(int, int, int)> expected = [(1, 3, 5), (2, 4, 6)];

            Assert.Equal(expected, first.Zip(second, third));
        }

        [Fact]
        public void Zip3_ThirdManyMore()
        {
            IEnumerable<int> first = [1, 2];
            IEnumerable<int> second = [3, 4];
            IEnumerable<int> third = [5, 6, 7, 8];
            IEnumerable<(int, int, int)> expected = [(1, 3, 5), (2, 4, 6)];

            Assert.Equal(expected, first.Zip(second, third));
        }

        [Fact]
        public void Zip3_ThirdOneLess()
        {
            IEnumerable<int> first = [1, 2];
            IEnumerable<int> second = [3, 4];
            IEnumerable<int> third = [5];
            IEnumerable<(int, int, int)> expected = [(1, 3, 5)];

            Assert.Equal(expected, first.Zip(second, third));
        }

        [Fact]
        public void Zip3_ThirdManyLess()
        {
            IEnumerable<int> first = [1, 2, 3];
            IEnumerable<int> second = [3, 4, 5];
            IEnumerable<int> third = [5];
            IEnumerable<(int, int, int)> expected = [(1, 3, 5)];

            Assert.Equal(expected, first.Zip(second, third));
        }

        [Fact]
        public void Zip3_RunOnce()
        {
            IEnumerable<int> first = [1, 2];
            IEnumerable<int> second = [3, 4];
            IEnumerable<int> third = [5, 6];
            IEnumerable<(int, int, int)> expected = [(1, 3, 5), (2, 4, 6)];

            Assert.Equal(expected, first.RunOnce().Zip(second.RunOnce(), third.RunOnce()));
        }
    }
}
