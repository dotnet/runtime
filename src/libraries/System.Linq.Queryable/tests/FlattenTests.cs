// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class FlattenTests : EnumerableBasedTests
    {
        [Theory]
        [MemberData(nameof(FlattenHasExpectedOutput_Data))]
        public void FlattenHasExpectedOutput<T>(IQueryable<IQueryable<T>> sources, IQueryable<T> expected)
        {
            Assert.Equal(expected, sources.Flatten());
        }

        public static IEnumerable<object[]> FlattenHasExpectedOutput_Data()
        {
            yield return Wrap(new IQueryable<int>[] { }.AsQueryable(), new int[] { }.AsQueryable());
            yield return Wrap(new[] { new [] { 0 }.AsQueryable(), new [] { 1, 2, 3 }.AsQueryable() }.AsQueryable(), new [] { 0, 1, 2, 3 }.AsQueryable());
            static object[] Wrap<T>(IQueryable<IQueryable<T>> sources, IQueryable<int> expected) => new object[] { sources, expected };
        }

        [Fact]
        public void SourcesNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("sources", () => ((IQueryable<IQueryable<int>>)null).Flatten());
        }
    }
}
