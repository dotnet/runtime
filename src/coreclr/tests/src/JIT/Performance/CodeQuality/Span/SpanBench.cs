// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

class Tests
{

#if DEBUG
    const int Iterations = 1;
#else
    const int Iterations = 10000;
#endif

    const int Size = 1024;


    [MethodImpl(MethodImplOptions.NoInlining)]
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
    }

    // XUNIT-PERF tests

    [Benchmark]
    public static void FillAllSpan()
    {
        byte[] a = new byte[Size];
        Span<byte> s = new Span<byte>(a);
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Iterations; i++)
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
                for (int i = 0; i < Iterations; i++)
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
                for (int i = 0; i < Iterations; i++)
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
                for (int i = 0; i < Iterations; i++)
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
                for (int i = 0; i < Iterations; i++)
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
                for (int i = 0; i < Iterations; i++)
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
                for (int i = 0; i < Iterations; i++)
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
                for (int i = 0; i < Iterations; i++)
                {
                    Array.Copy(unsortedData, data, Size);
                    TestBubbleSortArray(data);
                }
            }
        }
    }

    // EXE-based testing

    static void FillAllSpanBase()
    {
        byte[] a = new byte[Size];
        Span<byte> s = new Span<byte>(a);
        for (int i = 0; i < Iterations; i++)
        {
            TestFillAllSpan(s);
        }
    }

    static void FillAllArrayBase()
    {
        byte[] a = new byte[Size];
        for (int i = 0; i < Iterations; i++)
        {
            TestFillAllArray(a);
        }
    }

    static void FillAllReverseSpanBase()
    {
        byte[] a = new byte[Size];
        Span<byte> s = new Span<byte>(a);
        for (int i = 0; i < Iterations; i++)
        {
            TestFillAllReverseSpan(s);
        }
    }

    static void FillAllReverseArrayBase()
    {
        byte[] a = new byte[Size];
        for (int i = 0; i < Iterations; i++)
        {
            TestFillAllReverseArray(a);
        }
    }

    static void QuickSortSpanBase()
    {
        int[] data = new int[Size];
        int[] unsortedData = GetUnsortedData();
        Span<int> span = new Span<int>(data);

        for (int i = 0; i < Iterations; i++)
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

        for (int i = 0; i < Iterations; i++)
        {
            Array.Copy(unsortedData, data, Size);
            TestBubbleSortSpan(span);
        }
    }

    static void QuickSortArrayBase()
    {
        int[] data = new int[Size];
        int[] unsortedData = GetUnsortedData();

        for (int i = 0; i < Iterations; i++)
        {
            Array.Copy(unsortedData, data, Size);
            TestQuickSortArray(data);
        }
    }

    static void BubbleSortArrayBase()
    {
        int[] data = new int[Size];
        int[] unsortedData = GetUnsortedData();

        for (int i = 0; i < Iterations; i++)
        {
            Array.Copy(unsortedData, data, Size);
            TestBubbleSortArray(data);
        }
    }

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
        "FillAllSpanBase", "FillAllArrayBase",
        "FillAllReverseSpanBase", "FillAllReverseArrayBase",
        "BubbleSortSpanBase", "BubbleSortArrayBase",
        "QuickSortSpanBase", "QuickSortArrayBase"
    );

    static Action LookupFunc(object o)
    {
        TypeInfo t = typeof(Tests).GetTypeInfo();
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

