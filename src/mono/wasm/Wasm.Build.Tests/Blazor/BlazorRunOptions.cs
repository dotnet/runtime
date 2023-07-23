// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public record BlazorRunOptions
(
    string runArgs,
    string workingDirectory,
    Func<IPage, Task>? test = null,
    Action<IConsoleMessage>? onConsoleMessage = null,
    bool detectRuntimeFailures = true
);
