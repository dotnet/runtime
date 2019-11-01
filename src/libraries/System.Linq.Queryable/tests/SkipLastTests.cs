// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;
using static System.Linq.Tests.SkipTakeData;

namespace System.Linq.Tests
{
    public class SkipLastTests : EnumerableBasedTests
    {
        [Theory, MemberData(nameof(QueryableData), MemberType = typeof(SkipTakeData))]
        public void SkipLast(IQueryable<int> source, int count)
        {
            IQueryable<int> expected = source.Reverse().Skip(count).Reverse();
            IQueryable<int> actual = source.SkipLast(count);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SkipLastThrowsOnNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<DateTime>)null).SkipLast(3));
        }
    }
}
