// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<bool> GetVector128B([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128BOut([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15, Vector128<bool>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128BOut([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15, out Vector128<bool> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<bool>* GetVector128BPtr([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector128BPtr")]
    public static extern ref readonly Vector128<bool> GetVector128BRef([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<bool> AddVector128B(Vector128<bool> lhs, Vector128<bool> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<bool> AddVector128Bs(Vector128<bool>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<bool> AddVector128Bs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector128<bool>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<bool> AddVector128Bs(in Vector128<bool> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector128B()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128B(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false));

        Vector128<bool> value2;
        GenericsNative.GetVector128BOut(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, &value2);
        Vector128<byte> tValue2 = *(Vector128<byte>*)&value2;
        Assert.Equal(1, tValue2.GetElement(0));
        Assert.Equal(0, tValue2.GetElement(1));
        Assert.Equal(1, tValue2.GetElement(2));
        Assert.Equal(0, tValue2.GetElement(3));
        Assert.Equal(1, tValue2.GetElement(4));
        Assert.Equal(0, tValue2.GetElement(5));
        Assert.Equal(1, tValue2.GetElement(6));
        Assert.Equal(0, tValue2.GetElement(7));
        Assert.Equal(1, tValue2.GetElement(8));
        Assert.Equal(0, tValue2.GetElement(9));
        Assert.Equal(1, tValue2.GetElement(10));
        Assert.Equal(0, tValue2.GetElement(11));
        Assert.Equal(1, tValue2.GetElement(12));
        Assert.Equal(0, tValue2.GetElement(13));
        Assert.Equal(1, tValue2.GetElement(14));
        Assert.Equal(0, tValue2.GetElement(15));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128BOut(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, out Vector128<bool> value3));

        Vector128<bool>* value4 = GenericsNative.GetVector128BPtr(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false);
        Vector128<byte>* tValue4 = (Vector128<byte>*)value4;
        Assert.Equal(1, tValue4->GetElement(0));
        Assert.Equal(0, tValue4->GetElement(1));
        Assert.Equal(1, tValue4->GetElement(2));
        Assert.Equal(0, tValue4->GetElement(3));
        Assert.Equal(1, tValue4->GetElement(4));
        Assert.Equal(0, tValue4->GetElement(5));
        Assert.Equal(1, tValue4->GetElement(6));
        Assert.Equal(0, tValue4->GetElement(7));
        Assert.Equal(1, tValue4->GetElement(8));
        Assert.Equal(0, tValue4->GetElement(9));
        Assert.Equal(1, tValue4->GetElement(10));
        Assert.Equal(0, tValue4->GetElement(11));
        Assert.Equal(1, tValue4->GetElement(12));
        Assert.Equal(0, tValue4->GetElement(13));
        Assert.Equal(1, tValue4->GetElement(14));
        Assert.Equal(0, tValue4->GetElement(15));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128BRef(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128B(default, default));

        Vector128<bool>[] values = new Vector128<bool>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector128<bool>* pValues = &values[0])
            {
                GenericsNative.AddVector128Bs(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Bs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Bs(in values[0], values.Length));
    }
}
