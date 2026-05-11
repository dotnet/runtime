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

        Flags = target.ReadField<uint>(address, type, nameof(Flags));
        TryStartPC = target.ReadField<uint>(address, type, nameof(TryStartPC));
        TryEndPC = target.ReadField<uint>(address, type, nameof(TryEndPC));
        HandlerStartPC = target.ReadField<uint>(address, type, nameof(HandlerStartPC));
        HandlerEndPC = target.ReadField<uint>(address, type, nameof(HandlerEndPC));
        TypeHandle = target.ReadNUIntField(address, type, nameof(TypeHandle));
        ClassToken = (uint)TypeHandle.Value;
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

        Flags = target.ReadField<uint>(address, type, nameof(Flags));
        TryStartPC = target.ReadField<uint>(address, type, nameof(TryStartPC));
        TryEndPC = target.ReadField<uint>(address, type, nameof(TryEndPC));
        HandlerStartPC = target.ReadField<uint>(address, type, nameof(HandlerStartPC));
        HandlerEndPC = target.ReadField<uint>(address, type, nameof(HandlerEndPC));
        ClassToken = target.ReadField<uint>(address, type, nameof(ClassToken));
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
