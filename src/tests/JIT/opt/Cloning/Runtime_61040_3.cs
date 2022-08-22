// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

struct StructWithIndex
{
    public int Index;
    public int Value;
}

class Runtime_61040_3
{
    static int z = 100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void JitUse(int arg) { z++; }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int Problem(StructWithIndex a, int[] d)
    {
        var a1 = a;

        try
        {
            for (a1.Index = 0; a1.Index < 10; a1.Index = a1.Index + 1)
            {
                a1 = GetStructWithIndex();
                JitUse(d[a1.Index]);
            }
        }
        catch (IndexOutOfRangeException)
        {
            return z;
        }

        return -1;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static StructWithIndex GetStructWithIndex() => new() { Index = 100_000_000 };
    
    public static int Main() => Problem(new() { Index = 0, Value = 33 }, new int[10]);
}
