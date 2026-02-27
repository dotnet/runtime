// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class EEILExceptionClause : IData<EEILExceptionClause>
{
    static EEILExceptionClause IData<EEILExceptionClause>.Create(Target target, TargetPointer address)
        => new EEILExceptionClause(target, address);

    public EEILExceptionClause(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EEILExceptionClause);

        Flags = target.Read<uint>(address + (ulong)type.Fields[nameof(Flags)].Offset);
        FilterOffset = target.Read<uint>(address + (ulong)type.Fields[nameof(FilterOffset)].Offset);
    }

    public uint Flags { get; }
    public uint FilterOffset { get; }
}
