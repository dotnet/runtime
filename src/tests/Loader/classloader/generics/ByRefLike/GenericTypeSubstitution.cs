// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InvalidCSharp;

using Xunit;
using TestLibrary;

public class GenericTypeSubstitution
{
    [ActiveIssue("expected failure: unsupported type with ByRefLike parameters currently fails at AOT compile time, not runtime", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoFULLAOT))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void AllowByRefLike_Substituted_For_AllowByRefLike()
    {
        Console.WriteLine($"{nameof(AllowByRefLike_Substituted_For_AllowByRefLike)}...");

        Console.WriteLine($" -- Instantiate: {Exec.TypeSubstitutionInterfaceImplementationAllowByRefLike()}");
        Console.WriteLine($" -- Instantiate: {Exec.TypeSubstitutionInheritanceAllowByRefLike()}");
        Console.WriteLine($" -- Instantiate: {Exec.TypeSubstitutionFieldAllowByRefLike()}");
    }

    [ActiveIssue("expected failure: unsupported type with ByRefLike parameters currently fails at AOT compile time, not runtime", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoFULLAOT))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void NonByRefLike_Substituted_For_AllowByRefLike()
    {
        Console.WriteLine($"{nameof(NonByRefLike_Substituted_For_AllowByRefLike)}...");

        Console.WriteLine($" -- Instantiate: {Exec.TypeSubstitutionInterfaceImplementationNonByRefLike()}");
        Console.WriteLine($" -- Instantiate: {Exec.TypeSubstitutionInheritanceNonByRefLike()}");
        Console.WriteLine($" -- Instantiate: {Exec.TypeSubstitutionFieldNonByRefLike()}");
    }

    [ActiveIssue("expected failure: unsupported type with ByRefLike parameters currently fails at AOT compile time, not runtime", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoFULLAOT))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]
    [Fact]
    public static void AllowByRefLike_Substituted_For_NonByRefLike()
    {
        Console.WriteLine($"{nameof(AllowByRefLike_Substituted_For_NonByRefLike)}...");
        Exec.TypeSubstitutionFieldAllowNonByRefLikeIntoNonByRefLike();
    }
}