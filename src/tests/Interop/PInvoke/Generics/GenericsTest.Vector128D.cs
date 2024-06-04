// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<double> GetVector128D(double e00, double e01);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128DOut(double e00, double e01, Vector128<double>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128DOut(double e00, double e01, out Vector128<double> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<double>* GetVector128DPtr(double e00, double e01);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector128DPtr")]
    public static extern ref readonly Vector128<double> GetVector128DRef(double e00, double e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<double> AddVector128D(Vector128<double> lhs, Vector128<double> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<double> AddVector128Ds(Vector128<double>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<double> AddVector128Ds([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector128<double>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<double> AddVector128Ds(in Vector128<double> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector128D()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128D(1.0, 2.0));

        Vector128<double> value2;
        GenericsNative.GetVector128DOut(1.0, 2.0, &value2);
        Assert.Equal(1.0, value2.GetElement(0));
        Assert.Equal(2.0, value2.GetElement(1));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128DOut(1.0, 2.0, out Vector128<double> value3));

        Vector128<double>* value4 = GenericsNative.GetVector128DPtr(1.0, 2.0);
        Assert.Equal(1.0, value4->GetElement(0));
        Assert.Equal(2.0, value4->GetElement(1));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128DRef(1.0, 2.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128D(default, default));

        Vector128<double>[] values = new Vector128<double>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector128<double>* pValues = &values[0])
            {
                GenericsNative.AddVector128Ds(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Ds(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Ds(in values[0], values.Length));
    }
}
