// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Threading.Tests
{
    public class Perf_SpinLock
    {
        private const int IterationCount = 1_000_000;

        [Benchmark(InnerIterationCount = IterationCount)]
        public void EnterExit()
        {
            SpinLock spinLock = new SpinLock();

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < IterationCount; i++)
                    {
                        bool lockTaken = false;

                        spinLock.Enter(ref lockTaken);
                        spinLock.Exit();
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        public void TryEnterExit()
        {
            SpinLock spinLock = new SpinLock();

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < IterationCount; i++)
                    {
                        bool lockTaken = false;

                        spinLock.TryEnter(0, ref lockTaken);
                        spinLock.Exit();
                    }
                }
            }
        }
    }
}
