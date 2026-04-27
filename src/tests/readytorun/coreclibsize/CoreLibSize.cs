// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;
using TestLibrary;

public class CoreLibSizeTest
{
    // Debug/Checked builds embed additional information compared to Release builds.
    // Estimated from .NET 11 Preview 1 win-x64 data: debug (27,131,904) - release (16,763,144) = 10,368,760 bytes.
    private const long DebugOverReleaseSizeBytes = 10_368_760;

    [SkipOnMono("Ready-To-Run is a CoreCLR-only feature", TestPlatforms.Any)]
    [ConditionalFact(typeof(Utilities), nameof(Utilities.HasAssemblyFiles))]
    public static int TestEntryPoint()
    {
        string coreLibPath = typeof(string).Assembly.Location;

        AssemblyConfigurationAttribute? configAttr = typeof(string).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        bool isRelease = string.Equals(configAttr?.Configuration, "Release", StringComparison.OrdinalIgnoreCase);
        string buildConfiguration = configAttr?.Configuration ?? "Unknown";

        long baselineSizeBytes = GetReleaseBaselineSizeBytes();
        if (!isRelease)
            baselineSizeBytes += DebugOverReleaseSizeBytes;

        long maxAllowedSizeBytes = baselineSizeBytes + 100_000;

        long actualSize = new FileInfo(coreLibPath).Length;
        Console.WriteLine($"System.Private.CoreLib.dll size: {actualSize:N0} bytes");
        Console.WriteLine($"Build configuration: {buildConfiguration}");
        Console.WriteLine($"OS/Architecture: {RuntimeInformation.OSDescription} / {RuntimeInformation.ProcessArchitecture}");
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

    // Returns the per-OS/architecture baseline size for Release builds of System.Private.CoreLib.dll.
    // Baselines are measured from .NET 11 Preview 3 (11.0.0-preview.3.26207.106) NuGet packages.
    // To update after an intentional size increase: download the corresponding
    // Microsoft.NETCore.App.Runtime.<RID> package and measure System.Private.CoreLib.dll.
    private static long GetReleaseBaselineSizeBytes()
    {
        Architecture arch = RuntimeInformation.ProcessArchitecture;

        if (OperatingSystem.IsWindows())
            return arch switch
            {
                Architecture.X64 => 17_213_744,
                Architecture.X86 => 16_832_776,
                Architecture.Arm64 => 18_716_976,
                _ => 18_716_976
            };

        if (OperatingSystem.IsLinux())
            return arch switch
            {
                Architecture.X64 => 16_798_472,
                Architecture.Arm64 => 18_151_696,
                _ => 18_151_696
            };

        if (OperatingSystem.IsMacOS())
            return arch switch
            {
                Architecture.X64 => 16_629_512,
                Architecture.Arm64 => 18_153_232,
                _ => 18_153_232
            };

        // Fallback for other platforms: use the largest known value.
        return 18_716_976;
    }
}
