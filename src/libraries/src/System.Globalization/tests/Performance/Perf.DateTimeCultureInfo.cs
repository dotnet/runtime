// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
