// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.IO.Tests
{
    public class Perf_File : FileSystemTest
    {
        [Benchmark]
        public void Exists()
        {
            // Setup
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();

            foreach (var iteration in Benchmark.Iterations)
            {
                // Actual perf testing
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 20000; i++)
                    {
                        File.Exists(testFile); File.Exists(testFile); File.Exists(testFile);
                        File.Exists(testFile); File.Exists(testFile); File.Exists(testFile);
                        File.Exists(testFile); File.Exists(testFile); File.Exists(testFile);
                    }
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
                string testFile = GetTestFilePath();
                for (int i = 0; i < 10000; i++)
                    File.Create(testFile + 1).Dispose();

                // Actual perf testing
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        File.Delete(testFile + 1);
            }
        }
    }
}
