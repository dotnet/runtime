// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodDesc : IData<MethodDesc>
{
    static MethodDesc IData<MethodDesc>.Create(Target target, TargetPointer address) => new MethodDesc(target, address);
    public MethodDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodDesc);

        ChunkIndex = target.Read<byte>(address + (ulong)type.Fields[nameof(ChunkIndex)].Offset);
        Slot = target.Read<ushort>(address + (ulong)type.Fields[nameof(Slot)].Offset);
        Flags = target.Read<ushort>(address + (ulong)type.Fields[nameof(Flags)].Offset);
        Flags3AndTokenRemainder = target.Read<ushort>(address + (ulong)type.Fields[nameof(Flags3AndTokenRemainder)].Offset);
        EntryPointFlags = target.Read<byte>(address + (ulong)type.Fields[nameof(EntryPointFlags)].Offset);
        CodeData = target.ReadPointer(address + (ulong)type.Fields[nameof(CodeData)].Offset);
        if (type.Fields.ContainsKey(nameof(GCCoverageInfo)))
        {
            GCCoverageInfo = target.ReadPointer(address + (ulong)type.Fields[nameof(GCCoverageInfo)].Offset);
        }
    }

    public byte ChunkIndex { get; init; }
    public ushort Slot { get; init; }
    public ushort Flags { get; init; }
    public ushort Flags3AndTokenRemainder { get; init; }
    public byte EntryPointFlags { get; init; }

    public TargetPointer CodeData { get; init; }

    public TargetPointer? GCCoverageInfo { get; init; }
}

internal sealed class InstantiatedMethodDesc : IData<InstantiatedMethodDesc>
{
    static InstantiatedMethodDesc IData<InstantiatedMethodDesc>.Create(Target target, TargetPointer address) => new InstantiatedMethodDesc(target, address);
    public InstantiatedMethodDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InstantiatedMethodDesc);

        PerInstInfo = target.ReadPointer(address + (ulong)type.Fields[nameof(PerInstInfo)].Offset);
        NumGenericArgs = target.Read<ushort>(address + (ulong)type.Fields[nameof(NumGenericArgs)].Offset);
        Flags2 = target.Read<ushort>(address + (ulong)type.Fields[nameof(Flags2)].Offset);
    }

    public TargetPointer PerInstInfo { get; init; }
    public ushort NumGenericArgs { get; init; }
    public ushort Flags2 { get; init; }
}

internal sealed class DynamicMethodDesc : IData<DynamicMethodDesc>
{
    static DynamicMethodDesc IData<DynamicMethodDesc>.Create(Target target, TargetPointer address) => new DynamicMethodDesc(target, address);
    public DynamicMethodDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DynamicMethodDesc);

        MethodName = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodName)].Offset);
    }

    public TargetPointer MethodName { get; init; }
}

internal sealed class StoredSigMethodDesc : IData<StoredSigMethodDesc>
{
    static StoredSigMethodDesc IData<StoredSigMethodDesc>.Create(Target target, TargetPointer address) => new StoredSigMethodDesc(target, address);
    public StoredSigMethodDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StoredSigMethodDesc);

        Sig = target.ReadPointer(address + (ulong)type.Fields[nameof(Sig)].Offset);
        cSig = target.Read<uint>(address + (ulong)type.Fields[nameof(cSig)].Offset);
        ExtendedFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(ExtendedFlags)].Offset);
    }

    public TargetPointer Sig { get; init; }
    public uint cSig { get; init; }
    public uint ExtendedFlags { get; init; }
}
