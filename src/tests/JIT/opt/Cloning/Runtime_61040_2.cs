// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
struct StructWithHoles
{
    [FieldOffset(0)]
    public int Index;
    [FieldOffset(5)]
    public byte B;
    [FieldOffset(8)]
    public int C;
}

class Runtime_61040_2
{
    static int z = 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void JitUse(int arg) { z += arg; }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int Problem(StructWithHoles a, StructWithHoles b, int[] d)
    {
        var a1 = a;
        var b1 = b;

        try 
        {
            for (a1.Index = 0; a1.Index < 10; a1.Index = a1.Index + 1)
            {
                a1 = b1;
                JitUse(d[a1.Index]);
            }
        }
        catch (IndexOutOfRangeException)
        {
            return z + 100;
        }

        return -1;
    }

    public static int Main() => Problem(new() { Index = 0 }, new() { Index = 100_000_000 }, new int[10]);
}
