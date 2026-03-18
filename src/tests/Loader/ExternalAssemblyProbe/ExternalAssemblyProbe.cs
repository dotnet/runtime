// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public unsafe class ExternalAssemblyProbe
{
    [Fact]
    public static void ExternalAppAssemblies()
    {
        // In order to get to this point, the runtime must have been able to find the app assemblies
        // Check that the TPA is indeed empty - that is, the runtime is not relying on that property.
        string tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        Assert.True(string.IsNullOrEmpty(tpa), "TRUSTED_PLATFORM_ASSEMBLIES should be empty");
    }
}
