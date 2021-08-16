// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;
using Microsoft.Xunit.Performance;

[assembly: OptimizeForBenchmarks]

namespace Span
{
    public class SpanBench
    {

#if DEBUG
        const int BubbleSortIterations = 1;
        const int QuickSortIterations = 1;
        const int FillAllIterations = 1;
        const int BaseIterations = 1;
#else
        // Appropriately-scaled iteration counts for the various benchmarks
        const int BubbleSortIterations = 100;
        const int QuickSortIterations = 1000;
        const int FillAllIterations = 100000;
        const int BaseIterations = 10000000;
#endif

        // Seed for random set by environment variables
        public const int DefaultSeed = 20010415;
        public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
        {
            string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
            string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
            _ => DefaultSeed
        };

        // Default length for arrays of mock input data
        const int DefaultLength = 1024;

        // Helpers
        #region Helpers
        [StructLayout(LayoutKind.Sequential)]
        private sealed class TestClass<T>
        {
            private double _d;
            public T[] C0;
        }

        // Copying the result of a computation to Sink<T>.Instance is a way
        // to prevent the jit from considering the computation dead and removing it.
        private sealed class Sink<T>
        {
            public T Data;
            public static Sink<T> Instance = new Sink<T>();
        }

        // Use statics to smuggle some information from Main to Invoke when running tests
        // from the command line.
        static bool IsXunitInvocation = true; // xunit-perf leaves this true; command line Main sets to false
        static int CommandLineInnerIterationCount = 0;   // used to communicate iteration count from BenchmarkAttribute
                                                         // (xunit-perf exposes the same in static property Benchmark.InnerIterationCount)
        static bool DoWarmUp; // Main sets this when calling a new benchmark routine


        // Invoke routine to abstract away the difference between running under xunit-perf vs running from the
        // command line.  Inner loop to be measured is taken as an Action<int>, and invoked passing the number
        // of iterations that the inner loop should execute.
        static void Invoke(Action<int> innerLoop, string nameFormat, params object[] nameArgs)
        {
            if (IsXunitInvocation)
            {
                foreach (var iteration in Benchmark.Iterations)
                    using (iteration.StartMeasurement())
                        innerLoop((int)Benchmark.InnerIterationCount);
            }
            else
            {
                if (DoWarmUp)
                {
                    // Run some warm-up iterations before measuring
                    innerLoop(CommandLineInnerIterationCount);
                    // Clear the flag since we're now warmed up (caller will
                    // reset it before calling new code)
                    DoWarmUp = false;
                }

                // Now do the timed run of the inner loop.
                Stopwatch sw = Stopwatch.StartNew();
                innerLoop(CommandLineInnerIterationCount);
                sw.Stop();

                // Print result.
                string name = String.Format(nameFormat, nameArgs);
                double timeInMs = sw.Elapsed.TotalMilliseconds;
                Console.WriteLine("{0}: {1}ms", name, timeInMs);
            }
        }

        // Helper for the sort tests to get some pseudo-random input
        static int[] GetUnsortedData(int length)
        {
            int[] unsortedData = new int[length];
            Random r = new Random(Seed);
            for (int i = 0; i < unsortedData.Length; ++i)
            {
                unsortedData[i] = r.Next();
            }
            return unsortedData;
        }
        #endregion // helpers

        // Tests that implement some vary basic algorithms (fill/sort) over spans and arrays
        #region Algorithm tests

        #region TestFillAllSpan
        [Benchmark(InnerIterationCount = FillAllIterations)]
        [InlineData(DefaultLength)]
        public static void FillAllSpan(int length)
        {
            byte[] a = new byte[length];

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    TestFillAllSpan(s);
                }
            },
            "TestFillAllSpan({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestFillAllSpan(Span<byte> span)
        {
            for (int i = 0; i < span.Length; ++i)
            {
                span[i] = unchecked((byte)i);
            }
        }
        #endregion

