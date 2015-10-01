// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Text.EncodingTests
{
    public class Perf_Encoding
    {
        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        public void GetBytes_str(int size)
        {
            Encoding enc = Encoding.UTF8;
            PerfUtils utils = new PerfUtils();
            string toEncode = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 100; i++)
                    {
                        enc.GetBytes(toEncode); enc.GetBytes(toEncode); enc.GetBytes(toEncode);
                        enc.GetBytes(toEncode); enc.GetBytes(toEncode); enc.GetBytes(toEncode);
                        enc.GetBytes(toEncode); enc.GetBytes(toEncode); enc.GetBytes(toEncode);
                    }
        }
    }
}
