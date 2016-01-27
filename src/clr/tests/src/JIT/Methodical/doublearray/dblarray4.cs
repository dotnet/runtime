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

// Test DoubleArrayToLargeObjectHeap - need to set the key to <= 100

using System;

internal class DblArray4
{
    private static int s_LOH_GEN = 0;
    public static int Main()
    {
        if (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "x86")
        {
            s_LOH_GEN = 2;
        }

        Console.WriteLine("DoubleArrayToLargeObjectHeap is {0}", Environment.GetEnvironmentVariable("complus_DoubleArrayToLargeObjectHeap"));

        double[] arr = new double[101];
        if (GC.GetGeneration(arr) != s_LOH_GEN)
        {
            Console.WriteLine("Generation {0}", GC.GetGeneration(arr));
            Console.WriteLine("FAILED");
            return 1;
        }
        Console.WriteLine("PASSED");
        return 100;
    }
}
