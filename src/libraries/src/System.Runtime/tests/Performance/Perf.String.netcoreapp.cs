// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Tests
{
    public partial class Perf_String
    {
        private static readonly object[] s_testStringSizes = new object[]
        {
            10, 100, 1000
        };

        public static IEnumerable<object[]> ContainsStringComparisonArgs => Permutations(s_compareOptions, s_testStringSizes);

        [Benchmark]
        [MemberData(nameof(ContainsStringComparisonArgs))]
        public void Contains(StringComparison comparisonType, int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString = utils.CreateString(size);
            string subString = testString.Substring(testString.Length / 2, testString.Length / 4);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        testString.Contains(subString, comparisonType);
        }
    }
}
