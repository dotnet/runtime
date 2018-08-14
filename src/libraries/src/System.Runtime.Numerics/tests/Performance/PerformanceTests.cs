// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Xunit;
using Microsoft.Xunit.Performance;
using Xunit.Abstractions;

namespace System.Numerics.Tests
{
    public class PerformanceTests
    {
        private readonly Random _random;
        private readonly ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output)
        {
            _random = new Random(1138);
            _output = output;
        }

        [Benchmark] //PerformanceTest
        [InlineData(1000000, 16, 16)]
        [InlineData(1000000, 64, 64)]
        [InlineData(1000000, 256, 256)]
        [InlineData(1000000, 1024, 1024)]
        [InlineData(100000, 4096, 4096)]
        [InlineData(100000, 16384, 16384)]
        [InlineData(100000, 65536, 65536)]
        [ActiveIssue(18248)]
        public void Add(int count, int leftBits, int rightBits)
        {
            RunBenchmark(count, leftBits, rightBits, (l, r) => BigInteger.Add(l, r));
        }

        [Benchmark] //PerformanceTest
        [InlineData(1000000, 16, 16)]
        [InlineData(1000000, 64, 64)]
        [InlineData(1000000, 256, 256)]
        [InlineData(1000000, 1024, 1024)]
        [InlineData(100000, 4096, 4096)]
        [InlineData(100000, 16384, 16384)]
        [InlineData(100000, 65536, 65536)]
        [ActiveIssue(18248)]
        public void Subtract(int count, int leftBits, int rightBits)
        {
            RunBenchmark(count, leftBits, rightBits, (l, r) => BigInteger.Subtract(l, r));
        }

        [Benchmark] //PerformanceTest
        [InlineData(1000000, 16, 16)]
        [InlineData(1000000, 64, 64)]
        [InlineData(1000000, 256, 256)]
        [InlineData(100000, 1024, 1024)]
        [InlineData(10000, 4096, 4096)]
        [InlineData(1000, 16384, 16384)]
        [InlineData(100, 65536, 65536)]
        [ActiveIssue(18248)]
        public void Multiply(int count, int leftBits, int rightBits)
        {
            RunBenchmark(count, leftBits, rightBits, (l, r) => BigInteger.Multiply(l, r));
        }

        [Benchmark] //PerformanceTest
        [InlineData(1000000, 16)]
        [InlineData(1000000, 64)]
        [InlineData(1000000, 256)]
        [InlineData(100000, 1024)]
        [InlineData(10000, 4096)]
        [InlineData(1000, 16384)]
        [InlineData(100, 65536)]
        [ActiveIssue(18248)]
        public void Square(int count, int bits)
        {
            RunBenchmark(count, bits, v => BigInteger.Multiply(v, v));
        }

        [Benchmark] //PerformanceTest
        [InlineData(1000000, 16, 16)]
        [InlineData(1000000, 64, 16)]
        [InlineData(1000000, 256, 128)]
        [InlineData(100000, 1024, 512)]
        [InlineData(10000, 4096, 2048)]
        [InlineData(1000, 16384, 8192)]
        [InlineData(100, 65536, 32768)]
        [ActiveIssue(18248)]
        public void Divide(int count, int leftBits, int rightBits)
        {
            RunBenchmark(count, leftBits, rightBits, (l, r) => BigInteger.Divide(l, r));
        }

        [Benchmark] //PerformanceTest
        [InlineData(1000000, 16, 16)]
        [InlineData(1000000, 64, 16)]
        [InlineData(1000000, 256, 128)]
        [InlineData(100000, 1024, 512)]
        [InlineData(10000, 4096, 2048)]
        [InlineData(1000, 16384, 8192)]
        [InlineData(100, 65536, 32768)]
        [ActiveIssue(18248)]
        public void Remainder(int count, int leftBits, int rightBits)
        {
            RunBenchmark(count, leftBits, rightBits, (l, r) => BigInteger.Remainder(l, r));
        }

