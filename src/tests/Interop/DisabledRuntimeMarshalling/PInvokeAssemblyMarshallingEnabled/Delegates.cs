// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling.PInvokeAssemblyMarshallingEnabled;

public unsafe class DelegatesFromExternalAssembly
{
    [Fact]
    public static void StructWithDefaultNonBlittableFields()
    {
        short s = 42;
        bool b = true;

        var callback = Marshal.GetDelegateForFunctionPointer<CheckStructWithShortAndBoolCallback>((IntPtr)DisabledRuntimeMarshallingNative.GetStructWithShortAndBoolCallback());

        Assert.False(callback(new StructWithShortAndBool(s, b), s, b));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void StructWithDefaultNonBlittableFields_MarshalAsInfo()
    {
        short s = 41;
        bool b = true;

        var callback = Marshal.GetDelegateForFunctionPointer<CheckStructWithShortAndBoolWithMarshalAsAndVariantBoolCallback>((IntPtr)DisabledRuntimeMarshallingNative.GetStructWithShortAndBoolWithVariantBoolCallback());

        Assert.True(callback(new StructWithShortAndBoolWithMarshalAs(s, b), s, b));
    }
}
