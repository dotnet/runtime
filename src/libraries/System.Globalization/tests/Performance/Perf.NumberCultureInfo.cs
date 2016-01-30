// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Globalization.Tests
{
    /// <summary>
    /// Performance tests for converting numbers to different CultureInfos
    /// </summary>
    public class Perf_NumberCultureInfo
    {
        private const int innerIterations = 1000;

        [Benchmark]
        [InlineData("fr")]
        [InlineData("da")]
        [InlineData("ja")]
        [InlineData("")]
        public void ToString(string culturestring)
        {
            double number = 104234.343;
            CultureInfo cultureInfo = new CultureInfo(culturestring);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        number.ToString(cultureInfo); number.ToString(cultureInfo); number.ToString(cultureInfo);
                        number.ToString(cultureInfo); number.ToString(cultureInfo); number.ToString(cultureInfo);
                        number.ToString(cultureInfo); number.ToString(cultureInfo); number.ToString(cultureInfo);
                    }
                }
        }
    }
}
