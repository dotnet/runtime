// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling.PInvokeAssemblyMarshallingEnabled;

public class PInvokes
{
    [Fact]
    public static void StructWithDefaultNonBlittableFields()
    {
        short s = 42;
        bool b = true;

        // By default, bool is a 4-byte Windows BOOL, which will cause us to incorrectly marshal back the struct from native code.
        Assert.False(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBool(new StructWithShortAndBool(s, b), s, b));

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
    }

    [Fact]
    public static void StructWithNonBlittableGenericInstantiation_Fails()
    {
        short s = 41;
        char c = '✅';
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithShortAndGeneric<char>(s, c), s, c));
    }

    [Fact]
    public static void StructWithBlittableGenericInstantiation()
    {
        short s = 42;
        // We use a the "green check mark" character so that we use both bytes and
        // have a value that can't be accidentally round-tripped.
        char c = '✅';
        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithShortAndGeneric<short>(s, (short)c), s, (short)c));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void StructWithDefaultNonBlittableFields_MarshalAsInfo_WindowsOnly()
    {
        short s = 41;
        bool b = true;
        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBoolWithVariantBool(new StructWithShortAndBoolWithMarshalAs(s, b), s, b));
    }
}
