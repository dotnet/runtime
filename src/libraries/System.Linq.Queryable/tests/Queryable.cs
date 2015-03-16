// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Linq.Tests
{
    public class QueryableTests
    {
        [Fact]
        public void AsQueryable()
        {
            var x = ((IEnumerable)(new int[] { })).AsQueryable();
        }

        [Fact]
        public void AsQueryableT()
        {
            var x = (new int[] { }).AsQueryable();
        }

        [Fact]
        public void Count1()
        {
            var count = (new int[] { 0 }).AsQueryable().Count();
            Assert.Equal(1, count);
        }

        [Fact]
        public void Count2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Count(n => n > 0);
            Assert.Equal(2, count);
        }

        [Fact]
        public void Where1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Where(n => n > 1).Count();
            Assert.Equal(1, count);
        }

        [Fact]
        public void Where2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Where((n, i) => n > 1 || i == 0).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void OfType()
        {
            var count = (new object[] { 0, (long)1, 2 }).AsQueryable().OfType<int>().Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void Cast()
        {
            var count = (new object[] { 0, 1, 2 }).AsQueryable().Cast<int>().Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void Select1()
        {
            var count = (new object[] { 0, 1, 2 }).AsQueryable().Select(o => (int)o).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void Select2()
        {
            var count = (new object[] { 0, 1, 2 }).AsQueryable().Select((o, i) => (int)o + i).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void SelectMany1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().SelectMany(n => new int[] { n + 4, 5 }).Count();
            Assert.Equal(6, count);
        }

        [Fact]
        public void SelectMany2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().SelectMany((n, i) => new int[] { 4 + i, 5 + n }).Count();
            Assert.Equal(6, count);
        }

        [Fact]
        public void SelectMany3()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().SelectMany(n => new long[] { n + 4, 5 }, (n1, n2) => (n1 + n2).ToString()).Count();
            Assert.Equal(6, count);
        }

        [Fact]
        public void SelectMany4()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().SelectMany((n, i) => new long[] { 4 + i, 5 + n }, (n1, n2) => (n1 + n2).ToString()).Count();
            Assert.Equal(6, count);
        }

        [Fact]
        public void Join1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Join(new int[] { 1, 2, 3 }, n1 => n1, n2 => n2, (n1, n2) => n1 + n2).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void Join2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Join(new int[] { 1, 2, 3 }, n1 => n1, n2 => n2, (n1, n2) => n1 + n2, EqualityComparer<int>.Default).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void GroupJoin1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().GroupJoin(new int[] { 1, 2, 3 }, n1 => n1, n2 => n2, (n1, n2) => n1).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupJoin2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().GroupJoin(new int[] { 1, 2, 3 }, n1 => n1, n2 => n2, (n1, n2) => n1, EqualityComparer<int>.Default).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void OrderBy1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderBy(n => n).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void OrderBy2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderBy(n => n, Comparer<int>.Default).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void OrderByDescending1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderByDescending(n => n).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void OrderByDescending2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderByDescending(n => n, Comparer<int>.Default).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void ThenBy1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderBy(n => n).ThenBy(n => n).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void ThenBy2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderBy(n => n).ThenBy(n => n, Comparer<int>.Default).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void ThenByDescending1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderBy(n => n).ThenByDescending(n => n).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void ThenByDescending2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderBy(n => n).ThenByDescending(n => n, Comparer<int>.Default).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void Take()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Take(2).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void TakeWhile1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().TakeWhile(n => n < 2).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void TakeWhile2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().TakeWhile((n, i) => n + i < 4).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void Skip()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Skip(1).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void SkipWhile1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().SkipWhile(n => n < 1).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void SkipWhile2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().SkipWhile((n, i) => n + i < 1).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void GroupBy1()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy2()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, EqualityComparer<int>.Default).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy3()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, n => n).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy4()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, n => n, EqualityComparer<int>.Default).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy5()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, n => n, (k, g) => k).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy6()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, (k, g) => k).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy7()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, n => n, (k, g) => k, EqualityComparer<int>.Default).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupBy8()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().GroupBy(n => n, (k, g) => k, EqualityComparer<int>.Default).Count();
            Assert.Equal(3, count);
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

        [Fact]
        public void Concat()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Concat((new int[] { 10, 11, 12 }).AsQueryable()).Count();
            Assert.Equal(6, count);
        }

        [Fact]
        public void Zip()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Zip((new int[] { 10, 11, 12 }).AsQueryable(), (n1, n2) => n1 + n2).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void Union1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Union((new int[] { 1, 2, 3 }).AsQueryable()).Count();
            Assert.Equal(4, count);
        }

        [Fact]
        public void Union2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Union((new int[] { 1, 2, 3 }).AsQueryable(), EqualityComparer<int>.Default).Count();
            Assert.Equal(4, count);
        }

        [Fact]
        public void Intersect1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Intersect((new int[] { 1, 2, 3 }).AsQueryable()).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void Intersect2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Intersect((new int[] { 1, 2, 3 }).AsQueryable(), EqualityComparer<int>.Default).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void Except1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Except((new int[] { 1, 2, 3 }).AsQueryable()).Count();
            Assert.Equal(1, count);
        }

        [Fact]
        public void Except2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Except((new int[] { 1, 2, 3 }).AsQueryable(), EqualityComparer<int>.Default).Count();
            Assert.Equal(1, count);
        }

        [Fact]
        public void First1()
        {
            var val = (new int[] { 1, 2 }).AsQueryable().First();
            Assert.Equal(1, val);
        }

        [Fact]
        public void First2()
        {
            var val = (new int[] { 0, 1, 2 }).AsQueryable().First(n => n > 1);
            Assert.Equal(2, val);
        }

        [Fact]
        public void FirstOrDefault1()
        {
            var val = (new int[] { 1, 2 }).AsQueryable().FirstOrDefault();
            Assert.Equal(1, val);
        }

        [Fact]
        public void FirstOrDefault2()
        {
            var val = (new int[] { 0, 1, 2 }).AsQueryable().FirstOrDefault(n => n > 1);
            Assert.Equal(2, val);
        }

        [Fact]
        public void Last1()
        {
            var val = (new int[] { 0, 1, 2 }).AsQueryable().Last();
            Assert.Equal(2, val);
        }

        [Fact]
        public void Last2()
        {
            var val = (new int[] { 0, 1, 2 }).AsQueryable().Last(n => n > 1);
            Assert.Equal(2, val);
        }

        [Fact]
        public void LastOrDefault1()
        {
            var val = (new int[] { 0, 1, 2 }).AsQueryable().LastOrDefault();
            Assert.Equal(2, val);
        }

        [Fact]
        public void LastOrDefault2()
        {
            var val = (new int[] { 0, 1, 2 }).AsQueryable().LastOrDefault(n => n > 1);
            Assert.Equal(2, val);
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

        [Fact]
        public void SingleOrDefault1()
        {
            var val = (new int[] { 2 }).AsQueryable().SingleOrDefault();
            Assert.Equal(2, val);
        }

        [Fact]
        public void SingleOrDefault2()
        {
            var val = (new int[] { 2 }).AsQueryable().SingleOrDefault(n => n > 1);
            Assert.Equal(2, val);
        }

        [Fact]
        public void ElementAt()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().ElementAt(1);
            Assert.Equal(2, val);
        }

        [Fact]
        public void ElementAtOrDefault()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().ElementAtOrDefault(1);
            Assert.Equal(2, val);
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

        [Fact]
        public void Reverse()
        {
            var count = (new int[] { 0, 2, 1 }).AsQueryable().Reverse().Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void SequenceEqual1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().SequenceEqual(new int[] { 0, 2, 1 });
            Assert.True(val);
        }

        [Fact]
        public void SequenceEqual2()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().SequenceEqual(new int[] { 0, 2, 1 }, EqualityComparer<int>.Default);
            Assert.True(val);
        }

        [Fact]
        public void Any1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Any();
            Assert.True(val);
        }

        [Fact]
        public void Any2()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Any(n => n > 1);
            Assert.True(val);
        }

        [Fact]
        public void All()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().All(n => n > 1);
            Assert.False(val);
        }

        [Fact]
        public void LongCount1()
        {
            var count = (new int[] { 0 }).AsQueryable().LongCount();
            Assert.Equal(1L, count);
        }

        [Fact]
        public void LongCount2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().LongCount(n => n > 0);
            Assert.Equal(2L, count);
        }

        [Fact]
        public void Min1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Min();
            Assert.Equal(0, val);
        }

        [Fact]
        public void Min2()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Min(n => n);
            Assert.Equal(0, val);
        }

        [Fact]
        public void Max1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Max();
            Assert.Equal(2, val);
        }

        [Fact]
        public void Max2()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Max(n => n);
            Assert.Equal(2, val);
        }

        [Fact]
        public void Sum1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((int)3, val);
        }

        [Fact]
        public void Sum2()
        {
            var val = (new int?[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((int)3, val);
        }

        [Fact]
        public void Sum3()
        {
            var val = (new long[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((long)3, val);
        }

        [Fact]
        public void Sum4()
        {
            var val = (new long?[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((long)3, val);
        }

        [Fact]
        public void Sum5()
        {
            var val = (new float[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((float)3, val);
        }

        [Fact]
        public void Sum6()
        {
            var val = (new float?[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((float)3, val);
        }

        [Fact]
        public void Sum7()
        {
            var val = (new double[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((double)3, val);
        }

        [Fact]
        public void Sum8()
        {
            var val = (new double?[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((double)3, val);
        }

        [Fact]
        public void Sum9()
        {
            var val = (new decimal[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((decimal)3, val);
        }

        [Fact]
        public void Sum10()
        {
            var val = (new decimal?[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((decimal)3, val);
        }

        [Fact]
        public void Sum11()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((int)3, val);
        }

        [Fact]
        public void Sum12()
        {
            var val = (new int?[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((int)3, val);
        }

        [Fact]
        public void Sum13()
        {
            var val = (new long[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((long)3, val);
        }

        [Fact]
        public void Sum14()
        {
            var val = (new long?[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((long)3, val);
        }

        [Fact]
        public void Sum15()
        {
            var val = (new float[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((float)3, val);
        }

        [Fact]
        public void Sum16()
        {
            var val = (new float?[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((float)3, val);
        }

        [Fact]
        public void Sum17()
        {
            var val = (new double[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((double)3, val);
        }

        [Fact]
        public void Sum18()
        {
            var val = (new double?[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((double)3, val);
        }

        [Fact]
        public void Sum19()
        {
            var val = (new decimal[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((decimal)3, val);
        }

        [Fact]
        public void Sum20()
        {
            var val = (new decimal?[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((decimal)3, val);
        }

        [Fact]
        public void Average1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average2()
        {
            var val = (new int?[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average3()
        {
            var val = (new long[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average4()
        {
            var val = (new long?[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average5()
        {
            var val = (new float[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((float)1, val);
        }

        [Fact]
        public void Average6()
        {
            var val = (new float?[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((float)1, val);
        }

        [Fact]
        public void Average7()
        {
            var val = (new double[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average8()
        {
            var val = (new double?[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average9()
        {
            var val = (new decimal[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((decimal)1, val);
        }

        [Fact]
        public void Average10()
        {
            var val = (new decimal?[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((decimal)1, val);
        }

        [Fact]
        public void Average11()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average12()
        {
            var val = (new int?[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average13()
        {
            var val = (new long[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average14()
        {
            var val = (new long?[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average15()
        {
            var val = (new float[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((float)1, val);
        }

        [Fact]
        public void Average16()
        {
            var val = (new float?[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((float)1, val);
        }

        [Fact]
        public void Average17()
        {
            var val = (new double[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average18()
        {
            var val = (new double?[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average19()
        {
            var val = (new decimal[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((decimal)1, val);
        }

        [Fact]
        public void Average20()
        {
            var val = (new decimal?[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((decimal)1, val);
        }

        [Fact]
        public void Aggregate1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Aggregate((n1, n2) => n1 + n2);
            Assert.Equal((int)3, val);
        }

        [Fact]
        public void Aggregate2()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Aggregate("", (n1, n2) => n1 + n2.ToString());
            Assert.Equal("021", val);
        }

        [Fact]
        public void Aggregate3()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Aggregate(0L, (n1, n2) => n1 + n2, n => n.ToString());
            Assert.Equal("3", val);
        }
    }
}


