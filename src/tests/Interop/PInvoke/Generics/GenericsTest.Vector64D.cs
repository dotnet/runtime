// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<double> GetVector64D(double e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector64DOut(double e00, Vector64<double>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector64DOut(double e00, out Vector64<double> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<double>* GetVector64DPtr(double e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector64DPtr")]
    public static extern ref readonly Vector64<double> GetVector64DRef(double e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<double> AddVector64D(Vector64<double> lhs, Vector64<double> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<double> AddVector64Ds(Vector64<double>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<double> AddVector64Ds([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector64<double>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<double> AddVector64Ds(in Vector64<double> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector64D()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64D(1.0));

        Vector64<double> value2;
        GenericsNative.GetVector64DOut(1.0, &value2);
        Assert.Equal(1.0, value2.GetElement(0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64DOut(1.0, out Vector64<double> value3));

        Vector64<double>* value4 = GenericsNative.GetVector64DPtr(1.0);
        Assert.Equal(1.0, value4->GetElement(0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64DRef(1.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64D(default, default));

        Vector64<double>[] values = new Vector64<double>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector64<double>* pValues = &values[0])
            {
                GenericsNative.AddVector64Ds(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64Ds(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64Ds(in values[0], values.Length));
    }
}
