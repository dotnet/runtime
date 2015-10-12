// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;

namespace System.Runtime.Tests
{
    public class Perf_Object
    {
        [Benchmark]
        public void ctor()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        new Object(); new Object(); new Object();
                        new Object(); new Object(); new Object();
                        new Object(); new Object(); new Object();
                    }
        }

        [Benchmark]
        public void GetType_()
        {
            Object obj = new Object();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        obj.GetType(); obj.GetType(); obj.GetType();
                        obj.GetType(); obj.GetType(); obj.GetType();
                        obj.GetType(); obj.GetType(); obj.GetType();
                    }
        }
    }
}
