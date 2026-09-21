// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}
