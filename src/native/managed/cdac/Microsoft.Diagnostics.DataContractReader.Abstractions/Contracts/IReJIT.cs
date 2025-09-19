// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public enum RejitState
{
    Requested,
    Active
}

public enum OptimizationTierEnum
{
    Unknown = 0,
    MinOptJitted = 1,
    Optimized = 2,
    QuickJitted = 3,
    OptimizedTier1 = 4,
    ReadyToRun = 5,
    OptimizedTier1OSR = 6,
    QuickJittedInstrumented = 7,
    OptimizedTier1Instrumented = 8,
}

public interface IReJIT : IContract
{
    static string IContract.Name { get; } = nameof(ReJIT);

    bool IsEnabled() => throw new NotImplementedException();

    RejitState GetRejitState(ILCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();

    TargetNUInt GetRejitId(ILCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();
    IEnumerable<(TargetPointer, TargetPointer, OptimizationTierEnum)> GetTieredVersions(TargetPointer methodDesc, int rejitId) => throw new NotImplementedException();
}

public readonly struct ReJIT : IReJIT
{

}
