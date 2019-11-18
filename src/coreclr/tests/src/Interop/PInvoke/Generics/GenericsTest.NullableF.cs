// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern float? GetNullableF(bool hasValue, float value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetNullableFOut(bool hasValue, float value, float?* pValue);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetNullableFOut(bool hasValue, float value, out float? pValue);

    [DllImport(nameof(GenericsNative))]
    public static extern float?* GetNullableFPtr(bool hasValue, float value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetNullableFPtr")]
    public static extern ref readonly float? GetNullableFRef(bool hasValue, float value);

    [DllImport(nameof(GenericsNative))]
    public static extern float? AddNullableF(float? lhs, float? rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern float? AddNullableFs(float?* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern float? AddNullableFs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] float?[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern float? AddNullableFs(in float? pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestNullableF()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableF(true, 1.0f));

        Assert.Throws<MarshalDirectiveException>(() => {
            float? value2;
            GenericsNative.GetNullableFOut(true, 1.0f, &value2);
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableFOut(true, 1.0f, out float? value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableFPtr(true, 1.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableFRef(true, 1.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableF(default, default));

        float?[] values = new float?[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (float?* pValues = &values[0])
            {
                GenericsNative.AddNullableFs(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableFs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableFs(in values[0], values.Length));
    }
}
