// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using TestLibrary;

namespace ExtendedLayoutTests;

public static class ExtendedLayout
{
    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void ExtendedLayout_NoExtendedLayoutAttribute()
    {
        Assert.Throws<TypeLoadException>(() => typeof(NoExtendedAttributeOnType));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void ExtendedLayout_InvalidKind()
    {
        Assert.Throws<TypeLoadException>(() => typeof(ExtendedLayoutInvalidKind));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void ExtendedLayout_InlineArray_Invalid()
    {
        Assert.Throws<TypeLoadException>(() => typeof(InlineArrayOnExtendedLayout));
    }
}
