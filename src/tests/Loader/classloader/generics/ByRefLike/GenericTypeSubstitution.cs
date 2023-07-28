// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InvalidCSharp;

using Xunit;

class GenericTypeSubstitution
{
    [Fact]
    [SkipOnMono("Mono does not support ByRefLike generics yet")]
    public static void AllowByRefLike_Substituted_For_AllowByRefLike()
    {
        Console.WriteLine($"{nameof(AllowByRefLike_Substituted_For_AllowByRefLike)}...");
        
        Console.WriteLine($" -- Instantiate: {Exec.TypeSubstitutionInterfaceImplementationAllowByRefLike()}");
        Console.WriteLine($" -- Instantiate: {Exec.TypeSubstitutionInheritanceAllowByRefLike()}");
        Console.WriteLine($" -- Instantiate: {Exec.TypeSubstitutionFieldAllowByRefLike()}");
    }

    [Fact]
    [SkipOnMono("Mono does not support ByRefLike generics yet")]
    public static void NonByRefLike_Substituted_For_AllowByRefLike()
    {
        Console.WriteLine($" -- Instantiate: {Exec.TypeSubstitutionInterfaceImplementationNonByRefLike()}");
        Console.WriteLine($" -- Instantiate: {Exec.TypeSubstitutionInheritanceNonByRefLike()}");
        Console.WriteLine($" -- Instantiate: {Exec.TypeSubstitutionFieldNonByRefLike()}");
    }

    [Fact]
    [ActiveIssue("To be created", TestRuntimes.CoreCLR)]
    [SkipOnMono("Mono does not support ByRefLike generics yet")]
    public static void AllowByRefLike_Substituted_For_NonByRefLike_Invalid()
    {
        Assert.Throws<TypeLoadException>(() => { Exec.TypeSubstitutionInterfaceImplementationAllowByRefLikeIntoNonByRefLike(); });
        Assert.Throws<TypeLoadException>(() => { Exec.TypeSubstitutionInheritanceAllowByRefLikeIntoNonByRefLike(); });
        Assert.Throws<TypeLoadException>(() => { Exec.TypeSubstitutionFieldAllowByRefLikeIntoNonByRefLike(); });
    }
}