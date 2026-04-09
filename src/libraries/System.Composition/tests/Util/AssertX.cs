// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Composition.UnitTests.Util
{
    internal static class AssertX
    {
        public static void Equivalent<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            IDictionary<T, int> expectedCounts = expected.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
            IDictionary<T, int> actualCounts = actual.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

            Assert.Equal(expectedCounts, actualCounts);
        }
    }
}
