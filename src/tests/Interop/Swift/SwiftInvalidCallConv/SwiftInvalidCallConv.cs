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
        SwiftSelf self = new SwiftSelf(IntPtr.Zero);

        try
        {
            FuncWithTwoSelfParameters(self, self);
            Assert.Fail("FuncWithTwoSelfParameters should have thrown InvalidProgramException");
        }
        catch (InvalidProgramException e) { }
    }

    [Fact]
    public unsafe static void TestFuncWithTwoErrorParameters()
    {
        SwiftError error = new SwiftError(IntPtr.Zero);

        try
        {
            FuncWithTwoErrorParameters(&error, &error);
            Assert.Fail("FuncWithTwoErrorParameters should have thrown InvalidProgramException");
        }
        catch (InvalidProgramException e) { }
    }

    [Fact]
    public unsafe static void TestFuncWithMixedParameters()
    {
        SwiftSelf self = new SwiftSelf(IntPtr.Zero);
        SwiftError error = new SwiftError(IntPtr.Zero);

        try
        {
            FuncWithMixedParameters(self, self, &error, &error);
            Assert.Fail("FuncWithMixedParameters should have thrown InvalidProgramException");
        }
        catch (InvalidProgramException e) { }
    }

    [Fact]
    public unsafe static void TestFuncWithSwiftErrorAsArg()
    {
        SwiftError error = new SwiftError(IntPtr.Zero);

        try
        {
            FuncWithSwiftErrorAsArg(error);
            Assert.Fail("FuncWithSwiftErrorAsArg should have thrown InvalidProgramException");
        }
        catch (InvalidProgramException e) { }
    }
}
