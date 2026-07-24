// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CdacUsageGraph;

/// <summary>Parsed, validated command-line arguments for one analysis run.</summary>
internal sealed record AnalysisOptions(
    DirectoryInfo CdacRoot,
    DirectoryInfo OutputDirectory);
