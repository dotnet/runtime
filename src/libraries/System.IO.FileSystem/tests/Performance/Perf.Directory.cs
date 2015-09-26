// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;

namespace System.IO.FileSystem.Tests
{
    public class Perf_Directory
    {
        [Benchmark]
        public void GetCurrentDirectory()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Directory.GetCurrentDirectory(); Directory.GetCurrentDirectory(); Directory.GetCurrentDirectory();
                    Directory.GetCurrentDirectory(); Directory.GetCurrentDirectory(); Directory.GetCurrentDirectory();
                    Directory.GetCurrentDirectory(); Directory.GetCurrentDirectory(); Directory.GetCurrentDirectory();
                }
            }
        }

        [Benchmark]
        public void CreateDirectory()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup
                string testFile = PerfUtils.GetTestFilePath();

                // Actual perf testing
                using (iteration.StartMeasurement())
                    Directory.CreateDirectory(testFile);

                // Teardown
                Directory.Delete(testFile);
            }
        }

        [Benchmark]
        public void Exists()
        {
            // Setup
            string testFile = PerfUtils.GetTestFilePath();
            Directory.CreateDirectory(testFile);

            foreach (var iteration in Benchmark.Iterations)
            {
                // Actual perf testing
                using (iteration.StartMeasurement())
                {
                    Directory.Exists(testFile); Directory.Exists(testFile); Directory.Exists(testFile);
                    Directory.Exists(testFile); Directory.Exists(testFile); Directory.Exists(testFile);
                    Directory.Exists(testFile); Directory.Exists(testFile); Directory.Exists(testFile);
                }
            }

            // Teardown
            Directory.Delete(testFile);
        }
    }
}
