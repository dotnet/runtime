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

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_Byte()
        {
            Byte[] arrValues = GenerateRandomValuesForVector<Byte>();
            var spanValues = new Span<Byte>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<Byte>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_SByte()
        {
            SByte[] arrValues = GenerateRandomValuesForVector<SByte>();
            var spanValues = new Span<SByte>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<SByte>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_UInt16()
        {
            UInt16[] arrValues = GenerateRandomValuesForVector<UInt16>();
            var spanValues = new Span<UInt16>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<UInt16>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_Int16()
        {
            Int16[] arrValues = GenerateRandomValuesForVector<Int16>();
            var spanValues = new Span<Int16>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<Int16>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_UInt32()
        {
            UInt32[] arrValues = GenerateRandomValuesForVector<UInt32>();
            var spanValues = new Span<UInt32>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<UInt32>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_Int32()
        {
            Int32[] arrValues = GenerateRandomValuesForVector<Int32>();
            var spanValues = new Span<Int32>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<Int32>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_UInt64()
        {
            UInt64[] arrValues = GenerateRandomValuesForVector<UInt64>();
            var spanValues = new Span<UInt64>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<UInt64>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_Int64()
        {
            Int64[] arrValues = GenerateRandomValuesForVector<Int64>();
            var spanValues = new Span<Int64>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<Int64>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_Single()
        {
            Single[] arrValues = GenerateRandomValuesForVector<Single>();
            var spanValues = new Span<Single>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<Single>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void ConstructorBenchmark_Double()
        {
            Double[] arrValues = GenerateRandomValuesForVector<Double>();
            var spanValues = new Span<Double>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Construct<Double>(spanValues);
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

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_Byte()
        {
            Byte[] arrValues = GenerateRandomValuesForVector<Byte>();
            var spanValues = new ReadOnlySpan<Byte>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<Byte>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_SByte()
        {
            SByte[] arrValues = GenerateRandomValuesForVector<SByte>();
            var spanValues = new ReadOnlySpan<SByte>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<SByte>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_UInt16()
        {
            UInt16[] arrValues = GenerateRandomValuesForVector<UInt16>();
            var spanValues = new ReadOnlySpan<UInt16>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<UInt16>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_Int16()
        {
            Int16[] arrValues = GenerateRandomValuesForVector<Int16>();
            var spanValues = new ReadOnlySpan<Int16>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<Int16>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_UInt32()
        {
            UInt32[] arrValues = GenerateRandomValuesForVector<UInt32>();
            var spanValues = new ReadOnlySpan<UInt32>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<UInt32>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_Int32()
        {
            Int32[] arrValues = GenerateRandomValuesForVector<Int32>();
            var spanValues = new ReadOnlySpan<Int32>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<Int32>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_UInt64()
        {
            UInt64[] arrValues = GenerateRandomValuesForVector<UInt64>();
            var spanValues = new ReadOnlySpan<UInt64>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<UInt64>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_Int64()
        {
            Int64[] arrValues = GenerateRandomValuesForVector<Int64>();
            var spanValues = new ReadOnlySpan<Int64>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<Int64>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_Single()
        {
            Single[] arrValues = GenerateRandomValuesForVector<Single>();
            var spanValues = new ReadOnlySpan<Single>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<Single>(spanValues);
                }
            }
        }

        [Benchmark(InnerIterationCount = DefaultInnerIterationsCount)]
        public static void SpanCastBenchmark_Double()
        {
            Double[] arrValues = GenerateRandomValuesForVector<Double>();
            var spanValues = new ReadOnlySpan<Double>(arrValues);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    SpanCast<Double>(spanValues);
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
            if (typeof(T) == typeof(Int64) || typeof(T) == typeof(Single) || typeof(T) == typeof(Double) || typeof(T) == typeof(UInt32) || typeof(T) == typeof(UInt64))
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
            if (typeof(T) == typeof(Int64) || typeof(T) == typeof(Single) || typeof(T) == typeof(Double) || typeof(T) == typeof(UInt32) || typeof(T) == typeof(UInt64))
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
