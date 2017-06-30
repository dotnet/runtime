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

        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void ChangeExtension(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();
            string extension = utils.CreateString(4);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.ChangeExtension(testPath, extension); Path.ChangeExtension(testPath, extension); Path.ChangeExtension(testPath, extension);
                        Path.ChangeExtension(testPath, extension); Path.ChangeExtension(testPath, extension); Path.ChangeExtension(testPath, extension);
                        Path.ChangeExtension(testPath, extension); Path.ChangeExtension(testPath, extension); Path.ChangeExtension(testPath, extension);
                    }
        }

        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void GetExtension(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.GetExtension(testPath); Path.GetExtension(testPath); Path.GetExtension(testPath);
                        Path.GetExtension(testPath); Path.GetExtension(testPath); Path.GetExtension(testPath);
                        Path.GetExtension(testPath); Path.GetExtension(testPath); Path.GetExtension(testPath);
                    }
        }

        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void GetFileNameWithoutExtension(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.GetFileNameWithoutExtension(testPath); Path.GetFileNameWithoutExtension(testPath); Path.GetFileNameWithoutExtension(testPath);
                        Path.GetFileNameWithoutExtension(testPath); Path.GetFileNameWithoutExtension(testPath); Path.GetFileNameWithoutExtension(testPath);
                        Path.GetFileNameWithoutExtension(testPath); Path.GetFileNameWithoutExtension(testPath); Path.GetFileNameWithoutExtension(testPath);
                    }
        }

        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void GetFullPathForLegacyLength(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.CreateString(length: 200);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.GetFullPath(testPath); Path.GetFullPath(testPath); Path.GetFullPath(testPath);
                        Path.GetFullPath(testPath); Path.GetFullPath(testPath); Path.GetFullPath(testPath);
                        Path.GetFullPath(testPath); Path.GetFullPath(testPath); Path.GetFullPath(testPath);
                    }
        }

        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void GetFullPathForTypicalLongPath(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.CreateString(length: 500);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.GetFullPath(testPath); Path.GetFullPath(testPath); Path.GetFullPath(testPath);
                        Path.GetFullPath(testPath); Path.GetFullPath(testPath); Path.GetFullPath(testPath);
                        Path.GetFullPath(testPath); Path.GetFullPath(testPath); Path.GetFullPath(testPath);
                    }
        }

        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void GetFullPathForReallyLongPath(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.CreateString(length: 1000);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.GetFullPath(testPath); Path.GetFullPath(testPath); Path.GetFullPath(testPath);
                        Path.GetFullPath(testPath); Path.GetFullPath(testPath); Path.GetFullPath(testPath);
                        Path.GetFullPath(testPath); Path.GetFullPath(testPath); Path.GetFullPath(testPath);
                    }
        }

        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void GetPathRoot(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.GetPathRoot(testPath); Path.GetPathRoot(testPath); Path.GetPathRoot(testPath);
                        Path.GetPathRoot(testPath); Path.GetPathRoot(testPath); Path.GetPathRoot(testPath);
                        Path.GetPathRoot(testPath); Path.GetPathRoot(testPath); Path.GetPathRoot(testPath);
                    }
        }

        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void GetRandomFileName(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.GetRandomFileName(); Path.GetRandomFileName(); Path.GetRandomFileName();
                        Path.GetRandomFileName(); Path.GetRandomFileName(); Path.GetRandomFileName();
                        Path.GetRandomFileName(); Path.GetRandomFileName(); Path.GetRandomFileName();
                    }
        }

        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void GetTempPath(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.GetTempPath(); Path.GetTempPath(); Path.GetTempPath();
                        Path.GetTempPath(); Path.GetTempPath(); Path.GetTempPath();
                        Path.GetTempPath(); Path.GetTempPath(); Path.GetTempPath();
                    }
        }

        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void HasExtension(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.HasExtension(testPath); Path.HasExtension(testPath); Path.HasExtension(testPath);
                        Path.HasExtension(testPath); Path.HasExtension(testPath); Path.HasExtension(testPath);
                        Path.HasExtension(testPath); Path.HasExtension(testPath); Path.HasExtension(testPath);
                    }
        }

        [Benchmark]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(30000)]
        public void IsPathRooted(int innerIterations)
        {
            PerfUtils utils = new PerfUtils();
            string testPath = utils.GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Path.IsPathRooted(testPath); Path.IsPathRooted(testPath); Path.IsPathRooted(testPath);
                        Path.IsPathRooted(testPath); Path.IsPathRooted(testPath); Path.IsPathRooted(testPath);
                        Path.IsPathRooted(testPath); Path.IsPathRooted(testPath); Path.IsPathRooted(testPath);
                    }
        }
    }
}