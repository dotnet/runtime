// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using CdacUsageGraph;

namespace CdacUsageGraph.Compilation;

/// <summary>
/// Loads the real cDAC Contracts project through MSBuild/Roslyn, preserving evaluated Compile
/// items, project references, build properties, analyzer references, and source-generator output.
/// </summary>
internal static class CdacCompilationLoader
{
    public static CSharpCompilation Load(string cdacRoot)
    {
        string projectPath = Path.Combine(
            cdacRoot,
            CdacSymbols.ContractsProjectDirectory,
            CdacSymbols.ContractsProjectFile);
        if (!File.Exists(projectPath))
            throw new InvalidOperationException($"Could not find the cDAC Contracts project at '{projectPath}'.");

        Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Configuration"] = "Debug",
        };
        using MSBuildWorkspace workspace = MSBuildWorkspace.Create(properties);
        List<WorkspaceDiagnostic> failures = new();
        using IDisposable registration = workspace.RegisterWorkspaceFailedHandler(e =>
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                failures.Add(e.Diagnostic);
        });

        Project contractsProject = workspace.OpenProjectAsync(projectPath).GetAwaiter().GetResult();
        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "MSBuildWorkspace failed while loading the cDAC Contracts project:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, failures.Select(d => "  " + d.Message)));
        }

        Microsoft.CodeAnalysis.Compilation? result =
            contractsProject.GetCompilationAsync().GetAwaiter().GetResult();
        if (result is not CSharpCompilation compilation)
            throw new InvalidOperationException(
                $"MSBuildWorkspace did not produce a C# compilation for '{contractsProject.Name}'.");

        List<Diagnostic> errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"The MSBuild-backed '{contractsProject.Name}' compilation has errors:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, errors.Take(20).Select(
                    diagnostic => $"  {diagnostic.Id}: {diagnostic.GetMessage()}")));
        }

        return compilation;
    }
}
