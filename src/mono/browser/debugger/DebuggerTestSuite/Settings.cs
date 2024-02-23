// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#nullable enable

namespace DebuggerTests;

internal static class TestOptions
{
    internal static readonly bool LogToConsole        = Environment.GetEnvironmentVariable("SKIP_LOG_TO_CONSOLE") != "1";
}
