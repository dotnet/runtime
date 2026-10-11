// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

public class TrustedPlatformAssemblies
{
    public static bool HasSystemCoreLibFile => !string.IsNullOrEmpty(typeof(object).Assembly.Location);

    [ConditionalFact(typeof(TrustedPlatformAssemblies), nameof(HasSystemCoreLibFile))]
    public static void IsAvailable()
    {
        string tpa = Assert.IsType<string>(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"));
        string coreLibLocation = typeof(object).Assembly.Location;

        // On macOS, /tmp is a symlink to /private/tmp. Assembly.Location resolves the path, while host-provided TPA paths do not.
        if (OperatingSystem.IsMacOS() && coreLibLocation.StartsWith("/private/tmp/", StringComparison.Ordinal))
        {
            coreLibLocation = coreLibLocation["/private".Length..];
        }

        Assert.Contains(coreLibLocation, tpa.Split(Path.PathSeparator), StringComparer.OrdinalIgnoreCase);
    }
}
