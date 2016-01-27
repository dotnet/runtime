// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.IO.Tests
{
    public class Perf_Directory : FileSystemTest
    {
        [Benchmark]
        public void GetCurrentDirectory()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 20000; i++)
                    {
                        Directory.GetCurrentDirectory(); Directory.GetCurrentDirectory(); Directory.GetCurrentDirectory();
                        Directory.GetCurrentDirectory(); Directory.GetCurrentDirectory(); Directory.GetCurrentDirectory();
                        Directory.GetCurrentDirectory(); Directory.GetCurrentDirectory(); Directory.GetCurrentDirectory();
                    }
        }

        [Benchmark]
        public void CreateDirectory()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup
                string testFile = GetTestFilePath();

                // Actual perf testing
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 20000; i++)
                        Directory.CreateDirectory(testFile + i);
            }
        }

        [Benchmark]
        public void Exists()
        {
            // Setup
            string testFile = GetTestFilePath();
            Directory.CreateDirectory(testFile);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 20000; i++)
                    {
                        Directory.Exists(testFile); Directory.Exists(testFile); Directory.Exists(testFile);
                        Directory.Exists(testFile); Directory.Exists(testFile); Directory.Exists(testFile);
                        Directory.Exists(testFile); Directory.Exists(testFile); Directory.Exists(testFile);
                    }

            // Teardown
            Directory.Delete(testFile);
        }
    }
}
