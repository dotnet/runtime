// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;
using System.Runtime.InteropServices;

public class PhysicalPromotion
{
    private static S s_static = new S { A = 0xdeadbeef, B = 0xcafebabe };
    private static S2 s_static2 = new S2 { A = 0x12, B = 0x34, C = 0x56, D = 0x78, E = 0x9A, F = 0xBC, G = 0xDE, H = 0xF0 };

    [Fact]
    public static void FromPhysicalToOld()
    {
        SWithInner src;
        src.S = s_static;
        src.S.A = src.S.B + 3;
        src.S.B = 0x21222324;

        S dst;
        dst = src.S;
        dst.A = dst.B + 3;
        dst.B = 0x11121314;
        Consume(dst);
        Assert.Equal(0x21222327U, dst.A);
        Assert.Equal(0x11121314U, dst.B);
    }

    [Fact]
    public static void FromOldToPhysical()
    {
        S src;
        src = s_static;
        src.A = src.B + 3;
        src.B = 0x21222324;

        SWithInner dst;
        dst.Field = 0;
        dst.S = src;
        dst.S.A = dst.S.B + 3;
        dst.S.B = 0x11121314;
        Consume(dst);
        Assert.Equal(0x21222327U, dst.S.A);
        Assert.Equal(0x11121314U, dst.S.B);
    }

    [Fact]
    public static unsafe void FromOldToPhysicalMismatched()
    {
        S src = s_static;
        src.A = src.B + 3;
        src.B = 0x21222324;

        S2 dst = s_static2;
        dst.A = (byte)(dst.B + 2);
        dst.B = (byte)(dst.C + 2);
        dst.C = (byte)(dst.D + 2);
        dst.D = (byte)(dst.E + 2);
        dst.E = (byte)(dst.F + 2);
        dst.F = (byte)(dst.G + 2);
        dst.G = (byte)(dst.H + 2);
        dst.H = (byte)(dst.A + 2);
        Consume(dst);

        Assert.Equal(0xcafebac1U, src.A);
        Assert.Equal(0x21222324U, src.B);

        Assert.Equal(0x36, dst.A);
        Assert.Equal(0x58, dst.B);
        Assert.Equal(0x7A, dst.C);
        Assert.Equal(0x9C, dst.D);
        Assert.Equal(0xBE, dst.E);
        Assert.Equal(0xE0, dst.F);
        Assert.Equal(0xF2, dst.G);
        Assert.Equal(0x38, dst.H);

        dst = *(S2*)&src;
        Consume(dst);

        Assert.Equal(0xc1, dst.A);
        Assert.Equal(0xba, dst.B);
        Assert.Equal(0xfe, dst.C);
        Assert.Equal(0xca, dst.D);
        Assert.Equal(0x24, dst.E);
        Assert.Equal(0x23, dst.F);
        Assert.Equal(0x22, dst.G);
        Assert.Equal(0x21, dst.H);
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

    private struct S2
    {
        public byte A, B, C, D, E, F, G, H;
    }

    private struct SWithInner
    {
        public int Field;
        public S S;
    }
}
