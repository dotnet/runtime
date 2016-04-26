// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Tests
{
    public class Perf_Int32
    {
        [Benchmark]
        public void ToString_()
        {
            int i32 = 32;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        i32.ToString(); i32.ToString(); i32.ToString();
                        i32.ToString(); i32.ToString(); i32.ToString();
                        i32.ToString(); i32.ToString(); i32.ToString();
                    }
        }

        [Benchmark]
        public void Parse_str()
        {
            string builtString = "1111111";
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        int.Parse(builtString); int.Parse(builtString); int.Parse(builtString);
                        int.Parse(builtString); int.Parse(builtString); int.Parse(builtString);
                        int.Parse(builtString); int.Parse(builtString); int.Parse(builtString);
                    }
        }
    }
}
