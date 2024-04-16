// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public partial class ContractReader
{
    private readonly DataDescriptorModel.Builder _builder;

    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { PropertyNameCaseInsensitive = false, ReadCommentHandling = JsonCommentHandling.Skip };

    public ContractReader(DataDescriptorModel.Builder builder)
    {
        _builder = builder;
    }

    public async Task<bool> ParseContracts(string contractFilePath, CancellationToken token = default)
    {
        string? contents = await File.ReadAllTextAsync(contractFilePath, token).ConfigureAwait(false);
        var contractCollectionModel = JsonSerializer.Deserialize<DataDescriptorModel.ContractsCollectionModel>(contents, s_jsonSerializerOptions);
        if (contractCollectionModel is null)
            return false;
        _builder.AddOrupdateContracts(contractCollectionModel);
        return true;
    }
    public bool TryParseContractLine(string line, [NotNullWhen(true)] out string? contract, out int version)
    {
        var match = ContractLineRegex().Match(line);
        if (!match.Success)
        {
            contract = null;
            version = 0;
            return false;
        }
        GroupCollection groups = match.Groups;
        contract = groups["contractName"].Value;
        string versionStr = groups["contractVersion"].Value;
        if (!int.TryParse(versionStr, out version))
        {
            Console.Error.WriteLine($"Invalid version number in contract {versionStr}");
            contract = null;
            version = 0;
            return false;
        }
        return true;
    }

    // Matches a line of the form:
    //    contract name, version # optional trailing comment
    [GeneratedRegex(@"^\s*(?<contractName>\w+)\s*,\s*(?<contractVersion>\d+)\s*(?:#.*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex ContractLineRegex();

    // Matches a line of whitespace with an optional trailing comment
    [GeneratedRegex(@"^\s*(?:#.*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex BlankOrCommentRegex();
}
