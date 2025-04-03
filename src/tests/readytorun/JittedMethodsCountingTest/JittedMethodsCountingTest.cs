// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

using InteropServices = System.Runtime.InteropServices;
using JitInfo = System.Runtime.JitInfo;
using X86 = System.Runtime.Intrinsics.X86;

public class JittedMethodsCountingTest
{
    private const int MAX_JITTED_METHODS_ACCEPTED = 70;

    public static bool IsEnabled => IsReadyToRunEnabled() && IsHardwareIntrinsicsEnabled() && IsSSEEnabled();

    [ConditionalFact(typeof(JittedMethodsCountingTest), nameof(IsEnabled))]
    public static int TestEntryPoint()
    {
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

    private static bool IsSSEEnabled() => X86.Sse.IsSupported || X86.Sse2.IsSupported;
}
