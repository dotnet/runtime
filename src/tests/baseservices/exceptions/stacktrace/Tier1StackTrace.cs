// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public static class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        string tier0StackTrace = Capture(true);
        PromoteToTier1(() => Capture(false));
        string tier1StackTrace = Capture(true);
        if (tier0StackTrace != tier1StackTrace)
        {
            throw new Exception($"Stack trace mismatch:\n------\nTier 0:\n------\n{tier0StackTrace}\n------\nTier 1:\n------\n{tier1StackTrace}");
        }
    }

    private static void PromoteToTier1(Action action)
    {
        // Call the method once to register a call for call counting
        action();

        // Allow time for call counting to begin
        Thread.Sleep(500);

        // Call the method enough times to trigger tier 1 promotion
        for (int i = 0; i < 100; i++)
        {
            action();
        }

        // Allow time for the method to be jitted at tier 1
        Thread.Sleep(500);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Capture(bool doWork)
    {
        if (!doWork)
        {
            return null;
        }

        string stackTrace = new StackTrace(true).ToString().Trim();

        // Remove everything past the test entrypoint line
        int entrypointIndex = stackTrace.IndexOf("TestEntryPoint");
        if (entrypointIndex == -1)
        {
            return null;
        }
        while (entrypointIndex > 0 && stackTrace[entrypointIndex - 1] != '\n')
        {
            entrypointIndex--;
        }
        return stackTrace.Substring(0, entrypointIndex);
    }
}
