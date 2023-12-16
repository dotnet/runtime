// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Linq.Tests
{
    public class IndexTests : EnumerableBasedTests
    {
        [Fact]
        public void ThrowsOnNullSource()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Index());
        }

        [Fact]
        public void Index()
        {
            string[] source = ["a", "b"];
            var actual = source.AsQueryable().Index();
            (int Index, string Item)[] expected = [(0, "a"), (1, "b")];
            Assert.Equal<(int Index, string Item)>(expected, actual);
        }
    }
}
