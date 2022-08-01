// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class Int128Native
{
    [DllImport(nameof(Int128Native))]
    public static extern UInt128 GetUInt128(ulong upper, ulong lower);

    [DllImport(nameof(Int128Native))]
    public static extern void GetUInt128Out(ulong upper, ulong lower, UInt128* value);

    [DllImport(nameof(Int128Native))]
    public static extern void GetUInt128Out(ulong upper, ulong lower, out UInt128 value);

    [DllImport(nameof(Int128Native))]
    public static extern UInt128* GetUInt128Ptr(ulong upper, ulong lower);

    [DllImport(nameof(Int128Native), EntryPoint = "GetUInt128Ptr")]
    public static extern ref readonly UInt128 GetUInt128Ref(ulong upper, ulong lower);

    [DllImport(nameof(Int128Native))]
    public static extern UInt128 AddUInt128(UInt128 lhs, UInt128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern UInt128 AddUInt128s(UInt128* pValues, int count);

    [DllImport(nameof(Int128Native))]
    public static extern UInt128 AddUInt128s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] UInt128[] pValues, int count);

    [DllImport(nameof(Int128Native))]
    public static extern UInt128 AddUInt128s(in UInt128 pValues, int count);
}

unsafe partial class Int128Native
{
    private static void TestUInt128()
    {
        UInt128 value1 = Int128Native.GetUInt128(1, 2);
        Assert.Equal(new UInt128(1, 2), value1);

        UInt128 value2;
        Int128Native.GetUInt128Out(3, 4, &value2);
        Assert.Equal(new UInt128(3, 4), value2);

        Int128Native.GetUInt128Out(5, 6, out UInt128 value3);
        Assert.Equal(new UInt128(5, 6), value3);

        UInt128* value4 = Int128Native.GetUInt128Ptr(7, 8);
        Assert.Equal(new UInt128(7, 8), *value4);

        ref readonly UInt128 value5 = ref Int128Native.GetUInt128Ref(9, 10);
        Assert.Equal(new UInt128(9, 10), value5);

        UInt128 value6 = Int128Native.AddUInt128(new UInt128(11, 12), new UInt128(13, 14));
        Assert.Equal(new UInt128(24, 26), value6);

        UInt128[] values = new UInt128[] {
            new UInt128(15, 16),
            new UInt128(17, 18),
            new UInt128(19, 20),
            new UInt128(21, 22),
            new UInt128(23, 24),
        };

        fixed (UInt128* pValues = &values[0])
        {
            UInt128 value7 = Int128Native.AddUInt128s(pValues, values.Length);
            Assert.Equal(new UInt128(95, 100), value7);
        }

        UInt128 value8 = Int128Native.AddUInt128s(values, values.Length);
        Assert.Equal(new UInt128(95, 100), value8);

        UInt128 value9 = Int128Native.AddUInt128s(in values[0], values.Length);
        Assert.Equal(new UInt128(95, 100), value9);
    }
}
