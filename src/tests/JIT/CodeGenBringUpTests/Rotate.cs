// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test_Rotate
{
    static ulong s_field;

    ulong field;

    volatile uint volatile_field;

    ushort usfield;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint rol32(uint value, int amount)
    {
        return (value << amount) | (value >> (32 - amount));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint rol32_1(uint value)
    {
        return (value << 1) | (value >> (32 - 1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint rol32_3(uint value)
    {
        return (value << 3) | (value >> (32 - 3));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint rol32comm(uint value, int amount)
    {
        return  (value >> (32 - amount)) | (value << amount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool flag()
    {
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint rol32const()
    {
        uint value = flag() ? (uint)0x12345678 : (uint)0x12345678;
        int amount = 16;
        return  (value >> (32 - amount)) | (value << amount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint rol32xor(uint value, int amount)
    {
        return (value << amount) ^ (value >> (32 - amount));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint ror32(uint value, int amount)
    {
        return (value << ((32 - amount))) | (value >> amount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint ror32comm(uint value, int amount)
    {
        return (value >> amount) | (value << ((32 - amount)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint ror32const()
    {
        uint value = flag() ? (uint)0x12345678 : (uint)0x12345678;
        int amount = flag() ? 12 : 12;
        return (value >> amount) | (value << ((32 - amount)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    uint ror32vfield(int amount)
    {
        return (volatile_field << ((32 - amount))) | (volatile_field >> amount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong rol64(ulong value, int amount)
    {
        return (value << amount) | (value >> (64 - amount));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong rol64comm(ulong value, int amount)
    {
        return  (value >> (64 - amount)) | (value << amount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong rol64const()
    {
        ulong value = flag() ? (ulong)0x123456789abcdef : (ulong)0xabcdef123456789;
        int amount = 16;
        return (value >> (64 - amount)) | (value << amount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong rol64_16(ulong value)
    {
        return (value >> (64 - 16)) | (value << 16);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong rol64_32(ulong value)
    {
        return (value >> (64 - 32)) | (value << 32);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong rol64_32_inplace(ulong value, ulong added)
    {
        ulong x = value + added;
        x = (x >> (64 - 32)) | (x << 32);
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong rol64_33(ulong value)
    {
        return (value >> (64 - 33)) | (value << 33);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    ulong rol64field(int amount)
    {
        return (field << amount) | (field >> (64 - amount));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong ror64(ulong value, int amount)
    {
        return (value << (64 - amount)) | (value >> amount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong ror64comm(ulong value, int amount)
    {
        return (value >> amount) | (value << (64 - amount));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong ror64const()
    {
        ulong value = flag() ? (ulong)0x123456789abcdef : (ulong)0xabcdef123456789;
        int amount = flag() ? 5 : 5;
        return (value << (64 - amount)) | (value >> amount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong ror64_5(ulong value)
    {
        return (value << (64 - 5)) | (value >> 5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong ror64_32(ulong value)
    {
        return (value << (64 - 32)) | (value >> 32);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong ror64_33(ulong value)
    {
        return (value << (64 - 33)) | (value >> 33);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong ror64_32_inplace(ulong value, ulong added)
    {
        ulong x = value + added;
        x = (x << (64 - 32)) | (x >> 32);
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong ror64sfield(int amount)
    {
        return (s_field << (64 - amount)) | (s_field >> amount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint rol32_call(uint value, int amount)
    {
        return (foo(value) << amount) | (foo(value) >> (32 - amount));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint foo(uint value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint rol32_and(uint value, int amount)
    { 
        return (value << amount) | (value >> ((32 - amount) & 31));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint two_left_shifts(uint value, int amount)
    {
        return (value << amount) | (value << (32 - amount));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint not_rotation(uint value)
    {
        return (value >> 10) | (value << 5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    uint rol32ushort(int amount)
    {
        return ((uint)usfield << amount) | ((uint)usfield >> (32 - amount));
    }

    Test_Rotate(ulong i, uint j, ushort k)
    {
        field = i;
        volatile_field = j;
        usfield = k;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        const int Pass = 100;
        const int Fail = -1;

        s_field = 0x123456789abcdef;

        if (rol32(0x12345678, 16) != 0x56781234)
        {
            return Fail;
        }

        if (rol32_1(0x12345678) != 0x2468ACF0)
        {
            return Fail;
        }

        if (rol32_3(0x12345678) != 0x91A2B3C0)
        {
            return Fail;
        }

        if (rol32comm(0x12345678, 16) != 0x56781234)
        {
            return Fail;
        }

        if (rol32const() != 0x56781234)
        {
            return Fail;
        }

        if (ror32(0x12345678, 12) != 0x67812345)
        {
            return Fail;
        }

        if (ror32comm(0x12345678, 12) != 0x67812345)
        {
            return Fail;
        }
        
        if (ror32const() != 0x67812345)
        {
            return Fail;
        }

        if (rol64(0x123456789abcdef, 32) != 0x89abcdef01234567)
        {
            return Fail;
        }

        if (rol64comm(0x123456789abcdef, 32) != 0x89abcdef01234567)
        {
            return Fail;
        }

        if (rol64const() != 0x456789abcdef0123)
        {
            return Fail;
        }

        if (rol64_16(0x123456789abcdef) != 0x456789abcdef0123)
        {
            return Fail;
        }

        if (rol64_32(0x123456789abcdef) != rol64(0x123456789abcdef, 32))
        {
            return Fail;
        }

        if (rol64_33(0x123456789abcdef) != rol64(0x123456789abcdef, 33))
        {
            return Fail;
        }

        if (rol64_32_inplace(0x123456789abcdef, 0) != rol64(0x123456789abcdef, 32))
        {
            return Fail;
        }

        if (ror64(0x123456789abcdef, 0) != 0x123456789abcdef)
        {
            return Fail;
        }

        if (ror64comm(0x123456789abcdef, 0) != 0x123456789abcdef)
        {
            return Fail;
        }

        if (ror64const() != 0x78091a2b3c4d5e6f)
        {
            return Fail;
        }

        if (ror64_5(0x123456789abcdef) != 0x78091a2b3c4d5e6f)
        {
            return Fail;
        }

        if (ror64_32(0x123456789abcdef) != ror64(0x123456789abcdef, 32))
        {
            return Fail;
        }

        if (ror64_33(0x123456789abcdef) != ror64(0x123456789abcdef, 33))
        {
            return Fail;
        }

        if (ror64_32_inplace(0x123456789abcdef, 0) != ror64(0x123456789abcdef, 32))
        {
            return Fail;
        }

        if (rol32_call(0x12345678, 16) != 0x56781234)
        {
            return Fail;
        }

        if (rol32_and(0x12345678, 16) != 0x56781234)
        {
            return Fail;
        }

        if (two_left_shifts(0x12345678, 7) != 0xfa2b3c00)
        {
            return Fail;
        }

        if (not_rotation(0x87654321) != 0xeca9fd70)
        {
            return Fail;
        }

        if (rol32xor(0x12345678, 16) != 0x56781234)
        {
            return Fail;
        }

        if (ror64sfield(7) != 0xde02468acf13579b)
        {
            return Fail;
        }

        Test_Rotate test = new Test_Rotate(0x123456789abcdef, 0x12345678, 0x1234);

        if (test.rol64field(11) != 0x1a2b3c4d5e6f7809)
        {
            return Fail;
        }

        if (test.ror32vfield(3) != 0x2468acf)
        {
            return Fail;
        }

        if (test.rol32ushort(25) != 0x68000024)
        {
            return Fail;
        }

        return Pass;
    }
}
