// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IObjectiveCMarshal : IContract
{
    static string IContract.Name { get; } = nameof(ObjectiveCMarshal);

    // Get the tagged memory for an Objective-C tracked reference object.
    // Returns false if the object does not have tagged memory.
    // On success, size is set to the size of the tagged memory in bytes; otherwise size is set to default.
    bool TryGetTaggedMemory(TargetPointer address, out TargetNUInt size, out TargetPointer taggedMemory) => throw new NotImplementedException();
}

public readonly struct ObjectiveCMarshal : IObjectiveCMarshal
{
    // Everything throws NotImplementedException
}
