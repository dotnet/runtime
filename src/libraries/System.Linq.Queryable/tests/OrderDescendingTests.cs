// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Linq.Tests
{
    public sealed class OrderDescendingTests : EnumerableBasedTests
    {
        [Fact]
        public void OrderDescending1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderDescending().Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void OrderDescending2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderDescending(Comparer<int>.Default).Count();
            Assert.Equal(3, count);
        }
    }
}
