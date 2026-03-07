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

/// <summary>Data for a single RCW entry in the RCW cleanup list.</summary>
public record struct RCWCleanupInfo(
    TargetPointer RCW,
    TargetPointer Context,
    TargetPointer STAThread,
    bool IsFreeThreaded);

public interface IBuiltInCOM : IContract
{
    static string IContract.Name { get; } = nameof(BuiltInCOM);
    ulong GetRefCount(TargetPointer address) => throw new NotImplementedException();
    bool IsHandleWeak(TargetPointer address) => throw new NotImplementedException();
    // Returns true if the CCW has been neutered (CLEANUP_SENTINEL bit is set in the ref count).
    bool IsNeutered(TargetPointer address) => throw new NotImplementedException();
    // Returns true if the managed class extends a COM object (IsExtendsCom flag).
    bool IsExtendsCOMObject(TargetPointer address) => throw new NotImplementedException();
    // Returns true if the CCW is aggregated (IsAggregated flag).
    bool IsAggregated(TargetPointer address) => throw new NotImplementedException();
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
    // Returns the outer IUnknown pointer (m_pOuter) for aggregated CCWs.
    // All wrappers in a chain share the same SimpleComCallWrapper, so any wrapper address is accepted.
    TargetPointer GetOuterIUnknown(TargetPointer ccw) => throw new NotImplementedException();
    IEnumerable<RCWCleanupInfo> GetRCWCleanupList(TargetPointer cleanupListPtr) => throw new NotImplementedException();
}

public readonly struct BuiltInCOM : IBuiltInCOM
{
    // Everything throws NotImplementedException
}
