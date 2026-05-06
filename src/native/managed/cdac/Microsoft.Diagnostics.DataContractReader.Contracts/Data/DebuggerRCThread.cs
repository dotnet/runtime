// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class DebuggerRCThread : IData<DebuggerRCThread>
{
    static DebuggerRCThread IData<DebuggerRCThread>.Create(Target target, TargetPointer address)
        => new DebuggerRCThread(target, address);

    public DebuggerRCThread(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DebuggerRCThread);
        DCB = target.ReadPointerField(address, type, nameof(DCB));
    }

    public TargetPointer DCB { get; init; }
}
