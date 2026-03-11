// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using TestLibrary;

public class CoreLibSizeTest
{
    // Baseline size of System.Private.CoreLib.dll in bytes, measured from the .NET 11 Preview 1 build
    // (the largest observed size across supported platforms as of March 2026).
    // The test validates that the DLL has not grown more than 30% from this baseline.
    private const long BaselineSizeBytes = 16_763_144;
    // 30% above the baseline: 16,763,144 * 1.30 = ~21,791,887 bytes
    private const long MaxAllowedSizeBytes = 21_791_887;

    [ActiveIssue("These tests are not supposed to be run with mono.", TestRuntimes.Mono)]
    [Fact]
    public static int TestEntryPoint()
    {
        string? coreRootPath = Environment.GetEnvironmentVariable("CORE_ROOT");
        if (string.IsNullOrEmpty(coreRootPath))
        {
            Console.WriteLine("CORE_ROOT environment variable is not set, skipping test.");
            return 100;
        }

        string corLibPath = Path.Combine(coreRootPath, "System.Private.CoreLib.dll");
        if (!File.Exists(corLibPath))
        {
            Console.WriteLine($"System.Private.CoreLib.dll not found at {corLibPath}, skipping test.");
            return 100;
        }

        long actualSize = new FileInfo(corLibPath).Length;
        Console.WriteLine($"System.Private.CoreLib.dll size: {actualSize:N0} bytes");
        Console.WriteLine($"Baseline size: {BaselineSizeBytes:N0} bytes");
        Console.WriteLine($"Maximum allowed size (130% of baseline): {MaxAllowedSizeBytes:N0} bytes");

        if (actualSize <= MaxAllowedSizeBytes)
        {
            Console.WriteLine("PASS: System.Private.CoreLib.dll size is within the allowed threshold.");
            return 100;
        }

        Console.WriteLine($"FAIL: System.Private.CoreLib.dll ({actualSize:N0} bytes) exceeds the maximum " +
            $"allowed size of {MaxAllowedSizeBytes:N0} bytes (baseline: {BaselineSizeBytes:N0} bytes * 130%).");
        return 101;
    }
}
