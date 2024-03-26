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
    public static extern unsafe IntPtr NativeFunctionWithCallback(delegate* unmanaged[Swift]<SwiftSelf, SwiftError*, void> callback, SwiftSelf self, SwiftError* error);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static unsafe void ProxyMethod(SwiftSelf self, SwiftError* error) {
        int value = *(int*)self.Value;
        Console.WriteLine ("ProxyMethod: {0}", value);
        *error = new SwiftError(self.Value);
        Console.WriteLine("Error: {0}", *((int*)error->Value));
    }

    [Fact]
    public static unsafe void TestUnmanagedCallersOnly()
    {
        int expectedValue = 42;
        SwiftSelf self = new SwiftSelf(&expectedValue);
        SwiftError error;

        NativeFunctionWithCallback(&ProxyMethod, self, &error);

        Assert.True((IntPtr)error.Value != 0, "error.Value is zero!");
        int value = *(int*)error.Value;
        Assert.True(value == expectedValue, string.Format("The value retrieved does not match the expected value. Expected: {0}, Actual: {1}", expectedValue, value));
    }
}