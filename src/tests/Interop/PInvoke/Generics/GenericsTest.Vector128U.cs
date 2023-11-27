// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<uint> GetVector128U(uint e00, uint e01, uint e02, uint e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128UOut(uint e00, uint e01, uint e02, uint e03, Vector128<uint>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128UOut(uint e00, uint e01, uint e02, uint e03, out Vector128<uint> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<uint>* GetVector128UPtr(uint e00, uint e01, uint e02, uint e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector128UPtr")]
    public static extern ref readonly Vector128<uint> GetVector128URef(uint e00, uint e01, uint e02, uint e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<uint> AddVector128U(Vector128<uint> lhs, Vector128<uint> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<uint> AddVector128Us(Vector128<uint>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<uint> AddVector128Us([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector128<uint>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<uint> AddVector128Us(in Vector128<uint> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector128U()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128U(1u, 2u, 3u, 4u));

        Vector128<uint> value2;
        GenericsNative.GetVector128UOut(1u, 2u, 3u, 4u, &value2);
        Assert.Equal(1u, value2.GetElement(0));
        Assert.Equal(2u, value2.GetElement(1));
        Assert.Equal(3u, value2.GetElement(2));
        Assert.Equal(4u, value2.GetElement(3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128UOut(1u, 2u, 3u, 4u, out Vector128<uint> value3));

        Vector128<uint>* value4 = GenericsNative.GetVector128UPtr(1u, 2u, 3u, 4u);
        Assert.Equal(1u, value4->GetElement(0));
        Assert.Equal(2u, value4->GetElement(1));
        Assert.Equal(3u, value4->GetElement(2));
        Assert.Equal(4u, value4->GetElement(3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128URef(1u, 2u, 3u, 4u));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128U(default, default));

        Vector128<uint>[] values = new Vector128<uint>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector128<uint>* pValues = &values[0])
            {
                GenericsNative.AddVector128Us(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Us(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Us(in values[0], values.Length));
    }
}
