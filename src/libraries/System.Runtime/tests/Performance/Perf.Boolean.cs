// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Tests
{
    public class Perf_Boolean
    {
        [Benchmark]
        public void Parse_str()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        bool.Parse("True"); bool.Parse("True"); bool.Parse("True");
                        bool.Parse("True"); bool.Parse("True"); bool.Parse("True");
                        bool.Parse("True"); bool.Parse("True"); bool.Parse("True");
                    }
        }

        [Benchmark]
        public void ToString_()
        {
            bool boo = true;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        boo.ToString(); boo.ToString(); boo.ToString();
                        boo.ToString(); boo.ToString(); boo.ToString();
                        boo.ToString(); boo.ToString(); boo.ToString();
                    }
        }
    }
}
