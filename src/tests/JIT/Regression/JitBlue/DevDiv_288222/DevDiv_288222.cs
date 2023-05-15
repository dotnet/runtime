// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using Xunit;

// This test is a repro case for DevDiv VSO bug 288222.
// The failure mode is that the size was not being set for a "this" pointer
// with SIMD type.

public class Program
{
    // Declare a delegate type for calling the Vector2.CopyTo method.
    public delegate void CopyToDelegate(float[] array, int start);

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void MyCopyTo(CopyToDelegate doCopy, float[] array, int start)
    {
        doCopy(array, start);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            float x = 1.0F;
            float y = 2.0F;
            Vector2 v = new Vector2(x, y);
            float[] array = new float[4];
            MyCopyTo(new CopyToDelegate(v.CopyTo), array, 2);
            
            if ((array[2] != x) || (array[3] != y))
            {
                Console.WriteLine("Failed with wrong values");
                return -1;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed with exception: " + e.Message);
            return -1;   
        }

        Console.WriteLine("Pass");
        return 100;
    }
}
