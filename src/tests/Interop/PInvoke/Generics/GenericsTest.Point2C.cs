// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point2<char> GetPoint2C(char e00, char e01);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint2COut(char e00, char e01, out Point2<char> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint2CPtr")]
    public static extern ref readonly Point2<char> GetPoint2CRef(char e00, char e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<char> AddPoint2C(Point2<char> lhs, Point2<char> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<char> AddPoint2Cs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point2<char>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<char> AddPoint2Cs(in Point2<char> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint2C()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint2C('1', '2'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint2COut('1', '2', out GenericsNative.Point2<char> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint2CRef('1', '2'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint2C(default, default));

        GenericsNative.Point2<char>[] values = new GenericsNative.Point2<char>[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint2Cs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint2Cs(in values[0], values.Length));
    }
}
