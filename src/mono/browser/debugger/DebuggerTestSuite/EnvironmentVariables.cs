// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#nullable enable

namespace DebuggerTests;

internal static class EnvironmentVariables
{
    public static readonly string? DebuggerTestPath  = Environment.GetEnvironmentVariable("DEBUGGER_TEST_PATH");
    public static readonly string? TestLogPath       = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
    public static readonly bool    SkipCleanup       = GetEnvironmentVariableValue("SKIP_CLEANUP");
    public static readonly bool    WasmEnableThreads = GetEnvironmentVariableValue("WasmEnableThreads");

    private static bool GetEnvironmentVariableValue(string envVariable)
    {
        string? str = Environment.GetEnvironmentVariable(envVariable);
        if (str is null)
            return false;
        
        if (str == "1" || str.ToLower() == "true")
            return true;

        return false;
    }
}
