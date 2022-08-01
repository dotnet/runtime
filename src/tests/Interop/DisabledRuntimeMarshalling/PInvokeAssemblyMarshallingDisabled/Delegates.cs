// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling.PInvokeAssemblyMarshallingDisabled;

public unsafe class DelegatesFromExternalAssembly
{
    [Fact]
    public static void StructWithDefaultNonBlittableFields_DoesNotMarshal()
    {
        short s = 42;
        bool b = true;

        var callback = Marshal.GetDelegateForFunctionPointer<CheckStructWithShortAndBoolCallback>((IntPtr)DisabledRuntimeMarshallingNative.GetStructWithShortAndBoolCallback());

        Assert.True(callback(new StructWithShortAndBool(s, b), s, b));
    }

    [Fact]
    public static void StructWithDefaultNonBlittableFields_IgnoresMarshalAsInfo()
    {
        short s = 41;
        bool b = true;

        var callback = Marshal.GetDelegateForFunctionPointer<CheckStructWithShortAndBoolWithVariantBoolCallback>((IntPtr)DisabledRuntimeMarshallingNative.GetStructWithShortAndBoolWithVariantBoolCallback());

        Assert.False(callback(new StructWithShortAndBool(s, b), s, b));
    }
}
