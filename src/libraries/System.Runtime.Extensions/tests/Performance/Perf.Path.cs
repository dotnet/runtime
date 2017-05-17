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
        private volatile string str;
        private volatile bool b;

        [Benchmark(InnerIterationCount = 150000)]
        public void Combine()
        {
            PerfUtils utils = new PerfUtils();
            string testPath1 = utils.GetTestFilePath();
            string testPath2 = utils.CreateString(10);

            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Path.Combine(testPath1, testPath2);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Path.Combine(testPath1, testPath2); str = Path.Combine(testPath1, testPath2); str = Path.Combine(testPath1, testPath2);
                        str = Path.Combine(testPath1, testPath2); str = Path.Combine(testPath1, testPath2); str = Path.Combine(testPath1, testPath2);
                        str = Path.Combine(testPath1, testPath2); str = Path.Combine(testPath1, testPath2); str = Path.Combine(testPath1, testPath2);
                    }
        }

        [Benchmark(InnerIterationCount = 100000)]
        public void GetFileName()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();

            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Path.GetFileName(testPath);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Path.GetFileName(testPath); str = Path.GetFileName(testPath); str = Path.GetFileName(testPath);
                        str = Path.GetFileName(testPath); str = Path.GetFileName(testPath); str = Path.GetFileName(testPath);
                        str = Path.GetFileName(testPath); str = Path.GetFileName(testPath); str = Path.GetFileName(testPath);
                    }
        }

        [Benchmark(InnerIterationCount = 80000)]
        public void GetDirectoryName()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();

            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Path.GetDirectoryName(testPath);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Path.GetDirectoryName(testPath); str = Path.GetDirectoryName(testPath); str = Path.GetDirectoryName(testPath);
                        str = Path.GetDirectoryName(testPath); str = Path.GetDirectoryName(testPath); str = Path.GetDirectoryName(testPath);
                        str = Path.GetDirectoryName(testPath); str = Path.GetDirectoryName(testPath); str = Path.GetDirectoryName(testPath);
                    }
        }

        [Benchmark(InnerIterationCount = 120000)]
        public void ChangeExtension()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();
            string extension = utils.CreateString(4);

            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Path.ChangeExtension(testPath, extension);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Path.ChangeExtension(testPath, extension); str = Path.ChangeExtension(testPath, extension); str = Path.ChangeExtension(testPath, extension);
                        str = Path.ChangeExtension(testPath, extension); str = Path.ChangeExtension(testPath, extension); str = Path.ChangeExtension(testPath, extension);
                        str = Path.ChangeExtension(testPath, extension); str = Path.ChangeExtension(testPath, extension); str = Path.ChangeExtension(testPath, extension);
                    }
        }

        [Benchmark(InnerIterationCount = 150000)]
        public void GetExtension()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();

            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Path.GetExtension(testPath);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Path.GetExtension(testPath); str = Path.GetExtension(testPath); str = Path.GetExtension(testPath);
                        str = Path.GetExtension(testPath); str = Path.GetExtension(testPath); str = Path.GetExtension(testPath);
                        str = Path.GetExtension(testPath); str = Path.GetExtension(testPath); str = Path.GetExtension(testPath);
                    }
        }

        [Benchmark(InnerIterationCount = 100000)]
        public void GetFileNameWithoutExtension()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();

            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Path.GetFileNameWithoutExtension(testPath);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Path.GetFileNameWithoutExtension(testPath); str = Path.GetFileNameWithoutExtension(testPath); str = Path.GetFileNameWithoutExtension(testPath);
                        str = Path.GetFileNameWithoutExtension(testPath); str = Path.GetFileNameWithoutExtension(testPath); str = Path.GetFileNameWithoutExtension(testPath);
                        str = Path.GetFileNameWithoutExtension(testPath); str = Path.GetFileNameWithoutExtension(testPath); str = Path.GetFileNameWithoutExtension(testPath);
                    }
        }

        [Benchmark(InnerIterationCount = 20000)]
        public void GetFullPathForLegacyLength()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.CreateString(length: 200);

            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Path.GetFullPath(testPath);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath);
                        str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath);
                        str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath);
                    }
        }

        [Benchmark(InnerIterationCount = 10000)]
        public void GetFullPathForTypicalLongPath()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.CreateString(length: 500);

            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Path.GetFullPath(testPath);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath);
                        str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath);
                        str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath);
                    }
        }

        [Benchmark(InnerIterationCount = 5000)]
        public void GetFullPathForReallyLongPath()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.CreateString(length: 1000);

            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Path.GetFullPath(testPath);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath);
                        str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath);
                        str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath); str = Path.GetFullPath(testPath);
                    }
        }

        [Benchmark(InnerIterationCount = 100000)]
        public void GetPathRoot()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();

            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Path.GetPathRoot(testPath);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Path.GetPathRoot(testPath); str = Path.GetPathRoot(testPath); str = Path.GetPathRoot(testPath);
                        str = Path.GetPathRoot(testPath); str = Path.GetPathRoot(testPath); str = Path.GetPathRoot(testPath);
                        str = Path.GetPathRoot(testPath); str = Path.GetPathRoot(testPath); str = Path.GetPathRoot(testPath);
                    }
        }

        [Benchmark(InnerIterationCount = 150000)]
        public void GetRandomFileName()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();

            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Path.GetRandomFileName();
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Path.GetRandomFileName(); str = Path.GetRandomFileName(); str = Path.GetRandomFileName();
                        str = Path.GetRandomFileName(); str = Path.GetRandomFileName(); str = Path.GetRandomFileName();
                        str = Path.GetRandomFileName(); str = Path.GetRandomFileName(); str = Path.GetRandomFileName();
                    }
        }

        [Benchmark(InnerIterationCount = 20000)]
        public void GetTempPath()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();

            // warmup
            for (int i = 0; i < 100; i++)
            {
                str = Path.GetTempPath();
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        str = Path.GetTempPath(); str = Path.GetTempPath(); str = Path.GetTempPath();
                        str = Path.GetTempPath(); str = Path.GetTempPath(); str = Path.GetTempPath();
                        str = Path.GetTempPath(); str = Path.GetTempPath(); str = Path.GetTempPath();
                    }
        }

        [Benchmark(InnerIterationCount = 200000)]
        public void HasExtension()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();

            // warmup
            for (int i = 0; i < 100; i++)
            {
                b = Path.HasExtension(testPath);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        b = Path.HasExtension(testPath); b = Path.HasExtension(testPath); b = Path.HasExtension(testPath);
                        b = Path.HasExtension(testPath); b = Path.HasExtension(testPath); b = Path.HasExtension(testPath);
                        b = Path.HasExtension(testPath); b = Path.HasExtension(testPath); b = Path.HasExtension(testPath);
                    }
        }

        [Benchmark(InnerIterationCount = 250000)]
        public void IsPathRooted()
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();

            // warmup
            for (int i = 0; i < 100; i++)
            {
                b = Path.IsPathRooted(testPath);
            }

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        b = Path.IsPathRooted(testPath); b = Path.IsPathRooted(testPath); b = Path.IsPathRooted(testPath);
                        b = Path.IsPathRooted(testPath); b = Path.IsPathRooted(testPath); b = Path.IsPathRooted(testPath);
                        b = Path.IsPathRooted(testPath); b = Path.IsPathRooted(testPath); b = Path.IsPathRooted(testPath);
                    }
        }
    }
}
