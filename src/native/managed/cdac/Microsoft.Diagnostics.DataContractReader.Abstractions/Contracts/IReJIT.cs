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

public interface IReJIT : IContract
{
    static string IContract.Name { get; } = nameof(ReJIT);

    bool IsEnabled() => throw new NotImplementedException();

    RejitState GetRejitState(ILCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();

    TargetNUInt GetRejitId(ILCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();
}

public readonly struct ReJIT : IReJIT
{

}
