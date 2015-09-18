// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;

namespace System.IO.FileSystem.Tests
{
    public class Perf_File
    {
        [Benchmark]
        public void Exists()
        {
            // Setup
            string testFile = PerfUtils.GetTestFilePath();
            File.Create(testFile).Dispose();

            foreach (var iteration in Benchmark.Iterations)
            {
                // Actual perf testing
                using (iteration.StartMeasurement())
                {
                    File.Exists(testFile); File.Exists(testFile); File.Exists(testFile);
                    File.Exists(testFile); File.Exists(testFile); File.Exists(testFile);
                    File.Exists(testFile); File.Exists(testFile); File.Exists(testFile);
                }
            }

            // Teardown
            File.Delete(testFile);
        }

        [Benchmark]
        public void Delete()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup
                string testFile = PerfUtils.GetTestFilePath();
                File.Create(testFile + 1).Dispose(); File.Create(testFile + 2).Dispose(); File.Create(testFile + 3).Dispose();

                // Actual perf testing
                using (iteration.StartMeasurement())
                {
                    File.Delete(testFile + 1); File.Delete(testFile + 2); File.Delete(testFile + 3);
                }
            }
        }
    }
}
