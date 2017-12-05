// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System
{
    public class Perf_Marvin
    {
        private static IEnumerable<object[]> EnumerateRandomByteArrayTestCases()
        {
            var r = new Random(123);
            foreach (int size in
                Enumerable.Range(0, 25)
                .Union(new int[] { 50, 100, 200, 2000, 20000, 200000, 2000000 }))
            {
                byte[] array = new byte[size];
                r.NextBytes(array);

                int iterations = 2000000 / Math.Max(1, size);
                yield return new object[] { iterations, array };
            }
        }

        [Benchmark]
        [MemberData(nameof(EnumerateRandomByteArrayTestCases))]
        public void Add(int iterations, byte[] data)
        {
            Span<byte> otherData = new byte[] { 1, 2, 3 };
            var bytes = new Span<byte>(data);
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        Marvin.ComputeHash(bytes, 123123123123UL);
                        Marvin.ComputeHash(otherData, 555888555888UL);
                    }
                }
            }
        }
    }
}
