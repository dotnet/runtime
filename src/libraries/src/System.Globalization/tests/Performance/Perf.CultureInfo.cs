// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Globalization.Tests
{
    public class Perf_CultureInfo
    {
        [Benchmark]
        public void GetCurrentCulture()
        {
            CultureInfo result;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 40000; i++)
                    {
                        result = CultureInfo.CurrentCulture; result = CultureInfo.CurrentCulture; result = CultureInfo.CurrentCulture;
                        result = CultureInfo.CurrentCulture; result = CultureInfo.CurrentCulture; result = CultureInfo.CurrentCulture;
                        result = CultureInfo.CurrentCulture; result = CultureInfo.CurrentCulture; result = CultureInfo.CurrentCulture;
                    }
                }
        }

        [Benchmark]
        public void GetInvariantCulture()
        {
            CultureInfo result;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 40000; i++)
                    {
                        result = CultureInfo.InvariantCulture; result = CultureInfo.InvariantCulture; result = CultureInfo.InvariantCulture;
                        result = CultureInfo.InvariantCulture; result = CultureInfo.InvariantCulture; result = CultureInfo.InvariantCulture;
                        result = CultureInfo.InvariantCulture; result = CultureInfo.InvariantCulture; result = CultureInfo.InvariantCulture;
                    }
                }
        }
    }
}
