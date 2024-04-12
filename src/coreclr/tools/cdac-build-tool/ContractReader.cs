// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
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
        uint lineNum = 0;
        while (true)
        {
            var line = await reader.ReadLineAsync(token).ConfigureAwait(false);
            lineNum++;
            if (line == null)
            {
                break;
            }
            if (BlankOrCommentRegex().Match(line) is { Success: true })
            {
                continue;
            }
            if (!TryParseContractLine(line, out string? contract, out int version))
            {
                Console.Error.WriteLine($"{contractFilePath}:{lineNum}: Invalid contract line: {line}");
                return false;
            }
            _builder.AddOrUpdateContract(contract, version);
        }
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
