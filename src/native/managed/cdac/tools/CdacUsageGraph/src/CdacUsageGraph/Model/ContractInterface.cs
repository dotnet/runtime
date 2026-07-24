// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CdacUsageGraph.Model;

/// <summary>Identifies a contract interface by name, such as <c>IThread</c>.</summary>
/// <param name="Name">The contract interface name.</param>
public readonly record struct ContractInterface(string Name)
{
    /// <summary>The contract registry property and documentation name, such as <c>Thread</c>.</summary>
    public string ContractName => Name[1..];

    public override string ToString() => Name;
}
