// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CdacUsageGraph.Model;

/// <summary>
/// Identifies one registered contract implementation: the contract interface (e.g. <c>IThread</c>)
/// and its version string (e.g. <c>c1</c>). Used as the label threaded through the
/// walk and as the key of the result dictionaries -- named fields avoid positional mistakes.
/// </summary>
/// <param name="Interface">The contract interface.</param>
/// <param name="Version">The registered contract version.</param>
public readonly record struct ContractVersion(ContractInterface Interface, string Version);
