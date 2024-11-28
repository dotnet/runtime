// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright;

#nullable enable

namespace Wasm.Build.Tests;
public record RunOptions
(
    Configuration Configuration,
    RunHost Host = RunHost.DotnetRun,
    bool DetectRuntimeFailures = true,
    bool CheckCounter = true,
    Dictionary<string, string>? ServerEnvironment = null,
    Func<IPage, Task>? Test = null,
    string? TestScenario = null,
    Dictionary<string, string>? BrowserQueryString = null,
    Action<string, string>? OnConsoleMessage = null,
    Action<string>? OnServerMessage = null,
    Action<string>? OnErrorMessage = null,
    string ExtraArgs = "",
    string BrowserPath = "",
    string Locale = "en-US",
    int? ExpectedExitCode = 0,
    string CustomBundleDir = "",

    Func<RunOptions, IPage, Task>? ExecuteAfterLoaded = null
);

public enum RunHost { DotnetRun, WebServer };
