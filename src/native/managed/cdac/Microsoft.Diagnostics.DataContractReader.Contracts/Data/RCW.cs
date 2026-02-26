// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RCW : IData<RCW>
{
    static RCW IData<RCW>.Create(Target target, TargetPointer address)
        => new RCW(target, address);

    public RCW(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RCW);

        NextRCW = target.ReadPointer(address + (ulong)type.Fields[nameof(NextRCW)].Offset);
        NextCleanupBucket = target.ReadPointer(address + (ulong)type.Fields[nameof(NextCleanupBucket)].Offset);
        Flags = target.Read<uint>(address + (ulong)type.Fields[nameof(Flags)].Offset);
        CtxCookie = target.ReadPointer(address + (ulong)type.Fields[nameof(CtxCookie)].Offset);
        CtxEntry = target.ReadPointer(address + (ulong)type.Fields[nameof(CtxEntry)].Offset);
    }

    public TargetPointer NextRCW { get; init; }
    public TargetPointer NextCleanupBucket { get; init; }
    public uint Flags { get; init; }
    public TargetPointer CtxCookie { get; init; }
    public TargetPointer CtxEntry { get; init; }
}
