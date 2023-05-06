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
    GlobalizationMode?  GlobalizationMode         = null,
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
    string?             TargetFramework           = null,
    string?             MainJS                    = null,
    bool                IsBrowserProject          = true,
    IDictionary<string, string>? ExtraBuildEnvironmentVariables = null,
    IEnumerable<string>?      EnvironmentVariablesToRemove = null
)
{
    public IDictionary<string, string> CombineEnvironmentVariables(IDictionary<string, string>? existing = null)
    {
        Dictionary<string, string> envVars = existing is not null ? new(existing) : new();
        if (EnvironmentVariablesToRemove is not null)
        {
            foreach (string keyToRemove in EnvironmentVariablesToRemove)
                envVars.Remove(keyToRemove);
        }
        if (ExtraBuildEnvironmentVariables is not null)
        {
            foreach (var kvp in ExtraBuildEnvironmentVariables!)
                envVars[kvp.Key] = kvp.Value;
        }
        return envVars;
    }
}

