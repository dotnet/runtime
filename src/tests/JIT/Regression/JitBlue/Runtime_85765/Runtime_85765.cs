// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_85765
{
    public struct S0
    {
        public S0(bool f1): this()
        {
        }
    }

    public struct S1
    {
        public byte F0;
        public bool F1;
        public bool F2;
    }

    [Fact]
    public static void Test()
    {
        S1 vr2 = M4();
        vr2.F2 |= vr2.F1;
        Assert.False(Consume(vr2.F2));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static S1 M4()
    {
        S1 var1 = default(S1);
        var vr0 = new S0(false);
        return var1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Consume(bool value)
    {
        return value;
    }

    // ------

    [Fact]
    public unsafe static void Test2()
    {
        byte* bytes = stackalloc byte[1024];
        bytes[0x1A] = 1;
        bytes[0x1B] = 2;
        int sum = Foo(bytes);
        Assert.True(sum == 515);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public unsafe static int Foo(byte* b)
    {
        return Unsafe.ReadUnaligned<int>(ref b[0x1A]) + Unsafe.ReadUnaligned<int>(ref b[0x1B]);
    }
}
