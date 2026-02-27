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
        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);

        IPtr = address + (ulong)type.Fields[nameof(IPtr)].Offset;
        uint numInterfaces = target.ReadGlobal<uint>(Constants.Globals.CCWNumInterfaces);
        IPtrs = new TargetPointer[numInterfaces];
        for (int i = 0; i < numInterfaces; i++)
        {
            IPtrs[i] = target.ReadPointer(IPtr + (ulong)(i * target.PointerSize));
        }
    }

    public TargetPointer SimpleWrapper { get; init; }
    public TargetPointer IPtr { get; init; }
    public TargetPointer[] IPtrs { get; }
    public TargetPointer Next { get; init; }
}
