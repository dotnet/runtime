// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point3<char> GetPoint3C(char e00, char e01, char e02);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint3COut(char e00, char e01, char e02, out Point3<char> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint3CPtr")]
    public static extern ref readonly Point3<char> GetPoint3CRef(char e00, char e01, char e02);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<char> AddPoint3C(Point3<char> lhs, Point3<char> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<char> AddPoint3Cs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point3<char>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<char> AddPoint3Cs(in Point3<char> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint3C()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint3C('1', '2', '3'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint3COut('1', '2', '3', out GenericsNative.Point3<char> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint3CRef('1', '2', '3'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint3C(default, default));

        GenericsNative.Point3<char>[] values = new GenericsNative.Point3<char>[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint3Cs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint3Cs(in values[0], values.Length));
    }
}
