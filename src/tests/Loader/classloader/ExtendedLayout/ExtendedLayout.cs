// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace ExtendedLayoutTests;

public static class ExtendedLayout
{
    [Fact]
    public static void ExtendedLayout_NoExtendedLayoutAttribute()
    {
        Assert.Throws<TypeLoadException>(() => typeof(NoExtendedAttributeOnType));
    }

    [Fact]
    public static void ExtendedLayout_InvalidKind()
    {
        Assert.Throws<TypeLoadException>(() => typeof(ExtendedLayoutInvalidKind));
    }

    [Fact]
    public static void ExtendedLayout_InlineArray_Invalid()
    {
        Assert.Throws<TypeLoadException>(() => typeof(InlineArrayOnExtendedLayout));
    }
}
