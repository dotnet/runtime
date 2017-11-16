// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Numerics.Tests
{
    public partial class Perf_BigInteger
    {
        public static IEnumerable<object[]> NumberStrings()
        {
            yield return new object[] { "123" };
            yield return new object[] { int.MinValue.ToString() };
            yield return new object[] { string.Concat(Enumerable.Repeat("1234567890", 20)) };
        }

        // TODO #18249: Port disabled perf tests from tests\BigInteger\PerformanceTests.cs

        [Benchmark]
        [MemberData(nameof(NumberStrings))]
        public void Ctor_ByteArray(string numberString)
        {
            byte[] input = BigInteger.Parse(numberString).ToByteArray();
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 1000000; i++)
                    {
                        var bi = new BigInteger(input);
                    }
                }
            }
        }

        [Benchmark]
        [MemberData(nameof(NumberStrings))]
        public void ToByteArray(string numberString)
        {
            BigInteger bi = BigInteger.Parse(numberString);
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 1000000; i++)
                    {
                        bi.ToByteArray();
                    }
                }
            }
        }
        [Benchmark]
        [MemberData(nameof(NumberStrings))]
        public void Parse(string numberString)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 100000; i++)
                    {
                        BigInteger.Parse(numberString);
                    }
                }
            }
        }
    }
}
