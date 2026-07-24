// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CdacUsageGraph.Model;

namespace CdacUsageGraph;

/// <summary>Analyzes cDAC source and returns its contract usage graph.</summary>
public static class UsageGraphAnalyzer
{
    /// <summary>Builds the cDAC contract usage graph rooted at <paramref name="cdacRoot"/>.</summary>
    /// <param name="cdacRoot">The path to the cDAC source root.</param>
    /// <returns>The analyzed contract usage graph.</returns>
    public static UsageGraph Analyze(string cdacRoot) => AnalysisPipeline.BuildGraph(cdacRoot);
}
