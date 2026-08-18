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
    /// Flush only caches that may have become stale because the target process
    /// has executed forward in time since the last flush (e.g. a debugger
    /// resume/continue/step). Caches of immutable state that the runtime loads
    /// once and never unloads (modules, types, code metadata, ECMA metadata,
    /// execution-manager ranges, etc.) may be retained.
    /// <para>
    /// This corresponds to ICorDebug's <c>CorDebugStateChange.PROCESS_RUNNING</c>:
    /// see <see href="https://learn.microsoft.com/dotnet/core/unmanaged-api/debugging/icordebug/cordebugstatechange-enumeration"/>.
    /// Use <see cref="All"/> instead for arbitrary state snapshots where no
    /// continuity with the previous target state can be assumed.
    /// </para>
    /// <para>
    /// Each contract is responsible for its own correctness under this scope: a
    /// contract that captures a target-memory snapshot at construction MUST
    /// re-read that snapshot in its
    /// <see cref="Contracts.IContract.Flush(FlushScope)"/> implementation when
    /// called with <see cref="ForwardExecution"/>, otherwise the snapshot
    /// becomes stale across the flush.
    /// </para>
    /// </summary>
    ForwardExecution,
}
