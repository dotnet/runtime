// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
[assembly: MeasureInstructionsRetired]

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
        const int BubbleSortIterations = 100;
        const int QuickSortIterations = 1000;
        const int FillAllIterations = 100000;
        const int BaseIterations = 10000000;
#endif

        const int Size = 1024;

        // Helpers
        #region Helpers
        [StructLayout(LayoutKind.Sequential)]
        private sealed class TestClass<T>
        {
            private double _d;
            public T[] C0;
        }

        /*[MethodImpl(MethodImplOptions.NoInlining)]
        static void TestFillAllSpan(Span<byte> span)
        {
            for (int i = 0; i < span.Length; ++i) {
                span[i] = unchecked((byte)i);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestFillAllArray(byte[] data)
        {
            for (int i = 0; i < data.Length; ++i) {
                data[i] = unchecked((byte)i);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestFillAllReverseSpan(Span<byte> span)
        {
            for (int i = span.Length; --i >= 0;) {
                span[i] = unchecked((byte)i);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestFillAllReverseArray(byte[] data)
        {
            for (int i = data.Length; --i >= 0;) {
                data[i] = unchecked((byte)i);
            }
        }

        static int[] GetUnsortedData()
        {
            int[] unsortedData = new int[Size];
            Random r = new Random(42);
            for (int i = 0; i < unsortedData.Length; ++i)
            {
                unsortedData[i] = r.Next();
            }
            return unsortedData;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestBubbleSortSpan(Span<int> span)
        {
            bool swap;
            int temp;
            int n = span.Length - 1;
            do {
                swap = false;
                for (int i = 0; i < n; i++) {
                    if (span[i] > span[i + 1]) {
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestBubbleSortArray(int[] data)
        {
            bool swap;
            int temp;
            int n = data.Length - 1;
            do {
                swap = false;
                for (int i = 0; i < n; i++) {
                    if (data[i] > data[i + 1]) {
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

        static void TestQuickSortSpan(Span<int> data)
        {
            QuickSortSpan(data);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void QuickSortSpan(Span<int> data)
        {
            if (data.Length <= 1) {
                return;
            }

            int lo = 0;
            int hi = data.Length - 1;
            int i, j;
            int pivot, temp;
            for (i = lo, j = hi, pivot = data[hi]; i < j;) {
                while (i < j && data[i] <= pivot) {
                    ++i;
                }
                while (j > i && data[j] >= pivot) {
                    --j;
                }
                if (i < j) {
                    temp = data[i];
                    data[i] = data[j];
                    data[j] = temp;
                }
            }
            if (i != hi) {
                temp = data[i];
                data[i] = pivot;
                data[hi] = temp;
            }

            QuickSortSpan(data.Slice(0, i));
            QuickSortSpan(data.Slice(i + 1));
        }

        static void TestQuickSortArray(int[] data)
        {
            QuickSortArray(data, 0, data.Length - 1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void QuickSortArray(int[] data, int lo, int hi)
        {
            if (lo >= hi) {
                return;
            }

            int i, j;
            int pivot, temp;
            for (i = lo, j = hi, pivot = data[hi]; i < j;) {
                while (i < j && data[i] <= pivot) {
                    ++i;
                }
                while (j > i && data[j] >= pivot) {
                    --j;
                }
                if (i < j) {
                    temp = data[i];
                    data[i] = data[j];
                    data[j] = temp;
                }
            }
            if (i != hi) {
                temp = data[i];
                data[i] = pivot;
                data[hi] = temp;
            }

            QuickSortArray(data, lo, i - 1);
            QuickSortArray(data, i + 1, hi);
        }*/
        #endregion

        // XUNIT-PERF tests
        #region XUNIT-PERF tests
        /*[Benchmark]
        public static void FillAllSpan()
        {
            byte[] a = new byte[Size];
            Span<byte> s = new Span<byte>(a);
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < FillAllIterations; i++)
                    {
                        TestFillAllSpan(s);
                    }
                }
            }
        }

        [Benchmark]
        public static void FillAllArray()
        {
            byte[] a = new byte[Size];
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < FillAllIterations; i++)
                    {
                        TestFillAllArray(a);
                    }
                }
            }
        }

        [Benchmark]
        public static void FillAllReverseSpan()
        {
            byte[] a = new byte[Size];
            Span<byte> s = new Span<byte>(a);
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < FillAllIterations; i++)
                    {
                        TestFillAllReverseSpan(s);
                    }
                }
            }
        }

        [Benchmark]
        public static void FillAllReverseArray()
        {
            byte[] a = new byte[Size];
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < FillAllIterations; i++)
                    {
                        TestFillAllReverseArray(a);
                    }
                }
            }
        }

        [Benchmark]
        public static void QuickSortSpan()
        {
            int[] data = new int[Size];
            int[] unsortedData = GetUnsortedData();
            Span<int> span = new Span<int>(data);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < QuickSortIterations; i++)
                    {
                        Array.Copy(unsortedData, data, Size);
                        TestQuickSortSpan(span);
                    }
                }
            }
        }

        [Benchmark]
        public static void BubbleSortSpan()
        {
            int[] data = new int[Size];
            int[] unsortedData = GetUnsortedData();
            Span<int> span = new Span<int>(data);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < BubbleSortIterations; i++)
                    {
                        Array.Copy(unsortedData, data, Size);
                        TestBubbleSortSpan(span);
                    }
                }
            }
        }

        [Benchmark]
        public static void QuickSortArray()
        {
            int[] data = new int[Size];
            int[] unsortedData = GetUnsortedData();

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < QuickSortIterations; i++)
                    {
                        Array.Copy(unsortedData, data, Size);
                        TestQuickSortArray(data);
                    }
                }
            }
        }

        [Benchmark]
        public static void BubbleSortArray()
        {
            int[] data = new int[Size];
            int[] unsortedData = GetUnsortedData();

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < BubbleSortIterations; i++)
                    {
                        Array.Copy(unsortedData, data, Size);
                        TestBubbleSortArray(data);
                    }
                }
            }
        }*/
        #endregion

        // TestSpanAPIs (For comparison with Array and Slow Span)
        #region TestSpanAPIs

        #region TestSpanConstructor<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanConstructorByte(int length)
        {
            var array = new byte[length];
            Span<byte> span;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        span = new Span<byte>(array);
        }

        [Benchmark(InnerIterationCount = BaseIterations / 100)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanConstructorString(int length)
        {
            var array = new string[length];
            Span<string> span;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        span = new Span<string>(array);
        }
        #endregion
        
        #region TestSpanDangerousCreate<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanDangerousCreateByte(int length)
        {
            TestClass<byte> testClass = new TestClass<byte>();
            testClass.C0 = new byte[length];
            Span<byte> span;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        span = Span<byte>.DangerousCreate(testClass, ref testClass.C0[0], testClass.C0.Length);
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanDangerousCreateString(int length)
        {
            TestClass<string> testClass = new TestClass<string>();
            testClass.C0 = new string[length];
            Span<string> span;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        span = Span<string>.DangerousCreate(testClass, ref testClass.C0[0], testClass.C0.Length);
        }
        #endregion

        #region TestSpanDangerousGetPinnableReference<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanDangerousGetPinnableReferenceByte(int length)
        {
            var array = new byte[length];
            var span = new Span<byte>(array);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ref byte temp = ref span.DangerousGetPinnableReference();
                    }
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanDangerousGetPinnableReferenceString(int length)
        {
            var array = new string[length];
            var span = new Span<string>(array);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ref string temp = ref span.DangerousGetPinnableReference();
                    }
        }
        #endregion

        #region TestSpanIndex<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanIndexByte(int length)
        {
            var array = new byte[length];
            var span = new Span<byte>(array);
            byte temp;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                            temp = span[length/2];

        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanIndexString(int length)
        {
            var array = new string[length];
            var span = new Span<string>(array);
            string temp;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        temp = span[length / 2];
        }
        #endregion

        #region TestArrayIndex<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestArrayIndexByte(int length)
        {
            var array = new byte[length];
            byte temp;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        temp = array[length / 2];

        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestArrayIndexString(int length)
        {
            var array = new string[length];
            string temp;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        temp = array[length / 2];
        }
        #endregion

        #region TestSpanSlice<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanSliceByte(int length)
        {
            var array = new byte[length];
            var span = new Span<byte>(array);
            Span<byte> temp;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        temp = span.Slice(length / 2);
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanSliceString(int length)
        {
            var array = new string[length];
            var span = new Span<string>(array);
            Span<string> temp;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        temp = span.Slice(length / 2);
        }
        #endregion
        
        #region TestSpanToArray<T>
        [Benchmark(InnerIterationCount = BaseIterations / 100)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanToArrayByte(int length)
        {
            var array = new byte[length];
            var span = new Span<byte>(array);
            byte[] temp;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        temp = span.ToArray();
        }

        [Benchmark(InnerIterationCount = BaseIterations / 100)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanToArrayString(int length)
        {
            var array = new string[length];
            var span = new Span<string>(array);
            string[] temp;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        temp = span.ToArray();
        }
        #endregion
        
        #region TestSpanCopyTo<T>
        [Benchmark(InnerIterationCount = BaseIterations / 10)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanCopyToByte(int length)
        {
            var array = new byte[length];
            var span = new Span<byte>(array);
            var destArray = new byte[array.Length];
            var destination = new Span<byte>(destArray);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        span.CopyTo(destination);
        }

        [Benchmark(InnerIterationCount = BaseIterations / 100)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanCopyToString(int length)
        {
            var array = new string[length];
            var span = new Span<string>(array);
            var destArray = new string[array.Length];
            var destination = new Span<string>(destArray);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        span.CopyTo(destination);
        }
        #endregion

        #region TestArrayCopyTo<T>
        [Benchmark(InnerIterationCount = BaseIterations / 10)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestArrayCopyToByte(int length)
        {
            var array = new byte[length];
            var destination = new byte[array.Length];
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        array.CopyTo(destination, 0);
        }

        [Benchmark(InnerIterationCount = BaseIterations / 100)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestArrayCopyToString(int length)
        {
            var array = new string[length];
            var destination = new string[array.Length];
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        array.CopyTo(destination, 0);
        }
        #endregion

        #region TestSpanFill<T>
        [Benchmark(InnerIterationCount = BaseIterations * 10)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanFillByte(int length)
        {
            var array = new byte[length];
            var span = new Span<byte>(array);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        span.Fill(default(byte));
        }

        [Benchmark(InnerIterationCount = BaseIterations / 100)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanFillString(int length)
        {
            var array = new string[length];
            var span = new Span<string>(array);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        span.Fill(default(string));
        }
        #endregion

        #region TestSpanClear<T>
        [Benchmark(InnerIterationCount = BaseIterations / 10)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanClearByte(int length)
        {
            var array = new byte[length];
            var span = new Span<byte>(array);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        span.Clear();
        }

        [Benchmark(InnerIterationCount = BaseIterations / 10)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanClearString(int length)
        {
            var array = new string[length];
            var span = new Span<string>(array);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        span.Clear();
        }
        #endregion

        #region TestArrayClear<T>
        [Benchmark(InnerIterationCount = BaseIterations / 10)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestArrayClearByte(int length)
        {
            var array = new byte[length];
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Array.Clear(array, 0, length);
        }

        [Benchmark(InnerIterationCount = BaseIterations / 10)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestArrayClearString(int length)
        {
            var array = new string[length];
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Array.Clear(array, 0, length);
        }
        #endregion

        #region TestSpanAsBytes<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanAsBytesByte(int length)
        {
            var array = new byte[length];
            var span = new Span<byte>(array);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Span<byte> temp = span.AsBytes<byte>();
                    }
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanAsBytesInt(int length)
        {
            var array = new int[length];
            var span = new Span<int>(array);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Span<byte> temp = span.AsBytes<int>();
                    }
        }
        #endregion

        #region TestSpanNonPortableCast<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanNonPortableCastFromByteToInt(int length)
        {
            var array = new byte[length];
            var span = new Span<byte>(array);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Span<int> temp = span.NonPortableCast<byte, int>();
                    }
        }

        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanNonPortableCastFromIntToByte(int length)
        {
            var array = new int[length];
            var span = new Span<int>(array);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Span<byte> temp = span.NonPortableCast<int, byte>();
                    }
        }
        #endregion

        #region TestSpanSliceStringChar<T>
        [Benchmark(InnerIterationCount = BaseIterations)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void TestSpanSliceStringChar(int length)
        {
            StringBuilder sb = new StringBuilder();
            Random rand = new Random(42);
            char[] c = new char[1];
            for (int i = 0; i < length; i++)
            {
                c[0] = (char)rand.Next(32, 126);
                sb.Append(new string(c));
            }
            string s = sb.ToString();

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySpan<char> temp = s.Slice();
                    }
        }
        #endregion

        #endregion
        
        // EXE-based testing
        #region EXE-base testing
        /*static void FillAllSpanBase()
        {
            byte[] a = new byte[Size];
            Span<byte> s = new Span<byte>(a);
            for (int i = 0; i < FillAllIterations; i++)
            {
                TestFillAllSpan(s);
            }
        }

        static void FillAllArrayBase()
        {
            byte[] a = new byte[Size];
            for (int i = 0; i < FillAllIterations; i++)
            {
                TestFillAllArray(a);
            }
        }

        static void FillAllReverseSpanBase()
        {
            byte[] a = new byte[Size];
            Span<byte> s = new Span<byte>(a);
            for (int i = 0; i < FillAllIterations; i++)
            {
                TestFillAllReverseSpan(s);
            }
        }

        static void FillAllReverseArrayBase()
        {
            byte[] a = new byte[Size];
            for (int i = 0; i < FillAllIterations; i++)
            {
                TestFillAllReverseArray(a);
            }
        }

        static void QuickSortSpanBase()
        {
            int[] data = new int[Size];
            int[] unsortedData = GetUnsortedData();
            Span<int> span = new Span<int>(data);

            for (int i = 0; i < QuickSortIterations; i++)
            {
                Array.Copy(unsortedData, data, Size);
                TestQuickSortSpan(span);
            }
        }

        static void BubbleSortSpanBase()
        {
            int[] data = new int[Size];
            int[] unsortedData = GetUnsortedData();
            Span<int> span = new Span<int>(data);

            for (int i = 0; i < BubbleSortIterations; i++)
            {
                Array.Copy(unsortedData, data, Size);
                TestBubbleSortSpan(span);
            }
        }

        static void QuickSortArrayBase()
        {
            int[] data = new int[Size];
            int[] unsortedData = GetUnsortedData();

            for (int i = 0; i < QuickSortIterations; i++)
            {
                Array.Copy(unsortedData, data, Size);
                TestQuickSortArray(data);
            }
        }

        static void BubbleSortArrayBase()
        {
            int[] data = new int[Size];
            int[] unsortedData = GetUnsortedData();

            for (int i = 0; i < BubbleSortIterations; i++)
            {
                Array.Copy(unsortedData, data, Size);
                TestBubbleSortArray(data);
            }
        }*/
        #endregion

        static double Bench(Action f)
        {
            Stopwatch sw = Stopwatch.StartNew();
            f();
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }

        static IEnumerable<object[]> MakeArgs(params string[] args)
        {
            return args.Select(arg => new object[] { arg });
        }

        static IEnumerable<object[]> TestFuncs = MakeArgs(
            /*"FillAllSpanBase", "FillAllArrayBase",
            "FillAllReverseSpanBase", "FillAllReverseArrayBase",
            "BubbleSortSpanBase", "BubbleSortArrayBase",
            "QuickSortSpanBase", "QuickSortArrayBase"*/
        );

        static Action LookupFunc(object o)
        {
            TypeInfo t = typeof(SpanBench).GetTypeInfo();
            MethodInfo m = t.GetDeclaredMethod((string) o);
            return m.CreateDelegate(typeof(Action)) as Action;
        }

        public static int Main(string[] args)
        {
            bool result = true;

            foreach(object[] o in TestFuncs)
            {
                string funcName = (string) o[0];
                Action func = LookupFunc(funcName);
                double timeInMs = Bench(func);
                Console.WriteLine("{0}: {1}ms", funcName, timeInMs);
            }
            
            return (result ? 100 : -1);
        }
    }
}