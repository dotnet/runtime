// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using CdacUsageGraph.Model;

namespace CdacUsageGraph.Docs;

/// <summary>
/// Fills the <c>&lt;!-- BEGIN GENERATED: ... --&gt;</c> marker blocks in
/// <c>docs/design/datacontracts/*.md</c> from a <see cref="UsageGraph"/> merged with
/// <see cref="DocDescriptorMeanings"/>. This is the single source of truth for the generated
/// tables; both the <c>docs</c> command and the doc-drift unit test use it, and
/// <c>generate-docs.ps1</c> is a thin wrapper around the command.
/// </summary>
internal sealed partial class DocGenerator
{
    private const string TypeSizeField = "<type size>";

    private readonly UsageGraph _graph;
    private readonly DocDescriptorMeanings _meanings;

    public DocGenerator(UsageGraph graph, DocDescriptorMeanings meanings)
    {
        _graph = graph;
        _meanings = meanings;
    }

    /// <summary>Rewrites every marker block in <paramref name="docsDir"/>; returns the changed file names.</summary>
    public IReadOnlyList<string> Emit(string docsDir)
    {
        List<string> changed = new();
        foreach (FileInfo md in EnumerateMarkedDocs(docsDir))
        {
            string text = File.ReadAllText(md.FullName);
            string rewritten = Rewrite(text);
            if (!string.Equals(rewritten, text, StringComparison.Ordinal))
            {
                File.WriteAllText(md.FullName, rewritten);
                changed.Add(md.Name);
            }
        }
        return changed;
    }

    /// <summary>Returns the names of docs whose marker blocks are out of date (empty = up to date).</summary>
    public IReadOnlyList<string> Check(string docsDir)
    {
        List<string> drifted = new();
        foreach (FileInfo md in EnumerateMarkedDocs(docsDir))
        {
            string text = File.ReadAllText(md.FullName);
            if (!string.Equals(Rewrite(text), text, StringComparison.Ordinal))
                drifted.Add(md.Name);
        }
        return drifted;
    }

    private static IEnumerable<FileInfo> EnumerateMarkedDocs(string docsDir)
    {
        foreach (FileInfo md in new DirectoryInfo(docsDir).EnumerateFiles("*.md").OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            string text = File.ReadAllText(md.FullName);
            if (text.Contains("BEGIN GENERATED:", StringComparison.Ordinal) ||
                text.Contains("END GENERATED:", StringComparison.Ordinal))
                yield return md;
        }
    }

    private string Rewrite(string text)
    {
        ValidateMarkers(text);
        string newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        return MarkerRegex().Replace(text, m =>
        {
            string kind = m.Groups["kind"].Value;
            string contract = m.Groups["c"].Value;
            string version = m.Groups["v"].Value;
            string diffFrom = m.Groups["b"].Value;
            string options = diffFrom.Length == 0 ? "" : $" diff-from={diffFrom}";
            string begin = $"<!-- BEGIN GENERATED: {kind} contract={contract} version={version}{options} -->";
            string end = $"<!-- END GENERATED: {kind} contract={contract} version={version}{options} -->";
            IReadOnlyList<string> table = diffFrom.Length == 0
                ? BuildUsage(contract, version)
                : BuildUsageDiff(contract, version, diffFrom);
            return string.Join(newline, new[] { begin }.Concat(table).Append(end));
        });
    }

    private static void ValidateMarkers(string text)
    {
        MatchCollection begins = BeginMarkerRegex().Matches(text);
        MatchCollection ends = EndMarkerRegex().Matches(text);
        MatchCollection blocks = MarkerRegex().Matches(text);
        if (begins.Count != blocks.Count || ends.Count != blocks.Count)
        {
            throw new InvalidOperationException(
                $"Malformed generated-doc markers: found {begins.Count} BEGIN marker(s), " +
                $"{ends.Count} END marker(s), and {blocks.Count} complete block(s).");
        }

        HashSet<(string Kind, string Contract, string Version, string DiffFrom)> seen = new();
        foreach (Match block in blocks)
        {
            string kind = block.Groups["kind"].Value;
            if (kind != "usage")
                throw new InvalidOperationException($"Unknown generated-doc marker kind '{kind}'.");

            (string, string, string, string) key = (
                kind,
                block.Groups["c"].Value,
                block.Groups["v"].Value,
                block.Groups["b"].Value);
            if (!seen.Add(key))
            {
                throw new InvalidOperationException(
                    $"Duplicate generated-doc block: kind={key.Item1}, contract={key.Item2}, " +
                    $"version={key.Item3}, diff-from={key.Item4}.");
            }
        }
    }

