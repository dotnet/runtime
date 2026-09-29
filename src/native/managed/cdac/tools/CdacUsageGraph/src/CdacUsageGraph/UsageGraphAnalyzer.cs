// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CdacUsageGraph.Model;

namespace CdacUsageGraph;

/// <summary>Analyzes cDAC source and returns its contract usage graph.</summary>
public static class UsageGraphAnalyzer
{
    /// <summary>
    /// Builds a contract usage graph for a cDAC contracts project and registration type.
    /// </summary>
    /// <param name="options">The project and semantic anchors for the analysis.</param>
    /// <returns>The analyzed contract usage graph.</returns>
    public static UsageGraph Analyze(UsageGraphAnalysisOptions options) =>
        AnalysisPipeline.BuildGraph(options);
}

/// <summary>Inputs needed to analyze a cDAC contracts project.</summary>
/// <param name="ContractsProjectPath">Full path to the contracts project to load with MSBuild.</param>
/// <param name="ContractRegistrationTypeName">
/// Metadata name of the type containing calls to <c>ContractRegistry.Register</c>.
/// </param>
/// <param name="SourceRoot">Source root recorded in the resulting usage graph.</param>
/// <param name="OutputDirectory">
/// Optional report output directory used by the command-line pipeline.
/// </param>
public sealed record UsageGraphAnalysisOptions(
    string ContractsProjectPath,
    string ContractRegistrationTypeName,
    string SourceRoot,
    string? OutputDirectory = null);
