// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace ModuleInitializerTest;

public static class UnsafeAccessorModuleInitializer
{
    [Fact]
    public static void TestEntryPoint()
    {
        InitializerLibrary.Touch();
        Assert.True(FieldHolder.IsSet, "The module initializer did not update the private static field.");
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RvaFieldAccess()
    {
        Assert.Equal(42, RvaFieldHolder.Value);
        Assert.True(AppContext.TryGetSwitch("RvaFieldLibrary.Initialized", out bool initialized) && initialized,
            "Accessing the RVA field did not run its module initializer.");
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ValueTypeInstanceMethodAccess()
    {
        Assert.Equal(42, default(ValueTypeLibrary).GetValue());
        Assert.True(AppContext.TryGetSwitch("ValueTypeLibrary.Initialized", out bool initialized) && initialized,
            "Calling the value-type instance method did not run its module initializer.");
    }
}
