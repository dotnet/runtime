// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Runtime.Tests
{
    public class Perf_Encoding
    {
        [Benchmark]
        [InlineData(10000)]
        [InlineData(1000000)]
        public void GetBytes_str(int size)
        {
            Encoding enc = Encoding.UTF8;
            string toEncode = PerfUtils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    enc.GetBytes(toEncode); enc.GetBytes(toEncode); enc.GetBytes(toEncode);
                    enc.GetBytes(toEncode); enc.GetBytes(toEncode); enc.GetBytes(toEncode);
                    enc.GetBytes(toEncode); enc.GetBytes(toEncode); enc.GetBytes(toEncode);
                }
            }
        }
    }
}
