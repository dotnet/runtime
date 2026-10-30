// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ComCallWrapper))]
internal sealed partial class ComCallWrapper : IData<ComCallWrapper>
{
    [Field] public partial TargetPointer Handle { get; }
    [Field] public partial TargetPointer SimpleWrapper { get; }
    [Field] public partial TargetPointer Next { get; }

    [FieldAddress]
    public partial TargetPointer IPtr { get; }

    [CustomInit(nameof(InitIPtrs))] public partial TargetPointer[] IPtrs { get; }

    private partial TargetPointer[] InitIPtrs(Target target, TargetPointer address)
    {
        int numInterfaces = (int)target.ReadGlobal<uint>(Constants.Globals.CCWNumInterfaces);
        TargetPointer[] iptrs = new TargetPointer[numInterfaces];
        for (int i = 0; i < numInterfaces; i++)
        {
            iptrs[i] = target.ReadPointer(IPtr + (ulong)(i * target.PointerSize));
        }

        return iptrs;
    }
}
