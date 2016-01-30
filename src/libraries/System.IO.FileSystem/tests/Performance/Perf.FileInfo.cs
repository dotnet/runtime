// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.IO.Tests
{
    public class Perf_FileInfo : FileSystemTest
    {
        [Benchmark]
        public void ctor_str()
        {
            string path = GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 20000; i++)
                    {
                        new FileInfo(path); new FileInfo(path); new FileInfo(path);
                        new FileInfo(path); new FileInfo(path); new FileInfo(path);
                        new FileInfo(path); new FileInfo(path); new FileInfo(path);
                    }
                }
            }
        }
    }
}
