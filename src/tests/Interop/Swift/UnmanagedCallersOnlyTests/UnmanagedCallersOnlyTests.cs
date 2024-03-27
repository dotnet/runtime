// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using Xunit;

public class UnmanagedCallersOnlyTests
{
    private const string SwiftLib = "libUnmanagedCallersOnlyTests.dylib";

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s25UnmanagedCallersOnlyTests26nativeFunctionWithCallbackyyyyXEF")]
    public static extern unsafe IntPtr NativeFunctionWithCallback(delegate* unmanaged[Swift]<SwiftError*, void> callback, SwiftError* error);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static unsafe void ProxyMethod(SwiftError* error) {
        IntPtr addr = 42;
        *error = new SwiftError((void*)addr);
        Assert.True((IntPtr)error->Value == 42, "ProxyMethod: Mismatch");
    }

    [Fact]
    public static unsafe void TestUnmanagedCallersOnly()
    {
        int expectedValue = 42;
        SwiftError error;

        NativeFunctionWithCallback(&ProxyMethod, &error);

        Console.WriteLine("error.Value: {0}", (IntPtr)error.Value);
        Assert.True((IntPtr)error.Value == 42, "TestUnmanagedCallersOnly: Mismatch");
        // int value = *(int*)error.Value;
        // Assert.True(value == expectedValue, string.Format("The value retrieved does not match the expected value. Expected: {0}, Actual: {1}", expectedValue, value));
    }
}