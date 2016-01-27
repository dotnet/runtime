// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.IO.Tests
{
    public class Perf_Path
    {
        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void Combine(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath1 = utils.GetTestFilePath();
            string testPath2 = utils.CreateString(10);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.Combine(testPath1, testPath2); Path.Combine(testPath1, testPath2); Path.Combine(testPath1, testPath2);
                        Path.Combine(testPath1, testPath2); Path.Combine(testPath1, testPath2); Path.Combine(testPath1, testPath2);
                        Path.Combine(testPath1, testPath2); Path.Combine(testPath1, testPath2); Path.Combine(testPath1, testPath2);
                    }
        }

        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void GetFileName(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.GetFileName(testPath); Path.GetFileName(testPath); Path.GetFileName(testPath);
                        Path.GetFileName(testPath); Path.GetFileName(testPath); Path.GetFileName(testPath);
                        Path.GetFileName(testPath); Path.GetFileName(testPath); Path.GetFileName(testPath);
                    }
        }

        [Benchmark]
        public void GetDirectoryName()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 20000; i++)
                    {
                        Path.GetDirectoryName(testPath); Path.GetDirectoryName(testPath); Path.GetDirectoryName(testPath);
                        Path.GetDirectoryName(testPath); Path.GetDirectoryName(testPath); Path.GetDirectoryName(testPath);
                        Path.GetDirectoryName(testPath); Path.GetDirectoryName(testPath); Path.GetDirectoryName(testPath);
                    }
        }
    }
}
