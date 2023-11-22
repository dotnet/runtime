// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using Xunit;

public class InvalidCallingConvTests
{
    private const string SwiftLib = "libSwiftInvalidCallConv.dylib";

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s20SwiftInvalidCallConv10simpleFuncyyF")]
    public static extern nint FuncWithTwoSelfParameters(SwiftSelf self1, SwiftSelf self2);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s20SwiftInvalidCallConv10simpleFuncyyF")]
    public unsafe static extern nint FuncWithTwoErrorParameters(SwiftError* error1, SwiftError* error2);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s20SwiftInvalidCallConv10simpleFuncyyF")]
    public unsafe static extern nint FuncWithMixedParameters(SwiftSelf self1, SwiftSelf self2, SwiftError* error1, SwiftError* error2);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s20SwiftInvalidCallConv10simpleFuncyyF")]
    public static extern nint FuncWithSwiftErrorAsArg(SwiftError error1);

    [Fact]
    public static void TestFuncWithTwoSelfParameters()
    {
        // Invalid due to multiple SwiftSelf arguments.
        SwiftSelf self = new SwiftSelf();
        Assert.Throws<InvalidProgramException>(() => FuncWithTwoSelfParameters(self, self));
    }

    [Fact]
    public unsafe static void TestFuncWithTwoErrorParameters()
    {
        // Invalid due to multiple SwiftError arguments.
        SwiftError error = new SwiftError();
        SwiftError* errorPtr = &error;
        Assert.Throws<InvalidProgramException>(() => FuncWithTwoErrorParameters(errorPtr, errorPtr));
    }

    [Fact]
    public unsafe static void TestFuncWithMixedParameters()
    {
        // Invalid due to multiple SwiftSelf/SwiftError arguments.
        SwiftSelf self = new SwiftSelf();
        SwiftError error = new SwiftError();
        SwiftError* errorPtr = &error;
        Assert.Throws<InvalidProgramException>(() => FuncWithMixedParameters(self, self, errorPtr, errorPtr));
    }

    [Fact]
    public unsafe static void TestFuncWithSwiftErrorAsArg()
    {
        // Invalid due to SwiftError not passed as a pointer.
        SwiftError error = new SwiftError();
        Assert.Throws<InvalidProgramException>(() => FuncWithSwiftErrorAsArg(error));
    }
}
