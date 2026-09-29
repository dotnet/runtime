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
public partial class StringTests
{
    private static readonly string InitialString = "Hello World";

    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void String_ByValue()
    {
        Assert.True(MatchFunctionName(nameof(MatchFunctionName)));
    }

    [ActiveIssue("Crashes during LLVM AOT compilation.", TestRuntimes.Mono)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void String_ByRef()
    {
        string funcNameLocal = nameof(MatchFunctionNameByRef);
        Assert.True(MatchFunctionNameByRef(ref funcNameLocal));
    }

    [ActiveIssue("Crashes during LLVM AOT compilation.", TestRuntimes.Mono)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void String_ByRef_InCallback()
    {
        Assert.True(ReverseInCallback(InitialString, (string str, out string rev) => rev = Helpers.Reverse(InitialString)));
    }

    [ActiveIssue("Crashes during LLVM AOT compilation.", TestRuntimes.Mono)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void String_InPlace_ByRef()
    {
        string reversed = InitialString;
        ReverseInplaceByref(ref reversed);
        Assert.Equal(Helpers.Reverse(InitialString), reversed);
    }

    [ActiveIssue("Crashes during LLVM AOT compilation.", TestRuntimes.Mono)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void String_Out()
    {
        Reverse(InitialString, out string reversed);
        Assert.Equal(Helpers.Reverse(InitialString), reversed);
    }

    [ActiveIssue("Crashes during LLVM AOT compilation.", TestRuntimes.Mono)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void String_Return()
    {
        Assert.Equal(Helpers.Reverse(InitialString), ReverseAndReturn(InitialString));
    }

    [ActiveIssue("Crashes during LLVM AOT compilation.", TestRuntimes.Mono)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void String_Callback_ByValue()
    {
        Assert.True(VerifyReversed(InitialString, (orig, rev) => rev == Helpers.Reverse(orig)));
    }

    [ActiveIssue("Crashes during LLVM AOT compilation.", TestRuntimes.Mono)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void String_Callback_ByRef()
    {
        Assert.True(ReverseInCallback(InitialString, (string str, out string rev) => rev = Helpers.Reverse(InitialString)));
    }

    [ActiveIssue("Crashes during LLVM AOT compilation.", TestRuntimes.Mono)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void String_Callback_Return()
    {
        Assert.True(ReverseInCallbackReturned(InitialString, str => Helpers.Reverse(str)));
    }
}
