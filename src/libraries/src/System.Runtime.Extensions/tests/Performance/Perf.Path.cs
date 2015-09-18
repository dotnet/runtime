// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.Xunit.Performance;

namespace System.Runtime.Extensions.Tests
{
    public class Perf_Path
    {
        [Benchmark]
        public void Combine()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup
                string testPath1 = PerfUtils.GetTestFilePath();
                string testPath2 = PerfUtils.CreateString(10);

                // Actual perf testing
                using (iteration.StartMeasurement())
                {
                    Path.Combine(testPath1, testPath2); Path.Combine(testPath1, testPath2); Path.Combine(testPath1, testPath2);
                    Path.Combine(testPath1, testPath2); Path.Combine(testPath1, testPath2); Path.Combine(testPath1, testPath2);
                    Path.Combine(testPath1, testPath2); Path.Combine(testPath1, testPath2); Path.Combine(testPath1, testPath2);
                }
            }
        }

        [Benchmark]
        public void GetFileName()
        {
            string testPath = PerfUtils.GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Path.GetFileName(testPath); Path.GetFileName(testPath); Path.GetFileName(testPath);
                    Path.GetFileName(testPath); Path.GetFileName(testPath); Path.GetFileName(testPath);
                    Path.GetFileName(testPath); Path.GetFileName(testPath); Path.GetFileName(testPath);
                }
            }
        }

        [Benchmark]
        public void GetDirectoryName()
        {
            string testPath = PerfUtils.GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Path.GetDirectoryName(testPath); Path.GetDirectoryName(testPath); Path.GetDirectoryName(testPath);
                    Path.GetDirectoryName(testPath); Path.GetDirectoryName(testPath); Path.GetDirectoryName(testPath);
                    Path.GetDirectoryName(testPath); Path.GetDirectoryName(testPath); Path.GetDirectoryName(testPath);
                }
            }
        }
    }
}
