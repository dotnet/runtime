// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CdacUsageGraph;
using CdacUsageGraph.Docs;
using CdacUsageGraph.Model;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.UsageGraph;

[Collection(ContractUsageGraphCollection.Name)]
public sealed partial class ContractDocumentationTests
{
    private const string TodoMeaning = "_TODO: describe_";

    private readonly UsageGraphFixture _fixture;

    public ContractDocumentationTests(UsageGraphFixture fixture) => _fixture = fixture;

    [Fact]
    public void GeneratedUsageSectionsMatchCurrentGraph()
    {
        DocGenerator generator = new(
            _fixture.Usage,
            DocDescriptorMeanings.Load(Locator.MeaningsFile(_fixture.CdacRoot).FullName),
            DocDescriptorOverrides.Load(Locator.OverridesFile(_fixture.CdacRoot).FullName));

        IReadOnlyList<string> drifted = generator.Check(Locator.DocsDirectory(_fixture.CdacRoot).FullName);

        Assert.True(
            drifted.Count == 0,
            $"Generated cDAC usage documentation is stale: {string.Join(", ", drifted)}.{Environment.NewLine}" +
            "To update it, run from the repository root:" + Environment.NewLine +
            "  pwsh .\\src\\native\\managed\\cdac\\tools\\CdacUsageGraph\\generate-docs.ps1" + Environment.NewLine +
            "Review and commit the generated changes. If the generated dependencies are incorrect, " +
            "fix the usage-graph analysis or add a narrowly scoped entry to " +
            "docs\\design\\datacontracts\\data-descriptor-overrides.json.");
    }

    [Fact]
    public void EveryRegisteredContractVersionHasUsageDocumentation()
    {
        DirectoryInfo docsDirectory = Locator.DocsDirectory(_fixture.CdacRoot);
        HashSet<string> documented = [];
        foreach (FileInfo doc in docsDirectory.EnumerateFiles("*.md"))
        {
            foreach (Match marker in UsageMarkerRegex().Matches(File.ReadAllText(doc.FullName)))
                documented.Add($"{marker.Groups["contract"].Value}@{marker.Groups["version"].Value}");
        }

        string[] missing = _fixture.Usage.Contracts
            .Select(contract =>
                $"{contract.Label.Interface.Name[1..]}@{contract.Label.Version}")
            .Where(contractVersion => !documented.Contains(contractVersion))
            .OrderBy(contractVersion => contractVersion, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "Registered cDAC contract versions are missing generated usage documentation: " +
            $"{string.Join(", ", missing)}.{Environment.NewLine}" +
            "Add a usage marker to the corresponding docs\\design\\datacontracts\\<Contract>.md file:" +
            Environment.NewLine +
            "  <!-- BEGIN GENERATED: usage contract=<Contract> version=cN -->" + Environment.NewLine +
            "  <!-- END GENERATED: usage contract=<Contract> version=cN -->" + Environment.NewLine +
            "For a later version, use `diff-from=cN` when documenting changes from an earlier version. " +
            "Then run generate-docs.ps1 to populate the section.");
    }

    [Fact]
    public void GeneratedUsageSectionsHaveNoPlaceholderMeanings()
    {
        DirectoryInfo docsDirectory = Locator.DocsDirectory(_fixture.CdacRoot);
        string[] incomplete = docsDirectory
            .EnumerateFiles("*.md")
            .Where(doc => GeneratedUsageSectionRegex().Matches(File.ReadAllText(doc.FullName))
                .Any(section => section.Value.Contains(TodoMeaning, StringComparison.Ordinal)))
            .Select(doc => doc.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            incomplete.Length == 0,
            $"Generated cDAC usage documentation contains {TodoMeaning}: {string.Join(", ", incomplete)}.{Environment.NewLine}" +
            "Add the missing descriptor or global meaning to " +
            "docs\\design\\datacontracts\\data-descriptor-meanings.json, then run:" + Environment.NewLine +
            "  pwsh .\\src\\native\\managed\\cdac\\tools\\CdacUsageGraph\\generate-docs.ps1");
    }

    [GeneratedRegex(
        "<!-- BEGIN GENERATED: usage contract=(?<contract>[^ ]+) version=(?<version>[^ ]+)(?: diff-from=[^ ]+)? -->",
        RegexOptions.CultureInvariant)]
    private static partial Regex UsageMarkerRegex();

    [GeneratedRegex(
        "<!-- BEGIN GENERATED: usage contract=[^ ]+ version=[^ ]+(?: diff-from=[^ ]+)? -->.*?<!-- END GENERATED: usage contract=[^ ]+ version=[^ ]+(?: diff-from=[^ ]+)? -->",
        RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex GeneratedUsageSectionRegex();
}
