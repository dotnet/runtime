// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class AppendPrependTests
    {
        [Fact]
        public void SourceNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Append(1));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Prepend(1));
        }

        [Theory]
        [MemberData(nameof(AppendPrependOnceData))]
        public void AppendOnce<TSource>(IEnumerable<TSource> source, TSource element)
        {
            Assert.Equal(source.Concat(new[] { element }).AsQueryable(), source.AsQueryable().Append(element));
        }

        [Theory]
        [MemberData(nameof(AppendPrependOnceData))]
        public void PrependOnce<TSource>(IEnumerable<TSource> source, TSource element)
        {
            Assert.Equal(new[] { element }.Concat(source).AsQueryable(), source.AsQueryable().Prepend(element));
        }

        public static IEnumerable<object[]> AppendPrependOnceData()
        {
            return new[]
            {
                new object[] { Enumerable.Range(1, 10), 11 },
                new object[] { Enumerable.Repeat(new object(), 10), null }
            };
        }

        [Theory]
        [MemberData(nameof(AppendPrependManyData))]
        public void AppendMany(IEnumerable<int> source, IEnumerable<int> appends)
        {
            IQueryable<int> actual = AppendRange(source.AsQueryable(), appends);
            Assert.Equal(source.Concat(appends).AsQueryable(), actual);
        }

        [Theory]
        [MemberData(nameof(AppendPrependManyData))]
        public void PrependMany(IEnumerable<int> source, IEnumerable<int> prepends)
        {
            IQueryable<int> actual = PrependRange(source.AsQueryable(), prepends);
            Assert.Equal(prepends.Concat(source).AsQueryable(), actual);
        }

        public static IEnumerable<object[]> AppendPrependManyData()
        {
            return new[]
            {
                new object[] { Enumerable.Range(1, 10), Enumerable.Range(11, 2) },
                new object[] { Enumerable.Range(1, 10), Enumerable.Range(11, 10) }
            };
        }

        [Theory]
        [MemberData(nameof(BothAppendPrependData))]
        public void BothAppendPrepend(IEnumerable<int> source, IEnumerable<int> appends, IEnumerable<int> prepends)
        {
            // Prepend first
            IQueryable<int> first = AppendRange(PrependRange(source.AsQueryable(), prepends), appends);
            // Append first
            IQueryable<int> second = PrependRange(AppendRange(source.AsQueryable(), appends), prepends);

            // Interleave Append and Prepend
            IQueryable<int> third = source.AsQueryable();
            var tuples = appends.Zip(prepends.Reverse(), (a, p) => Tuple.Create(a, p));

            foreach (var tuple in tuples)
            {
                third = third.Append(tuple.Item1).Prepend(tuple.Item2);
            }

            third = appends.Count() < prepends.Count() ?
                PrependRange(third, prepends.SkipLast(appends.Count())) :
                AppendRange(third, appends.Skip(prepends.Count()));
            
            IQueryable<int> expected = prepends.Concat(source).Concat(appends).AsQueryable();

            Assert.Equal(expected, first);
            Assert.Equal(first, second);
            Assert.Equal(second, third);
        }

        public static IEnumerable<object[]> BothAppendPrependData()
        {
            return new[]
            {
                new object[] { Enumerable.Range(1, 10), new[] { 11 }, new[] { 12 } },
                new object[] { Enumerable.Range(1, 10), Enumerable.Range(11, 4), Enumerable.Range(15, 6) },
                new object[] { Enumerable.Range(1, 10), Enumerable.Range(11, 6), Enumerable.Range(17, 4) }
            };
        }

        private static IQueryable<TSource> AppendRange<TSource>(IQueryable<TSource> source, IEnumerable<TSource> appends)
        {
            return appends.Aggregate(source, (acc, x) => acc.Append(x));
        }

        private static IQueryable<TSource> PrependRange<TSource>(IQueryable<TSource> source, IEnumerable<TSource> prepends)
        {
            return prepends.Reverse().Aggregate(source, (acc, x) => acc.Prepend(x));
        }
    }
}
