// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System;
using System.Runtime.Intrinsics.X86;

public unsafe class Runtime_62692
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint Problem1(byte* pSrc, uint data) => Sse42.Crc32(*pSrc, data);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint Problem2(uint crc, ulong data) => Sse42.Crc32(crc, (uint)(data >> 16));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint Problem3(uint crc, uint data) => Sse42.Crc32(crc, (uint)(byte)data);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint Problem4(uint crc, ulong data) => Sse42.Crc32(crc, (uint)(data >> 16));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint Problem5(uint crc, double data) => Sse42.Crc32(crc, (uint)data);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint Problem6(uint crc, float data) => Sse42.Crc32(crc, (uint)data);

    static int Main()
    {
        if (Sse42.IsSupported)
        {
            long a = long.MaxValue;
            AssertEqual(Problem1((byte*)&a, 111), 3150215170);
            a = 333;
            AssertEqual(Problem1((byte*)&a, 44), 2714165716);
            AssertEqual(Problem2(uint.MaxValue, 42), 3080238136);
            AssertEqual(Problem2(1111, 0xFFFF_FFFF_0000_0001), 3414328792);
            AssertEqual(Problem3(1, 0xFFFF_0001), 0);
            AssertEqual(Problem4(1111, 0xFFFF_FFFF_0000_0001), 3414328792);
            AssertEqual(Problem5(1111, double.MaxValue), 3307008522);
            AssertEqual(Problem6(1111, float.MaxValue), 3307008522);
            AssertEqual(Problem5(1111, double.MinValue), 3307008522);
            AssertEqual(Problem6(1111, float.MinValue), 3307008522);
            AssertEqual(Problem5(1111, -0.0), 3307008522);
            AssertEqual(Problem6(1111, -0.0f), 3307008522);
        }

        Console.WriteLine(retCode);
        return retCode;
    }

    static int retCode = 100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void AssertEqual(uint a, uint b, [CallerLineNumber] int line = 0)
    {
        if (a != b)
        {
            Console.WriteLine($"{a} != {b}, Line:{line}");
            retCode++;
        }
    }
}
