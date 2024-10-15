// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point2<bool> GetPoint2B(bool e00, bool e01);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint2BOut(bool e00, bool e01, out Point2<bool> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint2BPtr")]
    public static extern ref readonly Point2<bool> GetPoint2BRef(bool e00, bool e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<bool> AddPoint2B(Point2<bool> lhs, Point2<bool> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<bool> AddPoint2Bs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point2<bool>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<bool> AddPoint2Bs(in Point2<bool> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint2B()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint2B(true, false));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint2BOut(true, false, out GenericsNative.Point2<bool> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint2BRef(true, false));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint2B(default, default));

        GenericsNative.Point2<bool>[] values = new GenericsNative.Point2<bool>[] {
            default,
            default,
            default,
            default,
            default
        };


        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint2Bs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint2Bs(in values[0], values.Length));
    }
}
