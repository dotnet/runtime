// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Scope of a <see cref="Target.Flush(FlushScope)"/> operation.
/// </summary>
public enum FlushScope
{
    /// <summary>
    /// Clear every cache held by the target, including immutable metadata caches
    /// (type layouts, contract instances, ECMA metadata, execution-manager ranges).
    /// This is the safe default and matches the historical no-argument Flush behavior.
    /// </summary>
    All,

    /// <summary>
    /// Clear only target-state caches (<see cref="Target.ProcessedData"/>). Contract
    /// instances and any metadata caches they own are retained.
    /// <para>
    /// Each contract is responsible for its own correctness under this scope: a
    /// contract that captures a target-memory snapshot at construction MUST re-read
    /// that snapshot in its <see cref="Contracts.IContract.Flush(FlushScope)"/>
    /// implementation when called with <see cref="TargetState"/>, otherwise the
    /// snapshot becomes stale across the flush.
    /// </para>
    /// </summary>
    TargetState,
}
