// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using TestLibrary;

[StructLayout(LayoutKind.Explicit)]
public struct S
{
}

[StructLayout(LayoutKind.Explicit)]
public struct S2
{
}

[StructLayout(LayoutKind.Explicit)]
public class C
{
}

[StructLayout(LayoutKind.Explicit)]
public class C2
{
}

public class Test_explicitStruct_empty
{
    // Mark as no-inlining so any test failures will show the right stack trace even after
    // we consolidate test assemblies.
    [ActiveIssue("needs triage", TestPlatforms.tvOS)]
    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void EmptyExplicitStructCanBeLoadedAndCreated()
    {
        S s = new S();
    }

    [ActiveIssue("needs triage", TestPlatforms.tvOS)]
    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void EmptyExplicitStructCanBeLoadedAndCreatedThroughReflection()
    {
        object s = Activator.CreateInstance(Type.GetType("S2"));
    }

    [ActiveIssue("needs triage", TestPlatforms.tvOS)]
    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void EmptyExplicitClassCanBeLoadedAndCreated()
    {
        C c = new C();
    }

    [ActiveIssue("needs triage", TestPlatforms.tvOS)]
    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void EmptyExplicitClassCanBeLoadedAndCreatedThroughReflection()
    {
        object c = Activator.CreateInstance(Type.GetType("C2"));
    }
}
