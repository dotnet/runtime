// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ProfControlBlock : IData<ProfControlBlock>
{
    static ProfControlBlock IData<ProfControlBlock>.Create(Target target, TargetPointer address)
        => new ProfControlBlock(target, address);

    public ProfControlBlock(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ProfControlBlock);
        GlobalEventMask = target.ReadField<ulong>(address, type, nameof(GlobalEventMask));
        RejitOnAttachEnabled = target.ReadField<byte>(address, type, nameof(RejitOnAttachEnabled)) != 0;
    }

    public ulong GlobalEventMask { get; init; }
    public bool RejitOnAttachEnabled { get; init; }
}
