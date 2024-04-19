// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point3<bool> GetPoint3B(bool e00, bool e01, bool e02);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint3BOut(bool e00, bool e01, bool e02, out Point3<bool> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint3BPtr")]
    public static extern ref readonly Point3<bool> GetPoint3BRef(bool e00, bool e01, bool e02);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<bool> AddPoint3B(Point3<bool> lhs, Point3<bool> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<bool> AddPoint3Bs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point3<bool>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<bool> AddPoint3Bs(in Point3<bool> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint3B()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint3B(true, false, true));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint3BOut(true, false, true, out GenericsNative.Point3<bool> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint3BRef(true, false, true));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint3B(default, default));

        GenericsNative.Point3<bool>[] values = new GenericsNative.Point3<bool>[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint3Bs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint3Bs(in values[0], values.Length));
    }
}
