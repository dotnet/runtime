// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#nullable enable

namespace DebuggerTests;

internal static class EnvironmentVariables
{
    public static readonly string? DebuggerTestPath = Environment.GetEnvironmentVariable("DEBUGGER_TEST_PATH");
    public static readonly string? TestLogPath      = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
}
