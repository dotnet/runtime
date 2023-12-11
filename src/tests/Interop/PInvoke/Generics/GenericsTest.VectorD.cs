// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector<double> GetVectorD128(double e00, double e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<double> GetVectorD256(double e00, double e01, double e02, double e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorD128Out(double e00, double e01, Vector<double>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorD256Out(double e00, double e01, double e02, double e03, Vector<double>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorD128Out(double e00, double e01, out Vector<double> value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorD256Out(double e00, double e01, double e02, double e03, out Vector<double> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<double>* GetVectorD128Ptr(double e00, double e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<double>* GetVectorD256Ptr(double e00, double e01, double e02, double e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVectorD128Ptr")]
    public static extern ref readonly Vector<double> GetVectorD128Ref(double e00, double e01);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVectorD256Ptr")]
    public static extern ref readonly Vector<double> GetVectorD256Ref(double e00, double e01, double e02, double e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<double> AddVectorD128(Vector<double> lhs, Vector<double> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<double> AddVectorD256(Vector<double> lhs, Vector<double> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<double> AddVectorD128s(Vector<double>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<double> AddVectorD256s(Vector<double>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<double> AddVectorD128s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector<double>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<double> AddVectorD256s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector<double>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<double> AddVectorD128s(in Vector<double> pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<double> AddVectorD256s(in Vector<double> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVectorD()
    {
        if (Vector<double>.Count == 4)
        {
            TestVectorD256();
        }
        else
        {
            Assert.Equal(2, Vector<double>.Count);
            TestVectorD128();
        }
    }

    public static void TestVectorD128()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorD128(1.0, 2.0));

        Vector<double> value2;
        GenericsNative.GetVectorD128Out(1.0, 2.0, &value2);
        Assert.Equal(1.0, value2[0]);
        Assert.Equal(2.0, value2[1]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorD128Out(1.0, 2.0, out Vector<double> value3));

        Vector<double>* value4 = GenericsNative.GetVectorD128Ptr(1.0, 2.0);
        Assert.Equal(1.0, (*value4)[0]);
        Assert.Equal(2.0, (*value4)[1]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorD128Ref(1.0, 2.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorD128(default, default));

        Vector<double>[] values = new Vector<double>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector<double>* pValues = &values[0])
            {
                GenericsNative.AddVectorD128s(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorD128s(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorD128s(in values[0], values.Length));
    }

    public static void TestVectorD256()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorD256(1.0, 2.0, 3.0, 4.0));

        Vector<double> value2;
        GenericsNative.GetVectorD256Out(1.0, 2.0, 3.0, 4.0, &value2);
        Assert.Equal(1.0, value2[0]);
        Assert.Equal(2.0, value2[1]);
        Assert.Equal(3.0, value2[2]);
        Assert.Equal(4.0, value2[3]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorD256Out(1.0, 2.0, 3.0, 4.0, out Vector<double> value3));

        Vector<double>* value4 = GenericsNative.GetVectorD256Ptr(1.0, 2.0, 3.0, 4.0);
        Assert.Equal(1.0, (*value4)[0]);
        Assert.Equal(2.0, (*value4)[1]);
        Assert.Equal(3.0, (*value4)[2]);
        Assert.Equal(4.0, (*value4)[3]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorD256Ref(1.0, 2.0, 3.0, 4.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorD256(default, default));

        Vector<double>[] values = new Vector<double>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector<double>* pValues = &values[0])
            {
                GenericsNative.AddVectorD256s(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorD256s(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorD256s(in values[0], values.Length));
    }
}
