// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InvalidCSharpNegative;

using Xunit;
using TestLibrary;

public class ValidateNegative
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoLLVMFULLAOT))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    [SkipOnMono("https://github.com/dotnet/runtime/issues/99820")]
    public static void AllowByRefLike_Substituted_For_NonByRefLike_Invalid()
    {
        Console.WriteLine($"{nameof(AllowByRefLike_Substituted_For_NonByRefLike_Invalid)}...");

        Assert.Throws<TypeLoadException>(() => { Exec.TypeSubstitutionInterfaceImplementationAllowByRefLikeIntoNonByRefLike(); });
        Assert.Throws<TypeLoadException>(() => { Exec.OverrideMethodNotByRefLike(); });
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoLLVMFULLAOT))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void AllowByRefLike_Substituted_For_NonByRefLike_Invalid_Class()
    {
        Console.WriteLine($"{nameof(AllowByRefLike_Substituted_For_NonByRefLike_Invalid_Class)}...");

        Assert.Throws<TypeLoadException>(() => { Exec.TypeSubstitutionInheritanceAllowByRefLikeIntoNonByRefLike(); });
        Assert.Throws<TypeLoadException>(() => { Exec.TypeSubstitutionFieldAllowByRefLikeIntoNonByRefLike(); });
    }
}