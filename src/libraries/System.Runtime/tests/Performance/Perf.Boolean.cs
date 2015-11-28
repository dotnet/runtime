// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
