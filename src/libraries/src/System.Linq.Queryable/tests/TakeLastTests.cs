// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;
using static System.Linq.Tests.SkipTakeData;

namespace System.Linq.Tests
{
    public class TakeLastTests : EnumerableBasedTests
    {
        [Theory, MemberData(nameof(QueryableData), MemberType = typeof(SkipTakeData))]
        public void TakeLast(IQueryable<int> equivalent, int count)
        {
            IQueryable<int> expected = equivalent.Reverse().Take(count).Reverse();
            IQueryable<int> actual = equivalent.TakeLast(count);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TakeLastThrowsOnNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<DateTime>)null).TakeLast(3));
        }
    }
}
