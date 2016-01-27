// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Globalization.Tests
{
    /// <summary>
    /// Performance tests for converting DateTime to different CultureInfos
    /// 
    /// Primary methods affected: Parse, ToString
    /// </summary>
    public class Perf_DateTimeCultureInfo
    {
        private const int innerIterations = 1000;

        [Benchmark]
        [InlineData("fr")]
        [InlineData("da")]
        [InlineData("ja")]
        [InlineData("")]
        public void ToString(string culturestring)
        {
            DateTime time = DateTime.Now;
            CultureInfo cultureInfo = new CultureInfo(culturestring);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        time.ToString(cultureInfo); time.ToString(cultureInfo); time.ToString(cultureInfo);
                        time.ToString(cultureInfo); time.ToString(cultureInfo); time.ToString(cultureInfo);
                        time.ToString(cultureInfo); time.ToString(cultureInfo); time.ToString(cultureInfo);
                    }
                }
        }

        [Benchmark]
        [InlineData("fr")]
        [InlineData("da")]
        [InlineData("ja")]
        [InlineData("")]
        public void Parse(string culturestring)
        {
            CultureInfo cultureInfo = new CultureInfo(culturestring);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        DateTime.Parse("10/10/2010 12:00:00 AM", cultureInfo);
                        DateTime.Parse("10/10/2010 12:00:00 AM", cultureInfo);
                        DateTime.Parse("10/10/2010 12:00:00 AM", cultureInfo);
                        DateTime.Parse("10/10/2010 12:00:00 AM", cultureInfo);
                    }
                }
        }
    }
}
