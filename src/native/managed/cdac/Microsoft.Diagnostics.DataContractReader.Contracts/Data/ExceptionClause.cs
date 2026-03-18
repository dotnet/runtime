// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal interface IExceptionClauseData
{
    uint Flags { get; }
    uint TryStartPC { get; }
    uint TryEndPC { get; }
    uint HandlerStartPC { get; }
    uint HandlerEndPC { get; }
    uint ClassToken { get; }
    uint FilterOffset { get; }
}

internal sealed class EEExceptionClause : IData<EEExceptionClause>, IExceptionClauseData
{
    static EEExceptionClause IData<EEExceptionClause>.Create(Target target, TargetPointer address) => new EEExceptionClause(target, address);
    public EEExceptionClause(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EEExceptionClause);

        Flags = target.Read<uint>(address + (ulong)type.Fields[nameof(Flags)].Offset);
        TryStartPC = target.Read<uint>(address + (ulong)type.Fields[nameof(TryStartPC)].Offset);
        TryEndPC = target.Read<uint>(address + (ulong)type.Fields[nameof(TryEndPC)].Offset);
        HandlerStartPC = target.Read<uint>(address + (ulong)type.Fields[nameof(HandlerStartPC)].Offset);
        HandlerEndPC = target.Read<uint>(address + (ulong)type.Fields[nameof(HandlerEndPC)].Offset);
        TypeHandle = target.ReadNUInt(address + (ulong)type.Fields[nameof(TypeHandle)].Offset);
        ClassToken = target.Read<uint>(address + (ulong)type.Fields[nameof(TypeHandle)].Offset);
        FilterOffset = ClassToken;
    }

    public uint Flags { get; init; }
    public uint TryStartPC { get; init; }
    public uint TryEndPC { get; init; }
    public uint HandlerStartPC { get; init; }
    public uint HandlerEndPC { get; init; }
    public TargetNUInt TypeHandle { get; init; }
    public uint ClassToken { get; init; }
    public uint FilterOffset { get; init; }
}

internal sealed class R2RExceptionClause : IData<R2RExceptionClause>, IExceptionClauseData
{
    static R2RExceptionClause IData<R2RExceptionClause>.Create(Target target, TargetPointer address) => new R2RExceptionClause(target, address);
    public R2RExceptionClause(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.R2RExceptionClause);

        Flags = target.Read<uint>(address + (ulong)type.Fields[nameof(Flags)].Offset);
        TryStartPC = target.Read<uint>(address + (ulong)type.Fields[nameof(TryStartPC)].Offset);
        TryEndPC = target.Read<uint>(address + (ulong)type.Fields[nameof(TryEndPC)].Offset);
        HandlerStartPC = target.Read<uint>(address + (ulong)type.Fields[nameof(HandlerStartPC)].Offset);
        HandlerEndPC = target.Read<uint>(address + (ulong)type.Fields[nameof(HandlerEndPC)].Offset);
        ClassToken = target.Read<uint>(address + (ulong)type.Fields[nameof(ClassToken)].Offset);
        FilterOffset = ClassToken;
    }

    public uint Flags { get; init; }
    public uint TryStartPC { get; init; }
    public uint TryEndPC { get; init; }
    public uint HandlerStartPC { get; init; }
    public uint HandlerEndPC { get; init; }
    public uint ClassToken { get; init; }
    public uint FilterOffset { get; init; }
}
