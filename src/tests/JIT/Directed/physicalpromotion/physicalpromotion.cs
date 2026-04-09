// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;
using System.Runtime.InteropServices;

public class PhysicalPromotion
{
    [Fact]
    public static void PartialOverlap1()
    {
        S s = default;
        s.A = 0x10101010;
        s.B = 0x20202020;

        Unsafe.InitBlockUnaligned(ref Unsafe.As<uint, byte>(ref s.C), 0xcc, 4);
        Assert.Equal(0xcccc1010U, s.A);
        Assert.Equal(0x2020ccccU, s.B);
    }

    private static S s_static = new S { A = 0x10101010, B = 0x20202020 };
    [Fact]
    public static void CopyFromLocalVar()
    {
        S src = s_static;
        S dst;
        dst = src;
        dst.A = dst.B + 3;
        dst.B = 0x20202020;
        Consume(dst);
        Assert.Equal(0x20202023U, dst.A);
        Assert.Equal(0x20202020U, dst.B);
    }

    [Fact]
    public static void CopyFromLocalField()
    {
        SWithInner src;
        src.S = s_static;
        S dst;
        dst = src.S;
        dst.A = dst.B + 3;
        dst.B = 0x20202020;
        Consume(dst);
        Assert.Equal(0x20202023U, dst.A);
        Assert.Equal(0x20202020U, dst.B);
    }

    [Fact]
    public static void CopyFromBlk()
    {
        S dst;
        dst = s_static;
        dst.A = dst.B + 3;
        dst.B = 0x20202020;
        Consume(dst);
        Assert.Equal(0x20202023U, dst.A);
        Assert.Equal(0x20202020U, dst.B);
    }

    [Fact]
    public static void CopyToBlk()
    {
        S s = default;
        CopyToBlkInner(ref s);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CopyToBlkInner(ref S mutate)
    {
        S src = s_static;
        src.A = src.B + 3;
        src.B = 0x20202020;
        mutate = src;
        Assert.Equal(0x20202023U, mutate.A);
        Assert.Equal(0x20202020U, mutate.B);
    }

    private static VeryOverlapping _overlappy1 = new VeryOverlapping { F0 = 0x12345678, F4 = 0xdeadbeef };
    private static VeryOverlapping _overlappy2 = new VeryOverlapping { F1 = 0xde, F2 = 0x1357, F5 = 0x17, F7 = 0x42 };

    [Fact]
    public static void Overlappy()
    {
        VeryOverlapping lcl1 = _overlappy1;
        VeryOverlapping lcl2 = _overlappy2;
        VeryOverlapping lcl3 = _overlappy1;

        lcl1.F0 = lcl3.F0 + 3;
        lcl1.F4 = lcl3.F0 + lcl3.F4;

        lcl3 = lcl1;

        lcl2.F1 = (byte)(lcl2.F2 + lcl2.F5 + lcl2.F7);
        lcl1 = lcl2;

        Consume(lcl1);
        Consume(lcl2);
        Consume(lcl3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T val)
    {
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct S
    {
        [FieldOffset(0)]
        public uint A;
        [FieldOffset(4)]
        public uint B;
        [FieldOffset(2)]
        public uint C;
    }

    private struct SWithInner
    {
        public int Field;
        public S S;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct VeryOverlapping
    {
        [FieldOffset(0)]
        public uint F0;
        [FieldOffset(1)]
        public byte F1;
        [FieldOffset(2)]
        public ushort F2;
        [FieldOffset(3)]
        public byte F3;
        [FieldOffset(4)]
        public uint F4;
        [FieldOffset(5)]
        public byte F5;
        [FieldOffset(6)]
        public ushort F6;
        [FieldOffset(7)]
        public byte F7;
    }
}
