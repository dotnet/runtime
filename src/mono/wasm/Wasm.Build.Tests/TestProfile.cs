// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

#nullable enable

namespace Workloads.Testing;

public record TestProfile(
    DotnetSdk Sdk,
    string DefaultBuildArgs,
    Dictionary<string, string> EnvVars)
{
    // FIXME: consistent casing
    public string DotNet => Sdk.DotnetPath;
    public string WorkloadPacksDir => Sdk.WorkloadPacksPath;
    public bool HasWorkload => Sdk.Workloads.Length > 0;

    public string GetRuntimePackVersion(string tfm)
        => Sdk.Workloads.FirstOrDefault(w => w.TargetFrameworkVersion == tfm)?.RuntimePackVersion
                ?? throw new Exception($"Cannot find a workload with tfm={tfm} in this test environment");

    // FIXME: rid
    public string GetRuntimePackDir(string tfm)
        => Sdk.Workloads.FirstOrDefault(w => w.TargetFrameworkVersion == tfm) is Workload workload
                ? Path.Combine(Sdk.WorkloadPacksPath, "Microsoft.NETCore.App.Runtime.Mono.browser-wasm", workload.RuntimePackVersion)
                : throw new Exception($"Cannot find a workload with tfm={tfm} in this test environment");

    public string GetRuntimeNativeDir(string tfm)
        => GetRuntimePackDir(tfm) is string packDir
                ? Path.Combine(packDir, "runtimes", "browser-wasm", "native")
                : throw new Exception($"Cannot find a workload with tfm={tfm} in this test environment");
}
