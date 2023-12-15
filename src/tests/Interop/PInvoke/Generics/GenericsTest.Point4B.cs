// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point4<bool> GetPoint4B(bool e00, bool e01, bool e02, bool e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint4BOut(bool e00, bool e01, bool e02, bool e03, out Point4<bool> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint4BPtr")]
    public static extern ref readonly Point4<bool> GetPoint4BRef(bool e00, bool e01, bool e02, bool e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<bool> AddPoint4B(Point4<bool> lhs, Point4<bool> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<bool> AddPoint4Bs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point4<bool>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<bool> AddPoint4Bs(in Point4<bool> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint4B()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint4B(true, false, true, false));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint4BOut(true, false, true, false, out GenericsNative.Point4<bool> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint4BRef(true, false, true, false));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint4B(default, default));

        GenericsNative.Point4<bool>[] values = new GenericsNative.Point4<bool>[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint4Bs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint4Bs(in values[0], values.Length));
    }
}
