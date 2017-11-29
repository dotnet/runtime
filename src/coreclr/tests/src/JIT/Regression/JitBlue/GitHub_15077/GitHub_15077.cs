// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

// Codegen bug when propagating an int cast through
// a long shift. Tests below have known and unknown
// long shifts where shift amount is 31 or 32.

class P
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
    public static UInt32 Gx(int q)
    {
        return (UInt32)((1UL << q) - 1);
    }

    public static int Main()
    {
        UInt32 r32 = G32();
        UInt32 r31 = G31();
        UInt32 r32a = Gx(32);
        UInt32 r31a = Gx(31);
        Console.WriteLine($"r32:{r32,0:X} r31:{r31,0:X} r32a:{r32a,0:X} r31a:{r31a,0:X}");
        return (r32 == 0xFFFFFFFF) && (r32a == 0xFFFFFFFF) && (r31 == 0x7FFFFFFF) && (r31a == 0x7FFFFFFF) ? 100 : 0;
    }
}
