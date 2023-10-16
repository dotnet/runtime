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

        Console.WriteLine("\nHello World from Jitted Methods Counting Test!");

        long jits = JitInfo.GetCompiledMethodCount(currentThread: false);

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

    private static bool IsHardwareIntrinsicsEnabled()
    {
        string? dotnetEnableHWIntrinsics =
            Environment.GetEnvironmentVariable("DOTNET_EnableHWIntrinsic");

        return (string.IsNullOrEmpty(dotnetEnableHWIntrinsics)
                || dotnetEnableHWIntrinsics != "0");
    }
}
