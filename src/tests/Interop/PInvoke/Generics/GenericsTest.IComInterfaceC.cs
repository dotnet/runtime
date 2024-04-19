// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterface")]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern IComInterface<char> GetIComInterfaceC();

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaceOut")]
    public static extern void GetIComInterfaceCOut([MarshalAs(UnmanagedType.Interface)] out IComInterface<char> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfacePtr")]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern ref readonly IComInterface<char> GetIComInterfaceCRef();

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaces")]
    public static extern void GetIComInterfaceCs([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface, SizeParamIndex = 1)] IComInterface<char>[] pValues, int count);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaces")]
    public static extern void GetIComInterfaceCs([MarshalAs(UnmanagedType.Interface)] ref IComInterface<char> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestIComInterfaceC()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceC());

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceCOut(out GenericsNative.IComInterface<char> value2));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceCRef());

        GenericsNative.IComInterface<char>[] values = new GenericsNative.IComInterface<char>[3];

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceCs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceCs(ref values[0], values.Length));
    }
}
