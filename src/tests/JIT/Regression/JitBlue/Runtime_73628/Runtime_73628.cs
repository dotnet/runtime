// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_73628
{
    [Fact]
    public static int TestEntryPoint()
    {
        return Problem() ? 101 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem()
    {
        byte* p = stackalloc byte[4];
        Unsafe.InitBlock(p, 0xBB, 4);

        return Problem(default, *(StructWithByte*)&p);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem(StructWithInt a, StructWithByte b)
    {
        *(int*)&a = 0;
        *(byte*)&b = 0;

        bool result = IsNotZero(a.Int);
        JitUse(b);

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsNotZero(int a) => a != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void JitUse<T>(T arg) { }

    struct StructWithInt
    {
        public int Int;
    }

    struct StructWithByte
    {
        public byte Byte;
    }
}
