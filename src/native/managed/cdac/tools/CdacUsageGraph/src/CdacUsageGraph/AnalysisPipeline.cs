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

    private readonly UsageGraphAnalysisOptions _options;

    public AnalysisPipeline(UsageGraphAnalysisOptions options) => _options = options;

    public static UsageGraph BuildGraph(UsageGraphAnalysisOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!File.Exists(options.ContractsProjectPath))
        {
            throw new InvalidOperationException(
                $"Could not find the cDAC Contracts project at '{options.ContractsProjectPath}'.");
        }

        return BuildGraph(
            CdacCompilationLoader.LoadProject(options.ContractsProjectPath),
            options.SourceRoot,
            options.ContractRegistrationTypeName);
    }

    private static UsageGraph BuildGraph(
        CSharpCompilation compilation,
        string cdacRoot,
        string contractRegistrationTypeName)
    {
        // Phase B: discovery.
        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);
        IReadOnlyList<ContractRegistration> registrations = ContractRegistrationParser.Parse(
            compilation,
            contractRegistrationTypeName);

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
        // Phases A-D.
        UsageGraph graph = BuildGraph(_options);
        Console.WriteLine($"Discovered {graph.DataTypeCount} Data types.");
        Console.WriteLine($"Parsed {graph.Contracts.Count} contract versions.");

        // Phase E: emit.
        string outDir = _options.OutputDirectory
            ?? throw new InvalidOperationException(
                "An output directory is required to emit usage graph reports.");
        Directory.CreateDirectory(outDir);
        Console.WriteLine($"Wrote outputs to {outDir}");
        foreach (IReportWriter writer in s_writers)
            Console.WriteLine("  " + writer.Write(graph, outDir));

        return 0;
    }
}
