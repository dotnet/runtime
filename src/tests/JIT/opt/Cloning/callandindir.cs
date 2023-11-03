// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Loops in F, G, H should all clone

public class CallAndIndir
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void S() { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void F(int[] a, int low, int high, ref int z)
    {
        for (int i = low; i < high; i++)
        {
             z += a[i];
             S(); 
        }  
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void G(int[] a, int low, int high, ref int z)
    {
        for (int i = low; i < high; i++)
        {
             z += a[i];
        }  
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void H(int[] a, int low, int high, ref int z)
    {
        int r = 0;
        for (int i = low; i < high; i++)
        {
             r += a[i];
             S();
        }  
        z += r;
    }

    [Fact]
    public static int TestEntryPoint()
    {
         int[] a = new int[] { 1, 2, 3, 4 };
         int z = 0;
         F(a, 2, 4, ref z);
         G(a, 2, 4, ref z);
         H(a, 2, 4, ref z);
         return z + 79;
    }
}
