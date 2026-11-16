// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace SuperPmi.Replay;

internal enum SpmiResult
{
    JitFailedToInit = -2,
    GeneralFailure = -1,
    Success = 0,
    Error = 1,
    Diffs = 2,
    Misses = 3,
}

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            JitMetrics.Initialize(SpmiJitInterface.GetJitMetrics());

            // As in C's argv, the first argument is the program as invoked.
            return Replay.Run([Environment.GetCommandLineArgs()[0], .. args]);
        }
        catch (Exception) when (FlushBeforeCrash())
        {
            throw;
        }
        finally
        {
            Logger.Shutdown();
        }
    }

    // Runs as an exception filter, before an unhandled exception terminates the process without running
    // finally blocks, so the output written so far isn't lost.
    private static bool FlushBeforeCrash()
    {
        Logger.Flush();
        return false;
    }
}
