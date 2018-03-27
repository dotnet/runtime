// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Xunit.Performance;
using Xunit;

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

        public string GetTestDeepFilePath(int depth)
        {
            string directory = Path.DirectorySeparatorChar + "a";
            StringBuilder sb = new StringBuilder(depth * 2);
            for (int i = 0; i < depth; i++)
            {
                sb.Append(directory);
            }

            return sb.ToString();
        }

        public static TheoryData<int, int> RecursiveDepthData
        {
            get
            {
                var data = new TheoryData<int, int>();
                data.Add(10, 100);

                // Length of the path can be 260 characters on netfx.
                if (AreAllLongPathsAvailable)
                {
                    data.Add(100, 10);
                    // Most Unix distributions have a maximum path length of 1024 characters (1024 UTF-8 bytes). 
                    if (PlatformDetection.IsWindows)
                        data.Add(1000, 1);
                }

                return data;
            }
        }

        [Benchmark]
        [MemberData(nameof(RecursiveDepthData))]
        [OuterLoop("Takes a lot of time to finish")]
        public void RecursiveCreateDirectoryTest(int depth, int times)
        {
            // Setup
            string rootDirectory = GetTestFilePath();
            string path = GetTestDeepFilePath(depth);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < times; i++)
                    {
                        Directory.CreateDirectory(rootDirectory + Path.DirectorySeparatorChar + i + path);
                    }
                }
                // TearDown For each iteration
                Directory.Delete(rootDirectory, recursive: true);
            }
        }

        [Benchmark]
        [MemberData(nameof(RecursiveDepthData))]
        [OuterLoop("Takes a lot of time to finish")]
        public void RecursiveDeleteDirectoryTest(int depth, int times)
        {
            // Setup
            string rootDirectory = GetTestFilePath();
            string path = GetTestDeepFilePath(depth);

            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup For each Iteration
                for (int i = 0; i < times; i++)
                {
                    Directory.CreateDirectory(rootDirectory + Path.DirectorySeparatorChar + i + path);
                }

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < times; i++)
                    {
                        Directory.Delete(rootDirectory + Path.DirectorySeparatorChar + i, recursive: true);
                    }
                }
            }
            // TearDown
            Directory.Delete(rootDirectory, recursive: true);
        }
    }
}
