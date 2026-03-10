// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public readonly struct COMInterfacePointerData
{
    public TargetPointer InterfacePointerAddress { get; init; }
    public TargetPointer MethodTable { get; init; }
}

/// <summary>Data read from a SimpleComCallWrapper.</summary>
public readonly struct SimpleComCallWrapperData
{
    /// <summary>The visible reference count (raw refcount with CLEANUP_SENTINEL and other non-count bits masked off via ComRefcountMask).</summary>
    public ulong RefCount { get; init; }
    /// <summary>True if the CCW has been neutered (CLEANUP_SENTINEL bit was set in the raw ref count).</summary>
    public bool IsNeutered { get; init; }
    /// <summary>Bit flags (IsAggregated = 0x1, IsExtendsCom = 0x2, IsHandleWeak = 0x4).</summary>
    public uint Flags { get; init; }
    /// <summary>Outer IUnknown pointer for aggregated CCWs (m_pOuter).</summary>
    public TargetPointer OuterIUnknown { get; init; }
    /// <summary>Pointer to the start (first) ComCallWrapper in the chain.</summary>
    public TargetPointer MainWrapper { get; init; }
}

/// <summary>Data for a single RCW entry in the RCW cleanup list.</summary>
public record struct RCWCleanupInfo(
    TargetPointer RCW,
    TargetPointer Context,
    TargetPointer STAThread,
    bool IsFreeThreaded);

public interface IBuiltInCOM : IContract
{
    static string IContract.Name { get; } = nameof(BuiltInCOM);
    // Resolves a COM interface pointer to the start ComCallWrapper.
    // Returns TargetPointer.Null if interfacePointer is not a recognised COM interface pointer.
    TargetPointer GetCCWFromInterfacePointer(TargetPointer interfacePointer) => throw new NotImplementedException();
    // Enumerates COM interfaces exposed by the ComCallWrapper chain.
    // ccw may be any ComCallWrapper in the chain; the implementation navigates to the start.
    IEnumerable<COMInterfacePointerData> GetCCWInterfaces(TargetPointer ccw) => throw new NotImplementedException();
    // Returns the address of the start ComCallWrapper for the given CCW address.
    // All wrappers in a chain share the same SimpleComCallWrapper, so any wrapper address is accepted.
    TargetPointer GetCCWAddress(TargetPointer ccw) => throw new NotImplementedException();
    // Returns the GC object handle (m_ppThis) of the start ComCallWrapper.
    TargetPointer GetCCWHandle(TargetPointer ccw) => throw new NotImplementedException();
    // Returns the address of the SimpleComCallWrapper associated with the given ComCallWrapper.
    TargetPointer GetSimpleComCallWrapper(TargetPointer ccw) => throw new NotImplementedException();
    // Returns the data stored in a SimpleComCallWrapper.
    // sccw must be a SimpleComCallWrapper address (obtain via GetSimpleComCallWrapper).
    SimpleComCallWrapperData GetSimpleComCallWrapperData(TargetPointer sccw) => throw new NotImplementedException();
    IEnumerable<RCWCleanupInfo> GetRCWCleanupList(TargetPointer cleanupListPtr) => throw new NotImplementedException();
}

public readonly struct BuiltInCOM : IBuiltInCOM
{
    // Everything throws NotImplementedException
}
