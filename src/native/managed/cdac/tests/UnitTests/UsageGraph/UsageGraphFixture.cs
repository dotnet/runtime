// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using CdacUsageGraph;
using CdacUsageGraph.Model;
using ContractUsageGraph = CdacUsageGraph.Model.UsageGraph;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.UsageGraph;

public sealed class UsageGraphFixture
{
    public UsageGraphFixture() => Usage = UsageGraphAnalyzer.Analyze(FindCdacRoot().FullName);

    public ContractUsageGraph Usage { get; }

    private static DirectoryInfo FindCdacRoot()
    {
        for (DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "src",
                "native",
                "managed",
                "cdac");
            if (Directory.Exists(candidate))
                return new DirectoryInfo(candidate);
        }

        throw new InvalidOperationException("Could not locate the cDAC source root.");
    }
}

[CollectionDefinition(Name)]
public sealed class ContractUsageGraphCollection : ICollectionFixture<UsageGraphFixture>
{
    public const string Name = "Contract usage graph";
}
