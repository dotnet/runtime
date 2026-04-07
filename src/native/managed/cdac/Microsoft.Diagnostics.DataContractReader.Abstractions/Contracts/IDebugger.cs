// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public record struct DebuggerData(uint DefinesBitField, uint MDStructuresVersion);

public interface IDebugger : IContract
{
    static string IContract.Name { get; } = nameof(Debugger);

    bool TryGetDebuggerData(out DebuggerData data) => throw new NotImplementedException();
    int GetAttachStateFlags() => throw new NotImplementedException();
    bool MetadataUpdatesApplied() => throw new NotImplementedException();
}

public readonly struct Debugger : IDebugger
{
    // Everything throws NotImplementedException
}
