// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public partial class ContractReader
{

    private readonly DataDescriptorModel.Builder _builder;

    public ContractReader(DataDescriptorModel.Builder builder)
    {
        _builder = builder;
    }

    public async Task<bool> ParseContracts(string contractFilePath, CancellationToken token = default)
    {
        var s = File.OpenRead(contractFilePath);
        using var reader = new StreamReader(s, System.Text.Encoding.UTF8);
        while (true)
        {
            var line = await reader.ReadLineAsync(token);
            if (line == null)
            {
                break;
            }
            if (BlankOrCommentRegex().Match(line) is { Success: true })
            {
                continue;
            }
            var (contract, version) = ParseContractLine(line);
            _builder.AddOrUpdateContract(contract, version);
        }
        return true;
    }
    public (string, int) ParseContractLine(string line)
    {
        var match = ContractLineRegex().Match(line);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Invalid contract line: {line}");
        }
        GroupCollection groups = match.Groups;
        var contract = groups["contractName"].Value;
        var version = int.Parse(groups["contractVersion"].Value);
        return (contract, version);
    }

    // Matches a line of the form:
    //    contract name, version # optional trailing comment
    [GeneratedRegex(@"^\s*(?<contractName>\w+)\s*,\s*(?<contractVersion>\d+)\s*(?:#.*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex ContractLineRegex();

    // Matches a line of whitespace with an optional trailing comment
    [GeneratedRegex(@"^\s*(?:#.*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex BlankOrCommentRegex();
}
