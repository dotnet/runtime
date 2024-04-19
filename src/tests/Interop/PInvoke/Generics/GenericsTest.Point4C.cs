// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point4<char> GetPoint4C(char e00, char e01, char e02, char e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint4COut(char e00, char e01, char e02, char e03, out Point4<char> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint4CPtr")]
    public static extern ref readonly Point4<char> GetPoint4CRef(char e00, char e01, char e02, char e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<char> AddPoint4C(Point4<char> lhs, Point4<char> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<char> AddPoint4Cs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point4<char>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<char> AddPoint4Cs(in Point4<char> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint4C()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint4C('1', '2', '3', '4'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint4COut('1', '2', '3', '4', out GenericsNative.Point4<char> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint4CRef('1', '2', '3', '4'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint4C(default, default));

        GenericsNative.Point4<char>[] values = new GenericsNative.Point4<char>[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint4Cs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint4Cs(in values[0], values.Length));
    }
}
