// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Tests
{
    public class Perf_Double
    {
        public static readonly double dValue = 1.23456789E+5;

        [Benchmark]
        public static void Double_ToString()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    dValue.ToString();
        }

        [Benchmark]
        public static void Decimal_ToString()
        {
            decimal decimalNum = new decimal(dValue);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    decimalNum.ToString();
        }
    }
}
