// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;

namespace System.Runtime.Tests
{
    public class Perf_IntPtr
    {
        [Benchmark]
        public void ctor_int32()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        new IntPtr(0); new IntPtr(0); new IntPtr(0);
                        new IntPtr(0); new IntPtr(0); new IntPtr(0);
                        new IntPtr(0); new IntPtr(0); new IntPtr(0);
                    }
        }

        [Benchmark]
        public void op_Equality_IntPtr_IntPtr()
        {
            bool res;
            IntPtr ptr1 = new IntPtr(0);
            IntPtr ptr2 = new IntPtr(0);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        res = ptr1 == ptr2; res = ptr1 == ptr2; res = ptr1 == ptr2;
                        res = ptr1 == ptr2; res = ptr1 == ptr2; res = ptr1 == ptr2;
                        res = ptr1 == ptr2; res = ptr1 == ptr2; res = ptr1 == ptr2;
                    }
        }
    }
}
