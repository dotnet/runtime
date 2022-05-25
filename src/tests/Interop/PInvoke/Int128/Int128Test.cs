// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class Int128Native
{
    [DllImport(nameof(Int128Native))]
    public static extern Int128 GetInt128(ulong upper, ulong lower);

    [DllImport(nameof(Int128Native))]
    public static extern void GetInt128Out(ulong upper, ulong lower, Int128* value);

    [DllImport(nameof(Int128Native))]
    public static extern void GetInt128Out(ulong upper, ulong lower, out Int128 value);

    [DllImport(nameof(Int128Native))]
    public static extern Int128* GetInt128Ptr(ulong upper, ulong lower);

    [DllImport(nameof(Int128Native), EntryPoint = "GetInt128Ptr")]
    public static extern ref readonly Int128 GetInt128Ref(ulong upper, ulong lower);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128(Int128 lhs, Int128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128s(Int128* pValues, int count);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Int128[] pValues, int count);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128s(in Int128 pValues, int count);
}

unsafe partial class Int128Native
{
    private static void TestInt128()
    {
        Int128 value1 = Int128Native.GetInt128(1, 2);
        Assert.Equal(new Int128(1, 2), value1);

        Int128 value2;
        Int128Native.GetInt128Out(3, 4, &value2);
        Assert.Equal(new Int128(3, 4), value2);

        Int128Native.GetInt128Out(5, 6, out Int128 value3);
        Assert.Equal(new Int128(5, 6), value3);

        Int128* value4 = Int128Native.GetInt128Ptr(7, 8);
        Assert.Equal(new Int128(7, 8), *value4);

        ref readonly Int128 value5 = ref Int128Native.GetInt128Ref(9, 10);
        Assert.Equal(new Int128(9, 10), value5);

        Int128 value6 = Int128Native.AddInt128(new Int128(11, 12), new Int128(13, 14));
        Assert.Equal(new Int128(24, 26), value6);

        Int128[] values = new Int128[] {
            new Int128(15, 16),
            new Int128(17, 18),
            new Int128(19, 20),
            new Int128(21, 22),
            new Int128(23, 24),
        };

        fixed (Int128* pValues = &values[0])
        {
            Int128 value7 = Int128Native.AddInt128s(pValues, values.Length);
            Assert.Equal(new Int128(95, 100), value7);
        }

        Int128 value8 = Int128Native.AddInt128s(values, values.Length);
        Assert.Equal(new Int128(95, 100), value8);

        Int128 value9 = Int128Native.AddInt128s(in values[0], values.Length);
        Assert.Equal(new Int128(95, 100), value9);
    }
}
