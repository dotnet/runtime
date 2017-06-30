// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.IO.MemoryMappedFiles.Tests
{
    /// <summary>
    /// Performance tests for the construction and disposal of MemoryMappedFiles of varying sizes
    /// </summary>
    public class Perf_MemoryMappedFile : MemoryMappedFilesTestBase
    {
        [Benchmark]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        [InlineData(10000000)]
        public void CreateNew(int capacity)
        {
            const int InnerIterations = 1000;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                    for (int i = 0; i < InnerIterations; i++)
                        MemoryMappedFile.CreateNew(null, capacity).Dispose();
            }
        }

        [Benchmark]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        [InlineData(10000000)]
        public void CreateFromFile(int capacity)
        {
            // Note that the test results will include the disposal overhead of both the MemoryMappedFile
            // as well as the Accessor for it
            foreach (var iteration in Benchmark.Iterations)
                using (TempFile file = new TempFile(GetTestFilePath(), capacity))
                using (iteration.StartMeasurement())
                using (MemoryMappedFile mmfile = MemoryMappedFile.CreateFromFile(file.Path))
                using (mmfile.CreateViewAccessor(capacity / 4, capacity / 2))
                { }
        }
    }
}
