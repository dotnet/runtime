// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Sdk;
using Microsoft.Xunit.Performance;

namespace System.Numerics.Tests
{
    public static class Constructor
    {
        private static Random s_random = new Random();

        public const int DefaultInnerIterationsCount = 100000000;

#if netcoreapp
        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_Byte()
        {
            byte[] arrValues = GenerateRandomValuesForVector<byte>();
            var spanValues = new Span<byte>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<byte>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_SByte()
        {
            sbyte[] arrValues = GenerateRandomValuesForVector<sbyte>();
            var spanValues = new Span<sbyte>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<sbyte>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_UInt16()
        {
            ushort[] arrValues = GenerateRandomValuesForVector<ushort>();
            var spanValues = new Span<ushort>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<ushort>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_Int16()
        {
            short[] arrValues = GenerateRandomValuesForVector<short>();
            var spanValues = new Span<short>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<short>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_UInt32()
        {
            uint[] arrValues = GenerateRandomValuesForVector<uint>();
            var spanValues = new Span<uint>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<uint>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_Int32()
        {
            int[] arrValues = GenerateRandomValuesForVector<int>();
            var spanValues = new Span<int>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<int>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_UInt64()
        {
            ulong[] arrValues = GenerateRandomValuesForVector<ulong>();
            var spanValues = new Span<ulong>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<ulong>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_Int64()
        {
            long[] arrValues = GenerateRandomValuesForVector<long>();
            var spanValues = new Span<long>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<long>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_Single()
        {
            float[] arrValues = GenerateRandomValuesForVector<float>();
            var spanValues = new Span<float>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<float>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_Double()
        {
            double[] arrValues = GenerateRandomValuesForVector<double>();
            var spanValues = new Span<double>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<double>(spanValues);
                }
            }
        }

        public static void Construct<T>(Span<T> values) where T : struct
        {
            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                Vector<T> vect = new Vector<T>(values);
            }
        }
#endif

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_Byte()
        {
            byte[] arrValues = GenerateRandomValuesForVector<byte>();
            var spanValues = new ReadOnlySpan<byte>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<byte>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_SByte()
        {
            sbyte[] arrValues = GenerateRandomValuesForVector<sbyte>();
            var spanValues = new ReadOnlySpan<sbyte>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<sbyte>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_UInt16()
        {
            ushort[] arrValues = GenerateRandomValuesForVector<ushort>();
            var spanValues = new ReadOnlySpan<ushort>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<ushort>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_Int16()
        {
            short[] arrValues = GenerateRandomValuesForVector<short>();
            var spanValues = new ReadOnlySpan<short>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<short>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_UInt32()
        {
            uint[] arrValues = GenerateRandomValuesForVector<uint>();
            var spanValues = new ReadOnlySpan<uint>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<uint>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_Int32()
        {
            int[] arrValues = GenerateRandomValuesForVector<int>();
            var spanValues = new ReadOnlySpan<int>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<int>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_UInt64()
        {
            ulong[] arrValues = GenerateRandomValuesForVector<ulong>();
            var spanValues = new ReadOnlySpan<ulong>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<ulong>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_Int64()
        {
            long[] arrValues = GenerateRandomValuesForVector<long>();
            var spanValues = new ReadOnlySpan<long>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<long>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_Single()
        {
            float[] arrValues = GenerateRandomValuesForVector<float>();
            var spanValues = new ReadOnlySpan<float>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<float>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_Double()
        {
            double[] arrValues = GenerateRandomValuesForVector<double>();
            var spanValues = new ReadOnlySpan<double>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<double>(spanValues);
                }
            }
        }


        public static void SpanCast<T>(ReadOnlySpan<T> values) where T : struct
        {
            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                ReadOnlySpan<Vector<T>> vectors = MemoryMarshal.Cast<T, Vector<T>>(values);
                Vector<T> vector = vectors[0];
            }
        }

        internal static T[] GenerateRandomValuesForVector<T>() where T : struct
        {
            int minValue = GetMinValue<T>();
            int maxValue = GetMaxValue<T>();
            return Util.GenerateRandomValues<T>(Vector<T>.Count, minValue, maxValue);
        }

        internal static int GetMinValue<T>() where T : struct
        {
            if (typeof(T) == typeof(long) || typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
            {
                return int.MinValue;
            }
            TypeInfo typeInfo = typeof(T).GetTypeInfo();
            FieldInfo field = typeInfo.GetDeclaredField("MinValue");
            var value = field.GetValue(null);
            return (int)(dynamic)value;
        }

        internal static int GetMaxValue<T>() where T : struct
        {
            if (typeof(T) == typeof(long) || typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
            {
                return int.MaxValue;
            }
            TypeInfo typeInfo = typeof(T).GetTypeInfo();
            FieldInfo field = typeInfo.GetDeclaredField("MaxValue");
            var value = field.GetValue(null);
            return (int)(dynamic)value;
        }
    }
}
