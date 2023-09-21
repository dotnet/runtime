// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

using InteropServices = System.Runtime.InteropServices;
using JitInfo = System.Runtime.JitInfo;

public class JittedMethodsCountingTest
{
    private const int MAX_JITTED_METHODS_ACCEPTED = 70;

    [Fact]
    public static int TestEntryPoint()
    {
        // If DOTNET_ReadyToRun is disabled, then this test ought to be skipped.
        if (!IsReadyToRunEnvSet())
        {
            Console.WriteLine("\nThis test is only supported in ReadyToRun scenarios."
                              + " Skipping...\n");
            return 100;
        }

        if (IsRunCrossgen2Set() && IsRunningOnARM64())
        {
            Console.WriteLine("\nThis test is currently unsupported on ARM64 when"
                              + " RunCrossGen2 is enabled. Skipping...\n");
            return 100;
        }

        Console.WriteLine("\nHello World from Jitted Methods Counting Test!");

        long jits = JitInfo.GetCompiledMethodCount(false);
        Console.WriteLine("Number of Jitted Methods in App: {0}\n", jits);

        return (jits >= 0 && jits <= MAX_JITTED_METHODS_ACCEPTED) ? 100 : 101;
    }

    private static bool IsReadyToRunEnvSet()
    {
        string? dotnetR2R = Environment.GetEnvironmentVariable("DOTNET_ReadyToRun");
        return (string.IsNullOrEmpty(dotnetR2R) || dotnetR2R == "1");
    }

    private static bool IsRunCrossgen2Set()
    {
        string? runCrossgen2 = Environment.GetEnvironmentVariable("RunCrossGen2");
        return (runCrossgen2 == "1");
    }

    private static bool IsRunningOnARM64()
    {
        InteropServices.Architecture thisMachineArch = InteropServices
                                                      .RuntimeInformation
                                                      .OSArchitecture;

        return (thisMachineArch == InteropServices.Architecture.Arm64);
    }
}
