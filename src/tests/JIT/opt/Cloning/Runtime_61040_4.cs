// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class Runtime_61040_4
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void JitUse<T>(T arg) { }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int Problem()
    {
        int[] a = GetArray();
        int[] b = a;
        int[] c = GetArray();
        
        JitUse(a);
        JitUse(b);
        JitUse(c);
        
        int r = 0;

        try 
        {
            for (int i = 0; i < a.Length; i++)
            {
                a = GetArrayLong();
                r += b[i];
            }
        }
        catch (IndexOutOfRangeException)
        {
            return r;
        }

        return -1;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int[] GetArray() => new int[] { 1, 2, 3, 4, 90 };
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int[] GetArrayLong() => new int[10000];

    public static int Main() => Problem();
}

