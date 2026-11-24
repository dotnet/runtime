// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

// Each instantiation over a new value type is a separate compilation of Holder<T>.Lengths. In optimized code the JIT
// takes a GC handle to each array to read its length, which is freed when the compilation ends. A helper allocates
// the arrays, so that the last one cannot go on the frozen heap, where it could not move.
public struct Wrap0<T>
{
    public T Value;
}

public struct Wrap1<T>
{
    public T Value;
}

public static class Holder<T>
{
    private static readonly int[] s_array0 = Runtime_117138.NewArray(1);
    private static readonly int[] s_array1 = Runtime_117138.NewArray(2);
    private static readonly int[] s_array2 = Runtime_117138.NewArray(3);
    private static readonly int[] s_array3 = Runtime_117138.NewArray(4);
    private static readonly int[] s_array4 = Runtime_117138.NewArray(5);
    private static readonly int[] s_array5 = Runtime_117138.NewArray(6);
    private static readonly int[] s_array6 = Runtime_117138.NewArray(7);
    private static readonly int[] s_array7 = Runtime_117138.NewArray(8);
    private static readonly int[] s_array8 = Runtime_117138.NewArray(9);
    private static readonly int[] s_array9 = Runtime_117138.NewArray(10);
    private static readonly int[] s_array10 = Runtime_117138.NewArray(11);
    private static readonly int[] s_array11 = Runtime_117138.NewArray(12);
    private static readonly int[] s_array12 = Runtime_117138.NewArray(13);
    private static readonly int[] s_array13 = Runtime_117138.NewArray(14);
    private static readonly int[] s_array14 = Runtime_117138.NewArray(15);
    private static readonly int[] s_array15 = Runtime_117138.NewArray(16);
    private static readonly int[] s_array16 = Runtime_117138.NewArray(17);
    private static readonly int[] s_array17 = Runtime_117138.NewArray(18);
    private static readonly int[] s_array18 = Runtime_117138.NewArray(19);
    private static readonly int[] s_array19 = Runtime_117138.NewArray(20);
    private static readonly int[] s_array20 = Runtime_117138.NewArray(21);
    private static readonly int[] s_array21 = Runtime_117138.NewArray(22);
    private static readonly int[] s_array22 = Runtime_117138.NewArray(23);
    private static readonly int[] s_array23 = Runtime_117138.NewArray(24);
    private static readonly int[] s_array24 = Runtime_117138.NewArray(25);
    private static readonly int[] s_array25 = Runtime_117138.NewArray(26);
    private static readonly int[] s_array26 = Runtime_117138.NewArray(27);
    private static readonly int[] s_array27 = Runtime_117138.NewArray(28);
    private static readonly int[] s_array28 = Runtime_117138.NewArray(29);
    private static readonly int[] s_array29 = Runtime_117138.NewArray(30);
    private static readonly int[] s_array30 = Runtime_117138.NewArray(31);
    private static readonly int[] s_array31 = Runtime_117138.NewArray(32);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Lengths() =>
        s_array0.Length + s_array1.Length + s_array2.Length + s_array3.Length + s_array4.Length + s_array5.Length +
        s_array6.Length + s_array7.Length + s_array8.Length + s_array9.Length + s_array10.Length + s_array11.Length +
        s_array12.Length + s_array13.Length + s_array14.Length + s_array15.Length + s_array16.Length + s_array17.Length +
        s_array18.Length + s_array19.Length + s_array20.Length + s_array21.Length + s_array22.Length + s_array23.Length +
        s_array24.Length + s_array25.Length + s_array26.Length + s_array27.Length + s_array28.Length + s_array29.Length +
        s_array30.Length + s_array31.Length;
}

public class Runtime_117138
{
    private static volatile bool s_done;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int[] NewArray(int length) => new int[length];

    // The bits of i below its top bit choose the wrappers, so each i gives a different type, nested at most 30 deep
    private static Type NewValueType(int i)
    {
        Type type = typeof(int);
        for (int bits = i; bits > 1; bits >>= 1)
        {
            type = ((bits & 1) == 0 ? typeof(Wrap0<>) : typeof(Wrap1<>)).MakeGenericType(type);
        }
        return type;
    }

    // Debug and Checked runtimes assert if a freed JIT-time handle kept a relocated reference
    [Fact]
    public static void TestEntryPoint()
    {
        var collector = new Thread(() =>
        {
            while (!s_done)
            {
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                Thread.Sleep(1);
            }
        });
        var allocator = new Thread(() =>
        {
            object[] sink = new object[64];
            var random = new Random(117138);
            for (int i = 0; !s_done; i++)
            {
                sink[i % sink.Length] = new byte[random.Next(32, 100_000)];
            }
        });
        collector.Start();
        allocator.Start();

        try
        {
            var stopwatch = Stopwatch.StartNew();
            for (int i = 2; stopwatch.ElapsedMilliseconds < 2000; i++)
            {
                Type holder = typeof(Holder<>).MakeGenericType(NewValueType(i));
                RuntimeHelpers.RunClassConstructor(holder.TypeHandle);
                Assert.Equal(528, (int)holder.GetMethod(nameof(Holder<int>.Lengths)).Invoke(null, null));
            }
        }
        finally
        {
            s_done = true;
            collector.Join();
            allocator.Join();
        }
    }
}
