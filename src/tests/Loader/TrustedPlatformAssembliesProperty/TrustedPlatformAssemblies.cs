// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class TrustedPlatformAssemblies
{
    [Fact]
    public static void IsAvailable()
    {
        string tpa = Assert.IsType<string>(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"));
        Assert.Contains(typeof(object).Assembly.Location, tpa, StringComparison.OrdinalIgnoreCase);
    }
}
