// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Playwright;

#nullable enable

namespace Wasm.Build.Tests;

public record BrowserRunOptions : RunOptions
{
    public string? TestScenario { get; init; }

    public BrowserRunOptions(
        Configuration               Configuration,
        bool                        AOT                     = false,
        RunHost                     Host                    = RunHost.DotnetRun,
        bool                        DetectRuntimeFailures   = true,
        Dictionary<string, string>? ServerEnvironment       = null,
        NameValueCollection?        BrowserQueryString      = null,
        Action<string, string>?     OnConsoleMessage        = null,
        Action<string>?             OnServerMessage         = null,
        Action<string>?             OnErrorMessage          = null,
        string                      ExtraArgs               = "",
        string                      BrowserPath             = "",
        string                      Locale                  = "en-US",
        int?                        ExpectedExitCode        = 0,
        string                      CustomBundleDir         = "",
        string?                     TestScenario            = null
    ) : base(
        Configuration,
        AOT,
        Host,
        DetectRuntimeFailures,
        ServerEnvironment,
        BrowserQueryString,
        OnConsoleMessage,
        OnServerMessage,
        OnErrorMessage,
        ExtraArgs,
        BrowserPath,
        Locale,
        ExpectedExitCode,
        CustomBundleDir
    )
    {
        this.TestScenario = TestScenario;
    }
}
