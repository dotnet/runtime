// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Numerics;
using Xunit;

public class InvalidCallingConvTests
{
    // Dummy class with a dummy attribute
    public class StringClass
    {
        public string value { get; set; }
    }
    private const string SwiftLib = "libSwiftInvalidCallConv.dylib";

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s20SwiftInvalidCallConv10simpleFuncyyF")]
    public static extern void FuncWithTwoSelfParameters(SwiftSelf self1, SwiftSelf self2);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s20SwiftInvalidCallConv10simpleFuncyyF")]
    public static extern void FuncWithTwoErrorParameters(ref SwiftError error1, ref SwiftError error2);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s20SwiftInvalidCallConv10simpleFuncyyF")]
    public static extern void FuncWithMixedParameters(SwiftSelf self1, SwiftSelf self2, ref SwiftError error1, ref SwiftError error2);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s20SwiftInvalidCallConv10simpleFuncyyF")]
    public static extern void FuncWithSwiftErrorAsArg(SwiftError error1);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s20SwiftInvalidCallConv10simpleFuncyyF")]
    public static extern void FuncWithNonPrimitiveArg(StringClass arg1);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s20SwiftInvalidCallConv10simpleFuncyyF")]
    public static extern void FuncWithSIMDArg(Vector4 vec);

    [Fact]
    public static void TestFuncWithTwoSelfParameters()
    {
        // Invalid due to multiple SwiftSelf arguments.
        SwiftSelf self = new SwiftSelf();
        Assert.Throws<InvalidProgramException>(() => FuncWithTwoSelfParameters(self, self));
    }

    [Fact]
    public static void TestFuncWithTwoErrorParameters()
    {
        // Invalid due to multiple SwiftError arguments.
        SwiftError error = new SwiftError();
        Assert.Throws<InvalidProgramException>(() => FuncWithTwoErrorParameters(ref error, ref error));
    }

    [Fact]
    public static void TestFuncWithMixedParameters()
    {
        // Invalid due to multiple SwiftSelf/SwiftError arguments.
        SwiftSelf self = new SwiftSelf();
        SwiftError error = new SwiftError();
        Assert.Throws<InvalidProgramException>(() => FuncWithMixedParameters(self, self, ref error, ref error));
    }

    [Fact]
    public static void TestFuncWithSwiftErrorAsArg()
    {
        // Invalid due to SwiftError not passed as a pointer.
        SwiftError error = new SwiftError();
        Assert.Throws<InvalidProgramException>(() => FuncWithSwiftErrorAsArg(error));
    }

    [Fact]
    public static void TestFuncWithNonPrimitiveArg()
    {
        // Invalid due to a non-primitive argument.
        StringClass arg1 = new StringClass();
        arg1.value = "fail";
        Assert.Throws<InvalidProgramException>(() => FuncWithNonPrimitiveArg(arg1));
    }

    [Fact]
    public static void TestFuncWithSIMDArg()
    {
        // Invalid due to a SIMD argument.
        Vector4 vec = new Vector4(); // Using Vector4 as it is a SIMD type across all architectures for Mono
        Assert.Throws<InvalidProgramException>(() => FuncWithSIMDArg(vec));
    }
}
