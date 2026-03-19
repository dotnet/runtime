// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using Xunit;

using static StringMarshalingTestNative;
using TestLibrary;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public partial class StringInStructTests
{
    private static readonly string InitialString = "Hello World";

    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void ByValue()
    {
        Assert.True(MatchFunctionNameInStruct(new StringInStruct { str = nameof(MatchFunctionNameInStruct)}));
    }

    [ActiveIssue("Crashes during LLVM AOT compilation.", TestRuntimes.Mono)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void ByRef()
    {
        var str = new StringInStruct
        {
            str = InitialString
        };

        ReverseInplaceByrefInStruct(ref str);

        Assert.Equal(Helpers.Reverse(InitialString), str.str);
    }
}
