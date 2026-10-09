// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public enum RejitState
{
    Requested,
    Active,
    // ReJIT parameter configuration is in progress (for example, the profiler's GetReJITParameters callback is
    // running) and the version has not yet become Active.
    GettingReJITParameters,
}

public interface IReJIT : IContract
{
    static string IContract.Name { get; } = nameof(ReJIT);

    bool IsEnabled() => throw new NotImplementedException();

    RejitState GetRejitState(ILCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();

    bool IsDeoptimized(ILCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();

    TargetNUInt GetRejitId(ILCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();
}

public readonly struct ReJIT : IReJIT
{

}
