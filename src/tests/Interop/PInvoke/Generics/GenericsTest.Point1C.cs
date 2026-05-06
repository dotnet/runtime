// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point1<char> GetPoint1C(char e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint1COut(char e00, out Point1<char> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint1CPtr")]
    public static extern ref readonly Point1<char> GetPoint1CRef(char e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<char> AddPoint1C(Point1<char> lhs, Point1<char> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<char> AddPoint1Cs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point1<char>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<char> AddPoint1Cs(in Point1<char> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint1C()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint1C('1'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint1COut('1', out GenericsNative.Point1<char> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint1CRef('1'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint1C(default, default));

        GenericsNative.Point1<char>[] values = new GenericsNative.Point1<char>[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint1Cs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint1Cs(in values[0], values.Length));
    }
}
