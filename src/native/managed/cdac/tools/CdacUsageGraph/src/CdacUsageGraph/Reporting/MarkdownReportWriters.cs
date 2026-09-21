// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using CdacUsageGraph.Model;

namespace CdacUsageGraph.Reporting;

/// <summary>Emits <c>contract-data-graph.md</c>: (contract, version) -&gt; Data types used.</summary>
internal sealed class DataGraphMarkdownWriter : IReportWriter
{
    public string Write(UsageGraph graph, string outputDirectory)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# cDAC Contract -> Data Type Usage Graph");
        sb.AppendLine();
        sb.AppendLine($"_Generated from `{graph.CdacRoot}`. {graph.DataTypeCount} Data types._");
        sb.AppendLine();
        sb.AppendLine("| Contract | Version | # Data types | Data types used |");
        sb.AppendLine("|---|---|--:|---|");
        foreach (ContractVersionUsage contract in graph.Contracts)
        {
            string[] dataTypes = contract.DataTypes.Select(usage => usage.Name).ToArray();
            sb.AppendLine(
                $"| {contract.Label.Interface.Name} | {contract.Label.Version} | " +
                $"{dataTypes.Length} | {string.Join(", ", dataTypes)} |");
        }

        File.WriteAllText(Path.Combine(outputDirectory, "contract-data-graph.md"), sb.ToString());
        return $"contract-data-graph.md ({graph.Contracts.Sum(contract => contract.DataTypes.Count)} contract/data edges)";
    }
}

/// <summary>Emits <c>contract-field-usage.md</c>: descriptor dependencies by contract/version.</summary>
internal sealed class FieldUsageMarkdownWriter : IReportWriter
{
    public string Write(UsageGraph graph, string outputDirectory)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# cDAC Contract/Version -> Data Descriptor Dependencies");
        sb.AppendLine();
        sb.AppendLine("Fields include dependencies and optional availability probes. Type size is reported separately from fields named `Size`.");
        sb.AppendLine();
        sb.AppendLine("| Contract | Version | Data type | Field | Native type |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (ContractVersionUsage contract in graph.Contracts)
        {
            foreach (DataTypeUsage dataType in contract.DataTypes)
            {
                if (dataType.UsesTypeSize)
                {
                    sb.AppendLine(
                        $"| {contract.Label.Interface.Name} | {contract.Label.Version} | " +
                        $"{dataType.Name} | *(type size)* | n/a |");
                }
                foreach (FieldUsage field in dataType.Fields)
                {
                    sb.AppendLine(
                        $"| {contract.Label.Interface.Name} | {contract.Label.Version} | " +
                        $"{dataType.Name} | {field.Name} | {field.Type} |");
                }
            }
        }

        File.WriteAllText(Path.Combine(outputDirectory, "contract-field-usage.md"), sb.ToString());
        return $"contract-field-usage.md ({graph.Contracts.Sum(contract => contract.DataTypes.Sum(dataType => dataType.Fields.Count + (dataType.UsesTypeSize ? 1 : 0)))} contract/field rows)";
    }
}

/// <summary>Emits <c>contract-global-usage.md</c>: globals read per contract/version.</summary>
internal sealed class GlobalUsageMarkdownWriter : IReportWriter
{
    public string Write(UsageGraph graph, string outputDirectory)
    {
        StringBuilder sb = new();
        sb.AppendLine("# cDAC Contract/Version -> Global Usage");
        sb.AppendLine();
        sb.AppendLine("| Contract | Version | Global | Type | Access |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (ContractVersionUsage contract in graph.Contracts)
        {
            foreach (GlobalUsage global in contract.Globals)
            {
                string access = global.IsOptional ? "Optional" : "Required";
                sb.AppendLine(
                    $"| {contract.Label.Interface.Name} | {contract.Label.Version} | " +
                    $"{global.Name} | {global.Type} | {access} |");
            }
        }

        File.WriteAllText(
            Path.Combine(outputDirectory, "contract-global-usage.md"),
            sb.ToString());
        return $"contract-global-usage.md ({graph.Contracts.Sum(contract => contract.Globals.Count)} contract/global edges)";
    }
}

/// <summary>Emits <c>contract-contracts-used.md</c>: (contract, version) -&gt; other contracts used.</summary>
internal sealed class ContractsUsedMarkdownWriter : IReportWriter
{
    public string Write(UsageGraph graph, string outputDirectory)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# cDAC Contract/Version -> Contracts Used");
        sb.AppendLine();
        sb.AppendLine("Other contracts accessed via `_target.Contracts.<X>` (a contract dependency, not a data descriptor).");
        sb.AppendLine();
        sb.AppendLine("| Contract | Version | Contracts used |");
        sb.AppendLine("|---|---|---|");
        foreach (ContractVersionUsage contract in graph.Contracts)
        {
            string[] contractsUsed = contract.ContractsUsed
                .Select(contractInterface => contractInterface.ContractName)
                .ToArray();
            sb.AppendLine(
                $"| {contract.Label.Interface.Name} | {contract.Label.Version} | " +
                $"{string.Join(", ", contractsUsed)} |");
        }

        File.WriteAllText(Path.Combine(outputDirectory, "contract-contracts-used.md"), sb.ToString());
        return $"contract-contracts-used.md ({graph.Contracts.Sum(contract => contract.ContractsUsed.Count)} contract/contract edges)";
    }
}
