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
        [Benchmark]
        [MemberData(nameof(NumberStrings))]
        public void Ctor_ByteSpan(string numberString)
        {
            Span<byte> input = BigInteger.Parse(numberString).ToByteArray();
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
        public void Ctor_ByteSpan_BigEndian(string numberString)
        {
            Span<byte> input = BigInteger.Parse(numberString).ToByteArray(isBigEndian: true);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 1000000; i++)
                    {
                        var bi = new BigInteger(input, isBigEndian: true);
                    }
                }
            }
        }

        [Benchmark]
        [MemberData(nameof(NumberStrings))]
        public void TryWriteBytes(string numberString)
        {
            BigInteger bi = BigInteger.Parse(numberString);
            Span<byte> destination = new byte[bi.GetByteCount()];
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 1000000; i++)
                    {
                        bi.TryWriteBytes(destination, out int bytesWritten);
                    }
                }
            }
        }

        [Benchmark]
        [MemberData(nameof(NumberStrings))]
        public void TryWriteBytes_BigEndian(string numberString)
        {
            BigInteger bi = BigInteger.Parse(numberString);
            Span<byte> destination = new byte[bi.GetByteCount()];
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 1000000; i++)
                    {
                        bi.TryWriteBytes(destination, out int bytesWritten, isBigEndian: true);
                    }
                }
            }
        }
    }
}
