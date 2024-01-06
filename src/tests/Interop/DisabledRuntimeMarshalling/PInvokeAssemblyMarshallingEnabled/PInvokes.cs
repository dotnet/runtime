// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling.PInvokeAssemblyMarshallingEnabled;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class PInvokes
{
    public static bool IsWindowsX86Process => OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X86;
    public static bool IsNotWindowsX86Process => !IsWindowsX86Process;

    [ConditionalFact(nameof(IsNotWindowsX86Process))]
    public static void StructWithDefaultNonBlittableFields_Bool()
    {
        short s = 42;
        bool b = true;

        // By default, bool is a 4-byte Windows BOOL, which will cause us to incorrectly marshal back the struct from native code.
        Assert.False(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBool(new StructWithShortAndBool(s, b), s, b));
    }

    [Fact]
    [SkipOnMono("Mono doesn't support marshalling a .NET Char to a C char (only a char16_t).")]
    public static void StructWithDefaultNonBlittableFields_Char()
    {
        short s = 42;
        // We use a the "green check mark" character so that we use both bytes and
        // have a value that can't be accidentally round-tripped.
        char c = '\u2705';
        Assert.False(DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithWCharAndShort(s, c), s, c));

    }

    [ConditionalFact(nameof(IsNotWindowsX86Process))]
    public static void StructWithDefaultNonBlittableFields_Bool_MarshalAsInfo()
    {
        short s = 41;
        bool b = true;

        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBool(new StructWithShortAndBoolWithMarshalAs(s, b), s, b));
    }

    [Fact]
    [SkipOnMono("Mono doesn't support marshalling a .NET Char to a C char (only a char16_t).")]
    public static void StructWithDefaultNonBlittableFields_Char_MarshalAsInfo()
    {
        short s = 41;
        // We use a the "green check mark" character so that we use both bytes and
        // have a value that can't be accidentally round-tripped.
        char c = '\u2705';
        Assert.False(DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithWCharAndShortWithMarshalAs(s, c), s, c));
    }

    [ConditionalFact(nameof(IsWindowsX86Process))]
    public static void EntryPoint_With_StructWithDefaultNonBlittableFields_NotFound()
    {
        short s = 42;
        bool b = true;

        // By default, bool is a 4-byte Windows BOOL, which will make the calculation of stack space for the stdcall calling convention
        // incorrect, causing the entry point to not be found.
        Assert.Throws<EntryPointNotFoundException>(() => DisabledRuntimeMarshallingNative.CheckStructWithShortAndBool(new StructWithShortAndBool(s, b), s, b));
    }

    [Fact]
    [SkipOnMono("Mono supports non-blittable generic instantiations in P/Invokes")]
    public static void StructWithNonBlittableGenericInstantiation_Fails()
    {
        short s = 41;
        char c = '\u2705';
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithShortAndGeneric<char>(s, c), s, c));
    }

    [Fact]
    public static void StructWithBlittableGenericInstantiation()
    {
        short s = 42;
        // We use a the "green check mark" character so that we use both bytes and
        // have a value that can't be accidentally round-tripped.
        char c = '\u2705';
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
