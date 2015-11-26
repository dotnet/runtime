// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;

namespace System.Runtime.Tests
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
