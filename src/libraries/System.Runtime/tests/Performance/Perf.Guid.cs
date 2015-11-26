// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;

namespace System.Runtime.Tests
{
    public class Perf_Guid
    {
        [Benchmark]
        public void NewGuid()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        Guid.NewGuid(); Guid.NewGuid(); Guid.NewGuid();
                        Guid.NewGuid(); Guid.NewGuid(); Guid.NewGuid();
                        Guid.NewGuid(); Guid.NewGuid(); Guid.NewGuid();
                    }
        }

        [Benchmark]
        public void ctor_str()
        {
            const string guidStr = "a8a110d5-fc49-43c5-bf46-802db8f843ff";
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        new Guid(guidStr); new Guid(guidStr); new Guid(guidStr);
                        new Guid(guidStr); new Guid(guidStr); new Guid(guidStr);
                        new Guid(guidStr); new Guid(guidStr); new Guid(guidStr);
                    }
        }
    }
}
