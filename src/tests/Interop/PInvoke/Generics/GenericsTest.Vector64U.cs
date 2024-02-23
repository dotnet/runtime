// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<uint> GetVector64U(uint e00, uint e01);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector64UOut(uint e00, uint e01, Vector64<uint>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector64UOut(uint e00, uint e01, out Vector64<uint> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<uint>* GetVector64UPtr(uint e00, uint e01);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector64UPtr")]
    public static extern ref readonly Vector64<uint> GetVector64URef(uint e00, uint e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<uint> AddVector64U(Vector64<uint> lhs, Vector64<uint> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<uint> AddVector64Us(Vector64<uint>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<uint> AddVector64Us([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector64<uint>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<uint> AddVector64Us(in Vector64<uint> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector64U()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64U(1u, 2u));

        Vector64<uint> value2;
        GenericsNative.GetVector64UOut(1u, 2u, &value2);
        Assert.Equal(1u, value2.GetElement(0));
        Assert.Equal(2u, value2.GetElement(1));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64UOut(1u, 2u, out Vector64<uint> value3));

        Vector64<uint>* value4 = GenericsNative.GetVector64UPtr(1u, 2u);
        Assert.Equal(1u, value4->GetElement(0));
        Assert.Equal(2u, value4->GetElement(1));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64URef(1u, 2u));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64U(default, default));

        Vector64<uint>[] values = new Vector64<uint>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector64<uint>* pValues = &values[0])
            {
                GenericsNative.AddVector64Us(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64Us(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64Us(in values[0], values.Length));
    }
}
