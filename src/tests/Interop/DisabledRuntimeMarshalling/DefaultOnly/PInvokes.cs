// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling.DefaultOnly;

public class PInvokes
{
    [Fact]
    public static void StructWithDefaultNonBlittableFields()
    {
        short s = 42;
        bool b = true;

        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBool(new StructWithShortAndBool(s, b), s, b));

        // We use a the "green check mark" character so that we use both bytes and
        // have a value that can't be accidentally round-tripped.
        char c = '✅';
        Assert.False(DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithWCharAndShort(s, c), s, c));

    }
    [Fact]
    public static void StructWithDefaultNonBlittableFields_MarshalAsInfo()
    {
        short s = 41;
        bool b = true;

        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBool(new StructWithShortAndBoolWithMarshalAs(s, b), s, b));

        // We use a the "green check mark" character so that we use both bytes and
        // have a value that can't be accidentally round-tripped.
        char c = '✅';
        Assert.False(DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithWCharAndShortWithMarshalAs(s, c), s, c));

        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBoolWithVariantBool(new StructWithShortAndBool(s, b), s, b));
    }
}
