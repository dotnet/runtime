// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using CdacUsageGraph.Docs;
using CdacUsageGraph.Model;

namespace CdacUsageGraph;

/// <summary>Defines the command-line surface. Kept separate so it can be unit-tested.</summary>
internal static class Commands
{
    public static RootCommand Create()
    {
        RootCommand root = new RootCommand("Extract the cDAC contract -> Data usage graph.");

        Option<DirectoryInfo> cdacRootOption = CdacRootOption();
        Option<DirectoryInfo> outputOption = new Option<DirectoryInfo>("--output", "-o")
        {
            Description = "Directory to write the report artifacts into. Defaults to the tool's output/ folder.",
            DefaultValueFactory = _ => Locator.DefaultOutputDirectory(),
        };

        root.Options.Add(cdacRootOption);
        root.Options.Add(outputOption);
        root.SetAction(parseResult =>
        {
            AnalysisOptions options = new AnalysisOptions(
                parseResult.GetValue(cdacRootOption)!,
                parseResult.GetValue(outputOption)!);
            return new AnalysisPipeline(options).Run();
        });

        root.Subcommands.Add(CreateDocsCommand());
        return root;
    }

    private static Command CreateDocsCommand()
    {
        Option<DirectoryInfo> cdacRootOption = CdacRootOption();
        Option<DirectoryInfo?> docsDirOption = new Option<DirectoryInfo?>("--docs-dir")
        {
            Description = "The docs/design/datacontracts directory. Auto-detected relative to the cDAC root if omitted.",
        };
        Option<FileInfo?> meaningsOption = new Option<FileInfo?>("--meanings")
        {
            Description = "The data-descriptor-meanings.json sidecar. Auto-detected next to the docs if omitted.",
        };
        Option<FileInfo?> overridesOption = new Option<FileInfo?>("--overrides")
        {
            Description = "The data-descriptor-overrides.json sidecar. Auto-detected next to the docs if omitted.",
        };
        Option<bool> checkOption = new Option<bool>("--check")
        {
            Description = "Fail (exit 1) if any doc's generated blocks are out of date instead of rewriting them.",
        };

        Command docs = new Command("docs", "Fill (or --check) the generated marker blocks in the datacontracts docs.")
        {
            cdacRootOption,
            docsDirOption,
            meaningsOption,
            overridesOption,
            checkOption,
        };

        docs.SetAction(parseResult =>
        {
            DirectoryInfo cdacRoot = parseResult.GetValue(cdacRootOption)!;
            DirectoryInfo docsDir = parseResult.GetValue(docsDirOption) ?? Locator.DocsDirectory(cdacRoot);
            FileInfo meanings = parseResult.GetValue(meaningsOption) ?? Locator.MeaningsFile(cdacRoot);
            FileInfo overrides = parseResult.GetValue(overridesOption) ?? Locator.OverridesFile(cdacRoot);

            UsageGraph graph = AnalysisPipeline.BuildGraph(cdacRoot.FullName);
            DocGenerator generator = new DocGenerator(
                graph,
                DocDescriptorMeanings.Load(meanings.FullName),
                DocDescriptorOverrides.Load(overrides.FullName));

            if (parseResult.GetValue(checkOption))
            {
                IReadOnlyList<string> drift = generator.Check(docsDir.FullName);
                if (drift.Count > 0)
                {
                    Console.Error.WriteLine(
                        $"Data-descriptor docs are stale for: {string.Join(", ", drift)}." +
                        " Run 'CdacUsageGraph docs' (without --check) to update.");
                    return 1;
                }
                Console.WriteLine("Docs are up to date.");
                return 0;
            }

            IReadOnlyList<string> changed = generator.Emit(docsDir.FullName);
            Console.WriteLine(changed.Count > 0 ? $"Updated: {string.Join(", ", changed)}" : "No changes.");
            return 0;
        });

        return docs;
    }

    private static Option<DirectoryInfo> CdacRootOption() =>
        new Option<DirectoryInfo>("--cdac-root", "-c")
        {
            Description = "The cDAC source root (src/native/managed/cdac). Auto-detected if omitted.",
            DefaultValueFactory = _ => Locator.FindCdacRoot()
                ?? throw new InvalidOperationException("Could not locate the cDAC root; pass --cdac-root."),
        };
}
