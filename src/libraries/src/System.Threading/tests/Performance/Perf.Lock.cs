// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.Xunit.Performance;

namespace System.Threading.Tests
{
    public class Perf_Lock
    {
        [Benchmark(InnerIterationCount = 100)]
        public static void ReaderWriterLockSlimPerf()
        {
            ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        rwLock.EnterReadLock();
                        rwLock.ExitReadLock();
                    }
                }
            }
        }
    }
}