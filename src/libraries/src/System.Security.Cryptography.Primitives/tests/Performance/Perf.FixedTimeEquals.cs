// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Primitives.Tests.Performance
{
    public class Perf_FixedTimeEquals
    {
        private const int IterationCountFor256Bit = 300000;

        [Benchmark(InnerIterationCount = IterationCountFor256Bit)]
        public static void FixedTimeEquals_256Bit_Equal()
        {
            MeasureFixedTimeEquals(
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336",
                "0000000000000000000000000000000000000000000000000000000000000000");
        }

        [Benchmark(InnerIterationCount = IterationCountFor256Bit)]
        public static void FixedTimeEquals_256Bit_LastBitDifferent()
        {
            MeasureFixedTimeEquals(
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336",
                "0000000000000000000000000000000000000000000000000000000000000001");
        }

        [Benchmark(InnerIterationCount = IterationCountFor256Bit)]
        public static void FixedTimeEquals_256Bit_FirstBitDifferent()
        {
            MeasureFixedTimeEquals(
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336",
                "8000000000000000000000000000000000000000000000000000000000000000");
        }

        [Benchmark(InnerIterationCount = IterationCountFor256Bit)]
        public static void FixedTimeEquals_256Bit_CascadingErrors()
        {
            MeasureFixedTimeEquals(
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336",
                "0102040810204080112244880000000000000000000000000000000000000000");
        }

        [Benchmark(InnerIterationCount = IterationCountFor256Bit)]
        public static void FixedTimeEquals_256Bit_AllBitsDifferent()
        {
            MeasureFixedTimeEquals(
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336",
                "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff");
        }

        [Benchmark(InnerIterationCount = IterationCountFor256Bit)]
        public static void FixedTimeEquals_256Bit_VersusZero()
        {
            MeasureFixedTimeEquals(
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336",
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336");
        }

        [Benchmark(InnerIterationCount = IterationCountFor256Bit)]
        public static void FixedTimeEquals_256Bit_SameReference()
        {
            byte[] test = "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336".HexToByteArray();

            Span<byte> left = test;
            Span<byte> right = test;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        CryptographicOperations.FixedTimeEquals(left, right);
                    }
                }
            }
        }

        // The important statistics from these perf runs aren't the mean, but the t-test for
        // every set of the same length being the same as when it was equal.
        private static void MeasureFixedTimeEquals(string baseValueHex, string errorVectorHex)
        {
            if (errorVectorHex.Length != baseValueHex.Length)
            {
                throw new InvalidOperationException();
            }

            byte[] a = baseValueHex.HexToByteArray();
            byte[] b = errorVectorHex.HexToByteArray();

            for (int i = 0; i < a.Length; i++)
            {
                b[i] ^= a[i];
            }

            Span<byte> left = a;
            Span<byte> right = b;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        CryptographicOperations.FixedTimeEquals(left, right);
                    }
                }
            }
        }
    }
}