        #region TestFillAllArray
        [Benchmark(InnerIterationCount = FillAllIterations)]
        [InlineData(DefaultLength)]
        public static void FillAllArray(int length)
        {
            byte[] a = new byte[length];

            Invoke((int innerIterationCount) =>
            {
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    TestFillAllArray(a);
                }
            },
            "TestFillArray({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestFillAllArray(byte[] data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = unchecked((byte)i);
            }
        }
        #endregion

        #region TestFillAllReverseSpan
        [Benchmark(InnerIterationCount = FillAllIterations)]
        [InlineData(DefaultLength)]
        public static void FillAllReverseSpan(int length)
        {
            byte[] a = new byte[length];

            Invoke((int innerIterationCount) =>
            {
                Span<byte> s = new Span<byte>(a);
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    TestFillAllReverseSpan(s);
                }
            },
            "TestFillAllReverseSpan({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestFillAllReverseSpan(Span<byte> span)
        {
            for (int i = span.Length; --i >= 0;)
            {
                span[i] = unchecked((byte)i);
            }
        }
        #endregion

        #region TestFillAllReverseArray
        [Benchmark(InnerIterationCount = FillAllIterations)]
        [InlineData(DefaultLength)]
        public static void FillAllReverseArray(int length)
        {
            byte[] a = new byte[length];

            Invoke((int innerIterationCount) =>
            {
                for (int i = 0; i < innerIterationCount; ++i)
                {
                    TestFillAllReverseArray(a);
                }
            },
            "TestFillAllReverseArray({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestFillAllReverseArray(byte[] data)
        {
            for (int i = data.Length; --i >= 0;)
            {
                data[i] = unchecked((byte)i);
            }
        }
        #endregion

        #region TestQuickSortSpan
        [Benchmark(InnerIterationCount = QuickSortIterations)]
        [InlineData(DefaultLength)]
        public static void QuickSortSpan(int length)
        {
            int[] data = new int[length];
            int[] unsortedData = GetUnsortedData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<int> span = new Span<int>(data);

                for (int i = 0; i < innerIterationCount; ++i)
                {
                    Array.Copy(unsortedData, data, length);
                    TestQuickSortSpan(span);
                }
            },
            "TestQuickSortSpan({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestQuickSortSpan(Span<int> data)
        {
            if (data.Length <= 1)
            {
                return;
            }

            int lo = 0;
            int hi = data.Length - 1;
            int i, j;
            int pivot, temp;
            for (i = lo, j = hi, pivot = data[hi]; i < j;)
            {
                while (i < j && data[i] <= pivot)
                {
                    ++i;
                }
                while (j > i && data[j] >= pivot)
                {
                    --j;
                }
                if (i < j)
                {
                    temp = data[i];
                    data[i] = data[j];
                    data[j] = temp;
                }
            }
            if (i != hi)
            {
                temp = data[i];
                data[i] = pivot;
                data[hi] = temp;
            }

            TestQuickSortSpan(data.Slice(0, i));
            TestQuickSortSpan(data.Slice(i + 1));
        }
        #endregion

        #region TestBubbleSortSpan
        [Benchmark(InnerIterationCount = BubbleSortIterations)]
        [InlineData(DefaultLength)]
        public static void BubbleSortSpan(int length)
        {
            int[] data = new int[length];
            int[] unsortedData = GetUnsortedData(length);

            Invoke((int innerIterationCount) =>
            {
                Span<int> span = new Span<int>(data);

                for (int i = 0; i < innerIterationCount; i++)
                {
                    Array.Copy(unsortedData, data, length);
                    TestBubbleSortSpan(span);
                }
            },
            "TestBubbleSortSpan({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestBubbleSortSpan(Span<int> span)
        {
            bool swap;
            int temp;
            int n = span.Length - 1;
            do
            {
                swap = false;
                for (int i = 0; i < n; i++)
                {
                    if (span[i] > span[i + 1])
                    {
                        temp = span[i];
                        span[i] = span[i + 1];
                        span[i + 1] = temp;
                        swap = true;
                    }
                }
                --n;
            }
            while (swap);
        }
        #endregion

        #region TestQuickSortArray
        [Benchmark(InnerIterationCount = QuickSortIterations)]
        [InlineData(DefaultLength)]
        public static void QuickSortArray(int length)
        {
            int[] data = new int[length];
            int[] unsortedData = GetUnsortedData(length);

            Invoke((int innerIterationCount) =>
            {
                for (int i = 0; i < innerIterationCount; i++)
                {
                    Array.Copy(unsortedData, data, length);
                    TestQuickSortArray(data, 0, data.Length - 1);
                }
            },
            "TestQuickSortArray({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestQuickSortArray(int[] data, int lo, int hi)
        {
            if (lo >= hi)
            {
                return;
            }

            int i, j;
            int pivot, temp;
            for (i = lo, j = hi, pivot = data[hi]; i < j;)
            {
                while (i < j && data[i] <= pivot)
                {
                    ++i;
                }
                while (j > i && data[j] >= pivot)
                {
                    --j;
                }
                if (i < j)
                {
                    temp = data[i];
                    data[i] = data[j];
                    data[j] = temp;
                }
            }
            if (i != hi)
            {
                temp = data[i];
                data[i] = pivot;
                data[hi] = temp;
            }

            TestQuickSortArray(data, lo, i - 1);
            TestQuickSortArray(data, i + 1, hi);
        }
        #endregion

        #region TestBubbleSortArray
        [Benchmark(InnerIterationCount = BubbleSortIterations)]
        [InlineData(DefaultLength)]
        public static void BubbleSortArray(int length)
        {
            int[] data = new int[length];
            int[] unsortedData = GetUnsortedData(length);

            Invoke((int innerIterationCount) =>
            {
                for (int i = 0; i < innerIterationCount; i++)
                {
                    Array.Copy(unsortedData, data, length);
                    TestBubbleSortArray(data);
                }
            },
            "TestBubbleSortArray({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestBubbleSortArray(int[] data)
        {
            bool swap;
            int temp;
            int n = data.Length - 1;
            do
            {
                swap = false;
                for (int i = 0; i < n; i++)
                {
                    if (data[i] > data[i + 1])
                    {
                        temp = data[i];
                        data[i] = data[i + 1];
                        data[i + 1] = temp;
                        swap = true;
                    }
                }
                --n;
            }
            while (swap);
        }
        #endregion

        #endregion // Algorithm tests

        // TestSpanAPIs (For comparison with Array and Slow Span)
        #region TestSpanAPIs

        #region TestSpanConstructor<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanConstructorByte(int length)
        {
            InvokeTestSpanConstructor<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations / 100)]
        [InlineData(100)]
        public static void TestSpanConstructorString(int length)
        {
            InvokeTestSpanConstructor<string>(length);
        }

        static void InvokeTestSpanConstructor<T>(int length)
        {
            var array = new T[length];

            Invoke((int innerIterationCount) => TestSpanConstructor<T>(array, innerIterationCount, false),
                "TestSpanConstructor<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanConstructor<T>(T[] array, int iterationCount, bool untrue)
        {
            var sink = Sink<T>.Instance;

            for (int i = 0; i < iterationCount; i++)
            {
                var span = new Span<T>(array);
                // Under a condition that we know is false but the jit doesn't,
                // add a read from 'span' to make sure it's not dead, and an assignment
                // to 'array' so the constructor call won't get hoisted.
                if (untrue) { sink.Data = span[0]; array = new T[iterationCount]; }
            }
        }
        #endregion

#if false // netcoreapp specific API https://github.com/dotnet/runtime/issues/9635
        #region TestSpanCreate<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanCreateByte(int length)
        {
            InvokeTestSpanCreate<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanCreateString(int length)
        {
            InvokeTestSpanCreate<string>(length);
        }

        static void InvokeTestSpanCreate<T>(int length)
        {
            TestClass<T> testClass = new TestClass<T>();
            testClass.C0 = new T[length];

            Invoke((int innerIterationCount) => TestSpanCreate<T>(testClass, innerIterationCount, false),
                "TestSpanCreate<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanCreate<T>(TestClass<T> testClass, int iterationCount, bool untrue)
        {
            var sink = Sink<T>.Instance;

            for (int i = 0; i < iterationCount; i++)
            {
                var span = MemoryMarshal.CreateSpan<T>(ref testClass.C0[0], testClass.C0.Length);
                // Under a condition that we know is false but the jit doesn't,
                // add a read from 'span' to make sure it's not dead, and an assignment
                // to 'testClass' so the Create call won't get hoisted.
                if (untrue) { sink.Data = span[0]; testClass = new TestClass<T>(); }
            }
        }
        #endregion
#endif

        #region TestMemoryMarshalGetReference<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestMemoryMarshalGetReferenceByte(int length)
        {
            InvokeTestMemoryMarshalGetReference<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestMemoryMarshalGetReferenceString(int length)
        {
            InvokeTestMemoryMarshalGetReference<string>(length);
        }

        static void InvokeTestMemoryMarshalGetReference<T>(int length)
        {
            var array = new T[length];

            Invoke((int innerIterationCount) => TestMemoryMarshalGetReference<T>(array, innerIterationCount),
                "TestMemoryMarshalGetReference<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestMemoryMarshalGetReference<T>(T[] array, int iterationCount)
        {
            var sink = Sink<T>.Instance;
            var span = new Span<T>(array);

            for (int i = 0; i < iterationCount; i++)
            {
                ref T temp = ref MemoryMarshal.GetReference(span);
                sink.Data = temp;
            }
        }
        #endregion

        #region TestSpanIndexHoistable<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanIndexHoistableByte(int length)
        {
            InvokeTestSpanIndexHoistable<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanIndexHoistableString(int length)
        {
            InvokeTestSpanIndexHoistable<string>(length);
        }

        static void InvokeTestSpanIndexHoistable<T>(int length)
        {
            var array = new T[length];

            Invoke((int innerIterationCount) => TestSpanIndexHoistable<T>(array, length, innerIterationCount),
                "TestSpanIndexHoistable<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanIndexHoistable<T>(T[] array, int length, int iterationCount)
        {
            var sink = Sink<T>.Instance;
            var span = new Span<T>(array);

            for (int i = 0; i < iterationCount; i++)
                    sink.Data = span[length/2];
        }
        #endregion

        #region TestArrayIndexHoistable<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestArrayIndexHoistableByte(int length)
        {
            InvokeTestArrayIndexHoistable<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestArrayIndexHoistableString(int length)
        {
            InvokeTestArrayIndexHoistable<string>(length);
        }

        static void InvokeTestArrayIndexHoistable<T>(int length)
        {
            var array = new T[length];
            Invoke((int innerIterationCount) => TestArrayIndexHoistable<T>(array, length, innerIterationCount),
                "TestArrayIndexHoistable<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestArrayIndexHoistable<T>(T[] array, int length, int iterationCount)
        {
            var sink = Sink<T>.Instance;

            for (int i = 0; i < iterationCount; i++)
                sink.Data = array[length / 2];
        }
        #endregion

        #region TestSpanIndexVariant<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanIndexVariantByte(int length)
        {
            InvokeTestSpanIndexVariant<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanIndexVariantString(int length)
        {
            InvokeTestSpanIndexVariant<string>(length);
        }

        static void InvokeTestSpanIndexVariant<T>(int length)
        {
            var array = new T[length];

            Invoke((int innerIterationCount) => TestSpanIndexVariant<T>(array, length, innerIterationCount),
                "TestSpanIndexVariant<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanIndexVariant<T>(T[] array, int length, int iterationCount)
        {
            var sink = Sink<T>.Instance;
            var span = new Span<T>(array);
            int mask = (length < 2 ? 0 : (length < 8 ? 1 : 7));

            for (int i = 0; i < iterationCount; i++)
                sink.Data = span[i & mask];
        }
        #endregion

        #region TestArrayIndexVariant<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestArrayIndexVariantByte(int length)
        {
            InvokeTestArrayIndexVariant<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestArrayIndexVariantString(int length)
        {
            InvokeTestArrayIndexVariant<string>(length);
        }

        static void InvokeTestArrayIndexVariant<T>(int length)
        {
            var array = new T[length];

            Invoke((int innerIterationCount) => TestArrayIndexVariant<T>(array, length, innerIterationCount),
                "TestArrayIndexVariant<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestArrayIndexVariant<T>(T[] array, int length, int iterationCount)
        {
            var sink = Sink<T>.Instance;
            int mask = (length < 2 ? 0 : (length < 8 ? 1 : 7));

            for (int i = 0; i < iterationCount; i++)
            {
                sink.Data = array[i & mask];
            }
        }
        #endregion

        #region TestSpanSlice<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanSliceByte(int length)
        {
            InvokeTestSpanSlice<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanSliceString(int length)
        {
            InvokeTestSpanSlice<string>(length);
        }

        static void InvokeTestSpanSlice<T>(int length)
        {
            var array = new T[length];

            Invoke((int innerIterationCount) => TestSpanSlice<T>(array, length, innerIterationCount, false),
                "TestSpanSlice<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanSlice<T>(T[] array, int length, int iterationCount, bool untrue)
        {
            var span = new Span<T>(array);
            var sink = Sink<T>.Instance;

            for (int i = 0; i < iterationCount; i++)
            {
                var slice = span.Slice(length / 2);
                // Under a condition that we know is false but the jit doesn't,
                // add a read from 'span' to make sure it's not dead, and an assignment
                // to 'array' so the slice call won't get hoisted.
                if (untrue) { sink.Data = slice[0]; array = new T[iterationCount]; }
            }
        }
        #endregion

        #region TestSpanToArray<T>
        [Benchmark(InnerIterationCount = BaseIterations / 100)]
        [InlineData(100)]
        public static void TestSpanToArrayByte(int length)
        {
            InvokeTestSpanToArray<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations / 100)]
        [InlineData(100)]
        public static void TestSpanToArrayString(int length)
        {
            InvokeTestSpanToArray<string>(length);
        }

        static void InvokeTestSpanToArray<T>(int length)
        {
            var array = new T[length];

            Invoke((int innerIterationCount) => TestSpanToArray<T>(array, length, innerIterationCount),
                "TestSpanToArray<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanToArray<T>(T[] array, int length, int iterationCount)
        {
            var span = new Span<T>(array);
            var sink = Sink<T[]>.Instance;

            for (int i = 0; i < iterationCount; i++)
                sink.Data = span.ToArray();
        }
        #endregion

        #region TestSpanCopyTo<T>
        [Benchmark(InnerIterationCount = BaseIterations / 10)]
        [InlineData(100)]
        public static void TestSpanCopyToByte(int length)
        {
            InvokeTestSpanCopyTo<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations / 100)]
        [InlineData(100)]
        public static void TestSpanCopyToString(int length)
        {
            InvokeTestSpanCopyTo<string>(length);
        }

        static void InvokeTestSpanCopyTo<T>(int length)
        {
            var array = new T[length];
            var destArray = new T[array.Length];

            Invoke((int innerIterationCount) => TestSpanCopyTo<T>(array, destArray, innerIterationCount),
                "TestSpanCopyTo<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanCopyTo<T>(T[] array, T[] destArray, int iterationCount)
        {
            var span = new Span<T>(array);
            var destination = new Span<T>(destArray);

            for (int i = 0; i < iterationCount; i++)
                span.CopyTo(destination);
        }
        #endregion

        #region TestArrayCopyTo<T>
        [Benchmark(InnerIterationCount = BaseIterations / 10)]
        [InlineData(100)]
        public static void TestArrayCopyToByte(int length)
        {
            InvokeTestArrayCopyTo<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations / 100)]
        [InlineData(100)]
        public static void TestArrayCopyToString(int length)
        {
            InvokeTestArrayCopyTo<string>(length);
        }

        static void InvokeTestArrayCopyTo<T>(int length)
        {
            var array = new T[length];
            var destination = new T[array.Length];

            Invoke((int innerIterationCount) => TestArrayCopyTo<T>(array, destination, innerIterationCount),
                "TestArrayCopyTo<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestArrayCopyTo<T>(T[] array, T[] destination, int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
                array.CopyTo(destination, 0);
        }
        #endregion

        #region TestSpanFill<T>
        [Benchmark(InnerIterationCount = BaseIterations * 10)]
        [InlineData(100)]
        public static void TestSpanFillByte(int length)
        {
            InvokeTestSpanFill<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations / 100)]
        [InlineData(100)]
        public static void TestSpanFillString(int length)
        {
            InvokeTestSpanFill<string>(length);
        }

        static void InvokeTestSpanFill<T>(int length)
        {
            var array = new T[length];

            Invoke((int innerIterationCount) => TestSpanFill<T>(array, innerIterationCount),
                "TestSpanFill<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanFill<T>(T[] array, int iterationCount)
        {
            var span = new Span<T>(array);
            for (int i = 0; i < iterationCount; i++)
                span.Fill(default(T));
        }
        #endregion

        #region TestSpanClear<T>
        [Benchmark(InnerIterationCount = BaseIterations / 10)]
        [InlineData(100)]
        public static void TestSpanClearByte(int length)
        {
            InvokeTestSpanClear<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations / 10)]
        [InlineData(100)]
        public static void TestSpanClearString(int length)
        {
            InvokeTestSpanClear<string>(length);
        }

        static void InvokeTestSpanClear<T>(int length)
        {
            var array = new T[length];

            Invoke((int innerIterationCount) => TestSpanClear<T>(array, innerIterationCount),
                "TestSpanClear<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanClear<T>(T[] array, int iterationCount)
        {
            var span = new Span<T>(array);
            for (int i = 0; i < iterationCount; i++)
                span.Clear();
        }
        #endregion

        #region TestArrayClear<T>
        [Benchmark(InnerIterationCount = BaseIterations / 10)]
        [InlineData(100)]
        public static void TestArrayClearByte(int length)
        {
            InvokeTestArrayClear<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations / 10)]
        [InlineData(100)]
        public static void TestArrayClearString(int length)
        {
            InvokeTestArrayClear<string>(length);
        }

        static void InvokeTestArrayClear<T>(int length)
        {
            var array = new T[length];

            Invoke((int innerIterationCount) => TestArrayClear<T>(array, length, innerIterationCount),
                "TestArrayClear<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestArrayClear<T>(T[] array, int length, int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
                Array.Clear(array, 0, length);
        }
        #endregion

        #region TestSpanAsBytes<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanAsBytesByte(int length)
        {
            InvokeTestSpanAsBytes<byte>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanAsBytesInt(int length)
        {
            InvokeTestSpanAsBytes<int>(length);
        }

        static void InvokeTestSpanAsBytes<T>(int length)
            where T : struct
        {
            var array = new T[length];

            Invoke((int innerIterationCount) => TestSpanAsBytes<T>(array, innerIterationCount, false),
                "TestSpanAsBytes<{0}>({1})", typeof(T).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanAsBytes<T>(T[] array, int iterationCount, bool untrue)
            where T : struct
        {
            var sink = Sink<byte>.Instance;
            var span = new Span<T>(array);

            for (int i = 0; i < iterationCount; i++)
            {
                var byteSpan = MemoryMarshal.AsBytes(span);
                // Under a condition that we know is false but the jit doesn't,
                // add a read from 'byteSpan' to make sure it's not dead, and an assignment
                // to 'span' so the AsBytes call won't get hoisted.
                if (untrue) { sink.Data = byteSpan[0]; span = new Span<T>(); }
            }
        }
        #endregion

        #region TestSpanCast<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanCastFromByteToInt(int length)
        {
            InvokeTestSpanCast<byte, int>(length);
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanCastFromIntToByte(int length)
        {
            InvokeTestSpanCast<int, byte>(length);
        }

        static void InvokeTestSpanCast<From, To>(int length)
            where From : struct
            where To : struct
        {
            var array = new From[length];

            Invoke((int innerIterationCount) => TestSpanCast<From, To>(array, innerIterationCount, false),
                "TestSpanCast<{0}, {1}>({2})", typeof(From).Name, typeof(To).Name, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanCast<From, To>(From[] array, int iterationCount, bool untrue)
            where From : struct
            where To : struct
        {
            var sink = Sink<To>.Instance;
            var span = new Span<From>(array);

            for (int i = 0; i < iterationCount; i++)
            {
                var toSpan = MemoryMarshal.Cast<From, To>(span);
                // Under a condition that we know is false but the jit doesn't,
                // add a read from 'toSpan' to make sure it's not dead, and an assignment
                // to 'span' so the AsBytes call won't get hoisted.
                if (untrue) { sink.Data = toSpan[0]; span = new Span<From>(); }
            }
        }
        #endregion

        #region TestSpanAsSpanStringChar<T>

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(100)]
        public static void TestSpanAsSpanStringCharWrapper(int length)
        {
            StringBuilder sb = new StringBuilder();
            Random rand = new Random(Seed);
            char[] c = new char[1];
            for (int i = 0; i < length; i++)
            {
                c[0] = (char)rand.Next(32, 126);
                sb.Append(new string(c));
            }
            string s = sb.ToString();

            Invoke((int innerIterationCount) => TestSpanAsSpanStringChar(s, innerIterationCount, false),
                "TestSpanAsSpanStringChar({0})", length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanAsSpanStringChar(string s, int iterationCount, bool untrue)
        {
            var sink = Sink<char>.Instance;

            for (int i = 0; i < iterationCount; i++)
            {
                var charSpan = s.AsSpan();
                // Under a condition that we know is false but the jit doesn't,
                // add a read from 'charSpan' to make sure it's not dead, and an assignment
                // to 's' so the AsBytes call won't get hoisted.
                if (untrue) { sink.Data = charSpan[0]; s = "block hoisting the call to AsSpan()"; }
            }
        }

        #endregion

        #endregion // TestSpanAPIs


        public static int Main(string[] args)
        {
            // When we call into Invoke, it'll need to know this isn't xunit-perf running
            IsXunitInvocation = false;

            // Now simulate xunit-perf's benchmark discovery so we know what tests to invoke
            TypeInfo t = typeof(SpanBench).GetTypeInfo();
            foreach(MethodInfo m in t.DeclaredMethods)
            {
                BenchmarkAttribute benchAttr = m.GetCustomAttribute<BenchmarkAttribute>();
                if (benchAttr != null)
                {
                    // All benchmark methods in this test set the InnerIterationCount on their BenchmarkAttribute.
                    // Take max of specified count and 1 since some tests use expressions for their count that
                    // evaluate to 0 under DEBUG.
                    CommandLineInnerIterationCount = Math.Max((int)benchAttr.InnerIterationCount, 1);

                    // Request a warm-up iteration before measuring this benchmark method.
                    DoWarmUp = true;

                    // Get the benchmark to measure as a delegate taking the number of inner-loop iterations to run
                    var invokeMethod = m.CreateDelegate(typeof(Action<int>)) as Action<int>;

                    // All the benchmarks methods in this test use [InlineData] to specify how many times and with
                    // what arguments they should be run.
                    foreach (InlineDataAttribute dataAttr in m.GetCustomAttributes<InlineDataAttribute>())
                    {
                        foreach (object[] data in dataAttr.GetData(m))
                        {
                            // All the benchmark methods in this test take a single int parameter
                            invokeMethod((int)data[0]);
                        }
                    }
                }
            }

            // The only failure modes are crash/exception.
            return 100;
        }
    }
}
