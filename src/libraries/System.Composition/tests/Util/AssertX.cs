// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace System.Composition.UnitTests.Util
{
    internal static class AssertX
    {
        public static void Equivalent<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = null, params object[] args)
        {
            IDictionary<T, int> expectedCounts = expected.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
            IDictionary<T, int> actualCounts = actual.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

            Assert.Equal(expectedCounts, actualCounts);
        }
    }
}
