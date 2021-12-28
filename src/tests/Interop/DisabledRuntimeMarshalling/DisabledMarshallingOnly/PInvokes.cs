// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling.DisabledMarshallingOnly;

public class PInvokes
{

    [Fact]
    public static void StructWithDefaultNonBlittableFields_MarshalAsInfo()
    {
        short s = 41;
        bool b = true;

        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBool(new StructWithShortAndBoolWithMarshalAs(s, b), s, b));

        // We use a the "green check mark" character so that we use both bytes and
        // have a value that can't be accidentally round-tripped.
        char c = '✅';
        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithWCharAndShortWithMarshalAs(s, c), s, c));
    }

    [Fact]
    public static void Strings_NotSupported()
    {
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CheckStringWithAnsiCharSet(""));
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CheckStringWithUnicodeCharSet(""));
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CheckStructWithStructWithString(new StructWithString("")));
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.GetStringWithUnicodeCharSet());
    }

    [Fact]
    public static void LayoutClass_NotSupported()
    {
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CheckLayoutClass(new LayoutClass()));
    }

    [Fact]
    public static void NoBooleanNormalization()
    {
        byte byteVal = 42;
        Assert.Equal(byteVal, Unsafe.As<bool, byte>(ref Unsafe.AsRef(DisabledRuntimeMarshallingNative.GetByteAsBool(byteVal))));
    }

    [Fact]
    public static void StructWithDefaultNonBlittableFields_DoesNotDoMarshalling()
    {
        short s = 42;
        bool b = true;

        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBool(new StructWithShortAndBool(s, b), s, b));

        // We use a the "green check mark" character so that we use both bytes and
        // have a value that can't be accidentally round-tripped.
        char c = '✅';
        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithWCharAndShort(s, c), s, c));

        Assert.False(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBoolWithVariantBool_FailureExpected(new StructWithShortAndBool(s, b), s, b));
    }
}
