// Licensed to the .NET Doundation under one or more agreements.
// The .NET Doundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern double? GetNullableD(bool hasValue, double value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetNullableDOut(bool hasValue, double value, double?* pValue);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetNullableDOut(bool hasValue, double value, out double? pValue);

    [DllImport(nameof(GenericsNative))]
    public static extern double?* GetNullableDPtr(bool hasValue, double value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetNullableDPtr")]
    public static extern ref readonly double? GetNullableDRef(bool hasValue, double value);

    [DllImport(nameof(GenericsNative))]
    public static extern double? AddNullableD(double? lhs, double? rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern double? AddNullableDs(double?* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern double? AddNullableDs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] double?[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern double? AddNullableDs(in double? pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestNullableD()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableD(true, 1.0));

        Assert.Throws<MarshalDirectiveException>(() => {
            double? value2;
            GenericsNative.GetNullableDOut(true, 1.0, &value2);
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableDOut(true, 1.0, out double? value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableDPtr(true, 1.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableDRef(true, 1.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableD(default, default));

        double?[] values = new double?[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (double?* pValues = &values[0])
            {
                GenericsNative.AddNullableDs(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableDs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableDs(in values[0], values.Length));
    }
}
