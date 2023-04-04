// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Program
{
    [StructLayout(LayoutKind.Sequential)]
    struct S
    {
        public uint i0;
        public uint i1;
        public uint i2;
        public uint i3;

        public int i4;
        public int i5;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct S16
    {
        public uint i0;
        public uint i1;
        public uint i2;
        public uint i3;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        S s = new S();
        s.i0 = 0x12345678;
        s.i1 = 0x87654321;
        return Test(s);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Call(int r0, int r1, int r2, int r3, int r4, int r5, int r6, S16 s)
    {
        return (s.i0 == 0x12345678 && s.i1 == 0x87654321) ? 100 : 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Escape<T>(ref T t)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test(S p)
    {
        S s = p;
        Escape(ref s);
        return Call(0, 1, 2, 3, 4, 5, 6, Unsafe.As<S, S16>(ref s));
    }
}
