// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IObjectiveCMarshal : IContract
{
    static string IContract.Name { get; } = nameof(ObjectiveCMarshal);
    TargetPointer GetTaggedMemory(TargetPointer address, out TargetNUInt size) => throw new NotImplementedException();
}

public readonly struct ObjectiveCMarshal : IObjectiveCMarshal
{
    // Everything throws NotImplementedException
}
