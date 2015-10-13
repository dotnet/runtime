// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;

namespace System.Runtime.Tests
{
    public class Perf_Type
    {
        [Benchmark]
        public void GetTypeFromHandle()
        {
            RuntimeTypeHandle type1 = typeof(int).TypeHandle;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 100000; i++)
                    {
                        Type.GetTypeFromHandle(type1); Type.GetTypeFromHandle(type1); Type.GetTypeFromHandle(type1);
                        Type.GetTypeFromHandle(type1); Type.GetTypeFromHandle(type1); Type.GetTypeFromHandle(type1);
                        Type.GetTypeFromHandle(type1); Type.GetTypeFromHandle(type1); Type.GetTypeFromHandle(type1);
                    }
        }

        [Benchmark]
        public void op_Equality()
        {
            bool result;
            Type type1 = typeof(int);
            Type type2 = typeof(string);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 100000; i++)
                    {
                        result = type1 == type2; result = type1 == type2; result = type1 == type2;
                        result = type1 == type2; result = type1 == type2; result = type1 == type2;
                        result = type1 == type2; result = type1 == type2; result = type1 == type2;
                    }
        }
    }
}
