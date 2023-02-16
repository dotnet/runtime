// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;

#nullable enable

namespace Workloads.Testing;

// InstallWork* can generate a dotnet-workload.descriptor in the dotnet path
// which has details of versions, workloads etc

// TODO: Merge DotnetEnv and TestEnv..
// TODO: consider adding GetRuntime* etc methods to TestConfiguration too.. so they can be accessed from s_buildenv
//       in static methods -NO can't do that! because we have net7.0 in multiple dotnet paths for example!
public sealed record DotnetSdk(
    string Id,
    string Version,
    string RelativePath,
    [property: JsonIgnore] string? FullPath,
    Workload[] Workloads)
{
    // FIXME: throw if fullPath is not set yet?
    // TODO: these will call p.combine every time
    public string DotnetPath => Path.Combine(FullPath ?? RelativePath, RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                                                    ? "dotnet.exe"
                                                                    : "dotnet");
    public string WorkloadPacksPath => Path.Combine(FullPath ?? RelativePath, "packs");
}

public sealed record SdksForTestingManifest(DotnetSdk[] Sdks);
