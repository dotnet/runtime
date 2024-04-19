// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<double> GetVector256D(double e00, double e01, double e02, double e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector256DOut(double e00, double e01, double e02, double e03, Vector256<double>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector256DOut(double e00, double e01, double e02, double e03, out Vector256<double> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<double>* GetVector256DPtr(double e00, double e01, double e02, double e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector256DPtr")]
    public static extern ref readonly Vector256<double> GetVector256DRef(double e00, double e01, double e02, double e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<double> AddVector256D(Vector256<double> lhs, Vector256<double> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<double> AddVector256Ds(Vector256<double>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<double> AddVector256Ds([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector256<double>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<double> AddVector256Ds(in Vector256<double> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsXArch))]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector256D()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256D(1.0, 2.0, 3.0, 4.0));

        Vector256<double> value2;
        GenericsNative.GetVector256DOut(1.0, 2.0, 3.0, 4.0, &value2);
        Assert.Equal(1.0, value2.GetElement(0));
        Assert.Equal(2.0, value2.GetElement(1));
        Assert.Equal(3.0, value2.GetElement(2));
        Assert.Equal(4.0, value2.GetElement(3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256DOut(1.0, 2.0, 3.0, 4.0, out Vector256<double> value3));

        Vector256<double>* value4 = GenericsNative.GetVector256DPtr(1.0, 2.0, 3.0, 4.0);
        Assert.Equal(1.0, value4->GetElement(0));
        Assert.Equal(2.0, value4->GetElement(1));
        Assert.Equal(3.0, value4->GetElement(2));
        Assert.Equal(4.0, value4->GetElement(3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256DRef(1.0, 2.0, 3.0, 4.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256D(default, default));

        Vector256<double>[] values = new Vector256<double>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector256<double>* pValues = &values[0])
            {
                GenericsNative.AddVector256Ds(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256Ds(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256Ds(in values[0], values.Length));
    }
}
