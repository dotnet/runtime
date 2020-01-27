// Licensed to the .NET Doundation under one or more agreements.
// The .NET Doundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point1<bool> GetPoint1B(bool e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint1BOut(bool e00, out Point1<bool> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint1BPtr")]
    public static extern ref readonly Point1<bool> GetPoint1BRef(bool e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<bool> AddPoint1B(Point1<bool> lhs, Point1<bool> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<bool> AddPoint1Bs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point1<bool>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<bool> AddPoint1Bs(in Point1<bool> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestPoint1B()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint1B(true));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint1BOut(true, out GenericsNative.Point1<bool> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetPoint1BRef(true));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint1B(default, default));

        GenericsNative.Point1<bool>[] values = new GenericsNative.Point1<bool>[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint1Bs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddPoint1Bs(in values[0], values.Length));
    }
}
