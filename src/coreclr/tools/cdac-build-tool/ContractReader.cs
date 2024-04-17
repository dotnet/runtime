// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public class ContractReader
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
        var contracts = JsonSerializer.Deserialize<Dictionary<string, int>>(contents, s_jsonSerializerOptions);
        if (contracts is null)
            return false;
        _builder.AddOrupdateContracts(contracts);
        return true;
    }
}
