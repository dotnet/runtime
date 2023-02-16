// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Workloads.Testing;

public sealed record Workload(
    string Id,
    string Version,
    string TargetFrameworkVersion,
    string RuntimePackVersion);
