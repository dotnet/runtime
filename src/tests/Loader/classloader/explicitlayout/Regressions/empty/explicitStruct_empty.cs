// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
public struct S
{
}

[StructLayout(LayoutKind.Explicit)]
public struct S2
{
}

public class Test_explicitStruct_empty
{
    // Mark as no-inlining so any test failures will show the right stack trace even after
    // we consolidate test assemblies.
    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void EmptyExplicitStructCanBeLoadedAndCreated()
    {
        S s = new S();
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void EmptyExplicitStructCanBeLoadedAndCreatedThroughReflection()
    {
        object s = Activator.CreateInstance(Type.GetType("S2"));
    }
}
