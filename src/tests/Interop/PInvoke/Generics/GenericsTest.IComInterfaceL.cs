// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterface")]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern IComInterface<long> GetIComInterfaceL();

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaceOut")]
    public static extern void GetIComInterfaceLOut([MarshalAs(UnmanagedType.Interface)] out IComInterface<long> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfacePtr")]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern ref readonly IComInterface<long> GetIComInterfaceLRef();

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaces")]
    public static extern void GetIComInterfaceLs([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface, SizeParamIndex = 1)] IComInterface<long>[] pValues, int count);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaces")]
    public static extern void GetIComInterfaceLs([MarshalAs(UnmanagedType.Interface)] ref IComInterface<long> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestIComInterfaceL()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceL());

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceLOut(out GenericsNative.IComInterface<long> value2));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceLRef());

        GenericsNative.IComInterface<long>[] values = new GenericsNative.IComInterface<long>[3];

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceLs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceLs(ref values[0], values.Length));
    }
}
