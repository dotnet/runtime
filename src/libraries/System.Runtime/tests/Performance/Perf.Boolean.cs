// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Runtime.Tests
{
    public class Perf_Boolean
    {
        [Benchmark]
        public void Parse_str()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    Boolean.Parse("True"); Boolean.Parse("True"); Boolean.Parse("True");
                    Boolean.Parse("True"); Boolean.Parse("True"); Boolean.Parse("True");
                    Boolean.Parse("True"); Boolean.Parse("True"); Boolean.Parse("True");
                }
        }

        [Benchmark]
        public void ToString_()
        {
            Boolean boo = true;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    boo.ToString(); boo.ToString(); boo.ToString();
                    boo.ToString(); boo.ToString(); boo.ToString();
                    boo.ToString(); boo.ToString(); boo.ToString();
                }
        }
    }
}
