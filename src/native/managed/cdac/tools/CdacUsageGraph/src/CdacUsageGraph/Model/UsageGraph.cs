// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CdacUsageGraph.Model;

/// <summary>
/// Immutable result of the analysis, organized by contract version. The analysis uses mutable
/// indexes while walking code, then materializes this ordered record model for reporting.
/// </summary>
/// <param name="CdacRoot">The cDAC source root used for analysis.</param>
/// <param name="DataTypeCount">The number of discovered IData descriptor types.</param>
/// <param name="Contracts">The usage result for each registered contract version.</param>
public sealed record UsageGraph(
    string CdacRoot,
    int DataTypeCount,
    IReadOnlyCollection<ContractVersionUsage> Contracts);

/// <summary>Descriptor, global, and contract dependencies for one contract version.</summary>
/// <param name="Label">The analyzed contract version.</param>
/// <param name="DataTypes">The descriptor types and fields used by the contract.</param>
/// <param name="Globals">The target globals used by the contract.</param>
/// <param name="ContractsUsed">The other contracts used by the contract.</param>
public sealed record ContractVersionUsage(
    ContractVersion Label,
    IReadOnlyCollection<DataTypeUsage> DataTypes,
    IReadOnlyCollection<GlobalUsage> Globals,
    IReadOnlyCollection<ContractInterface> ContractsUsed);

/// <summary>Usage of one cDAC descriptor type.</summary>
/// <param name="Name">The descriptor type name.</param>
/// <param name="UsesTypeSize">Whether the descriptor's instance size is used.</param>
/// <param name="Fields">The descriptor fields used by the contract.</param>
public sealed record DataTypeUsage(
    string Name,
    bool UsesTypeSize,
    IReadOnlyCollection<FieldUsage> Fields);

/// <summary>Usage of one descriptor field.</summary>
/// <param name="Name">The native descriptor field name.</param>
/// <param name="Type">The native storage type of the field.</param>
public sealed record FieldUsage(
    string Name,
    string Type);

/// <summary>Usage of one target global.</summary>
/// <param name="Name">The target global name or symbolic name pattern.</param>
/// <param name="Type">The native storage type of the global.</param>
/// <param name="IsOptional">Whether every observed access to the global is optional.</param>
public sealed record GlobalUsage(
    string Name,
    string Type,
    bool IsOptional);
