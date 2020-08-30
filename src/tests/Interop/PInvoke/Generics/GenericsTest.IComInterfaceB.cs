// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterface")]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern IComInterface<bool> GetIComInterfaceB();

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaceOut")]
    public static extern void GetIComInterfaceBOut([MarshalAs(UnmanagedType.Interface)] out IComInterface<bool> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfacePtr")]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern ref readonly IComInterface<bool> GetIComInterfaceBRef();

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaces")]
    public static extern void GetIComInterfaceBs([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface, SizeParamIndex = 1)] IComInterface<bool>[] pValues, int count);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetIComInterfaces")]
    public static extern void GetIComInterfaceBs([MarshalAs(UnmanagedType.Interface)] ref IComInterface<bool> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestIComInterfaceB()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceB());

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceBOut(out GenericsNative.IComInterface<bool> value2));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceBRef());

        GenericsNative.IComInterface<bool>[] values = new GenericsNative.IComInterface<bool>[3];

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceBs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetIComInterfaceBs(ref values[0], values.Length));
    }
}
