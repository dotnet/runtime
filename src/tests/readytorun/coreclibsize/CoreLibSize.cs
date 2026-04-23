// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using Xunit;
using TestLibrary;

public class CoreLibSizeTest
{
    // Baseline size of System.Private.CoreLib.dll in bytes for Release builds, measured from the
    // .NET 11 Preview 1 build (the largest observed size across supported platforms, win-x64).
    // To update this baseline after an intentional size increase:
    // measure System.Private.CoreLib.dll in Core_Root from a release build and update this constant.
    private const long ReleaseBaselineSizeBytes = 16_763_144;
    // 100kB above the release baseline
    private const long ReleaseMaxAllowedSizeBytes = ReleaseBaselineSizeBytes + 100_000;

    // Baseline size for Debug/Checked builds, measured from the .NET 11 Preview 1 build.
    // To update this baseline after an intentional size increase:
    // measure System.Private.CoreLib.dll in Core_Root from a debug/checked build and update this constant.
    private const long NonReleaseBaselineSizeBytes = 27_131_904;
    // 100kB above the non-release baseline
    private const long NonReleaseMaxAllowedSizeBytes = NonReleaseBaselineSizeBytes + 100_000;

    [SkipOnMono("Ready-To-Run is a CoreCLR-only feature", TestPlatforms.Any)]
    [ConditionalFact(typeof(Utilities), nameof(Utilities.HasAssemblyFiles))]
    public static int TestEntryPoint()
    {
        string coreLibPath = typeof(string).Assembly.Location;

        AssemblyConfigurationAttribute? configAttr = typeof(string).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        bool isRelease = string.Equals(configAttr?.Configuration, "Release", StringComparison.OrdinalIgnoreCase);
        long baselineSizeBytes = isRelease ? ReleaseBaselineSizeBytes : NonReleaseBaselineSizeBytes;
        long maxAllowedSizeBytes = isRelease ? ReleaseMaxAllowedSizeBytes : NonReleaseMaxAllowedSizeBytes;
        string buildConfiguration = configAttr?.Configuration ?? "Unknown";

        long actualSize = new FileInfo(coreLibPath).Length;
        Console.WriteLine($"System.Private.CoreLib.dll size: {actualSize:N0} bytes");
        Console.WriteLine($"Build configuration: {buildConfiguration}");
        Console.WriteLine($"Baseline size: {baselineSizeBytes:N0} bytes");
        Console.WriteLine($"Maximum allowed size (baseline + 100kB): {maxAllowedSizeBytes:N0} bytes");

        if (actualSize <= maxAllowedSizeBytes)
        {
            Console.WriteLine("PASS: System.Private.CoreLib.dll size is within the allowed threshold.");
            return 100;
        }

        Console.WriteLine($"FAIL: System.Private.CoreLib.dll ({actualSize:N0} bytes) exceeds the maximum " +
            $"allowed size of {maxAllowedSizeBytes:N0} bytes (baseline: {baselineSizeBytes:N0} bytes + 100kB).");
        return 101;
    }
}
