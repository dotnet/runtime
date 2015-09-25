// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace System.Linq.Tests
{
    public class OfTypeTests : EnumerableBasedTests
    {
        [Fact]
        public void EmptySource()
        {
            object[] source = { };
            Assert.Empty(source.AsQueryable().OfType<int>());
        }

        [Fact]
        public void HeterogenousSourceOnlyFirstOfType()
        {
            object[] source = { 10, "Hello", 3.5, "Test" };
            int[] expected = { 10 };

            Assert.Equal(expected, source.AsQueryable().OfType<int>());
        }

        [Fact]
        public void NullSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<object>)null).OfType<string>());
        }

        [Fact]
        public void OfType()
        {
            var count = (new object[] { 0, (long)1, 2 }).AsQueryable().OfType<int>().Count();
            Assert.Equal(2, count);
        }
    }
}
