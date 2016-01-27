// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Goal: Test arrays of doubles are allocated on large object heap and therefore 8 byte aligned
// Assumptions:
// 1) large object heap is always 8 byte aligned
// 2) double array greater than 1000 elements is on large object heap
// 3) non-double array greater than 1000 elements but less than 85K is NOT on large object heap
// 4) new arrays allocated in large object heap is of generation 2
// 5) new arrays NOT allocated in large object heap is of generation 0 
// 6) the threshold can be set by registry key DoubleArrayToLargeObjectHeap


using System;
internal class DblArray
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

    public static int Main()
    {
        if (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "x86")
        {
            s_LOH_GEN = 2;
        }
        try
        {
            f0();
            f1a();
            f2a();
            f3a();
            f4a();
            f5a();
            f6a();
            f7a();
            f8a();
            f9a();
            f10a();
            f11a();
            f12a();
            f1b();
            f2b();
            f3b();
            f4b();
            f5b();
            f6b();
            f7b();
            f8b();
            f9b();
            f10b();
            f11b();
            f12b();
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
