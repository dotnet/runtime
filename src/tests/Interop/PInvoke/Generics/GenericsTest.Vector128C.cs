// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<char> GetVector128C([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128COut([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, Vector128<char>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128COut([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, out Vector128<char> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<char>* GetVector128CPtr([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector128CPtr")]
    public static extern ref readonly Vector128<char> GetVector128CRef([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<char> AddVector128C(Vector128<char> lhs, Vector128<char> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<char> AddVector128Cs(Vector128<char>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<char> AddVector128Cs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector128<char>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<char> AddVector128Cs(in Vector128<char> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector128C()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128C('0', '1', '2', '3', '4', '5', '6', '7'));

        Vector128<char> value2;
        GenericsNative.GetVector128COut('0', '1', '2', '3', '4', '5', '6', '7', &value2);
        Vector128<short> tValue2 = *(Vector128<short>*)&value2;
        Assert.Equal((short)'0', tValue2.GetElement(0));
        Assert.Equal((short)'1', tValue2.GetElement(1));
        Assert.Equal((short)'2', tValue2.GetElement(2));
        Assert.Equal((short)'3', tValue2.GetElement(3));
        Assert.Equal((short)'4', tValue2.GetElement(4));
        Assert.Equal((short)'5', tValue2.GetElement(5));
        Assert.Equal((short)'6', tValue2.GetElement(6));
        Assert.Equal((short)'7', tValue2.GetElement(7));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128COut('0', '1', '2', '3', '4', '5', '6', '7', out Vector128<char> value3));

        Vector128<char>* value4 = GenericsNative.GetVector128CPtr('0', '1', '2', '3', '4', '5', '6', '7');
        Vector128<short>* tValue4 = (Vector128<short>*)value4;
        Assert.Equal((short)'0', tValue4->GetElement(0));
        Assert.Equal((short)'1', tValue4->GetElement(1));
        Assert.Equal((short)'2', tValue4->GetElement(2));
        Assert.Equal((short)'3', tValue4->GetElement(3));
        Assert.Equal((short)'4', tValue4->GetElement(4));
        Assert.Equal((short)'5', tValue4->GetElement(5));
        Assert.Equal((short)'6', tValue4->GetElement(6));
        Assert.Equal((short)'7', tValue4->GetElement(7));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128CRef('0', '1', '2', '3', '4', '5', '6', '7'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128C(default, default));

        Vector128<char>[] values = new Vector128<char>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector128<char>* pValues = &values[0])
            {
                GenericsNative.AddVector128Cs(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Cs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Cs(in values[0], values.Length));
    }
}
