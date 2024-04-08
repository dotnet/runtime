// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterface")]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern IComInterface<double> GetIComInterfaceD();

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaceOut")]
    public static extern void GetIComInterfaceDOut([MarshalAs(UnmanagedType.Interface)] out IComInterface<double> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfacePtr")]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern ref readonly IComInterface<double> GetIComInterfaceDRef();

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaces")]
    public static extern void GetIComInterfaceDs([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface, SizeParamIndex = 1)] IComInterface<double>[] pValues, int count);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaces")]
    public static extern void GetIComInterfaceDs([MarshalAs(UnmanagedType.Interface)] ref IComInterface<double> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestIComInterfaceD()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceD());

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceDOut(out GenericsNative.IComInterface<double> value2));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceDRef());

        GenericsNative.IComInterface<double>[] values = new GenericsNative.IComInterface<double>[3];

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceDs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceDs(ref values[0], values.Length));
    }
}
