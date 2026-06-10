// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ComCallWrapper))]
internal sealed partial class ComCallWrapper : IData<ComCallWrapper>
{
    [Field] public TargetPointer Handle { get; }
    [Field] public TargetPointer SimpleWrapper { get; }
    [Field] public TargetPointer Next { get; }

    [FieldAddress]
    public TargetPointer IPtr { get; }

    public TargetPointer[] IPtrs { get; private set; }

    [MemberNotNull(nameof(IPtrs))]
    partial void OnInit(Target target, TargetPointer address)
    {
        int numInterfaces = (int)target.ReadGlobal<uint>(Constants.Globals.CCWNumInterfaces);
        IPtrs = new TargetPointer[numInterfaces];
        for (int i = 0; i < numInterfaces; i++)
        {
            IPtrs[i] = target.ReadPointer(IPtr + (ulong)(i * target.PointerSize));
        }
    }
}
