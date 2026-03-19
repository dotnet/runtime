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
    // .NET 11 Preview 1 build (the largest observed size across supported platforms as of March 2026).
    // To update this baseline after an intentional size increase:
    // measure System.Private.CoreLib.dll in Core_Root from a release build and update this constant.
    private const long ReleaseBaselineSizeBytes = 16_763_144;
    // 30% above the release baseline: 16,763,144 * 1.30 = ~21,791,887 bytes
    private const long ReleaseMaxAllowedSizeBytes = 21_791_887;

    // Baseline size for Debug/Checked builds, measured from the .NET 11 Preview 1 build.
    // To update this baseline after an intentional size increase:
    // measure System.Private.CoreLib.dll in Core_Root from a debug/checked build and update this constant.
    private const long NonReleaseBaselineSizeBytes = 27_131_904;
    // 30% above the non-release baseline: 27,131,904 * 1.30 = ~35,271,475 bytes
    private const long NonReleaseMaxAllowedSizeBytes = 35_271_475;

    [ActiveIssue("These tests are not supposed to be run with mono.", TestRuntimes.Mono)]
    [Fact]
    public static int TestEntryPoint()
    {
        string? coreRootPath = Environment.GetEnvironmentVariable("CORE_ROOT");
        if (string.IsNullOrEmpty(coreRootPath))
        {
            Console.WriteLine("FAIL: CORE_ROOT environment variable is not set. The ReadyToRun test environment is misconfigured.");
            return 101;
        }

        string coreLibPath = Path.Combine(coreRootPath, "System.Private.CoreLib.dll");
        if (!File.Exists(coreLibPath))
        {
            Console.WriteLine($"FAIL: System.Private.CoreLib.dll not found at '{coreLibPath}'. The ReadyToRun test environment is misconfigured.");
            return 101;
        }

        AssemblyConfigurationAttribute? configAttr = typeof(string).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        bool isRelease = string.Equals(configAttr?.Configuration, "Release", StringComparison.OrdinalIgnoreCase);
        long baselineSizeBytes = isRelease ? ReleaseBaselineSizeBytes : NonReleaseBaselineSizeBytes;
        long maxAllowedSizeBytes = isRelease ? ReleaseMaxAllowedSizeBytes : NonReleaseMaxAllowedSizeBytes;
        string buildConfiguration = configAttr?.Configuration ?? "Unknown";

        long actualSize = new FileInfo(coreLibPath).Length;
        Console.WriteLine($"System.Private.CoreLib.dll size: {actualSize:N0} bytes");
        Console.WriteLine($"Build configuration: {buildConfiguration}");
        Console.WriteLine($"Baseline size: {baselineSizeBytes:N0} bytes");
        Console.WriteLine($"Maximum allowed size (130% of baseline): {maxAllowedSizeBytes:N0} bytes");

        if (actualSize <= maxAllowedSizeBytes)
        {
            Console.WriteLine("PASS: System.Private.CoreLib.dll size is within the allowed threshold.");
            return 100;
        }

        Console.WriteLine($"FAIL: System.Private.CoreLib.dll ({actualSize:N0} bytes) exceeds the maximum " +
            $"allowed size of {maxAllowedSizeBytes:N0} bytes (baseline: {baselineSizeBytes:N0} bytes * 130%).");
        return 101;
    }
}
