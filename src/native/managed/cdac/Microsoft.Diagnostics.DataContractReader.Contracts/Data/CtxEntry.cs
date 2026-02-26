// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CtxEntry : IData<CtxEntry>
{
    static CtxEntry IData<CtxEntry>.Create(Target target, TargetPointer address)
        => new CtxEntry(target, address);

    public CtxEntry(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CtxEntry);

        STAThread = target.ReadPointer(address + (ulong)type.Fields[nameof(STAThread)].Offset);
    }

    public TargetPointer STAThread { get; init; }
}
