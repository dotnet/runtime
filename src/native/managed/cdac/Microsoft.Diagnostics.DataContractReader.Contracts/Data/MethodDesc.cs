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

        ChunkIndex = target.ReadField<byte>(address, type, nameof(ChunkIndex));
        Slot = target.ReadField<ushort>(address, type, nameof(Slot));
        Flags = target.ReadField<ushort>(address, type, nameof(Flags));
        Flags3AndTokenRemainder = target.ReadField<ushort>(address, type, nameof(Flags3AndTokenRemainder));
        EntryPointFlags = target.ReadField<byte>(address, type, nameof(EntryPointFlags));
        CodeData = target.ReadPointerField(address, type, nameof(CodeData));
        if (type.Fields.ContainsKey(nameof(GCCoverageInfo)))
        {
            GCCoverageInfo = target.ReadPointerField(address, type, nameof(GCCoverageInfo));
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

        PerInstInfo = target.ReadPointerField(address, type, nameof(PerInstInfo));
        NumGenericArgs = target.ReadField<ushort>(address, type, nameof(NumGenericArgs));
        Flags2 = target.ReadField<ushort>(address, type, nameof(Flags2));
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

        MethodName = target.ReadPointerField(address, type, nameof(MethodName));
    }

    public TargetPointer MethodName { get; init; }
}

internal sealed class StoredSigMethodDesc : IData<StoredSigMethodDesc>
{
    static StoredSigMethodDesc IData<StoredSigMethodDesc>.Create(Target target, TargetPointer address) => new StoredSigMethodDesc(target, address);
    public StoredSigMethodDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StoredSigMethodDesc);

        Sig = target.ReadPointerField(address, type, nameof(Sig));
        cSig = target.ReadField<uint>(address, type, nameof(cSig));
        ExtendedFlags = target.ReadField<uint>(address, type, nameof(ExtendedFlags));
    }

    public TargetPointer Sig { get; init; }
    public uint cSig { get; init; }
    public uint ExtendedFlags { get; init; }
}
