// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;
using System.Runtime.InteropServices;

public class PhysicalPromotion
{
    private static S s_static = new S { A = 0x10101010, B = 0x20202020 };

    [Fact]
    public static unsafe void FromPhysicalToOld()
    {
        SWithInner src;
        src.S = s_static;
        src.S.A = src.S.B + 3;
        src.S.B = 0x20202020;

        S dst;
        dst = src.S;
        dst.A = dst.B + 3;
        dst.B = 0x10101010;
        Consume(dst);
        Assert.Equal(0x20202023U, dst.A);
        Assert.Equal(0x10101010U, dst.B);
    }

    [Fact]
    public static unsafe void FromOldToPhysical()
    {
        S src;
        src = s_static;
        src.A = src.B + 3;
        src.B = 0x20202020;

        SWithInner dst;
        dst.Field = 0;
        dst.S = src;
        dst.S.A = dst.S.B + 3;
        dst.S.B = 0x10101010;
        Consume(dst);
        Assert.Equal(0x20202023U, dst.S.A);
        Assert.Equal(0x10101010U, dst.S.B);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T val)
    {
    }

    private struct S
    {
        public uint A;
        public uint B;
    }

    private struct SWithInner
    {
        public int Field;
        public S S;
    }
}
