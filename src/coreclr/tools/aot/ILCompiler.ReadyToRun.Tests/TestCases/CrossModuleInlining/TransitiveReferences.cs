// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public static class TransitiveReferences
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestTransitiveValue()
    {
        return InlineableLibTransitive.GetExternalValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestNestedTypeAccess()
    {
        return InlineableLibTransitive.GetNestedValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object TestTransitiveTypeCreation()
    {
        return InlineableLibTransitive.CreateExternal();
    }
}
