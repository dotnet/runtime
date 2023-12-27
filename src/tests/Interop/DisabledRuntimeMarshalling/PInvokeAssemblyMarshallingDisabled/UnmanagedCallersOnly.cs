// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling.PInvokeAssemblyMarshallingDisabled;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public unsafe class UnmanagedCallersOnly
{
    [Fact]
    public static void UnmanagedCallersOnly_WithNonBlittableParameters_DoesNotMarshal()
    {
        short s = 41;
        bool b = true;

        Assert.True(DisabledRuntimeMarshallingNative.CallCheckStructWithShortAndBoolCallback(&DisabledRuntimeMarshallingNative.CheckStructWithShortAndBoolManaged, new StructWithShortAndBool(s, b), s, b));
    }
}
