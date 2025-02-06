// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IStackWalkHandle { };
public interface IStackDataFrameHandle { };

public interface IStackWalk : IContract
{
    static string IContract.Name => nameof(StackWalk);

    public virtual IStackWalkHandle CreateStackWalk(ThreadData threadData) => throw new NotImplementedException();
    public virtual bool Next(IStackWalkHandle stackWalkHandle) => throw new NotImplementedException();
    public virtual IStackDataFrameHandle GetCurrentFrame(IStackWalkHandle stackWalkHandle) => throw new NotImplementedException();
    public virtual byte[] GetRawContext(IStackDataFrameHandle stackDataFrameHandle) => throw new NotImplementedException();
    public virtual TargetPointer GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle) => throw new NotImplementedException();
}

public struct StackWalk : IStackWalk
{
    // Everything throws NotImplementedException
}
