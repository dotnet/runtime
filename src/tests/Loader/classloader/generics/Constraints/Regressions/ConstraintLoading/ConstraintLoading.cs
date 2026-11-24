// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class BypassReadyToRunAttribute : Attribute
    {
    }
}

internal class Base : IComparable<Base>
{
    public int CompareTo(Base other) => 0;
}

public static class ConstraintLoading
{
    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run<TBase, TDerived>(bool recurse = false)
        where TBase : Base
        where TDerived : TBase, IComparable<TDerived>
    {
        if (recurse)
            Run<TDerived, TDerived>();
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Run<Base, Base>();
    }
}
