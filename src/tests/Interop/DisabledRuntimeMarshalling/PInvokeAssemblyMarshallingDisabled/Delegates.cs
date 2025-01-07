// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling.PInvokeAssemblyMarshallingDisabled;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public unsafe class DelegatesFromExternalAssembly
{
    [Fact]
    public static void StructWithDefaultNonBlittableFields_DoesNotMarshal()
    {
        short s = 42;
        bool b = true;

        var cb = Marshal.GetDelegateForFunctionPointer<CheckStructWithShortAndBoolCallback>(DisabledRuntimeMarshallingNative.GetStructWithShortAndBoolCallback());
        Assert.True(cb(new StructWithShortAndBool(s, b), s, b));
    }

    [Fact]
    public static void StructWithDefaultNonBlittableFields_IgnoresMarshalAsInfo()
    {
        short s = 41;
        bool b = true;

        var cb = Marshal.GetDelegateForFunctionPointer<CheckStructWithShortAndBoolWithVariantBoolCallback>(DisabledRuntimeMarshallingNative.GetStructWithShortAndBoolWithVariantBoolCallback());
        Assert.False(cb(new StructWithShortAndBool(s, b), s, b));
    }
}
