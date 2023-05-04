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
    public static unsafe void PartialOverlap1()
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
    public static unsafe void CopyFromLocalVar()
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
    public static unsafe void CopyFromLocalField()
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
    public static unsafe void CopyFromBlk()
    {
        S dst;
        dst = s_static;
        dst.A = dst.B + 3;
        dst.B = 0x20202020;
        Consume(dst);
        Assert.Equal(0x20202023U, dst.A);
        Assert.Equal(0x20202020U, dst.B);
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
}
