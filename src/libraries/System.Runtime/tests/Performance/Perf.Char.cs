// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Tests
{
    public class Perf_Char
    {
        [Benchmark]
        public static char Char_ToLower()
        {
            char ret = default(char);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    ret = Char.ToLower('A');

            return ret;
        }
    }
}
