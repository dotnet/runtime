// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<char> GetVector64C([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector64COut([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, Vector64<char>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector64COut([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, out Vector64<char> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<char>* GetVector64CPtr([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector64CPtr")]
    public static extern ref readonly Vector64<char> GetVector64CRef([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<char> AddVector64C(Vector64<char> lhs, Vector64<char> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<char> AddVector64Cs(Vector64<char>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<char> AddVector64Cs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector64<char>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<char> AddVector64Cs(in Vector64<char> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector64C()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64C('0', '1', '2', '3'));

        Vector64<char> value2;
        GenericsNative.GetVector64COut('0', '1', '2', '3', &value2);
        Vector64<short> tValue2 = *(Vector64<short>*)&value2;
        Assert.Equal((short)'0', tValue2.GetElement(0));
        Assert.Equal((short)'1', tValue2.GetElement(1));
        Assert.Equal((short)'2', tValue2.GetElement(2));
        Assert.Equal((short)'3', tValue2.GetElement(3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64COut('0', '1', '2', '3', out Vector64<char> value3));

        Vector64<char>* value4 = GenericsNative.GetVector64CPtr('0', '1', '2', '3');
        Vector64<short>* tValue4 = (Vector64<short>*)value4;
        Assert.Equal((short)'0', tValue4->GetElement(0));
        Assert.Equal((short)'1', tValue4->GetElement(1));
        Assert.Equal((short)'2', tValue4->GetElement(2));
        Assert.Equal((short)'3', tValue4->GetElement(3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64CRef('0', '1', '2', '3'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64C(default, default));

        Vector64<char>[] values = new Vector64<char>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector64<char>* pValues = &values[0])
            {
                GenericsNative.AddVector64Cs(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64Cs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64Cs(in values[0], values.Length));
    }
}
