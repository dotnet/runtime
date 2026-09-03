// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using CdacUsageGraph;
using CdacUsageGraph.Model;
using ContractUsageGraph = CdacUsageGraph.Model.UsageGraph;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.UsageGraph;

[Collection(ContractUsageGraphCollection.Name)]
public sealed class ContractUsageGraphTests
{
    private readonly ContractUsageGraph _usage;

    // Contract dependencies should remain acyclic. Update this allowlist only when a new
    // circular dependency is unavoidable and its members have been reviewed together.
    private static readonly string[][] s_allowedDependencyCycles =
    [
        [
            nameof(ICodeVersions),
            nameof(IExecutionManager),
            nameof(IRuntimeTypeSystem),
            nameof(ISignature),
        ],
        [
            nameof(IEcmaMetadata),
            nameof(ILoader),
        ],
    ];

    public ContractUsageGraphTests(UsageGraphFixture fixture) => _usage = fixture.Usage;

    [Fact]
    public void ContractDependencyCyclesAreAllowlisted()
    {
        Dictionary<ContractInterface, HashSet<ContractInterface>> dependencies = [];
        foreach (ContractVersionUsage contract in _usage.Contracts)
        {
            if (!dependencies.TryGetValue(
                    contract.Label.Interface,
                    out HashSet<ContractInterface>? used))
            {
                dependencies[contract.Label.Interface] = used = [];
            }

            used.UnionWith(contract.ContractsUsed);
        }

        List<string[]> actualCycles = FindStronglyConnectedComponents(dependencies)
            .Where(component => component.Count > 1)
            .Select(component => component
                .OrderBy(contract => contract.Name, StringComparer.Ordinal)
                .Select(contract => contract.Name)
                .ToArray())
            .ToList();

        Assert.Equal(s_allowedDependencyCycles.Length, actualCycles.Count);
        foreach (string[] expectedCycle in s_allowedDependencyCycles)
        {
            Assert.Contains(
                actualCycles,
                actualCycle => actualCycle.SequenceEqual(
                    expectedCycle.OrderBy(contract => contract, StringComparer.Ordinal)));
        }
    }

    private static IReadOnlyCollection<IReadOnlyCollection<ContractInterface>>
        FindStronglyConnectedComponents(
            IReadOnlyDictionary<ContractInterface, HashSet<ContractInterface>> dependencies)
    {
        Dictionary<ContractInterface, int> indexes = [];
        Dictionary<ContractInterface, int> lowLinks = [];
        Stack<ContractInterface> stack = new();
        HashSet<ContractInterface> onStack = [];
        List<IReadOnlyCollection<ContractInterface>> components = [];
        int nextIndex = 0;

        foreach (ContractInterface contract in dependencies.Keys)
        {
            if (!indexes.ContainsKey(contract))
                Visit(contract);
        }

        return components;

        void Visit(ContractInterface contract)
        {
            indexes[contract] = nextIndex;
            lowLinks[contract] = nextIndex;
            nextIndex++;
            stack.Push(contract);
            onStack.Add(contract);

            foreach (ContractInterface dependency in dependencies.GetValueOrDefault(contract, []))
            {
                if (!indexes.ContainsKey(dependency))
                {
                    Visit(dependency);
                    lowLinks[contract] = Math.Min(lowLinks[contract], lowLinks[dependency]);
                }
                else if (onStack.Contains(dependency))
                {
                    lowLinks[contract] = Math.Min(lowLinks[contract], indexes[dependency]);
                }
            }

            if (lowLinks[contract] != indexes[contract])
                return;

            List<ContractInterface> component = [];
            ContractInterface member;
            do
            {
                member = stack.Pop();
                onStack.Remove(member);
                component.Add(member);
            }
            while (member != contract);
            components.Add(component);
        }
    }
}
