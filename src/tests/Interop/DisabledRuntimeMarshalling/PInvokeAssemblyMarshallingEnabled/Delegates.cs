// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling.PInvokeAssemblyMarshallingEnabled;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public unsafe class DelegatesFromExternalAssembly
{
    public static bool IsWindowsX86Process => OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X86;
    public static bool IsNotWindowsX86Process => !IsWindowsX86Process;

    [ConditionalFact(nameof(IsNotWindowsX86Process))]
    public static void StructWithDefaultNonBlittableFields()
    {
        short s = 42;
        bool b = true;

        var cb = Marshal.GetDelegateForFunctionPointer<CheckStructWithShortAndBoolCallback>(DisabledRuntimeMarshallingNative.GetStructWithShortAndBoolCallback());

        Assert.False(cb(new StructWithShortAndBool(s, b), s, b));
    }
    
    [ConditionalFact(nameof(IsNotWindowsX86Process))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void StructWithDefaultNonBlittableFields_MarshalAsInfo()
    {
        short s = 41;
        bool b = true;

        var cb = Marshal.GetDelegateForFunctionPointer<CheckStructWithShortAndBoolWithMarshalAsAndVariantBoolCallback>(DisabledRuntimeMarshallingNative.GetStructWithShortAndBoolWithVariantBoolCallback());

        Assert.True(cb(new StructWithShortAndBoolWithMarshalAs(s, b), s, b));
    }
}
