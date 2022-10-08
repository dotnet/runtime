// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Goal: Test arrays of doubles are allocated on large object heap and therefore 8 byte aligned
// Assumptions:
// 1) large object heap is always 8 byte aligned
// 2) double array greater than 1000 elements is on large object heap
// 3) non-double array greater than 1000 elements but less than 85K is NOT on large object heap
// 4) new arrays allocated in large object heap is of generation 2
// 5) new arrays NOT allocated in large object heap is of generation 0 
// 6) the threshold can be set by registry key DoubleArrayToLargeObjectHeap

// Variation on array length

using System.Runtime.InteropServices;
using System;
using Xunit;
public class DblArray1
{
    private static int s_LOH_GEN = 0;
    public static void f0()
    {
        double[] arr = new double[1];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f1()
    {
        double[] arr = new double[99];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f2()
    {
        double[] arr = new double[100];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f3()
    {
        double[] arr = new double[999];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f4()
    {
        double[] arr = new double[1000];
        if (GC.GetGeneration(arr) != s_LOH_GEN)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f5()
    {
        double[] arr = new double[1001];
        if (GC.GetGeneration(arr) != s_LOH_GEN)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void Run(Action f)
    {
        try
        {
            GC.TryStartNoGCRegion(500_000);
            f();
        }
        finally
        {
            GC.EndNoGCRegion();
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
        {
            s_LOH_GEN = 2;
        }

        try
        {
            Run(f0);
            Run(f1);
            Run(f2);
            Run(f3);
            Run(f4);
            Run(f5);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            Console.WriteLine("FAILED");
            Console.WriteLine();
            Console.WriteLine(@"// Goal: Test arrays of doubles are allocated on large object heap and therefore 8 byte aligned
// Assumptions:
// 1) large object heap is always 8 byte aligned
// 2) double array greater than 1000 elements is on large object heap
// 3) non-double array greater than 1000 elements but less than 85K is NOT on large object heap
// 4) new arrays allocated in large object heap is of generation 2
// 5) new arrays NOT allocated in large object heap is of generation 0 ");

            return -1;
        }
        Console.WriteLine("PASSED");
        return 100;
    }
}
