// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Tests
{
    public class Perf_Char
    {
        private const int InnerIterations = 1_000_000;

        public static IEnumerable<object[]> Char_ChangeCase_MemberData()
        {
            yield return new object[] { 'A', "en-US" }; // ASCII upper case
            yield return new object[] { 'a', "en-US" }; // ASCII lower case
            yield return new object[] { '\u0130', "en-US" }; // non-ASCII, English
            yield return new object[] { '\u4F60', "zh-Hans" }; // non-ASCII, Chinese
        }

        [Benchmark(InnerIterationCount = InnerIterations)]
        [MemberData(nameof(Char_ChangeCase_MemberData))]
        public static char Char_ToLower(char c, string cultureName)
        {
            char ret = default(char);
            CultureInfo culture = new CultureInfo(cultureName);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < InnerIterations; i++)
                    {
                        ret = Char.ToLower(c, culture);
                    }
                }
            }

            return ret;
        }

        [Benchmark(InnerIterationCount = InnerIterations)]
        [MemberData(nameof(Char_ChangeCase_MemberData))]
        public static char Char_ToUpper(char c, string cultureName)
        {
            char ret = default(char);
            CultureInfo culture = new CultureInfo(cultureName);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < InnerIterations; i++)
                    {
                        ret = Char.ToUpper(c, culture);
                    }
                }
            }

            return ret;
        }
    }
}
