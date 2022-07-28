// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling;

public unsafe class FunctionPointers
{
    [Fact]
    public static void StructWithDefaultNonBlittableFields_DoesNotMarshal()
    {
        short s = 42;
        bool b = true;
        Assert.True(DisabledRuntimeMarshallingNative.GetStructWithShortAndBoolCallback()(new StructWithShortAndBool(s, b), s, b));
    }

    [Fact]
    public static void StructWithDefaultNonBlittableFields_IgnoresMarshalAsInfo()
    {
        short s = 41;
        bool b = true;

        Assert.False(DisabledRuntimeMarshallingNative.GetStructWithShortAndBoolWithVariantBoolCallback()(new StructWithShortAndBool(s, b), s, b));
    }
}
