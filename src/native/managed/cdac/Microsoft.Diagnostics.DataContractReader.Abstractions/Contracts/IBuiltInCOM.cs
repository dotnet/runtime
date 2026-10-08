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
    public ulong RefCount { get; init; }
    public bool IsNeutered { get; init; }
    public bool IsAggregated { get; init; }
    public bool IsExtendsCOMObject { get; init; }
    public bool IsHandleWeak { get; init; }
    public TargetPointer OuterIUnknown { get; init; }
}

/// <summary>Data for a single RCW entry in the RCW cleanup list.</summary>
public record struct RCWCleanupInfo(
    TargetPointer RCW,
    TargetPointer Context,
    TargetPointer STAThread,
    bool IsFreeThreaded);

public record struct RCWData(
    TargetPointer IdentityPointer,
    TargetPointer UnknownPointer,
    TargetPointer ManagedObject,
    TargetPointer VTablePtr,
    TargetPointer CreatorThread,
    TargetPointer CtxCookie,
    uint RefCount,
    bool IsAggregated,
    bool IsContained,
    bool IsFreeThreaded,
    bool IsDisconnected);

public interface IBuiltInCOM : IContract
{
    static string IContract.Name { get; } = nameof(BuiltInCOM);
    // Resolves a COM interface pointer to a ComCallWrapper in the chain.
    // Returns TargetPointer.Null if interfacePointer is not a recognised COM interface pointer.
    // Use GetStartWrapper on the result to navigate to the start of the chain.
    TargetPointer GetCCWFromInterfacePointer(TargetPointer interfacePointer) => throw new NotImplementedException();
    // Enumerates COM interfaces exposed by the ComCallWrapper chain.
    // ccw may be any ComCallWrapper in the chain; the implementation navigates to the start.
    IEnumerable<COMInterfacePointerData> GetCCWInterfaces(TargetPointer ccw) => throw new NotImplementedException();
    // Returns the GC object handle (m_ppThis) from the given ComCallWrapper.
    TargetPointer GetObjectHandle(TargetPointer ccw) => throw new NotImplementedException();
    // Returns the data stored in the SimpleComCallWrapper associated with the given ComCallWrapper.
    SimpleComCallWrapperData GetSimpleComCallWrapperData(TargetPointer ccw) => throw new NotImplementedException();
    // Navigates to the start ComCallWrapper in a linked chain.
    // If ccw is already the start wrapper (or the only wrapper), returns ccw unchanged.
    TargetPointer GetStartWrapper(TargetPointer ccw) => throw new NotImplementedException();
    IEnumerable<RCWCleanupInfo> GetRCWCleanupList(TargetPointer cleanupListPtr) => throw new NotImplementedException();
    IEnumerable<(TargetPointer MethodTable, TargetPointer Unknown)> GetRCWInterfaces(TargetPointer rcw) => throw new NotImplementedException();
    TargetPointer GetRCWContext(TargetPointer rcw) => throw new NotImplementedException();
    RCWData GetRCWData(TargetPointer rcw) => throw new NotImplementedException();
}

public readonly struct BuiltInCOM : IBuiltInCOM
{
    // Everything throws NotImplementedException
}
