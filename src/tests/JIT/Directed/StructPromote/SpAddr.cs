// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class SpAddr
{
    struct S
    {
        public int i0;
        public int i1;
    }

    struct Pair
    {
        public int A;
        public int B;
    }

    struct FloatPair
    {
        public float A;
        public float B;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Foo(S s0, S s1)
    {
        // Console.WriteLine("s0 = [{0}, {1}], s1 = [{2}, {3}]", s0.i0, s0.i1, s1.i0, s1.i1);
        return s0.i0 + s0.i1 + s1.i0 + s1.i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int M(int i0, int i1, int i2, int i3)
    {
        S s0;
        s0.i0 = i1;
        s0.i1 = i0;
        S s1;
        s1.i0 = i2;
        s1.i1 = i3;
        return Foo(s0, s1); // r0 <= r1; r1 <= r0; r2 <= r3; r3 <= r2
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Consume(Pair p) => ((long)p.B << 32) | (uint)p.A;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Consume(FloatPair p) =>
        ((long)(uint)BitConverter.SingleToInt32Bits(p.B) << 32) | (uint)BitConverter.SingleToInt32Bits(p.A);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Expose(ref Pair p)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long NonAddressExposed(int a, int b)
    {
        Pair p;
        p.A = a;
        p.B = b;
        return Consume(p);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long AddressExposed(int a, int b)
    {
        Pair p = default;
        Expose(ref p);
        p.A = a;
        p.B = b;
        return Consume(p);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int OverlappingNonZeroThenShort()
    {
        Pair p = default;
        p.A = unchecked((int)0xDEADBEEF);
        Unsafe.As<Pair, short>(ref p) = 0;
        return p.A;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long AdjacentFloatNonZeroThenZero()
    {
        FloatPair p = default;
        p.A = 1.0f;
        p.B = 0.0f;
        return Consume(p);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int res = M(1, 2, 3, 4);
        Console.WriteLine("M(1, 2, 3, 4) is {0}.", res);
        long nonAddressExposed = NonAddressExposed(0x11223344, 0x55667788);
        long addressExposed    = AddressExposed(0x10203040, 0x50607080);
        int  overlapping       = OverlappingNonZeroThenShort();
        long floatPair         = AdjacentFloatNonZeroThenZero();

        int expectedOverlapping = BitConverter.IsLittleEndian
            ? unchecked((int)0xDEAD0000)
            : unchecked((int)0x0000BEEF);

        if ((res == 10) &&
            (nonAddressExposed == 0x5566778811223344L) &&
            (addressExposed == 0x5060708010203040L) &&
            (overlapping == expectedOverlapping) &&
            (floatPair == 0x000000003F800000L))
            return 100;
        else
            return 99;
    }
}
