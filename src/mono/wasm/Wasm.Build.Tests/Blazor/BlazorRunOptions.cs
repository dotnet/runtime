// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright;

#nullable enable

namespace Wasm.Build.Tests.Blazor;
public record BlazorRunOptions
(
    BlazorRunHost Host = BlazorRunHost.DotnetRun,
    bool DetectRuntimeFailures = true,
    bool CheckCounter = true,
    Dictionary<string, string>? ServerEnvironment = null,
    Func<IPage, Task>? Test = null,
    Action<IPage>? OnPageLoaded = null,
    Action<IPage, IConsoleMessage>? OnConsoleMessage = null,
    Action<string>? OnServerMessage = null,
    Action<string>? OnErrorMessage = null,
    string Config = "Debug",
    string? ExtraArgs = null,
    string BrowserPath = "",
    string QueryString = ""
);

public enum BlazorRunHost { DotnetRun, WebServer };
