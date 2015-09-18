// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;

namespace System.IO.FileSystem.Tests
{
    public class Perf_FileInfo
    {
        [Benchmark]
        public void ctor_str()
        {
            string path = PerfUtils.GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    new FileInfo(path); new FileInfo(path); new FileInfo(path);
                    new FileInfo(path); new FileInfo(path); new FileInfo(path);
                    new FileInfo(path); new FileInfo(path); new FileInfo(path);
                }
            }
        }
    }
}
