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


using System.Runtime.InteropServices;
using System;
using Xunit;
public class DblArray
{
    private static int s_LOH_GEN = 0;
    public static void f0()
    {
        double[] arr = new double[1000];
        if (GC.GetGeneration(arr) != s_LOH_GEN)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f1a()
    {
        float[] arr = new float[1000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f1b()
    {
        float[] arr = new float[3000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f2a()
    {
        decimal[] arr = new decimal[1000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f2b()
    {
        decimal[] arr = new decimal[3000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f3a()
    {
        long[] arr = new long[1000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f3b()
    {
        long[] arr = new long[3000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f4a()
    {
        ulong[] arr = new ulong[1000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f4b()
    {
        ulong[] arr = new ulong[3000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f5a()
    {
        int[] arr = new int[1000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f5b()
    {
        int[] arr = new int[3000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f6a()
    {
        uint[] arr = new uint[1000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f6b()
    {
        uint[] arr = new uint[3000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f7a()
    {
        short[] arr = new short[1000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f7b()
    {
        short[] arr = new short[5000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f8a()
    {
        ushort[] arr = new ushort[1000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f8b()
    {
        ushort[] arr = new ushort[5000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f9a()
    {
        byte[] arr = new byte[1000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f9b()
    {
        byte[] arr = new byte[10000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f10a()
    {
        sbyte[] arr = new sbyte[1000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f10b()
    {
        sbyte[] arr = new sbyte[10000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f11a()
    {
        char[] arr = new char[1000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f11b()
    {
        char[] arr = new char[10000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f12a()
    {
        bool[] arr = new bool[1000];
        if (GC.GetGeneration(arr) != 0)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            throw new Exception();
        }
    }

    public static void f12b()
    {
        bool[] arr = new bool[10000];
        if (GC.GetGeneration(arr) != 0)
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
            Run(f1a);
            Run(f2a);
            Run(f3a);
            Run(f4a);
            Run(f5a);
            Run(f6a);
            Run(f7a);
            Run(f8a);
            Run(f9a);
            Run(f10a);
            Run(f11a);
            Run(f12a);
            Run(f1b);
            Run(f2b);
            Run(f3b);
            Run(f4b);
            Run(f5b);
            Run(f6b);
            Run(f7b);
            Run(f8b);
            Run(f9b);
            Run(f10b);
            Run(f11b);
            Run(f12b);
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
