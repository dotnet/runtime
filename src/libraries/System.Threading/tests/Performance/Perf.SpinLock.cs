// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Threading.Tests
{
    public class Perf_SpinLock
    {
        [Benchmark(InnerIterationCount = 100)]
        public void EnterExit()
        {
            SpinLock spinLock = new SpinLock();

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        bool lockTaken = false;

                        spinLock.Enter(ref lockTaken);
                        spinLock.Exit();
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 100)]
        public void TryEnterExit()
        {
            SpinLock spinLock = new SpinLock();

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
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
