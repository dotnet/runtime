// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector<uint> GetVectorU128(uint e00, uint e01, uint e02, uint e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<uint> GetVectorU256(uint e00, uint e01, uint e02, uint e03, uint e04, uint e05, uint e06, uint e07);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorU128Out(uint e00, uint e01, uint e02, uint e03, Vector<uint>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorU256Out(uint e00, uint e01, uint e02, uint e03, uint e04, uint e05, uint e06, uint e07, Vector<uint>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorU128Out(uint e00, uint e01, uint e02, uint e03, out Vector<uint> value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorU256Out(uint e00, uint e01, uint e02, uint e03, uint e04, uint e05, uint e06, uint e07, out Vector<uint> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<uint>* GetVectorU128Ptr(uint e00, uint e01, uint e02, uint e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<uint>* GetVectorU256Ptr(uint e00, uint e01, uint e02, uint e03, uint e04, uint e05, uint e06, uint e07);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVectorU128Ptr")]
    public static extern ref readonly Vector<uint> GetVectorU128Ref(uint e00, uint e01, uint e02, uint e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVectorU256Ptr")]
    public static extern ref readonly Vector<uint> GetVectorU256Ref(uint e00, uint e01, uint e02, uint e03, uint e04, uint e05, uint e06, uint e07);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<uint> AddVectorU128(Vector<uint> lhs, Vector<uint> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<uint> AddVectorU256(Vector<uint> lhs, Vector<uint> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<uint> AddVectorU128s(Vector<uint>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<uint> AddVectorU256s(Vector<uint>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<uint> AddVectorU128s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector<uint>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<uint> AddVectorU256s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector<uint>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<uint> AddVectorU128s(in Vector<uint> pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<uint> AddVectorU256s(in Vector<uint> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVectorU()
    {
        if (Vector<uint>.Count == 8)
        {
            TestVectorU256();
        }
        else
        {
            Assert.Equal(4, Vector<uint>.Count);
            TestVectorU128();
        }
    }

    public static void TestVectorU128()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorU128(1u, 2u, 3u, 4u));

        Vector<uint> value2;
        GenericsNative.GetVectorU128Out(1u, 2u, 3u, 4u, &value2);
        Assert.Equal(1u, value2[0]);
        Assert.Equal(2u, value2[1]);
        Assert.Equal(3u, value2[2]);
        Assert.Equal(4u, value2[3]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorU128Out(1u, 2u, 3u, 4u, out Vector<uint> value3));

        Vector<uint>* value4 = GenericsNative.GetVectorU128Ptr(1u, 2u, 3u, 4u);
        Assert.Equal(1u, (*value4)[0]);
        Assert.Equal(2u, (*value4)[1]);
        Assert.Equal(3u, (*value4)[2]);
        Assert.Equal(4u, (*value4)[3]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorU128Ref(1u, 2u, 3u, 4u));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorU128(default, default));

        Vector<uint>[] values = new Vector<uint>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector<uint>* pValues = &values[0])
            {
                GenericsNative.AddVectorU128s(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorU128s(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorU128s(in values[0], values.Length));
    }

    public static void TestVectorU256()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorU256(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u));

        Vector<uint> value2;
        GenericsNative.GetVectorU256Out(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u, &value2);
        Assert.Equal(1u, value2[0]);
        Assert.Equal(2u, value2[1]);
        Assert.Equal(3u, value2[2]);
        Assert.Equal(4u, value2[3]);
        Assert.Equal(5u, value2[4]);
        Assert.Equal(6u, value2[5]);
        Assert.Equal(7u, value2[6]);
        Assert.Equal(8u, value2[7]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorU256Out(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u, out Vector<uint> value3));

        Vector<uint>* value4 = GenericsNative.GetVectorU256Ptr(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u);
        Assert.Equal(1u, (*value4)[0]);
        Assert.Equal(2u, (*value4)[1]);
        Assert.Equal(3u, (*value4)[2]);
        Assert.Equal(4u, (*value4)[3]);
        Assert.Equal(5u, (*value4)[4]);
        Assert.Equal(6u, (*value4)[5]);
        Assert.Equal(7u, (*value4)[6]);
        Assert.Equal(8u, (*value4)[7]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorU256Ref(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorU256(default, default));

        Vector<uint>[] values = new Vector<uint>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector<uint>* pValues = &values[0])
            {
                GenericsNative.AddVectorU256s(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorU256s(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorU256s(in values[0], values.Length));
    }
}
