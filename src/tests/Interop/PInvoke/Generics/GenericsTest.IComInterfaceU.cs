// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterface")]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern IComInterface<uint> GetIComInterfaceU();

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaceOut")]
    public static extern void GetIComInterfaceUOut([MarshalAs(UnmanagedType.Interface)] out IComInterface<uint> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfacePtr")]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern ref readonly IComInterface<uint> GetIComInterfaceURef();

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaces")]
    public static extern void GetIComInterfaceUs([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface, SizeParamIndex = 1)] IComInterface<uint>[] pValues, int count);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaces")]
    public static extern void GetIComInterfaceUs([MarshalAs(UnmanagedType.Interface)] ref IComInterface<uint> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestIComInterfaceU()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceU());

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceUOut(out GenericsNative.IComInterface<uint> value2));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceURef());

        GenericsNative.IComInterface<uint>[] values = new GenericsNative.IComInterface<uint>[3];

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceUs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceUs(ref values[0], values.Length));
    }
}
