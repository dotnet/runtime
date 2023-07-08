// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Codegen bug when propagating an int cast through
// a long shift. Tests below have known and unknown
// long shifts where shift amount is 31 or 32.

public class P
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static UInt32 G32()
    {
        int q = 32;
        return (UInt32)((1UL << q) - 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static UInt32 G31()
    {
        int q = 31;
        return (UInt32)((1UL << q) - 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static UInt32 G64()
    {
        int q = 64;
        return (UInt32)((1UL << q) - 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static UInt32 G63()
    {
        int q = 63;
        return (UInt32)((1UL << q) - 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static UInt32 GM1()
    {
        int q = -1;
        return (UInt32)((1UL << q) - 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static UInt32 Gx(int q)
    {
        return (UInt32)((1UL << q) - 1);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        UInt32 r64 = G64();
        UInt32 r63 = G63();
        UInt32 r32 = G32();
        UInt32 r31 = G31();
        UInt32 rm1 = GM1();

        UInt32 r64a = Gx(64);
        UInt32 r63a = Gx(63);
        UInt32 r32a = Gx(32);
        UInt32 r31a = Gx(31);
        UInt32 rm1a = Gx(-1);

        Console.WriteLine($"r64:{r64,0:X8} r64a:{r64a,0:X8}");
        Console.WriteLine($"r63:{r63,0:X8} r63a:{r63a,0:X8}");
        Console.WriteLine($"r32:{r32,0:X8} r32a:{r32a,0:X8}");
        Console.WriteLine($"r31:{r31,0:X8} r31a:{r31a,0:X8}");
        Console.WriteLine($"rm1:{rm1,0:X8} rm1a:{rm1a,0:X8}");

        bool b64 = (r64 == 0x00000000) && (r64a == r64);
        bool b63 = (r63 == 0xFFFFFFFF) && (r63a == r63);
        bool b32 = (r32 == 0xFFFFFFFF) && (r32a == r32);
        bool b31 = (r31 == 0x7FFFFFFF) && (r31a == r31);
        bool bm1 = (rm1 == 0xFFFFFFFF) && (rm1a == rm1);

        return b64 && b63 && b32 && b31 && bm1 ? 100 : 0;
    }
}
