// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

public sealed class EEClass : IData<EEClass>
{
    static EEClass IData<EEClass>.Create(Target target, TargetPointer address) => new EEClass(target, address);
    public EEClass(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EEClass);

        MethodTable = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodTable)].Offset);
        NumMethods = target.Read<ushort>(address + (ulong)type.Fields[nameof(NumMethods)].Offset);
        AttrClass = target.Read<uint>(address + (ulong)type.Fields[nameof(AttrClass)].Offset);
    }

    public TargetPointer MethodTable { get; init; }
    public ushort NumMethods { get; init; }
    public uint AttrClass { get; init; }
}
