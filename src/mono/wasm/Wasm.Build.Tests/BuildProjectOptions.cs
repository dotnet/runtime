// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

#nullable enable

namespace Wasm.Build.Tests;

public record BuildProjectOptions
(
    Action?             InitProject               = null,
    bool?               DotnetWasmFromRuntimePack = null,
    GlobalizationMode   GlobalizationMode         = GlobalizationMode.Sharded,
    string?             PredefinedIcudt           = null,
    bool                UseCache                  = true,
    bool                ExpectSuccess             = true,
    bool                AssertAppBundle           = true,
    bool                CreateProject             = true,
    bool                Publish                   = true,
    bool                BuildOnlyAfterPublish     = true,
    bool                HasV8Script               = true,
    string?             Verbosity                 = null,
    string?             Label                     = null,
    string              TargetFramework           = BuildTestBase.DefaultTargetFramework,
    string?             MainJS                    = null,
    bool                IsBrowserProject          = true,
    IDictionary<string, string>? ExtraBuildEnvironmentVariables = null,
    string?             BinFrameworkDir           = null
);
