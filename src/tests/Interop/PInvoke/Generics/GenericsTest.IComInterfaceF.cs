// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterface")]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern IComInterface<float> GetIComInterfaceF();

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaceOut")]
    public static extern void GetIComInterfaceFOut([MarshalAs(UnmanagedType.Interface)] out IComInterface<float> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfacePtr")]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern ref readonly IComInterface<float> GetIComInterfaceFRef();

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaces")]
    public static extern void GetIComInterfaceFs([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface, SizeParamIndex = 1)] IComInterface<float>[] pValues, int count);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaces")]
    public static extern void GetIComInterfaceFs([MarshalAs(UnmanagedType.Interface)] ref IComInterface<float> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestIComInterfaceF()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceF());

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceFOut(out GenericsNative.IComInterface<float> value2));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceFRef());

        GenericsNative.IComInterface<float>[] values = new GenericsNative.IComInterface<float>[3];

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceFs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceFs(ref values[0], values.Length));
    }
}
