// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CdacUsageGraph;

/// <summary>Locates the cDAC source root and the tool's output directory relative to the binary.</summary>
internal static class Locator
{
    /// <summary>
    /// Finds <c>src/native/managed/cdac</c> by walking up from <paramref name="start"/>
    /// (defaults to the executable directory).
    /// </summary>
    public static DirectoryInfo? FindCdacRoot(string? start = null)
    {
        for (DirectoryInfo? d = new DirectoryInfo(start ?? AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            string candidate = Path.Combine(d.FullName, "src", "native", "managed", "cdac");
            if (Directory.Exists(Path.Combine(candidate, CdacSymbols.ContractsProjectDirectory)))
                return new DirectoryInfo(candidate);
            if (d.Name == "cdac" &&
                Directory.Exists(Path.Combine(d.FullName, CdacSymbols.ContractsProjectDirectory)))
                return d;
        }
        return null;
    }

    /// <summary>The default output directory: the tool root's <c>output/</c>, else <c>./output</c>.</summary>
    public static DirectoryInfo DefaultOutputDirectory(string? start = null)
    {
        DirectoryInfo? cdacRoot = FindCdacRoot(start);
        if (cdacRoot is not null)
        {
            string toolRoot = Path.Combine(
                cdacRoot.FullName,
                "tools",
                "CdacUsageGraph");
            if (File.Exists(Path.Combine(toolRoot, "generate-docs.ps1")))
                return new DirectoryInfo(Path.Combine(toolRoot, "output"));
        }

        return new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "output"));
    }

    /// <summary>The datacontracts docs directory (<c>docs/design/datacontracts</c>) relative to the cDAC root.</summary>
    public static DirectoryInfo DocsDirectory(DirectoryInfo cdacRoot) =>
        new(Path.GetFullPath(Path.Combine(cdacRoot.FullName, "..", "..", "..", "..", "docs", "design", "datacontracts")));

    /// <summary>The data-descriptor meanings sidecar next to the datacontracts docs.</summary>
    public static FileInfo MeaningsFile(DirectoryInfo cdacRoot) =>
        new(Path.Combine(DocsDirectory(cdacRoot).FullName, "data-descriptor-meanings.json"));
}
