// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

public class SystemCoreLibDirectory
{
    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.HasAssemblyFiles))]
    public static void HostProvidedPath()
    {
        string configuredDirectory = AppContext.GetData("SYSTEM_CORELIB_DIRECTORY") as string;
        Assert.False(string.IsNullOrEmpty(configuredDirectory), "SYSTEM_CORELIB_DIRECTORY should be set by the test harness");
        Assert.True(Directory.Exists(configuredDirectory), $"SYSTEM_CORELIB_DIRECTORY should reference an existing directory: '{configuredDirectory}'");

        string spclLocation = Path.GetFullPath(typeof(object).Assembly.Location);
        string expectedLocation = Path.GetFullPath(Path.Combine(configuredDirectory, "System.Private.CoreLib.dll"));

        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        Assert.True(
            string.Equals(spclLocation, expectedLocation, comparison),
            $"SPCL should be loaded from '{expectedLocation}', but was loaded from '{spclLocation}'");

        string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        if (!string.IsNullOrEmpty(coreRoot))
        {
            string defaultPath = Path.Combine(coreRoot, "System.Private.CoreLib.dll");
            Assert.False(
                string.Equals(spclLocation, Path.GetFullPath(defaultPath), comparison),
                $"SPCL should not be loaded from the default location beside coreclr: '{defaultPath}'");
        }
    }
}
