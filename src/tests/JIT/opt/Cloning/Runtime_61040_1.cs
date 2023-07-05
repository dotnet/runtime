// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

struct ArrayWrapper
{
    public int[] Array;
}

public class Runtime_61040_1
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void JitUse<T>(T arg) { }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static void Problem(ArrayWrapper a)
    {
        a = GetArrayLong();
        
        JitUse(a);
        JitUse(a);
        
        for (int i = 0; i < 10000; i++)
        {
            a = GetArray();
            JitUse(a.Array[i]);
        }
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static ArrayWrapper GetArray() => new() { Array = new int[0] };
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static ArrayWrapper GetArrayLong() => new() { Array = new int[10000] };

    [Fact]
    public static int TestEntryPoint()
    {
        int result = -1;
        try
        {
            Problem(default);
        }
        catch (IndexOutOfRangeException e)
        {
            Console.WriteLine(e.Message);
            result = 100;
        }
        return result;
    }
}
    
