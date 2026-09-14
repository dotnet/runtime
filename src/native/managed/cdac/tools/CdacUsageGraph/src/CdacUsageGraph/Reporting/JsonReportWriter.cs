// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using CdacUsageGraph.Model;

namespace CdacUsageGraph.Reporting;

/// <summary>Emits <c>contract-usage.json</c>: the full machine-readable usage graph.</summary>
internal sealed class JsonReportWriter : IReportWriter
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    // Named model (rather than an anonymous type) so every local can be explicitly typed. Property
    // names are the serialized JSON keys; declaration order is the emitted key order.
    private sealed record ContractUsageJson(
        string contract,
        string version,
        string[] dataTypes,
        string[] dataTypeSizesUsed,
        string[] contractsUsed,
        Dictionary<string, GlobalUsageJson> globalsUsed,
        Dictionary<string, Dictionary<string, string>> fieldTypes);

    private sealed record GlobalUsageJson(
        string type,
        bool optional);

    public string Write(UsageGraph graph, string outputDirectory)
    {
        List<ContractUsageJson> jsonModel = graph.Contracts
            .Select(contract =>
            {
                Dictionary<string, GlobalUsageJson> globals = contract.Globals
                    .ToDictionary(
                        global => global.Name,
                        global => new GlobalUsageJson(global.Type, global.IsOptional),
                        StringComparer.Ordinal);
                Dictionary<string, Dictionary<string, string>> types = contract.DataTypes
                    .ToDictionary(
                        dataType => dataType.Name,
                        dataType => dataType.Fields.ToDictionary(
                            field => field.Name,
                            field => field.Type,
                            StringComparer.Ordinal),
                        StringComparer.Ordinal);
                return new ContractUsageJson(
                    contract.Label.Interface.Name,
                    contract.Label.Version,
                    contract.DataTypes.Select(dataType => dataType.Name).ToArray(),
                    contract.DataTypes
                        .Where(dataType => dataType.UsesTypeSize)
                        .Select(dataType => dataType.Name)
                        .ToArray(),
                    contract.ContractsUsed
                        .Select(contract => contract.ContractName)
                        .ToArray(),
                    globals,
                    types);
            })
            .ToList();

        File.WriteAllText(
            Path.Combine(outputDirectory, "contract-usage.json"),
            JsonSerializer.Serialize(jsonModel, s_jsonOptions));
        return "contract-usage.json";
    }
}
