// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
