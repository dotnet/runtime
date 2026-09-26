// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

namespace JitTest_Directed_physicalpromotion_physicalpromotion;

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

    [Theory]
    [InlineData(0, 0U)]
    [InlineData(1, 60U)]
    [InlineData(2, 120U)]
    [InlineData(3, 54U)]
    [InlineData(4, 49U)]
    [InlineData(5, 70U)]
    [InlineData(6, 0x56781245U)]
    [InlineData(7, 46U)]
    public static void ReadbacksAcrossBranches(int path, uint expected)
    {
        S value = new S { A = 17, B = 29 };
        Assert.Equal(expected, path is 6 ? ReadbackAfterPartialWrite(value) : ReadbacksAcrossBranchesCore(value, path));
        Assert.Equal(path is 0 ? 0U : path is 1 ? 1U : 46U, ReadbackOnlyOnSelectedPath(value, path));
        Assert.Equal(path switch { 0 => 29U, 1 => 63U, 2 => 80U, 3 => 70U, 4 => 120U, _ => 46U },
            ReadbacksAtLiveJoin(value, path));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint ReadbacksAtLiveJoin(S value, int path)
    {
        if (path == 0)
        {
            return value.B;
        }

        uint result = 0;
        if (path == 1)
        {
            result = value.A;
        }
        else if (path == 2)
        {
            result = value.A * 2;
        }
        else if (path == 3)
        {
            value.A = 41;
        }
        else if (path == 4)
        {
            value = GetReadbackValue();
        }

        return result + value.A + value.B;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint ReadbackAfterPartialWrite(S value)
    {
        value.C = 0x12345678;
        return value.A + value.B;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint ReadbackOnlyOnSelectedPath(S value, int path)
    {
        if (path == 0)
        {
            return 0;
        }

        if (path == 1)
        {
            return 1;
        }

        return value.A + value.B;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint ReadbacksAcrossBranchesCore(S value, int path)
    {
        if (path == 0)
        {
            return 0;
        }

        Consume(value);
        switch (path)
        {
            case 1:
                value.A = 31;
                break;
            case 2:
                value = GetReadbackValue();
                break;
            case 3:
                value.B = 37;
                break;
            case 4:
                for (int i = 0; i < 3; i++)
                {
                    value.A += (uint)i;
                    Consume(value);
                }
                break;
            case 5:
                try
                {
                    value.A = 41;
                    ThrowForReadback();
                }
                catch (InvalidOperationException)
                {
                    Consume(value);
                }
                break;
        }

        Consume(value);
        return value.A + value.B;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S GetReadbackValue() => new S { A = 73, B = 47 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowForReadback() => throw new InvalidOperationException();

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
