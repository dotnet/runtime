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
        // If either DOTNET_ReadyToRun or DOTNET_EnableHWIntrinsics are disabled
        // (i.e. set to "0"), then this test ought to be skipped.
        if (!IsReadyToRunEnabled() || !IsHardwareIntrinsicsEnabled())
        {
            Console.WriteLine("\nThis test is only supported in ReadyToRun scenarios"
                              + " with Hardware Intrinsics enabled. Skipping...\n");
            return 100;
        }

        // This test is currently not compatible with the R2R-CG2 pipelines on Arm64.
        // if (IsRunCrossgen2Set() && IsRunningOnARM64())
        // {
        //     Console.WriteLine("\nThis test is currently unsupported on ARM64 when"
        //                       + " RunCrossGen2 is enabled. Skipping...\n");
        //     return 100;
        // }

        Console.WriteLine("\nHello World from Jitted Methods Counting Test!");

        // Get the total amount of Jitted Methods.
        long jits = JitInfo.GetCompiledMethodCount(false);

        Console.WriteLine("Number of Jitted Methods in App: {0} - Max Threshold: {1}\n",
                          jits,
                          MAX_JITTED_METHODS_ACCEPTED);

        return (jits >= 0 && jits <= MAX_JITTED_METHODS_ACCEPTED) ? 100 : 101;
    }

    private static bool IsReadyToRunEnabled()
    {
        string? dotnetR2R = Environment.GetEnvironmentVariable("DOTNET_ReadyToRun");
        return (string.IsNullOrEmpty(dotnetR2R) || dotnetR2R == "1");
    }

    private static bool IsRunCrossgen2Set()
    {
        string? runCrossgen2 = Environment.GetEnvironmentVariable("RunCrossGen2");
        return (!string.IsNullOrEmpty(runCrossgen2) && runCrossgen2 == "1");
    }

    private static bool IsRunningOnARM64()
    {
        InteropServices.Architecture thisMachineArch = InteropServices
                                                      .RuntimeInformation
                                                      .OSArchitecture;

        return (thisMachineArch == InteropServices.Architecture.Arm64);
    }

    private static bool IsHardwareIntrinsicsEnabled()
    {
        string? dotnetEnableHWIntrinsics =
            Environment.GetEnvironmentVariable("DOTNET_EnableHWIntrinsic");

        return (string.IsNullOrEmpty(dotnetEnableHWIntrinsics)
                || dotnetEnableHWIntrinsics != "0");
    }
}
