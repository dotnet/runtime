// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class AutoLayout
{
    [Fact]
    public static void FieldsWithOffsets()
    {
        Assert.Throws<TypeLoadException>(NoInlineMethod);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type NoInlineMethod()
    {
        return typeof(AutoLayoutTypeWithFieldsWithOffsets);
    }
}
