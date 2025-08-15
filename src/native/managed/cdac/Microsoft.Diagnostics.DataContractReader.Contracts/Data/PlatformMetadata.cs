// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class PlatformMetadata : IData<PlatformMetadata>
{
    static PlatformMetadata IData<PlatformMetadata>.Create(Target target, TargetPointer address)
        => new PlatformMetadata(target, address);

    public PlatformMetadata(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.PlatformMetadata);
        PrecodeMachineDescriptor = address + (ulong)type.Fields[nameof(PrecodeMachineDescriptor)].Offset;
        CodePointerFlags = target.Read<byte>(address + (ulong)type.Fields[nameof(CodePointerFlags)].Offset);
    }

    /* Address of */
    public TargetPointer PrecodeMachineDescriptor { get; init; }
    public byte CodePointerFlags { get; init; }
}
