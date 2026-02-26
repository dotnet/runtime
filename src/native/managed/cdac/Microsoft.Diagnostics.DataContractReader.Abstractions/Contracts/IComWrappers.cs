// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public record struct RCWCleanupData(
    TargetPointer RCW,
    TargetPointer Context,
    TargetPointer STAThread,
    bool IsFreeThreaded);

public interface IComWrappers : IContract
{
    static string IContract.Name { get; } = nameof(ComWrappers);
    TargetPointer GetComWrappersIdentity(TargetPointer address) => throw new NotImplementedException();
    TargetPointer GetManagedObjectWrapperFromCCW(TargetPointer ccw) => throw new NotImplementedException();
    TargetPointer GetComWrappersObjectFromMOW(TargetPointer mow) => throw new NotImplementedException();
    long GetMOWReferenceCount(TargetPointer mow) => throw new NotImplementedException();
    bool IsComWrappersRCW(TargetPointer rcw) => throw new NotImplementedException();
    IEnumerable<RCWCleanupData> GetRCWCleanupList(TargetPointer cleanupListAddress) => throw new NotImplementedException();
}

public readonly struct ComWrappers : IComWrappers
{
    // Everything throws NotImplementedException
}
