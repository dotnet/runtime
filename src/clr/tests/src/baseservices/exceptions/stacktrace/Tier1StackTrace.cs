// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

internal static class Program
{
    private static int Main()
    {
        const int Pass = 100, Fail = 1;

        string tier0StackTrace = Capture(true);
        PromoteToTier1(() => Capture(false));
        string tier1StackTrace = Capture(true);
        return tier0StackTrace == tier1StackTrace ? Pass : Fail;
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

        // Remove the last line of the stack trace, which would correspond with Main()
        int lastNewLineIndex = stackTrace.LastIndexOf('\n');
        if (lastNewLineIndex == -1)
        {
            return null;
        }
        return stackTrace.Substring(0, lastNewLineIndex).Trim();
    }
}