    private List<string> BuildDataDescriptors(string contractShort, string version)
    {
        string[] keys = GetDescriptorKeys(contractShort, version).ToArray();
        if (keys.Length == 0)
            return ["_None._"];

        List<string> rows = new()
        {
            "| Data Descriptor | Field | Type | Meaning |",
            "| --- | --- | --- | --- |",
        };
        foreach (string key in keys)
        {
            (string type, string field) = SplitDescriptorKey(key);
            rows.Add(
                $"| {FormatCode(type)} | {FormatDescriptorField(field)} | " +
                $"{FormatCode(GetFieldType(type, field))} | {DescriptorMeaning(type, key, field)} |");
        }
        return rows;
    }

    private List<string> BuildUsage(string contractShort, string version)
    {
        List<string> rows =
        [
            "### Data descriptors used",
            "",
        ];
        rows.AddRange(BuildDataDescriptors(contractShort, version));
        rows.Add("");
        rows.Add("### Global variables used");
        rows.Add("");
        rows.AddRange(BuildGlobalsUsed(contractShort, version));
        rows.Add("");
        rows.Add("### Contracts used");
        rows.Add("");
        rows.AddRange(BuildContractsUsed(contractShort, version));
        return rows;
    }

    private List<string> BuildUsageDiff(
        string contractShort,
        string version,
        string diffFrom)
    {
        ValidateDiffVersions(contractShort, version, diffFrom);

        List<string> rows =
        [
            $"### Data descriptor changes from `{diffFrom}`",
            "",
        ];
        rows.AddRange(BuildDataDescriptorDiff(contractShort, version, diffFrom));
        rows.Add("");
        rows.Add($"### Global variable changes from `{diffFrom}`");
        rows.Add("");
        rows.AddRange(BuildGlobalsDiff(contractShort, version, diffFrom));
        rows.Add("");
        rows.Add($"### Contract dependency changes from `{diffFrom}`");
        rows.Add("");
        rows.AddRange(BuildContractsDiff(contractShort, version, diffFrom));
        return rows;
    }

    private List<string> BuildDataDescriptorDiff(
        string contractShort,
        string version,
        string diffFrom)
    {
        HashSet<string> current = GetDescriptorKeys(contractShort, version).ToHashSet(
            StringComparer.Ordinal);
        HashSet<string> baseline = GetDescriptorKeys(contractShort, diffFrom).ToHashSet(
            StringComparer.Ordinal);
        List<(string Change, string Key)> changes = baseline
            .Except(current)
            .Select(key => (Change: "Removed", Key: key))
            .Concat(current.Except(baseline).Select(key => (Change: "Added", Key: key)))
            .OrderBy(entry => entry.Key.Substring(0, entry.Key.LastIndexOf('.')),
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Key.Substring(entry.Key.LastIndexOf('.') + 1),
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Change, StringComparer.Ordinal)
            .ToList();
        if (changes.Count == 0)
            return ["_No changes._"];

        List<string> rows =
        [
            "| Change | Data Descriptor | Field | Type | Meaning |",
            "| --- | --- | --- | --- | --- |",
        ];
        foreach ((string change, string key) in changes)
        {
            (string type, string field) = SplitDescriptorKey(key);
            rows.Add(
                $"| {change} | {FormatCode(type)} | {FormatDescriptorField(field)} | " +
                $"{FormatCode(GetFieldType(type, field))} | {DescriptorMeaning(type, key, field)} |");
        }
        return rows;
    }

