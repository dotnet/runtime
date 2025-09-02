// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IStackDataFrameHandle { };

public interface IStackWalk : IContract
{
    static string IContract.Name => nameof(StackWalk);

    public virtual IEnumerable<IStackDataFrameHandle> CreateStackWalk(ThreadData threadData) => throw new NotImplementedException();
    byte[] GetRawContext(IStackDataFrameHandle stackDataFrameHandle) => throw new NotImplementedException();
    TargetPointer GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle) => throw new NotImplementedException();
    string GetFrameName(TargetPointer frameIdentifier) => throw new NotImplementedException();
    TargetPointer GetMethodDescPtr(IStackDataFrameHandle stackDataFrameHandle) => throw new NotImplementedException();
    TargetPointer GetMethodDescPtr(TargetPointer framePtr) => throw new NotImplementedException();
}

public struct StackWalk : IStackWalk
{
    // Everything throws NotImplementedException
}
