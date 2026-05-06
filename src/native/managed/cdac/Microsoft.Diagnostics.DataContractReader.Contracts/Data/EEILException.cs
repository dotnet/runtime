// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class EEILException : IData<EEILException>
{
    static EEILException IData<EEILException>.Create(Target target, TargetPointer address) => new EEILException(target, address);
    public EEILException(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EEILException);
        Address = address;
        Clauses = address + (ulong)type.Fields[nameof(Clauses)].Offset;
    }

    public TargetPointer Address { get; init; }
    public TargetPointer Clauses { get; init; }
}