    private List<string> BuildGlobalsDiff(
        string contractShort,
        string version,
        string diffFrom)
    {
        Dictionary<string, GlobalUsage> current =
            GetGlobals(contractShort, version);
        Dictionary<string, GlobalUsage> baseline =
            GetGlobals(contractShort, diffFrom);
        List<(string Change, string Name, string Types)> changes = baseline.Keys
            .Except(current.Keys, StringComparer.Ordinal)
            .Select(name => (
                Change: "Removed",
                Name: name,
                Types: baseline[name].Type))
            .Concat(current.Keys
                .Except(baseline.Keys, StringComparer.Ordinal)
                .Select(name => (
                    Change: "Added",
                    Name: name,
                    Types: current[name].Type)))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Change, StringComparer.Ordinal)
            .ToList();
        if (changes.Count == 0)
            return ["_No changes._"];

        List<string> rows =
        [
            "| Change | Global | Type | Meaning |",
            "| --- | --- | --- | --- |",
        ];
        foreach ((string change, string name, string types) in changes)
        {
            rows.Add(
                $"| {change} | {FormatGlobalName(name)} | {FormatCode(types)} | " +
                $"{_meanings.GlobalMeaning(name)} |");
        }
        return rows;
    }

    private List<string> BuildContractsDiff(
        string contractShort,
        string version,
        string diffFrom)
    {
        HashSet<string> current = GetContracts(contractShort, version);
        HashSet<string> baseline = GetContracts(contractShort, diffFrom);
        List<(string Change, string Name)> changes = baseline
            .Except(current)
            .Select(name => (Change: "Removed", Name: name))
            .Concat(current.Except(baseline).Select(name => (Change: "Added", Name: name)))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Change, StringComparer.Ordinal)
            .ToList();
        if (changes.Count == 0)
            return ["_No changes._"];

        List<string> rows =
        [
            "| Change | Contract Name |",
            "| --- | --- |",
        ];
        foreach ((string change, string name) in changes)
            rows.Add($"| {change} | {FormatCode(name)} |");
        return rows;
    }

    private List<string> BuildGlobalsUsed(string contractShort, string version)
    {
        ContractVersion label = new(new ContractInterface($"I{contractShort}"), version);
        ContractVersionUsage contract = GetContract(label);
        if (contract.Globals.Count == 0)
            return ["_None._"];

        List<string> rows =
        [
            "| Global | Type | Meaning |",
            "| --- | --- | --- |",
        ];
        foreach (GlobalUsage global in contract.Globals.OrderBy(
            global => global.Name,
            StringComparer.OrdinalIgnoreCase))
        {
            string meaning = _meanings.GlobalMeaning(global.Name);
            rows.Add(
                $"| {FormatGlobalName(global.Name)} | {FormatCode(global.Type)} | {meaning} |");
        }
        return rows;
    }

    private IEnumerable<string> GetDescriptorKeys(string contractShort, string version)
    {
        ContractVersion label = new(new ContractInterface($"I{contractShort}"), version);
        HashSet<string> keys = new(StringComparer.Ordinal);
        foreach (DataTypeUsage dataType in GetContract(label).DataTypes)
        {
            string type = StripDataPrefix(dataType.Name);
            if (dataType.UsesTypeSize)
                keys.Add($"{type}.{TypeSizeField}");
            foreach (FieldUsage field in dataType.Fields)
                keys.Add($"{type}.{field.Name}");
        }
        foreach (string key in _meanings.Supplement(contractShort))
            keys.Add(key);
        foreach (string key in _meanings.Suppress(contractShort))
            keys.Remove(key);

        return keys
            .OrderBy(key => key.Substring(0, key.LastIndexOf('.')),
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(key => key.Substring(key.LastIndexOf('.') + 1),
                StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, GlobalUsage> GetGlobals(
        string contractShort,
        string version)
    {
        ContractVersion label = new(new ContractInterface($"I{contractShort}"), version);
        return GetContract(label).Globals.ToDictionary(
            global => global.Name,
            StringComparer.Ordinal);
    }

    private HashSet<string> GetContracts(string contractShort, string version)
    {
        ContractVersion label = new(new ContractInterface($"I{contractShort}"), version);
        return GetContract(label).ContractsUsed
            .Select(contract => contract.ContractName)
            .ToHashSet(StringComparer.Ordinal);
    }

    private void ValidateDiffVersions(
        string contractShort,
        string version,
        string diffFrom)
    {
        if (version == diffFrom)
        {
            throw new InvalidOperationException(
                $"Generated-doc usage diff for {contractShort} {version} cannot compare a version to itself.");
        }

        ContractInterface contract = new($"I{contractShort}");
        foreach (string candidate in new[] { version, diffFrom })
        {
            ContractVersion label = new(contract, candidate);
            if (!_graph.Contracts.Any(usage => usage.Label == label))
            {
                throw new InvalidOperationException(
                    $"Generated-doc usage diff references unregistered contract version " +
                    $"{contractShort} {candidate}.");
            }
        }
    }

    private string GetFieldType(string type, string field)
    {
        if (field == TypeSizeField)
            return "uint32";

        string dataTypeName = "Data." + type;
        return _graph.Contracts
            .SelectMany(contract => contract.DataTypes)
            .Where(dataType => dataType.Name == dataTypeName)
            .SelectMany(dataType => dataType.Fields)
            .FirstOrDefault(candidate => candidate.Name == field)?.Type
            ?? "unknown";
    }

    private ContractVersionUsage GetContract(ContractVersion label) =>
        _graph.Contracts.Single(contract => contract.Label == label);

    private static (string Type, string Field) SplitDescriptorKey(string key)
    {
        int dot = key.LastIndexOf('.', StringComparison.Ordinal);
        return (key.Substring(0, dot), key.Substring(dot + 1));
    }

    private string DescriptorMeaning(string type, string key, string field)
    {
        if (field != TypeSizeField)
            return _meanings.Meaning(key);

        bool hasSizeField = _graph.Contracts
            .SelectMany(contract => contract.DataTypes)
            .Where(dataType => dataType.Name == "Data." + type)
            .SelectMany(dataType => dataType.Fields)
            .Any(candidate => candidate.Name == "Size");
        return hasSizeField
            ? "Size of the data descriptor layout"
            : _meanings.Meaning($"{type}.Size");
    }

    private static string FormatDescriptorField(string field) =>
        field == TypeSizeField ? "*(type size)*" : FormatCode(field);

    private static string FormatGlobalName(string name) =>
        name.Contains('<', StringComparison.Ordinal)
            ? $"{FormatCode(name)} *(name pattern)*"
            : FormatCode(name);

    private static string FormatCode(string value)
    {
        int delimiterLength = 1;
        while (value.Contains(new string('`', delimiterLength), StringComparison.Ordinal))
            delimiterLength++;

        string delimiter = new('`', delimiterLength);
        return $"{delimiter}{value}{delimiter}";
    }

    private List<string> BuildContractsUsed(string contractShort, string version)
    {
        ContractVersion label = new(new ContractInterface($"I{contractShort}"), version);
        IReadOnlyCollection<ContractInterface> contractsUsed = GetContract(label).ContractsUsed;
        if (contractsUsed.Count == 0)
            return ["_None._"];

        List<string> rows = new()
        {
            "| Contract Name |",
            "| --- |",
        };
        foreach (ContractInterface contract in contractsUsed.OrderBy(
            contract => contract.ContractName,
            StringComparer.OrdinalIgnoreCase))
        {
            rows.Add($"| {FormatCode(contract.ContractName)} |");
        }
        return rows;
    }

    private static string StripDataPrefix(string dataType) =>
        dataType.StartsWith("Data.", StringComparison.Ordinal) ? dataType.Substring("Data.".Length) : dataType;

    [GeneratedRegex(@"<!-- BEGIN GENERATED: (?<kind>[\w-]+) contract=(?<c>\w+) version=(?<v>\w+)(?<options>(?: diff-from=(?<b>\w+))?) -->.*?<!-- END GENERATED: \k<kind> contract=\k<c> version=\k<v>\k<options> -->", RegexOptions.Singleline)]
    private static partial Regex MarkerRegex();

    [GeneratedRegex(@"<!-- BEGIN GENERATED: (?<kind>[\w-]+) contract=(?<c>\w+) version=(?<v>\w+)(?: [^>]*)? -->")]
    private static partial Regex BeginMarkerRegex();

    [GeneratedRegex(@"<!-- END GENERATED: (?<kind>[\w-]+) contract=(?<c>\w+) version=(?<v>\w+)(?: [^>]*)? -->")]
    private static partial Regex EndMarkerRegex();
}