        [Benchmark] //PerformanceTest
        [InlineData(1000000, 16, 16)]
        [InlineData(1000000, 64, 64)]
        [InlineData(100000, 256, 256)]
        [InlineData(100000, 1024, 1024)]
        [InlineData(10000, 4096, 4096)]
        [InlineData(1000, 16384, 16384)]
        [InlineData(100, 65536, 65536)]
        [ActiveIssue(18248)]
        public void GreatestCommonDivisor(int count, int leftBits, int rightBits)
        {
            RunBenchmark(count, leftBits, rightBits, (l, r) => BigInteger.GreatestCommonDivisor(l, r));
        }

        [Benchmark] //PerformanceTest
        [InlineData(100000, 16, 16, 16)]
        [InlineData(10000, 64, 64, 64)]
        [InlineData(1000, 256, 256, 256)]
        [InlineData(100, 1024, 1024, 1024)]
        [InlineData(10, 4096, 4096, 4096)]
        [InlineData(1, 16384, 16384, 16384)]
        [ActiveIssue(18248)]
        public void ModPow(int count, int leftBits, int rightBits, int otherBits)
        {
            RunBenchmark(count, leftBits, rightBits, otherBits, (l, r, o) => BigInteger.ModPow(l, r, o));
        }

        private void RunBenchmark(int count, int bits, Action<BigInteger> operation)
        {
            BigInteger[] value = CreateIntegerSeed(count, bits);

            long result = RunBenchmark(count, i => operation(value[i]));

            _output.WriteLine("({1:N0}) : {2:N0} ms / {0:N0} ops", count, bits, result);
        }

        private void RunBenchmark(int count, int leftBits, int rightBits, Action<BigInteger, BigInteger> operation)
        {
            BigInteger[] left = CreateIntegerSeed(count, leftBits);
            BigInteger[] right = CreateIntegerSeed(count, rightBits);

            long result = RunBenchmark(count, i => operation(left[i], right[i]));

            _output.WriteLine("({1:N0}; {2:N0}) : {3:N0} ms / {0:N0} ops", count, leftBits, rightBits, result);
        }

        private void RunBenchmark(int count, int leftBits, int rightBits, int otherBits, Action<BigInteger, BigInteger, BigInteger> operation)
        {
            BigInteger[] left = CreateIntegerSeed(count, leftBits);
            BigInteger[] right = CreateIntegerSeed(count, rightBits);
            BigInteger[] other = CreateIntegerSeed(count, otherBits);

            long result = RunBenchmark(count, i => operation(left[i], right[i], other[i]));

            _output.WriteLine("({1:N0}; {2:N0}; {3:N0}) : {4:N0} ms / {0:N0} ops", count, leftBits, rightBits, otherBits, result);
        }

        private const int MAX_SEED = 10;
        private const int RUN_COUNT = 3;

        private long RunBenchmark(int count, Action<int> operation)
        {
            Stopwatch watch = new Stopwatch();
            long result = long.MaxValue;

            operation(0);

            for (int j = 0; j < RUN_COUNT; j++)
            {
                watch.Restart();
                for (int i = 0; i < count; i++)
                    operation(i % MAX_SEED);
                watch.Stop();

                result = Math.Min(result, watch.ElapsedMilliseconds);
            }

            return result;
        }

        private BigInteger[] CreateIntegerSeed(int count, int bits)
        {
            BigInteger[] seed = new BigInteger[Math.Min(count, MAX_SEED)];

            for (int i = 0; i < seed.Length; i++)
                seed[i] = CreateRandomInteger(bits);

            return seed;
        }

        private BigInteger CreateRandomInteger(int bits)
        {
            byte[] value = new byte[(bits + 8) / 8];
            BigInteger result = BigInteger.Zero;

            while (result.IsZero)
            {
                _random.NextBytes(value);

                // ensure actual bit count (remaining bits not set)
                // ensure positive value (highest-order bit not set)
                value[value.Length - 1] &= (byte)(0xFF >> 8 - bits % 8);

                result = new BigInteger(value);
            }

            return result;
        }
    }
}
