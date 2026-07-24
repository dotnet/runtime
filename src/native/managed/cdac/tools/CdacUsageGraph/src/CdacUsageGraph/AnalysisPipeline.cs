// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CdacUsageGraph.Analysis;
using CdacUsageGraph.Compilation;
using CdacUsageGraph.Discovery;
using CdacUsageGraph.Model;
using CdacUsageGraph.Reporting;
using Microsoft.CodeAnalysis.CSharp;

namespace CdacUsageGraph;

/// <summary>
/// Orchestrates the analysis: load compilation (A) -&gt; discover Data types and registrations (B)
/// -&gt; forward interprocedural walk (C/D) -&gt; emit reports (E).
/// </summary>
internal sealed class AnalysisPipeline
{
    private static readonly IReportWriter[] s_writers =
    [
        new DataGraphMarkdownWriter(),
        new FieldUsageMarkdownWriter(),
        new GlobalUsageMarkdownWriter(),
        new ContractsUsedMarkdownWriter(),
        new JsonReportWriter(),
    ];

    private readonly AnalysisOptions _options;

    public AnalysisPipeline(AnalysisOptions options) => _options = options;

    /// <summary>
    /// Builds the usage graph for the cDAC source rooted at <paramref name="cdacRoot"/> (phases A-D).
    /// Shared by the report pipeline, the <c>docs</c> command and the tests so they all analyze the
    /// same way.
    /// </summary>
    public static UsageGraph BuildGraph(string cdacRoot)
    {
        if (!Directory.Exists(Path.Combine(cdacRoot, CdacSymbols.ContractsProjectDirectory)))
            throw new InvalidOperationException($"Could not find the cDAC Contracts project under '{cdacRoot}'; pass --cdac-root.");

        return BuildGraph(CdacCompilationLoader.Load(cdacRoot), cdacRoot);
    }

    private static UsageGraph BuildGraph(CSharpCompilation compilation, string cdacRoot)
    {
        // Phase B: discovery.
        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);
        IReadOnlyList<ContractRegistration> registrations = ContractRegistrationParser.Parse(compilation);

        // Sanity guard: if discovery found no Data types or no registrations, the compilation
        // input has drifted (renamed anchor types, missing source) -- fail fast rather than emit
        // an empty/misleading graph.
        if (index.Count == 0 || registrations.Count == 0)
            throw new InvalidOperationException(
                $"Sanity check failed: discovered {index.Count} Data types and {registrations.Count} " +
                "contract registrations. The cDAC compilation input is likely broken or has drifted.");

        // Phase C/D: forward interprocedural walk.
        return new UsageWalker(compilation, index).Walk(registrations, cdacRoot);
    }

    public int Run()
    {
        string cdacRoot = _options.CdacRoot.FullName;

        // Phases A-D.
        UsageGraph graph = BuildGraph(cdacRoot);
        Console.WriteLine($"Discovered {graph.DataTypeCount} Data types.");
        Console.WriteLine($"Parsed {graph.Contracts.Count} contract versions.");

        // Phase E: emit.
        string outDir = _options.OutputDirectory.FullName;
        Directory.CreateDirectory(outDir);
        Console.WriteLine($"Wrote outputs to {outDir}");
        foreach (IReportWriter writer in s_writers)
            Console.WriteLine("  " + writer.Write(graph, outDir));

        return 0;
    }
}
