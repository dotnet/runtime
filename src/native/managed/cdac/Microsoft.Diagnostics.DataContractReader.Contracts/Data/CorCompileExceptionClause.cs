// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CorCompileExceptionClause : IData<CorCompileExceptionClause>
{
    static CorCompileExceptionClause IData<CorCompileExceptionClause>.Create(Target target, TargetPointer address)
        => new CorCompileExceptionClause(target, address);

    public CorCompileExceptionClause(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CorCompileExceptionClause);

        Flags = target.Read<uint>(address + (ulong)type.Fields[nameof(Flags)].Offset);
        FilterOffset = target.Read<uint>(address + (ulong)type.Fields[nameof(FilterOffset)].Offset);
    }

    public uint Flags { get; }
    public uint FilterOffset { get; }
}
