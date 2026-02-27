// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ComCallWrapper : IData<ComCallWrapper>
{
    static ComCallWrapper IData<ComCallWrapper>.Create(Target target, TargetPointer address) => new ComCallWrapper(target, address);
    public ComCallWrapper(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ComCallWrapper);

        SimpleWrapper = target.ReadPointer(address + (ulong)type.Fields[nameof(SimpleWrapper)].Offset);
    }

    public TargetPointer SimpleWrapper { get; init; }
}
