// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

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
    IEnumerable<RCWCleanupInfo> GetRCWCleanupList(TargetPointer cleanupListPtr) => throw new NotImplementedException();
}

public readonly struct BuiltInCOM : IBuiltInCOM
{
    // Everything throws NotImplementedException
}
