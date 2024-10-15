// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector<long> GetVectorL128(long e00, long e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<long> GetVectorL256(long e00, long e01, long e02, long e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorL128Out(long e00, long e01, Vector<long>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorL256Out(long e00, long e01, long e02, long e03, Vector<long>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorL128Out(long e00, long e01, out Vector<long> value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorL256Out(long e00, long e01, long e02, long e03, out Vector<long> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<long>* GetVectorL128Ptr(long e00, long e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<long>* GetVectorL256Ptr(long e00, long e01, long e02, long e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVectorL128Ptr")]
    public static extern ref readonly Vector<long> GetVectorL128Ref(long e00, long e01);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVectorL256Ptr")]
    public static extern ref readonly Vector<long> GetVectorL256Ref(long e00, long e01, long e02, long e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<long> AddVectorL128(Vector<long> lhs, Vector<long> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<long> AddVectorL256(Vector<long> lhs, Vector<long> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<long> AddVectorL128s(Vector<long>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<long> AddVectorL256s(Vector<long>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<long> AddVectorL128s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector<long>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<long> AddVectorL256s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector<long>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<long> AddVectorL128s(in Vector<long> pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<long> AddVectorL256s(in Vector<long> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVectorL()
    {
        if (Vector<long>.Count == 4)
        {
            TestVectorL256();
        }
        else
        {
            Assert.Equal(2, Vector<long>.Count);
            TestVectorL128();
        }
    }

    public static void TestVectorL128()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorL128(1L, 2L));

        Vector<long> value2;
        GenericsNative.GetVectorL128Out(1L, 2L, &value2);
        Assert.Equal(1L, value2[0]);
        Assert.Equal(2L, value2[1]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorL128Out(1L, 2L, out Vector<long> value3));

        Vector<long>* value4 = GenericsNative.GetVectorL128Ptr(1L, 2L);
        Assert.Equal(1L, (*value4)[0]);
        Assert.Equal(2L, (*value4)[1]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorL128Ref(1L, 2L));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorL128(default, default));

        Vector<long>[] values = new Vector<long>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector<long>* pValues = &values[0])
            {
                GenericsNative.AddVectorL128s(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorL128s(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorL128s(in values[0], values.Length));
    }

    public static void TestVectorL256()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorL256(1L, 2L, 3L, 4L));

        Vector<long> value2;
        GenericsNative.GetVectorL256Out(1L, 2L, 3L, 4L, &value2);
        Assert.Equal(1L, value2[0]);
        Assert.Equal(2L, value2[1]);
        Assert.Equal(3L, value2[2]);
        Assert.Equal(4L, value2[3]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorL256Out(1L, 2L, 3L, 4L, out Vector<long> value3));

        Vector<long>* value4 = GenericsNative.GetVectorL256Ptr(1L, 2L, 3L, 4L);
        Assert.Equal(1L, (*value4)[0]);
        Assert.Equal(2L, (*value4)[1]);
        Assert.Equal(3L, (*value4)[2]);
        Assert.Equal(4L, (*value4)[3]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorL256Ref(1L, 2L, 3L, 4L));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorL256(default, default));

        Vector<long>[] values = new Vector<long>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector<long>* pValues = &values[0])
            {
                GenericsNative.AddVectorL256s(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorL256s(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorL256s(in values[0], values.Length));
    }
}
